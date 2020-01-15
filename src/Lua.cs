﻿using System;
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
        private LuaHookFunction _hookCallback;
        #endregion
        #region Globals auto-complete
        private readonly List<string> _globals = new List<string>();
        private bool _globalsSorted;
        #endregion
        private LuaState _luaState;
        /// <summary>
        /// True while a script is being executed
        /// </summary>
        public bool IsExecuting => _executing;

        public LuaState State => _luaState;

        private ObjectTranslator _translator;

        /// <summary>
        /// Used to ensure multiple .net threads all get serialized by this single lock for access to the lua stack/objects
        /// </summary>
        //private object luaLock = new object();
        private bool _StatePassed;
        private bool _executing;

        // The commented code bellow is the initLua, the code assigned here is minified for size/performance reasons.
        private const string InitLuanet = @"local a={}local rawget=rawget;local b=luanet.import_type;local c=luanet.load_assembly;luanet.error,luanet.type=error,type;function a:__index(d)local e=rawget(self,'.fqn')e=(e and e..'.'or'')..d;local f=rawget(luanet,d)or b(e)if f==nil then pcall(c,e)f={['.fqn']=e}setmetatable(f,a)end;rawset(self,d,f)return f end;function a:__call(...)error('No such type: '..rawget(self,'.fqn'),2)end;luanet['.fqn']=false;setmetatable(luanet,a)luanet.load_assembly('mscorlib')";
 //@"local metatable = {}
 //       local rawget = rawget
 //       local import_type = luanet.import_type
 //       local load_assembly = luanet.load_assembly
 //       luanet.error, luanet.type = error, type
 //       -- Lookup a .NET identifier component.
 //       function metatable:__index(key) -- key is e.g. 'Form'
 //           -- Get the fully-qualified name, e.g. 'System.Windows.Forms.Form'
 //           local fqn = rawget(self,'.fqn')
 //           fqn = ((fqn and fqn .. '.') or '') .. key

 //           -- Try to find either a luanet function or a CLR type
 //           local obj = rawget(luanet,key) or import_type(fqn)

 //           -- If key is neither a luanet function or a CLR type, then it is simply
 //           -- an identifier component.
 //           if obj == nil then
 //               -- It might be an assembly, so we load it too.
 //               pcall(load_assembly,fqn)
 //               obj = { ['.fqn'] = fqn }
 //               setmetatable(obj, metatable)
 //           end

 //           -- Cache this lookup
 //           rawset(self, key, obj)
 //           return obj
 //       end

 //       -- A non-type has been called; e.g. foo = System.Foo()
 //       function metatable:__call(...)
 //           error('No such type: ' .. rawget(self,'.fqn'), 2)
 //       end

 //       -- This is the root of the .NET namespace
 //       luanet['.fqn'] = false
 //       setmetatable(luanet, metatable)

 //       -- Preload the mscorlib assembly
 //       luanet.load_assembly('mscorlib')";

 private const string ClrPackage = @"if not luanet then require'luanet'end;local a,b=luanet.import_type,luanet.load_assembly;local c={__index=function(d,e)local f=rawget(d,e)if f==nil then f=a(d.packageName.."".""..e)if f==nil then f=a(e)end;d[e]=f end;return f end}function luanet.namespace(g)if type(g)=='table'then local h={}for i=1,#g do h[i]=luanet.namespace(g[i])end;return unpack(h)end;local j={packageName=g}setmetatable(j,c)return j end;local k,l;local function m()l={}k={__index=function(n,e)for i,d in ipairs(l)do local f=d[e]if f then _G[e]=f;return f end end end}setmetatable(_G,k)end;function CLRPackage(o,p)p=p or o;local q=pcall(b,o)return luanet.namespace(p)end;function import(o,p)if not k then m()end;if not p then local i=o:find('%.dll$')if i then p=o:sub(1,i-1)else p=o end end;local j=CLRPackage(o,p)table.insert(l,j)return j end;function luanet.make_array(r,s)local t=r[#s]for i,u in ipairs(s)do t:SetValue(u,i-1)end;return t end;function luanet.each(v)local w=v:GetEnumerator()return function()if w:MoveNext()then return w.Current end end end";
//@"---
//--- This lua module provides auto importing of .net classes into a named package.
//--- Makes for super easy use of LuaInterface glue
//---
//--- example:
//---   Threading = CLRPackage(""System"", ""System.Threading"")
//---   Threading.Thread.Sleep(100)
//---
//--- Extensions:
//--- import() is a version of CLRPackage() which puts the package into a list which is used by a global __index lookup,
//--- and thus works rather like C#'s using statement. It also recognizes the case where one is importing a local
//--- assembly, which must end with an explicit .dll extension.

//--- Alternatively, luanet.namespace can be used for convenience without polluting the global namespace:
//---   local sys,sysi = luanet.namespace {'System','System.IO'}
//--    sys.Console.WriteLine(""we are at {0}"",sysi.Directory.GetCurrentDirectory())


//-- LuaInterface hosted with stock Lua interpreter will need to explicitly require this...
//if not luanet then require 'luanet' end

//local import_type, load_assembly = luanet.import_type, luanet.load_assembly

//local mt = {
//    --- Lookup a previously unfound class and add it to our table
//    __index = function(package, classname)
//        local class = rawget(package, classname)
//        if class == nil then
//            class = import_type(package.packageName .. ""."" .. classname)
//            if class == nil then class = import_type(classname) end
//            package[classname] = class		-- keep what we found around, so it will be shared
//        end
//        return class
//    end
//}

//function luanet.namespace(ns)
//    if type(ns) == 'table' then
//        local res = {}
//        for i = 1,#ns do
//            res[i] = luanet.namespace(ns[i])
//        end
//        return unpack(res)
//    end
//    -- FIXME - table.packageName could instead be a private index (see Lua 13.4.4)
//    local t = { packageName = ns }
//    setmetatable(t,mt)
//    return t
//end

//local globalMT, packages

//local function set_global_mt()
//    packages = {}
//    globalMT = {
//        __index = function(T,classname)
//                for i,package in ipairs(packages) do
//                    local class = package[classname]
//                    if class then
//                        _G[classname] = class
//                        return class
//                    end
//                end
//        end
//    }
//    setmetatable(_G, globalMT)
//end

//--- Create a new Package class
//function CLRPackage(assemblyName, packageName)
//  -- a sensible default...
//  packageName = packageName or assemblyName
//  local ok = pcall(load_assembly,assemblyName)			-- Make sure our assembly is loaded
//  return luanet.namespace(packageName)
//end

//function import (assemblyName, packageName)
//    if not globalMT then
//        set_global_mt()
//    end
//    if not packageName then
//        local i = assemblyName:find('%.dll$')
//        if i then packageName = assemblyName:sub(1,i-1)
//        else packageName = assemblyName end
//    end
//    local t = CLRPackage(assemblyName,packageName)
//    table.insert(packages,t)
//    return t
//end


//function luanet.make_array (tp,tbl)
//    local arr = tp[#tbl]
//    for i,v in ipairs(tbl) do
//        arr:SetValue(v,i-1)
//    end
//    return arr
//end

//function luanet.each(o)
//   local e = o:GetEnumerator()
//   return function()
//      if e:MoveNext() then
//        return e.Current
//     end
//   end
//end
//";
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
                if (!_globalsSorted)
                {
                    _globals.Sort();
                    _globalsSorted = true;
                }

                return _globals;
            }
        }
        #endregion

        public Lua()
        {
            _luaState = new LuaState();
            Init();
            // We need to keep this in a managed reference so the delegate doesn't get garbage collected
            _luaState.AtPanic(PanicCallback);
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

            _luaState = luaState;
            _StatePassed = true;
            luaState.SetTop(-2);
            Init();
        }

        void Init()
        {
            _luaState.PushString("NLua_Loaded");
            _luaState.PushBoolean(true);
            _luaState.SetTable((int)LuaRegistry.Index);
            if (_StatePassed == false)
            {
                _luaState.NewTable();
                _luaState.SetGlobal("luanet");
            }
            _luaState.PushGlobalTable();
            _luaState.GetGlobal("luanet");
            _luaState.PushString("getmetatable");
            _luaState.GetGlobal("getmetatable");
            _luaState.SetTable(-3);
            _luaState.PopGlobalTable();
            _translator = new ObjectTranslator(this, _luaState);

            ObjectTranslatorPool.Instance.Add(_luaState, _translator);

            _luaState.PopGlobalTable();
            _luaState.DoString(InitLuanet);
        }

        public void Close()
        {
            if (_StatePassed || _luaState == null)
                return;

            _luaState.Close();
            ObjectTranslatorPool.Instance.Remove(_luaState);
            _luaState = null;
        }

#if __IOS__ || __TVOS__ || __WATCHOS__
        [MonoPInvokeCallback(typeof(LuaNativeFunction))]
#endif
        static int PanicCallback(IntPtr state)
        {
            var luaState = LuaState.FromIntPtr(state);
            string reason = string.Format("Unprotected error in call to Lua API ({0})", luaState.ToString(-1, false));
            throw new LuaException(reason);
        }

        /// <summary>
        /// Assuming we have a Lua error string sitting on the stack, throw a C# exception out to the user's app
        /// </summary>
        /// <exception cref = "LuaScriptException">Thrown if the script caused an exception</exception>
        private void ThrowExceptionFromError(int oldTop)
        {
            object err = _translator.GetObject(_luaState, -1);
            _luaState.SetTop(oldTop);

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
            int errIndex = -argCount - 2;
            luaState.Insert(errIndex);
            return errIndex;
        }

        /// <summary>
        /// <para>Return a debug.traceback() call result (a multi-line string, containing a full stack trace, including C calls.</para>
        /// <para>Note: it won't return anything unless the interpreter is in the middle of execution - that is, it only makes sense to call it from a method called from Lua, or during a coroutine yield.</para>
        /// </summary>
        public string GetDebugTraceback()
        {
            int oldTop = _luaState.GetTop();
            _luaState.GetGlobal("debug"); // stack: debug
            _luaState.GetField(-1, "traceback"); // stack: debug,traceback
            _luaState.Remove(-2); // stack: traceback
            _luaState.PCall(0, -1, 0);
            return _translator.PopValues(_luaState, oldTop)[0] as string;
        }

        /// <summary>
        /// Convert C# exceptions into Lua errors
        /// </summary>
        /// <returns>num of things on stack</returns>
        /// <param name = "e">null for no pending exception</param>
        internal int SetPendingException(Exception e)
        {
            var caughtExcept = e;

            if (caughtExcept == null)
                return 0;

            _translator.ThrowError(_luaState, caughtExcept);
            //_luaState.PushNil();
            return 1;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name = "chunk"></param>
        /// <param name = "name"></param>
        /// <returns></returns>
        public LuaFunction LoadString(string chunk, string name)
        {
            int oldTop = _luaState.GetTop();
            _executing = true;

            try
            {
                if (_luaState.LoadString(chunk, name) != LuaStatus.OK)
                    ThrowExceptionFromError(oldTop);
            }
            finally
            {
                _executing = false;
            }

            var result = _translator.GetFunction(_luaState, -1);
            _translator.PopValues(_luaState, oldTop);
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
            int oldTop = _luaState.GetTop();
            _executing = true;

            try
            {
                if (_luaState.LoadBuffer(chunk, name) != LuaStatus.OK)
                    ThrowExceptionFromError(oldTop);
            }
            finally
            {
                _executing = false;
            }

            var result = _translator.GetFunction(_luaState, -1);
            _translator.PopValues(_luaState, oldTop);
            return result;
        }

        /// <summary>
        /// Load a File on, and return a LuaFunction to execute the file loaded (useful to see if the syntax of a file is ok)
        /// </summary>
        /// <param name = "fileName"></param>
        /// <returns></returns>
        public LuaFunction LoadFile(string fileName)
        {
            int oldTop = _luaState.GetTop();

            if (_luaState.LoadFile(fileName) != LuaStatus.OK)
                ThrowExceptionFromError(oldTop);

            var result = _translator.GetFunction(_luaState, -1);
            _translator.PopValues(_luaState, oldTop);
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
            int oldTop = _luaState.GetTop();
            _executing = true;

            if (_luaState.LoadBuffer(chunk, chunkName) != LuaStatus.OK)
                ThrowExceptionFromError(oldTop);

            int errorFunctionIndex = 0;

            if (UseTraceback)
            {
                errorFunctionIndex = PushDebugTraceback(_luaState, 0);
                oldTop++;
            }

            try
            {
                if (_luaState.PCall(0, -1, errorFunctionIndex) != LuaStatus.OK)
                    ThrowExceptionFromError(oldTop);

                return _translator.PopValues(_luaState, oldTop);
            }
            finally
            {
                _executing = false;
            }
        }

        /// <summary>
        /// Executes a Lua chunk and returns all the chunk's return values in an array.
        /// </summary>
        /// <param name = "chunk">Chunk to execute</param>
        /// <param name = "chunkName">Name to associate with the chunk. Defaults to "chunk".</param>
        /// <returns></returns>
        public object[] DoString(string chunk, string chunkName = "chunk")
        {
            int oldTop = _luaState.GetTop();
            _executing = true;

            if (_luaState.LoadString(chunk, chunkName) != LuaStatus.OK)
                ThrowExceptionFromError(oldTop);

            int errorFunctionIndex = 0;

            if (UseTraceback)
            {
                errorFunctionIndex = PushDebugTraceback(_luaState, 0);
                oldTop++;
            }

            try
            {
                if (_luaState.PCall(0, -1, errorFunctionIndex) != LuaStatus.OK)
                    ThrowExceptionFromError(oldTop);

                return _translator.PopValues(_luaState, oldTop);
            }
            finally
            {
                _executing = false;
            }
        }

        /*
        * Executes a Lua file and returns all the chunk's return
        * values in an array
        */
        public object[] DoFile(string fileName)
        {
            int oldTop = _luaState.GetTop();

            if (_luaState.LoadFile(fileName) != LuaStatus.OK)
                ThrowExceptionFromError(oldTop);

            _executing = true;

            int errorFunctionIndex = 0;
            if (UseTraceback)
            {
                errorFunctionIndex = PushDebugTraceback(_luaState, 0);
                oldTop++;
            }

            try
            {
                if (_luaState.PCall(0, -1, errorFunctionIndex) != LuaStatus.OK)
                    ThrowExceptionFromError(oldTop);

                 return _translator.PopValues(_luaState, oldTop);
            }
            finally
            {
                _executing = false;
            }
        }

        public object GetObjectFromPath(string fullPath)
        {
            int oldTop = _luaState.GetTop();
            string[] path = FullPathToArray(fullPath);
            _luaState.GetGlobal(path[0]);
            object returnValue = _translator.GetObject(_luaState, -1);

            if (path.Length > 1)
            {
                var dispose = returnValue as LuaBase;
                string[] remainingPath = new string[path.Length - 1];
                Array.Copy(path, 1, remainingPath, 0, path.Length - 1);
                returnValue = GetObject(remainingPath);
                dispose?.Dispose();
            }

            _luaState.SetTop(oldTop);
            return returnValue;
        }

        public void SetObjectToPath(string fullPath, object value)
        {
            int oldTop = _luaState.GetTop();
            string[] path = FullPathToArray(fullPath);

            if (path.Length == 1)
            {
                _translator.Push(_luaState, value);
                _luaState.SetGlobal(fullPath);
            }
            else
            {
                _luaState.GetGlobal(path[0]);
                string[] remainingPath = new string[path.Length - 1];
                Array.Copy(path, 1, remainingPath, 0, path.Length - 1);
                SetObject(remainingPath, value);
            }

            _luaState.SetTop(oldTop);

            // Globals auto-complete
            if (value == null)
            {
                // Remove now obsolete entries
                _globals.Remove(fullPath);
            }
            else
            {
                // Add new entries
                if (!_globals.Contains(fullPath))
                    RegisterGlobal(fullPath, value.GetType(), 0);
            }
        }
        /*
            * Indexer for global variables from the LuaInterpreter
            * Supports navigation of tables by using . operator
            */
        public object this[string fullPath] {
            get
            {
                // Silently convert Lua integer to double for backward compatibility with index[] operator
                object obj = GetObjectFromPath(fullPath);
                if (obj is long l)
                    return (double)l;
                return obj;
            }
            set
            {
               SetObjectToPath(fullPath, value);
            }
        }

        #region Globals auto-complete
        /// <summary>
        /// Adds an entry to <see cref = "_globals"/> (recursivley handles 2 levels of members)
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
                _globals.Add(path + "(");
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
                        _globals.Add(command);
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
                _globals.Add(path); // Otherwise simply add the element to the list

            // List will need to be sorted on next access
            _globalsSorted = false;
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
                _luaState.PushString(remainingPath[i]);
                _luaState.GetTable(-2);
                returnValue = _translator.GetObject(_luaState, -1);

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
            // Silently convert Lua integer to double for backward compatibility with GetNumber method
            object obj = GetObjectFromPath(fullPath);
            if (obj is long l)
                return l;
            return (double)obj;
        }

        public int GetInteger(string fullPath)
        {
            return (int)(long)GetObjectFromPath(fullPath);
        }

        public long GetLong(string fullPath)
        {
            return (long)GetObjectFromPath(fullPath);
        }

        /*
            * Gets a string global variable
            */
        public string GetString(string fullPath)
        {
            object obj = GetObjectFromPath(fullPath);
            return obj.ToString();
        }

        /*
            * Gets a table global variable
            */
        public LuaTable GetTable(string fullPath)
        {
            return (LuaTable)GetObjectFromPath(fullPath);
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
            object obj = GetObjectFromPath(fullPath);
            var luaFunction = obj as LuaFunction;
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

        // ReSharper disable once InconsistentNaming
        public void LoadCLRPackage()
        {
            _luaState.DoString(ClrPackage);
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
            int oldTop = _luaState.GetTop();

            if (!_luaState.CheckStack(args.Length + 6))
                throw new LuaException("Lua stack overflow");

            _translator.Push(_luaState, function);

            if (args.Length > 0)
            {
                nArgs = args.Length;

                for (int i = 0; i < args.Length; i++)
                    _translator.Push(_luaState, args[i]);
            }

            _executing = true;

            try
            {
                int errfunction = 0;
                if (UseTraceback)
                {
                    errfunction = PushDebugTraceback(_luaState, nArgs);
                    oldTop++;
                }

                LuaStatus error = _luaState.PCall(nArgs, -1, errfunction);
                if (error != LuaStatus.OK)
                    ThrowExceptionFromError(oldTop);
            }
            finally
            {
                _executing = false;
            }

            if (returnTypes != null)
                return _translator.PopValues(_luaState, oldTop, returnTypes);

            return _translator.PopValues(_luaState, oldTop);
        }

        /*
            * Navigates a table to set the value of one of its fields
            */
        void SetObject(string[] remainingPath, object val)
        {
            for (int i = 0; i < remainingPath.Length - 1; i++)
            {
                _luaState.PushString(remainingPath[i]);
                _luaState.GetTable(-2);
            }

            _luaState.PushString(remainingPath[remainingPath.Length - 1]);
            _translator.Push(_luaState, val);
            _luaState.SetTable(-3);
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
            int oldTop = _luaState.GetTop();

            if (path.Length == 1)
            {
                _luaState.NewTable();
                _luaState.SetGlobal(fullPath);
            }
            else
            {
                _luaState.GetGlobal(path[0]);

                for (int i = 1; i < path.Length - 1; i++)
                {
                    _luaState.PushString(path[i]);
                    _luaState.GetTable(-2);
                }

                _luaState.PushString(path[path.Length - 1]);
                _luaState.NewTable();
                _luaState.SetTable(-3);
            }

            _luaState.SetTop( oldTop);
        }

        public Dictionary<object, object> GetTableDict(LuaTable table)
        {
            if (table == null)
                throw new ArgumentNullException(nameof(table));

            var dict = new Dictionary<object, object>();
            int oldTop = _luaState.GetTop();
            _translator.Push(_luaState, table);
            _luaState.PushNil();

            while (_luaState.Next(-2))
            {
                dict[_translator.GetObject(_luaState, -2)] = _translator.GetObject(_luaState, -1);
                _luaState.SetTop(-2);
            }

            _luaState.SetTop(oldTop);
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
            if (_hookCallback == null)
            {
                _hookCallback = DebugHookCallback;
                _luaState.SetHook(_hookCallback, mask, count);
            }

            return -1;
        }

        /// <summary>
        /// Removes the debug hook
        /// </summary>
        /// <returns>see lua docs</returns>
        public void RemoveDebugHook()
        {
            _hookCallback = null;
            _luaState.SetHook(null, LuaHookMask.Disabled, 0);
        }

        /// <summary>
        /// Gets the hook mask.
        /// </summary>
        /// <returns>hook mask</returns>
        public LuaHookMask GetHookMask()
        {
            return _luaState.HookMask;
        }

        /// <summary>
        /// Gets the hook count
        /// </summary>
        /// <returns>see lua docs</returns>
        public int GetHookCount()
        {
            return _luaState.HookCount;
        }


        /// <summary>
        /// Gets local (see lua docs)
        /// </summary>
        /// <param name = "luaDebug">lua debug structure</param>
        /// <param name = "n">see lua docs</param>
        /// <returns>see lua docs</returns>
        public string GetLocal(LuaDebug luaDebug, int n)
        {
            return _luaState.GetLocal(luaDebug, n);
        }

        /// <summary>
        /// Sets local (see lua docs)
        /// </summary>
        /// <param name = "luaDebug">lua debug structure</param>
        /// <param name = "n">see lua docs</param>
        /// <returns>see lua docs</returns>
        public string SetLocal(LuaDebug luaDebug, int n)
        {
            return _luaState.SetLocal(luaDebug, n);
        }

        public int GetStack(int level, ref LuaDebug ar)
        {
            return _luaState.GetStack(level, ref ar);
        }

        public bool GetInfo(string what, ref LuaDebug ar)
        {
            return _luaState.GetInfo(what, ref ar);
        }

        /// <summary>
        /// Gets up value (see lua docs)
        /// </summary>
        /// <param name = "funcindex">see lua docs</param>
        /// <param name = "n">see lua docs</param>
        /// <returns>see lua docs</returns>
        public string GetUpValue(int funcindex, int n)
        {
            return _luaState.GetUpValue(funcindex, n);
        }

        /// <summary>
        /// Sets up value (see lua docs)
        /// </summary>
        /// <param name = "funcindex">see lua docs</param>
        /// <param name = "n">see lua docs</param>
        /// <returns>see lua docs</returns>
        public string SetUpValue(int funcindex, int n)
        {
            return _luaState.SetUpValue(funcindex, n);
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
            lua.DebugHookCallbackInternal(debug);
        }

        private void DebugHookCallbackInternal(LuaDebug luaDebug)
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
            int top = _luaState.GetTop();
            return _translator.PopValues(_luaState, top - 1)[0];
        }

        /// <summary>
        /// Pushes a value onto the lua stack.
        /// </summary>
        /// <param name = "value">Value to push.</param>
        public void Push(object value)
        {
            _translator.Push(_luaState, value);
        }
        #endregion

        internal void DisposeInternal(int reference, bool finalized)
        {
            if (finalized && _translator != null)
            {
                _translator.AddFinalizedReference(reference);
                return;
            }

            if (_luaState != null && !finalized)
                _luaState.Unref(reference);
        }

        /*
         * Gets a field of the table corresponding to the provided reference
         * using rawget (do not use metatables)
         */
        internal object RawGetObject(int reference, string field)
        {
            int oldTop = _luaState.GetTop();
            _luaState.GetRef(reference);
            _luaState.PushString(field);
            _luaState.RawGet(-2);
            object obj = _translator.GetObject(_luaState, -1);
            _luaState.SetTop(oldTop);
            return obj;
        }

        /*
         * Gets a field of the table or userdata corresponding to the provided reference
         */
        internal object GetObject(int reference, string field)
        {
            int oldTop = _luaState.GetTop();
            _luaState.GetRef(reference);
            object returnValue = GetObject(FullPathToArray(field));
            _luaState.SetTop(oldTop);
            return returnValue;
        }

        /*
         * Gets a numeric field of the table or userdata corresponding the the provided reference
         */
        internal object GetObject(int reference, object field)
        {
            int oldTop = _luaState.GetTop();
            _luaState.GetRef(reference);
            _translator.Push(_luaState, field);
            _luaState.GetTable(-2);
            object returnValue = _translator.GetObject(_luaState, -1);
            _luaState.SetTop(oldTop);
            return returnValue;
        }

        /*
         * Sets a field of the table or userdata corresponding the the provided reference
         * to the provided value
         */
        internal void SetObject(int reference, string field, object val)
        {
            int oldTop = _luaState.GetTop();
            _luaState.GetRef(reference);
            SetObject(FullPathToArray(field), val);
            _luaState.SetTop(oldTop);
        }

        /*
         * Sets a numeric field of the table or userdata corresponding the the provided reference
         * to the provided value
         */
        internal void SetObject(int reference, object field, object val)
        {
            int oldTop = _luaState.GetTop();
            _luaState.GetRef(reference);
            _translator.Push(_luaState, field);
            _translator.Push(_luaState, val);
            _luaState.SetTable(-3);
            _luaState.SetTop(oldTop);
        }

        public LuaFunction RegisterFunction(string path, MethodBase function)
        {
            return RegisterFunction(path, null, function);
        }

        /*
         * Registers an object's method as a Lua function (global or table field)
         * The method may have any signature
         */
        public LuaFunction RegisterFunction(string path, object target, MethodBase function)
        {
            // We leave nothing on the stack when we are done
            int oldTop = _luaState.GetTop();
            var wrapper = new LuaMethodWrapper(_translator, target, new ProxyType(function.DeclaringType), function);
            
            _translator.Push(_luaState, new LuaNativeFunction(wrapper.InvokeFunction));
            
            object value = _translator.GetObject(_luaState, -1);
            SetObjectToPath(path, value);
            
            LuaFunction f = GetFunction(path);
            _luaState.SetTop(oldTop);
            return f;
        }

        /*
         * Compares the two values referenced by ref1 and ref2 for equality
         */
        internal bool CompareRef(int ref1, int ref2)
        {
            int top = _luaState.GetTop();
            _luaState.GetRef(ref1);
            _luaState.GetRef(ref2);
            bool equal = _luaState.AreEqual(-1, -2);
            _luaState.SetTop(top);
            return equal;
        }

        // ReSharper disable once InconsistentNaming
        internal void PushCSFunction(LuaNativeFunction function)
        {
            _translator.PushFunction(_luaState, function);
        }

        #region IDisposable Members

        ~Lua()
        {
            Dispose();
        }
        public virtual void Dispose()
        {
            if (_translator != null)
            {
                _translator.PendingEvents.Dispose();
                if (_translator.Tag != IntPtr.Zero)
                    Marshal.FreeHGlobal(_translator.Tag);
                _translator = null;
            }

            Close();
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
