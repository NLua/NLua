using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using KeraLua;

using NLua.Event;
using NLua.Method;
using NLua.Exceptions;
using NLua.Extensions;

#if __IOS__ || __TVOS__ || __WATCHOS__
    using ObjCRuntime;
#endif

using LuaState = KeraLua.Lua;
using LuaNativeFunction = KeraLua.LuaFunction;


namespace NLua
{
    public class Lua : IDisposable
    {
        #region lua debug functions
        /// <summary>
        /// Event that is raised when an exception occures during a hook call.
        /// </summary>
        public event EventHandler<HookExceptionEventArgs> HookException;
        /// <summary>
        /// Event when lua hook callback is called
        /// </summary>
        /// <remarks>
        /// Is only raised if SetDebugHook is called before.
        /// </remarks>
        public event EventHandler<DebugHookEventArgs> DebugHook;
        /// <summary>
        /// lua hook calback delegate
        /// </summary>
        private LuaHookFunction hookCallback = null;
        #endregion
        #region Globals auto-complete
        private readonly List<string> globals = new List<string>();
        private bool globalsSorted;
        #endregion
        private LuaState luaState;
        /// <summary>
        /// True while a script is being executed
        /// </summary>
        public bool IsExecuting { get { return executing; } }

        public LuaState State => luaState;

        private LuaNativeFunction panicCallback;
        private ObjectTranslator translator;
        /// <summary>
        /// Used to protect the (global) object translator pool during add/remove
        /// </summary>
        private static readonly object translatorPoolLock = new object();

        /// <summary>
        /// Used to ensure multiple .net threads all get serialized by this single lock for access to the lua stack/objects
        /// </summary>
        //private object luaLock = new object();
        private bool _StatePassed;
        private bool executing;
        static string initLuanet =
 @"local metatable = {}
        local rawget = rawget
        local import_type = luanet.import_type
        local load_assembly = luanet.load_assembly
        luanet.error, luanet.type = error, type
        -- Lookup a .NET identifier component.
        function metatable:__index(key) -- key is e.g. 'Form'
            -- Get the fully-qualified name, e.g. 'System.Windows.Forms.Form'
            local fqn = rawget(self,'.fqn')
            fqn = ((fqn and fqn .. '.') or '') .. key

            -- Try to find either a luanet function or a CLR type
            local obj = rawget(luanet,key) or import_type(fqn)

            -- If key is neither a luanet function or a CLR type, then it is simply
            -- an identifier component.
            if obj == nil then
                -- It might be an assembly, so we load it too.
                pcall(load_assembly,fqn)
                obj = { ['.fqn'] = fqn }
                setmetatable(obj, metatable)
            end

            -- Cache this lookup
            rawset(self, key, obj)
            return obj
        end

        -- A non-type has been called; e.g. foo = System.Foo()
        function metatable:__call(...)
            error('No such type: ' .. rawget(self,'.fqn'), 2)
        end

        -- This is the root of the .NET namespace
        luanet['.fqn'] = false
        setmetatable(luanet, metatable)

        -- Preload the mscorlib assembly
        luanet.load_assembly('mscorlib')";

        static string clr_package = @"---
--- This lua module provides auto importing of .net classes into a named package.
--- Makes for super easy use of LuaInterface glue
---
--- example:
---   Threading = CLRPackage(""System"", ""System.Threading"")
---   Threading.Thread.Sleep(100)
---
--- Extensions:
--- import() is a version of CLRPackage() which puts the package into a list which is used by a global __index lookup,
--- and thus works rather like C#'s using statement. It also recognizes the case where one is importing a local
--- assembly, which must end with an explicit .dll extension.

--- Alternatively, luanet.namespace can be used for convenience without polluting the global namespace:
---   local sys,sysi = luanet.namespace {'System','System.IO'}
--    sys.Console.WriteLine(""we are at {0}"",sysi.Directory.GetCurrentDirectory())


-- LuaInterface hosted with stock Lua interpreter will need to explicitly require this...
if not luanet then require 'luanet' end

local import_type, load_assembly = luanet.import_type, luanet.load_assembly

local mt = {
    --- Lookup a previously unfound class and add it to our table
    __index = function(package, classname)
        local class = rawget(package, classname)
        if class == nil then
            class = import_type(package.packageName .. ""."" .. classname)
            if class == nil then class = import_type(classname) end
            package[classname] = class		-- keep what we found around, so it will be shared
        end
        return class
    end
}

function luanet.namespace(ns)
    if type(ns) == 'table' then
        local res = {}
        for i = 1,#ns do
            res[i] = luanet.namespace(ns[i])
        end
        return unpack(res)
    end
    -- FIXME - table.packageName could instead be a private index (see Lua 13.4.4)
    local t = { packageName = ns }
    setmetatable(t,mt)
    return t
end

local globalMT, packages

local function set_global_mt()
    packages = {}
    globalMT = {
        __index = function(T,classname)
                for i,package in ipairs(packages) do
                    local class = package[classname]
                    if class then
                        _G[classname] = class
                        return class
                    end
                end
        end
    }
    setmetatable(_G, globalMT)
end

--- Create a new Package class
function CLRPackage(assemblyName, packageName)
  -- a sensible default...
  packageName = packageName or assemblyName
  local ok = pcall(load_assembly,assemblyName)			-- Make sure our assembly is loaded
  return luanet.namespace(packageName)
end

function import (assemblyName, packageName)
    if not globalMT then
        set_global_mt()
    end
    if not packageName then
        local i = assemblyName:find('%.dll$')
        if i then packageName = assemblyName:sub(1,i-1)
        else packageName = assemblyName end
    end
    local t = CLRPackage(assemblyName,packageName)
    table.insert(packages,t)
    return t
end


function luanet.make_array (tp,tbl)
    local arr = tp[#tbl]
    for i,v in ipairs(tbl) do
        arr:SetValue(v,i-1)
    end
    return arr
end

function luanet.each(o)
   local e = o:GetEnumerator()
   return function()
      if e:MoveNext() then
        return e.Current
     end
   end
end
";
        public bool UseTraceback { get; set; } = false;

        #region Globals auto-complete
        /// <summary>
        /// An alphabetically sorted list of all globals (objects, methods, etc.) externally added to this Lua instance
        /// </summary>
        /// <remarks>Members of globals are also listed. The formatting is optimized for text input auto-completion.</remarks>
        public IEnumerable<string> Globals {
            get
            {
                // Only sort list when necessary
                if (!globalsSorted)
                {
                    globals.Sort();
                    globalsSorted = true;
                }

                return globals;
            }
        }
        #endregion

        public Lua()
        {
            luaState = new LuaState();
            Init();
            // We need to keep this in a managed reference so the delegate doesn't get garbage collected
            panicCallback = PanicCallback;
            luaState.AtPanic(panicCallback);
        }

        /*
            * CAUTION: NLua.Lua instances can't share the same lua state! 
            */
        public Lua(LuaState luaState)
        {
            luaState.PushString("NLua_Loaded");
            luaState.GetTable((int)LuaRegistry.Index);

            if (luaState.ToBoolean(-1))
            {
                luaState.SetTop(-2);
                throw new LuaException("There is already a NLua.Lua instance associated with this Lua state");
            }
            else
            {
                this.luaState = luaState;
                _StatePassed = true;
                luaState.SetTop(-2);
                Init();
            }
        }

        void Init()
        {
            luaState.PushString("NLua_Loaded");
            luaState.PushBoolean(true);
            luaState.SetTable((int)LuaRegistry.Index);
            if (_StatePassed == false)
            {
                luaState.NewTable();
                luaState.SetGlobal("luanet");
            }
            luaState.PushGlobalTable();
            luaState.GetGlobal("luanet");
            luaState.PushString("getmetatable");
            luaState.GetGlobal("getmetatable");
            luaState.SetTable(-3);
            luaState.PopGlobalTable();
            translator = new ObjectTranslator(this, luaState);
            lock (translatorPoolLock)
            {
                ObjectTranslatorPool.Instance.Add(luaState, translator);
            }
            luaState.PopGlobalTable();
            luaState.DoString(initLuanet);
        }

        public void Close()
        {
            if (_StatePassed)
                return;

            if (luaState != null)
            {
                lock (translatorPoolLock)
                {
                    luaState.Close();
                    ObjectTranslatorPool.Instance.Remove(luaState);
                    luaState = null;
                }
            }
        }

#if __IOS__ || __TVOS__ || __WATCHOS__
        [MonoPInvokeCallback(typeof(LuaNativeFunction))]
#endif
        static int PanicCallback(IntPtr state)
        {
            var luaState = LuaState.FromIntPtr(state);
            string reason = string.Format("Unprotected error in call to Lua API ({0})", luaState.ToString(-1));
            throw new LuaException(reason);
        }

        /// <summary>
        /// Assuming we have a Lua error string sitting on the stack, throw a C# exception out to the user's app
        /// </summary>
        /// <exception cref = "LuaScriptException">Thrown if the script caused an exception</exception>
        private void ThrowExceptionFromError(int oldTop)
        {
            object err = translator.GetObject(luaState, -1);
            luaState.SetTop(oldTop);

            // A pre-wrapped exception - just rethrow it (stack trace of InnerException will be preserved)
            var luaEx = err as LuaScriptException;

            if (luaEx != null)
                throw luaEx;

            // A non-wrapped Lua error (best interpreted as a string) - wrap it and throw it
            if (err == null)
                err = "Unknown Lua Error";

            throw new LuaScriptException(err.ToString(), string.Empty);
        }

        /// <summary>
        /// Push a debug.traceback reference onto the stack, for a pcall function to use as error handler. (Remember to increment any top-of-stack markers!)
        /// </summary>
        private static int PushDebugTraceback(LuaState luaState, int argCount)
        {
            luaState.GetGlobal("debug");
            luaState.GetField(-1, "traceback");
            luaState.Remove(-2);
            int errindex = -argCount - 2;
            luaState.Insert(errindex);
            return errindex;
        }

        /// <summary>
        /// <para>Return a debug.traceback() call result (a multi-line string, containing a full stack trace, including C calls.</para>
        /// <para>Note: it won't return anything unless the interpreter is in the middle of execution - that is, it only makes sense to call it from a method called from Lua, or during a coroutine yield.</para>
        /// </summary>
        public string GetDebugTraceback()
        {
            int oldTop = luaState.GetTop();
            luaState.GetGlobal("debug"); // stack: debug
            luaState.GetField(-1, "traceback"); // stack: debug,traceback
            luaState.Remove(-2); // stack: traceback
            luaState.PCall(0, -1, 0);
            return translator.PopValues(luaState, oldTop)[0] as string;
        }

        /// <summary>
        /// Convert C# exceptions into Lua errors
        /// </summary>
        /// <returns>num of things on stack</returns>
        /// <param name = "e">null for no pending exception</param>
        internal int SetPendingException(Exception e)
        {
            var caughtExcept = e;

            if (caughtExcept != null)
            {
                translator.ThrowError(luaState, caughtExcept);
                luaState.PushNil();
                return 1;
            }
            else
                return 0;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name = "chunk"></param>
        /// <param name = "name"></param>
        /// <returns></returns>
        public LuaFunction LoadString(string chunk, string name)
        {
            int oldTop = luaState.GetTop();
            executing = true;

            try
            {
                if (luaState.LoadString(chunk, name) != LuaStatus.OK)
                    ThrowExceptionFromError(oldTop);
            }
            finally
            {
                executing = false;
            }

            var result = translator.GetFunction(luaState, -1);
            translator.PopValues(luaState, oldTop);
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name = "chunk"></param>
        /// <param name = "name"></param>
        /// <returns></returns>
        public LuaFunction LoadString(byte[] chunk, string name)
        {
            int oldTop = luaState.GetTop();
            executing = true;

            try
            {
                if (luaState.LoadBuffer(chunk, name) != LuaStatus.OK)
                    ThrowExceptionFromError(oldTop);
            }
            finally
            {
                executing = false;
            }

            var result = translator.GetFunction(luaState, -1);
            translator.PopValues(luaState, oldTop);
            return result;
        }

        /// <summary>
        /// Load a File on, and return a LuaFunction to execute the file loaded (useful to see if the syntax of a file is ok)
        /// </summary>
        /// <param name = "fileName"></param>
        /// <returns></returns>
        public LuaFunction LoadFile(string fileName)
        {
            int oldTop = luaState.GetTop();

            if (luaState.LoadFile(fileName) != LuaStatus.OK)
                ThrowExceptionFromError(oldTop);

            var result = translator.GetFunction(luaState, -1);
            translator.PopValues(luaState, oldTop);
            return result;
        }

        /// <summary>
        /// Executes a Lua chunk and returns all the chunk's return values in an array.
        /// </summary>
        /// <param name = "chunk">Chunk to execute</param>
        /// <param name = "chunkName">Name to associate with the chunk. Defaults to "chunk".</param>
        /// <returns></returns>
        public object[] DoString(byte[] chunk, string chunkName = "chunk")
        {
            int oldTop = luaState.GetTop();
            executing = true;

            if (luaState.LoadBuffer(chunk, chunkName) == LuaStatus.OK)
            {
                int errfunction = 0;
                if (UseTraceback)
                {
                    errfunction = PushDebugTraceback(luaState, 0);
                    oldTop++;
                }

                try
                {
                    if (luaState.PCall(0, -1, errfunction) == LuaStatus.OK)
                        return translator.PopValues(luaState, oldTop);
                    else
                        ThrowExceptionFromError(oldTop);
                }
                finally
                {
                    executing = false;
                }
            }
            else
                ThrowExceptionFromError(oldTop);

            return null;
        }

        /// <summary>
        /// Executes a Lua chunk and returns all the chunk's return values in an array.
        /// </summary>
        /// <param name = "chunk">Chunk to execute</param>
        /// <param name = "chunkName">Name to associate with the chunk. Defaults to "chunk".</param>
        /// <returns></returns>
        public object[] DoString(string chunk, string chunkName = "chunk")
        {
            int oldTop = luaState.GetTop();
            executing = true;

            if (luaState.LoadString(chunk, chunkName) == LuaStatus.OK)
            {
                int errfunction = 0;
                if (UseTraceback)
                {
                    errfunction = PushDebugTraceback(luaState, 0);
                    oldTop++;
                }

                try
                {
                    if (luaState.PCall(0, -1, errfunction) == LuaStatus.OK)
                        return translator.PopValues(luaState, oldTop);
                    else
                        ThrowExceptionFromError(oldTop);
                }
                finally
                {
                    executing = false;
                }
            }
            else
                ThrowExceptionFromError(oldTop);

            return null;
        }

        /*
            * Excutes a Lua file and returns all the chunk's return
            * values in an array
            */
        public object[] DoFile(string fileName)
        {
            int oldTop = luaState.GetTop();

            if (luaState.LoadFile(fileName) == LuaStatus.OK)
            {
                executing = true;

                int errfunction = 0;
                if (UseTraceback)
                {
                    errfunction = PushDebugTraceback(luaState, 0);
                    oldTop++;
                }

                try
                {
                    if (luaState.PCall(0, -1, errfunction) == LuaStatus.OK)
                        return translator.PopValues(luaState, oldTop);
                    else
                        ThrowExceptionFromError(oldTop);
                }
                finally
                {
                    executing = false;
                }
            }
            else
                ThrowExceptionFromError(oldTop);

            return null;
        }


        /*
            * Indexer for global variables from the LuaInterpreter
            * Supports navigation of tables by using . operator
            */
        public object this[string fullPath] {
            get
            {
                object returnValue = null;
                int oldTop = luaState.GetTop();
                string[] path = FullPathToArray(fullPath);
                luaState.GetGlobal(path[0]);
                returnValue = translator.GetObject(luaState, -1);

                if (path.Length > 1)
                {
                    var dispose = returnValue as LuaBase;
                    string[] remainingPath = new string[path.Length - 1];
                    Array.Copy(path, 1, remainingPath, 0, path.Length - 1);
                    returnValue = GetObject(remainingPath);
                    if (dispose != null)
                        dispose.Dispose();
                }

                luaState.SetTop(oldTop);
                return returnValue;
            }
            set
            {
                int oldTop = luaState.GetTop();
                string[] path = FullPathToArray(fullPath);
                if (path.Length == 1)
                {
                    translator.Push(luaState, value);
                    luaState.SetGlobal(fullPath);
                }
                else
                {
                    luaState.GetGlobal(path[0]);
                    string[] remainingPath = new string[path.Length - 1];
                    Array.Copy(path, 1, remainingPath, 0, path.Length - 1);
                    SetObject(remainingPath, value);
                }

                luaState.SetTop(oldTop);

                // Globals auto-complete
                if (value == null)
                {
                    // Remove now obsolete entries
                    globals.Remove(fullPath);
                }
                else
                {
                    // Add new entries
                    if (!globals.Contains(fullPath))
                        RegisterGlobal(fullPath, value.GetType(), 0);
                }
            }
        }

        #region Globals auto-complete
        /// <summary>
        /// Adds an entry to <see cref = "globals"/> (recursivley handles 2 levels of members)
        /// </summary>
        /// <param name = "path">The index accessor path ot the entry</param>
        /// <param name = "type">The type of the entry</param>
        /// <param name = "recursionCounter">How deep have we gone with recursion?</param>
        private void RegisterGlobal(string path, Type type, int recursionCounter)
        {
            // If the type is a global method, list it directly
            if (type == typeof(LuaFunction))
            {
                // Format for easy method invocation
                globals.Add(path + "(");
            }
            // If the type is a class or an interface and recursion hasn't been running too long, list the members
            else if ((type.IsClass || type.IsInterface) && type != typeof(string) && recursionCounter < 2)
            {
                #region Methods
                foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
                {
                    string name = method.Name;
                    if (
                        // Check that the LuaHideAttribute and LuaGlobalAttribute were not applied
                        (!method.GetCustomAttributes(typeof(LuaHideAttribute), false).Any()) &&
                        (!method.GetCustomAttributes(typeof(LuaGlobalAttribute), false).Any()) &&
                        // Exclude some generic .NET methods that wouldn't be very usefull in Lua
                        name != "GetType" && name != "GetHashCode" && name != "Equals" &&
                        name != "ToString" && name != "Clone" && name != "Dispose" &&
                        name != "GetEnumerator" && name != "CopyTo" &&
                        !name.StartsWith("get_", StringComparison.Ordinal) &&
                        !name.StartsWith("set_", StringComparison.Ordinal) &&
                        !name.StartsWith("add_", StringComparison.Ordinal) &&
                        !name.StartsWith("remove_", StringComparison.Ordinal))
                    {
                        // Format for easy method invocation
                        string command = path + ":" + name + "(";

                        if (method.GetParameters().Length == 0)
                            command += ")";
                        globals.Add(command);
                    }
                }
                #endregion

                #region Fields
                foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (
                        // Check that the LuaHideAttribute and LuaGlobalAttribute were not applied
                        (!field.GetCustomAttributes(typeof(LuaHideAttribute), false).Any()) &&
                        (!field.GetCustomAttributes(typeof(LuaGlobalAttribute), false).Any()))
                    {
                        // Go into recursion for members
                        RegisterGlobal(path + "." + field.Name, field.FieldType, recursionCounter + 1);
                    }
                }
                #endregion

                #region Properties
                foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (
                        // Check that the LuaHideAttribute and LuaGlobalAttribute were not applied
                        (!property.GetCustomAttributes(typeof(LuaHideAttribute), false).Any()) &&
                        (!property.GetCustomAttributes(typeof(LuaGlobalAttribute), false).Any())
                        // Exclude some generic .NET properties that wouldn't be very useful in Lua
                        && property.Name != "Item")
                    {
                        // Go into recursion for members
                        RegisterGlobal(path + "." + property.Name, property.PropertyType, recursionCounter + 1);
                    }
                }
                #endregion
            }
            else
                globals.Add(path); // Otherwise simply add the element to the list

            // List will need to be sorted on next access
            globalsSorted = false;
        }
        #endregion

        /*
            * Navigates a table in the top of the stack, returning
            * the value of the specified field
            */
        object GetObject(string[] remainingPath)
        {
            object returnValue = null;

            for (int i = 0; i < remainingPath.Length; i++)
            {
                luaState.PushString(remainingPath[i]);
                luaState.GetTable(-2);
                returnValue = translator.GetObject(luaState, -1);

                if (returnValue == null)
                    break;
            }

            return returnValue;
        }

        /*
            * Gets a numeric global variable
            */
        public double GetNumber(string fullPath)
        {
            return (double)this[fullPath];
        }

        /*
            * Gets a string global variable
            */
        public string GetString(string fullPath)
        {
            return this[fullPath].ToString();
        }

        /*
            * Gets a table global variable
            */
        public LuaTable GetTable(string fullPath)
        {
            return (LuaTable)this[fullPath];
        }

        /*
            * Gets a table global variable as an object implementing
            * the interfaceType interface
            */
        public object GetTable(Type interfaceType, string fullPath)
        {
            return CodeGeneration.Instance.GetClassInstance(interfaceType, GetTable(fullPath));
        }

        /*
            * Gets a function global variable
            */
        public LuaFunction GetFunction(string fullPath)
        {
            object obj = this[fullPath];
            LuaFunction luaFunction = obj as LuaFunction;
            if (luaFunction != null)
                return luaFunction;

            luaFunction = new LuaFunction((LuaNativeFunction) obj, this);
            return luaFunction;
        }

        /*
            * Register a delegate type to be used to convert Lua functions to C# delegates (useful for iOS where there is no dynamic code generation)
            * type delegateType
            */
        public void RegisterLuaDelegateType(Type delegateType, Type luaDelegateType)
        {
            CodeGeneration.Instance.RegisterLuaDelegateType(delegateType, luaDelegateType);
        }

        public void RegisterLuaClassType(Type klass, Type luaClass)
        {
            CodeGeneration.Instance.RegisterLuaClassType(klass, luaClass);
        }

        public void LoadCLRPackage()
        {
            luaState.DoString(Lua.clr_package);
        }
        /*
            * Gets a function global variable as a delegate of
            * type delegateType
            */
        public Delegate GetFunction(Type delegateType, string fullPath)
        {
            return CodeGeneration.Instance.GetDelegate(delegateType, GetFunction(fullPath));
        }

        /*
            * Calls the object as a function with the provided arguments, 
            * returning the function's returned values inside an array
            */
        internal object[] CallFunction(object function, object[] args)
        {
            return CallFunction(function, args, null);
        }

        /*
            * Calls the object as a function with the provided arguments and
            * casting returned values to the types in returnTypes before returning
            * them in an array
            */
        internal object[] CallFunction(object function, object[] args, Type[] returnTypes)
        {
            int nArgs = 0;
            int oldTop = luaState.GetTop();

            if (!luaState.CheckStack(args.Length + 6))
                throw new LuaException("Lua stack overflow");

            translator.Push(luaState, function);

            if (args.Length > 0)
            {
                nArgs = args.Length;

                for (int i = 0; i < args.Length; i++)
                    translator.Push(luaState, args[i]);
            }

            executing = true;

            try
            {
                int errfunction = 0;
                if (UseTraceback)
                {
                    errfunction = PushDebugTraceback(luaState, nArgs);
                    oldTop++;
                }

                LuaStatus error = luaState.PCall(nArgs, -1, errfunction);
                if (error != LuaStatus.OK)
                    ThrowExceptionFromError(oldTop);
            }
            finally
            {
                executing = false;
            }

            if (returnTypes != null)
                return translator.PopValues(luaState, oldTop, returnTypes);

            return translator.PopValues(luaState, oldTop);
        }

        /*
            * Navigates a table to set the value of one of its fields
            */
        void SetObject(string[] remainingPath, object val)
        {
            for (int i = 0; i < remainingPath.Length - 1; i++)
            {
                luaState.PushString(remainingPath[i]);
                luaState.GetTable(-2);
            }

            luaState.PushString(remainingPath[remainingPath.Length - 1]);
            translator.Push(luaState, val);
            luaState.SetTable(-3);
        }

        string[] FullPathToArray(string fullPath)
        {
            return fullPath.SplitWithEscape('.', '\\').ToArray();
        }
        /*
            * Creates a new table as a global variable or as a field
            * inside an existing table
            */
        public void NewTable(string fullPath)
        {
            string[] path = FullPathToArray(fullPath);
            int oldTop = luaState.GetTop();

            if (path.Length == 1)
            {
                luaState.NewTable();
                luaState.SetGlobal(fullPath);
            }
            else
            {
                luaState.GetGlobal(path[0]);

                for (int i = 1; i < path.Length - 1; i++)
                {
                    luaState.PushString(path[i]);
                    luaState.GetTable(-2);
                }

                luaState.PushString(path[path.Length - 1]);
                luaState.NewTable();
                luaState.SetTable(-3);
            }

            luaState.SetTop( oldTop);
        }

        public Dictionary<object, object> GetTableDict(LuaTable table)
        {
            var dict = new Dictionary<object, object>();
            int oldTop = luaState.GetTop();
            translator.Push(luaState, table);
            luaState.PushNil();

            while (luaState.Next(-2))
            {
                dict[translator.GetObject(luaState, -2)] = translator.GetObject(luaState, -1);
                luaState.SetTop(-2);
            }

            luaState.SetTop(oldTop);
            return dict;
        }

        /*
         * Lets go of a previously allocated reference to a table, function
         * or userdata
         */
        #region lua debug functions
        /// <summary>
        /// Activates the debug hook
        /// </summary>
        /// <param name = "mask">Mask</param>
        /// <param name = "count">Count</param>
        /// <returns>see lua docs. -1 if hook is already set</returns>
        public int SetDebugHook(LuaHookMask mask, int count)
        {
            if (hookCallback == null)
            {
                hookCallback = new LuaHookFunction(Lua.DebugHookCallback);
                luaState.SetHook(hookCallback, mask, count);
            }

            return -1;
        }

        /// <summary>
        /// Removes the debug hook
        /// </summary>
        /// <returns>see lua docs</returns>
        public void RemoveDebugHook()
        {
            hookCallback = null;
            luaState.SetHook(null, LuaHookMask.Disabled, 0);
        }

        /// <summary>
        /// Gets the hook mask.
        /// </summary>
        /// <returns>hook mask</returns>
        public LuaHookMask GetHookMask()
        {
            return luaState.HookMask;
        }

        /// <summary>
        /// Gets the hook count
        /// </summary>
        /// <returns>see lua docs</returns>
        public int GetHookCount()
        {
            return luaState.HookCount;
        }


        /// <summary>
        /// Gets local (see lua docs)
        /// </summary>
        /// <param name = "luaDebug">lua debug structure</param>
        /// <param name = "n">see lua docs</param>
        /// <returns>see lua docs</returns>
        public string GetLocal(LuaDebug luaDebug, int n)
        {
            return luaState.GetLocal(luaDebug, n);
        }

        /// <summary>
        /// Sets local (see lua docs)
        /// </summary>
        /// <param name = "luaDebug">lua debug structure</param>
        /// <param name = "n">see lua docs</param>
        /// <returns>see lua docs</returns>
        public string SetLocal(LuaDebug luaDebug, int n)
        {
            return luaState.SetLocal(luaDebug, n);
        }

        public int GetStack(int level, ref LuaDebug ar)
        {
            return luaState.GetStack(level, ref ar);
        }

        public bool GetInfo(string what, ref LuaDebug ar)
        {
            return luaState.GetInfo(what, ref ar);
        }

        /// <summary>
        /// Gets up value (see lua docs)
        /// </summary>
        /// <param name = "funcindex">see lua docs</param>
        /// <param name = "n">see lua docs</param>
        /// <returns>see lua docs</returns>
        public string GetUpValue(int funcindex, int n)
        {
            return luaState.GetUpValue(funcindex, n);
        }

        /// <summary>
        /// Sets up value (see lua docs)
        /// </summary>
        /// <param name = "funcindex">see lua docs</param>
        /// <param name = "n">see lua docs</param>
        /// <returns>see lua docs</returns>
        public string SetUpValue(int funcindex, int n)
        {
            return luaState.SetUpValue(funcindex, n);
        }

        /// <summary>
        /// Delegate that is called on lua hook callback
        /// </summary>
        /// <param name = "luaState">lua state</param>
        /// <param name = "luaDebug">Pointer to LuaDebug (lua_debug) structure</param>
        /// 
#if __IOS__ || __TVOS__ || __WATCHOS__
        [MonoPInvokeCallback(typeof(LuaHookFunction))]
#endif
        static void DebugHookCallback(IntPtr luaState, IntPtr luaDebug)
        {
            var state = LuaState.FromIntPtr(luaState);

            state.GetStack(0, luaDebug);

            if (!state.GetInfo("Snlu", luaDebug))
                return;

            var debug = LuaDebug.FromIntPtr(luaDebug);

            ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(state);
            Lua lua = translator.Interpreter;
            lua.DebugHookCallbackInternal(state, debug);
        }

        private void DebugHookCallbackInternal(LuaState luaState, LuaDebug luaDebug)
        {
            try
            {
                var temp = DebugHook;

                if (temp != null)
                    temp(this, new DebugHookEventArgs(luaDebug));
            }
            catch (Exception ex)
            {
                OnHookException(new HookExceptionEventArgs(ex));
            }
        }

        private void OnHookException(HookExceptionEventArgs e)
        {
            var temp = HookException;
            if (temp != null)
                temp(this, e);
        }

        /// <summary>
        /// Pops a value from the lua stack.
        /// </summary>
        /// <returns>Returns the top value from the lua stack.</returns>
        public object Pop()
        {
            int top = luaState.GetTop();
            return translator.PopValues(luaState, top - 1)[0];
        }

        /// <summary>
        /// Pushes a value onto the lua stack.
        /// </summary>
        /// <param name = "value">Value to push.</param>
        public void Push(object value)
        {
            translator.Push(luaState, value);
        }
        #endregion

        internal void DisposeInternal(int reference)
        {
            if (luaState != null)
                luaState.Unref(reference);
        }

        /*
         * Gets a field of the table corresponding to the provided reference
         * using rawget (do not use metatables)
         */
        internal object RawGetObject(int reference, string field)
        {
            int oldTop = luaState.GetTop();
            luaState.GetRef(reference);
            luaState.PushString(field);
            luaState.RawGet(-2);
            object obj = translator.GetObject(luaState, -1);
            luaState.SetTop(oldTop);
            return obj;
        }

        /*
         * Gets a field of the table or userdata corresponding to the provided reference
         */
        internal object GetObject(int reference, string field)
        {
            int oldTop = luaState.GetTop();
            luaState.GetRef(reference);
            object returnValue = GetObject(FullPathToArray(field));
            luaState.SetTop(oldTop);
            return returnValue;
        }

        /*
         * Gets a numeric field of the table or userdata corresponding the the provided reference
         */
        internal object GetObject(int reference, object field)
        {
            int oldTop = luaState.GetTop();
            luaState.GetRef(reference);
            translator.Push(luaState, field);
            luaState.GetTable(-2);
            object returnValue = translator.GetObject(luaState, -1);
            luaState.SetTop(oldTop);
            return returnValue;
        }

        /*
         * Sets a field of the table or userdata corresponding the the provided reference
         * to the provided value
         */
        internal void SetObject(int reference, string field, object val)
        {
            int oldTop = luaState.GetTop();
            luaState.GetRef(reference);
            SetObject(FullPathToArray(field), val);
            luaState.SetTop(oldTop);
        }

        /*
         * Sets a numeric field of the table or userdata corresponding the the provided reference
         * to the provided value
         */
        internal void SetObject(int reference, object field, object val)
        {
            int oldTop = luaState.GetTop();
            luaState.GetRef(reference);
            translator.Push(luaState, field);
            translator.Push(luaState, val);
            luaState.SetTable(-3);
            luaState.SetTop(oldTop);
        }

        public LuaFunction RegisterFunction(string path, MethodBase function /*MethodInfo function*/)
        {
            return RegisterFunction(path, null, function);
        }

        /*
         * Registers an object's method as a Lua function (global or table field)
         * The method may have any signature
         */
        public LuaFunction RegisterFunction(string path, object target, MethodBase function /*MethodInfo function*/)  //CP: Fix for struct constructor by Alexander Kappner (link: http://luaforge.net/forum/forum.php?thread_id = 2859&forum_id = 145)
        {
            // We leave nothing on the stack when we are done
            int oldTop = luaState.GetTop();
            var wrapper = new LuaMethodWrapper(translator, target, new ProxyType(function.DeclaringType), function);
            translator.Push(luaState, new LuaNativeFunction(wrapper.invokeFunction));
            this[path] = translator.GetObject(luaState, -1);
            var f = GetFunction(path);
            luaState.SetTop(oldTop);
            return f;
        }

        /*
         * Compares the two values referenced by ref1 and ref2 for equality
         */
        internal bool CompareRef(int ref1, int ref2)
        {
            int top = luaState.GetTop();
            luaState.GetRef(ref1);
            luaState.GetRef(ref2);
            bool equal = luaState.AreEqual(-1, -2);
            luaState.SetTop(top);
            return equal;
        }

        internal void PushCSFunction(LuaNativeFunction function)
        {
            translator.PushFunction(luaState, function);
        }

        #region IDisposable Members
        public virtual void Dispose()
        {
            if (translator != null)
            {
                translator.pendingEvents.Dispose();
                translator = null;
            }

            Close();
            GC.WaitForPendingFinalizers();
        }
        #endregion
    }
}
