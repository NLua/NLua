/*
 * This file is part of NLua.
 * 
 * Copyright (c) 2014 Vinicius Jarina (viniciusjarina@gmail.com)
 * Copyright (C) 2003-2005 Fabio Mascarenhas de Queiroz.
 * Copyright (C) 2012 Megax <http://megax.yeahunter.hu/>
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.  IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using NLua.Event;
using NLua.Method;
using NLua.Exceptions;
using NLua.Extensions;

namespace NLua
{
	#if USE_KOPILUA
	using LuaCore  = KopiLua.Lua;
	using LuaState = KopiLua.LuaState;
	using LuaHook  = KopiLua.LuaHook;
	using LuaDebug = KopiLua.LuaDebug;
	using LuaNativeFunction = KopiLua.LuaNativeFunction;
	#else
	using LuaCore  = KeraLua.Lua;
	using LuaState = KeraLua.LuaState;
	using LuaHook  = KeraLua.LuaHook;
	using LuaDebug = KeraLua.LuaDebug;
	using LuaNativeFunction = KeraLua.LuaNativeFunction;
	#endif

	/*
	 * Main class of NLua
	 * Object-oriented wrapper to Lua API
	 *
	 * Author: Fabio Mascarenhas
	 * Version: 1.0
	 * 
	 * // steffenj: important changes in Lua class:
	 * - removed all Open*Lib() functions 
	 * - all libs automatically open in the Lua class constructor (just assign nil to unwanted libs)
	 * */
#if !UNITY_3D
	[CLSCompliant(true)]
#endif
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
		private LuaHook hookCallback = null;
		#endregion
		#region Globals auto-complete
		private readonly List<string> globals = new List<string> ();
		private bool globalsSorted;
		#endregion
		private LuaState luaState;
		/// <summary>
		/// True while a script is being executed
		/// </summary>
		public bool IsExecuting { get { return executing; } }

		private LuaNativeFunction panicCallback;
		private ObjectTranslator translator;
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

		#region Globals auto-complete
		/// <summary>
		/// An alphabetically sorted list of all globals (objects, methods, etc.) externally added to this Lua instance
		/// </summary>
		/// <remarks>Members of globals are also listed. The formatting is optimized for text input auto-completion.</remarks>
		public IEnumerable<string> Globals {
			get {
				// Only sort list when necessary
				if (!globalsSorted) {
					globals.Sort ();
					globalsSorted = true;
				}

				return globals;
			}
		}
		#endregion

		public Lua ()
		{
			luaState = LuaLib.LuaLNewState ();
			LuaLib.LuaLOpenLibs (luaState);
			Init ();
			// We need to keep this in a managed reference so the delegate doesn't get garbage collected
			panicCallback = new LuaNativeFunction (PanicCallback);
			LuaLib.LuaAtPanic (luaState, panicCallback);
		}

		/*
			* CAUTION: NLua.Lua instances can't share the same lua state! 
			*/
		public Lua (LuaState lState)
		{
			LuaLib.LuaPushString (lState, "LUAINTERFACE LOADED");
			LuaLib.LuaGetTable (lState, (int)LuaIndexes.Registry);

			if (LuaLib.LuaToBoolean (lState, -1)) {
				LuaLib.LuaSetTop (lState, -2);
				throw new LuaException ("There is already a NLua.Lua instance associated with this Lua state");
			} else {
				luaState = lState;
				_StatePassed = true;
				LuaLib.LuaSetTop (luaState, -2);
				Init ();
			}
		}

		void Init ()
		{
			LuaLib.LuaPushString (luaState, "LUAINTERFACE LOADED");
			LuaLib.LuaPushBoolean (luaState, true);
			LuaLib.LuaSetTable (luaState, (int)LuaIndexes.Registry);
			if (_StatePassed == false) {
				LuaLib.LuaNewTable (luaState);
				LuaLib.LuaSetGlobal (luaState, "luanet");
			}
			LuaLib.LuaNetPushGlobalTable (luaState);
			LuaLib.LuaGetGlobal (luaState, "luanet");
			LuaLib.LuaPushString (luaState, "getmetatable");
			LuaLib.LuaGetGlobal (luaState, "getmetatable");
			LuaLib.LuaSetTable (luaState, -3);
			LuaLib.LuaNetPopGlobalTable (luaState);
			translator = new ObjectTranslator (this, luaState);
			ObjectTranslatorPool.Instance.Add (luaState, translator);
			LuaLib.LuaNetPopGlobalTable (luaState);
			LuaLib.LuaLDoString (luaState, Lua.initLuanet);
		}

		public void Close ()
		{
			if (_StatePassed)
				return;

			if (! CheckNull.IsNull(luaState)) {
				LuaCore.LuaClose (luaState);
				ObjectTranslatorPool.Instance.Remove (luaState);
			}
		}

#if MONOTOUCH
		[MonoTouch.MonoPInvokeCallback (typeof (LuaNativeFunction))]
#endif
		[System.Runtime.InteropServices.AllowReversePInvokeCalls]
		static int PanicCallback (LuaState luaState)
		{
			string reason = string.Format ("unprotected error in call to Lua API ({0})", LuaLib.LuaToString (luaState, -1));
			throw new LuaException (reason);
		}

		/// <summary>
		/// Assuming we have a Lua error string sitting on the stack, throw a C# exception out to the user's app
		/// </summary>
		/// <exception cref = "LuaScriptException">Thrown if the script caused an exception</exception>
		private void ThrowExceptionFromError (int oldTop)
		{
			object err = translator.GetObject (luaState, -1);
			LuaLib.LuaSetTop (luaState, oldTop);

			// A pre-wrapped exception - just rethrow it (stack trace of InnerException will be preserved)
			var luaEx = err as LuaScriptException;

			if (luaEx != null)
				throw luaEx;

			// A non-wrapped Lua error (best interpreted as a string) - wrap it and throw it
			if (err == null)
				err = "Unknown Lua Error";

			throw new LuaScriptException (err.ToString (), string.Empty);
		}

		/// <summary>
		/// Convert C# exceptions into Lua errors
		/// </summary>
		/// <returns>num of things on stack</returns>
		/// <param name = "e">null for no pending exception</param>
		internal int SetPendingException (Exception e)
		{
			var caughtExcept = e;

			if (caughtExcept != null) {
				translator.ThrowError (luaState, caughtExcept);
				LuaLib.LuaPushNil (luaState);
				return 1;
			} else
				return 0;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name = "chunk"></param>
		/// <param name = "name"></param>
		/// <returns></returns>
		public LuaFunction LoadString (string chunk, string name)
		{
			int oldTop = LuaLib.LuaGetTop (luaState);
			executing = true;

			try {
				if (LuaLib.LuaLLoadBuffer (luaState, chunk, name) != 0)
					ThrowExceptionFromError (oldTop);
			} finally {
				executing = false;
			}

			var result = translator.GetFunction (luaState, -1);
			translator.PopValues (luaState, oldTop);
			return result;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name = "chunk"></param>
		/// <param name = "name"></param>
		/// <returns></returns>
		public LuaFunction LoadString (byte[] chunk, string name)
		{
			int oldTop = LuaLib.LuaGetTop (luaState);
			executing = true;
			
			try {
				if (LuaLib.LuaLLoadBuffer (luaState, chunk, name) != 0)
					ThrowExceptionFromError (oldTop);
			} finally {
				executing = false;
			}
			
			var result = translator.GetFunction (luaState, -1);
			translator.PopValues (luaState, oldTop);
			return result;
		}
		
		/// <summary>
		/// 
		/// </summary>
		/// <param name = "fileName"></param>
		/// <returns></returns>
		public LuaFunction LoadFile (string fileName)
		{
			int oldTop = LuaLib.LuaGetTop (luaState);

			if (LuaLib.LuaLLoadFile (luaState, fileName) != 0)
				ThrowExceptionFromError (oldTop);

			var result = translator.GetFunction (luaState, -1);
			translator.PopValues (luaState, oldTop);
			return result;
		}

		/// <summary>
		/// Executes a Lua chunk and returns all the chunk's return values in an array.
		/// </summary>
		/// <param name = "chunk">Chunk to execute</param>
		/// <param name = "chunkName">Name to associate with the chunk. Defaults to "chunk".</param>
		/// <returns></returns>
		public object[] DoString (byte[] chunk, string chunkName = "chunk")
		{
			int oldTop = LuaLib.LuaGetTop(luaState);
			executing = true;

			if (LuaLib.LuaLLoadBuffer(luaState, chunk, chunkName) == 0)
			{
				try
				{
					if (LuaLib.LuaPCall(luaState, 0, -1, 0) == 0)
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

			return null;			// Never reached - keeps compiler happy
		}

		/// <summary>
		/// Executes a Lua chunk and returns all the chunk's return values in an array.
		/// </summary>
		/// <param name = "chunk">Chunk to execute</param>
		/// <param name = "chunkName">Name to associate with the chunk. Defaults to "chunk".</param>
		/// <returns></returns>
		public object[] DoString (string chunk, string chunkName = "chunk")
		{
			int oldTop = LuaLib.LuaGetTop(luaState);
			executing = true;

			if (LuaLib.LuaLLoadBuffer(luaState, chunk, chunkName) == 0)
			{
				try
				{
					if (LuaLib.LuaPCall(luaState, 0, -1, 0) == 0)
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

			return null;			// Never reached - keeps compiler happy
		}

		/*
			* Excutes a Lua file and returns all the chunk's return
			* values in an array
			*/
		public object[] DoFile (string fileName)
		{
			int oldTop = LuaLib.LuaGetTop (luaState);

			if (LuaLib.LuaLLoadFile (luaState, fileName) == 0) {
				executing = true;

				try {
					if (LuaLib.LuaPCall (luaState, 0, -1, 0) == 0)
						return translator.PopValues (luaState, oldTop);
					else
						ThrowExceptionFromError (oldTop);
				} finally {
					executing = false;
				}
			} else
				ThrowExceptionFromError (oldTop);

			return null;			// Never reached - keeps compiler happy
		}


		/*
			* Indexer for global variables from the LuaInterpreter
			* Supports navigation of tables by using . operator
			*/
		public object this [string fullPath] {
			get {
				object returnValue = null;
				int oldTop = LuaLib.LuaGetTop (luaState);
				string [] path = FullPathToArray (fullPath);
				LuaLib.LuaGetGlobal (luaState, path [0]);
				returnValue = translator.GetObject (luaState, -1);
				LuaBase dispose = null;

				if (path.Length > 1) {
					dispose = returnValue as LuaBase;
					string[] remainingPath = new string[path.Length - 1];
					Array.Copy (path, 1, remainingPath, 0, path.Length - 1);
					returnValue = GetObject (remainingPath);
					if (dispose != null)
						dispose.Dispose ();
				}

				LuaLib.LuaSetTop (luaState, oldTop);
				return returnValue;
			}
			set {
				int oldTop = LuaLib.LuaGetTop (luaState);
				string [] path = FullPathToArray (fullPath);
				if (path.Length == 1) {
					translator.Push (luaState, value);
					LuaLib.LuaSetGlobal (luaState, fullPath);
				} else {
					LuaLib.LuaGetGlobal (luaState, path [0]);
					string[] remainingPath = new string[path.Length - 1];
					Array.Copy (path, 1, remainingPath, 0, path.Length - 1);
					SetObject (remainingPath, value);
				}

				LuaLib.LuaSetTop (luaState, oldTop);

				// Globals auto-complete
				if (value == null) {
					// Remove now obsolete entries
					globals.Remove (fullPath);
				} else {
					// Add new entries
					if (!globals.Contains (fullPath))
						RegisterGlobal (fullPath, value.GetType (), 0);
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
		private void RegisterGlobal (string path, Type type, int recursionCounter)
		{
			// If the type is a global method, list it directly
			if (type == typeof(LuaNativeFunction)) {
				// Format for easy method invocation
				globals.Add (path + "(");
			}
			// If the type is a class or an interface and recursion hasn't been running too long, list the members
			else if ((type.IsClass || type.IsInterface) && type != typeof(string) && recursionCounter < 2) {
				#region Methods
				foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance)) {
					string name = method.Name;
					if (
						// Check that the LuaHideAttribute and LuaGlobalAttribute were not applied
						(method.GetCustomAttributes (typeof(LuaHideAttribute), false).Length == 0) &&
						(method.GetCustomAttributes (typeof(LuaGlobalAttribute), false).Length == 0) &&
					// Exclude some generic .NET methods that wouldn't be very usefull in Lua
						name != "GetType" && name != "GetHashCode" && name != "Equals" &&
						name != "ToString" && name != "Clone" && name != "Dispose" &&
						name != "GetEnumerator" && name != "CopyTo" &&
						!name.StartsWith ("get_", StringComparison.Ordinal) &&
						!name.StartsWith ("set_", StringComparison.Ordinal) &&
						!name.StartsWith ("add_", StringComparison.Ordinal) &&
						!name.StartsWith ("remove_", StringComparison.Ordinal)) {
						// Format for easy method invocation
						string command = path + ":" + name + "(";

						if (method.GetParameters ().Length == 0)
							command += ")";
						globals.Add (command);
					}
				}
				#endregion

				#region Fields
				foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance)) {
					if (
						// Check that the LuaHideAttribute and LuaGlobalAttribute were not applied
						(field.GetCustomAttributes (typeof(LuaHideAttribute), false).Length == 0) &&
						(field.GetCustomAttributes (typeof(LuaGlobalAttribute), false).Length == 0)) {
						// Go into recursion for members
						RegisterGlobal (path + "." + field.Name, field.FieldType, recursionCounter + 1);
					}
				}
				#endregion

				#region Properties
				foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
					if (
						// Check that the LuaHideAttribute and LuaGlobalAttribute were not applied
						(property.GetCustomAttributes (typeof(LuaHideAttribute), false).Length == 0) &&
						(property.GetCustomAttributes (typeof(LuaGlobalAttribute), false).Length == 0)
					// Exclude some generic .NET properties that wouldn't be very usefull in Lua
						&& property.Name != "Item") {
						// Go into recursion for members
						RegisterGlobal (path + "." + property.Name, property.PropertyType, recursionCounter + 1);
					}
				}
				#endregion
			} else
				globals.Add (path); // Otherwise simply add the element to the list

			// List will need to be sorted on next access
			globalsSorted = false;
		}
		#endregion

		/*
			* Navigates a table in the top of the stack, returning
			* the value of the specified field
			*/
		internal object GetObject (string[] remainingPath)
		{
			object returnValue = null;

			for (int i = 0; i < remainingPath.Length; i++) {
				LuaLib.LuaPushString (luaState, remainingPath [i]);
				LuaLib.LuaGetTable (luaState, -2);
				returnValue = translator.GetObject (luaState, -1);

				if (returnValue == null)
					break;	
			}

			return returnValue;	
		}

		/*
			* Gets a numeric global variable
			*/
		public double GetNumber (string fullPath)
		{
			return (double)this [fullPath];
		}

		/*
			* Gets a string global variable
			*/
		public string GetString (string fullPath)
		{
			return this [fullPath].ToString ();
		}

		/*
			* Gets a table global variable
			*/
		public LuaTable GetTable (string fullPath)
		{
			return (LuaTable)this [fullPath];
		}

		/*
			* Gets a table global variable as an object implementing
			* the interfaceType interface
			*/
		public object GetTable (Type interfaceType, string fullPath)
		{
			return CodeGeneration.Instance.GetClassInstance (interfaceType, GetTable (fullPath));
		}

		/*
			* Gets a function global variable
			*/
		public LuaFunction GetFunction (string fullPath)
		{
			object obj = this [fullPath];
			return (obj is LuaNativeFunction ? new LuaFunction ((LuaNativeFunction)obj, this) : (LuaFunction)obj);
		}

		/*
			* Register a delegate type to be used to convert Lua functions to C# delegates (useful for iOS where there is no dynamic code generation)
			* type delegateType
			*/
		public void RegisterLuaDelegateType (Type delegateType, Type luaDelegateType)
		{
			CodeGeneration.Instance.RegisterLuaDelegateType (delegateType, luaDelegateType);
		}

		public void RegisterLuaClassType (Type klass, Type luaClass)
		{
			CodeGeneration.Instance.RegisterLuaClassType (klass, luaClass);
		}

		public void LoadCLRPackage ()
		{
			LuaLib.LuaLDoString (luaState, Lua.clr_package);
		}
		/*
			* Gets a function global variable as a delegate of
			* type delegateType
			*/
		public Delegate GetFunction (Type delegateType, string fullPath)
		{
			return CodeGeneration.Instance.GetDelegate (delegateType, GetFunction (fullPath));
		}

		/*
			* Calls the object as a function with the provided arguments, 
			* returning the function's returned values inside an array
			*/
		internal object[] CallFunction (object function, object[] args)
		{
			return CallFunction (function, args, null);
		}

		/*
			* Calls the object as a function with the provided arguments and
			* casting returned values to the types in returnTypes before returning
			* them in an array
			*/
		internal object[] CallFunction (object function, object[] args, Type[] returnTypes)
		{
			int nArgs = 0;
			int oldTop = LuaLib.LuaGetTop (luaState);

			if (!LuaLib.LuaCheckStack (luaState, args.Length + 6))
				throw new LuaException ("Lua stack overflow");

			translator.Push (luaState, function);

			if (args != null) {
				nArgs = args.Length;

				for (int i = 0; i < args.Length; i++) 
					translator.Push (luaState, args [i]);
			}

			executing = true;

			try {
				int error = LuaLib.LuaPCall (luaState, nArgs, -1, 0);
				if (error != 0)
					ThrowExceptionFromError (oldTop);
			} finally {
				executing = false;
			}

			return returnTypes != null ? translator.PopValues (luaState, oldTop, returnTypes) : translator.PopValues (luaState, oldTop);
		}

		/*
			* Navigates a table to set the value of one of its fields
			*/
		internal void SetObject (string[] remainingPath, object val)
		{
			for (int i = 0; i < remainingPath.Length-1; i++) {
				LuaLib.LuaPushString (luaState, remainingPath [i]);
				LuaLib.LuaGetTable (luaState, -2);
			}

			LuaLib.LuaPushString (luaState, remainingPath [remainingPath.Length - 1]);
			translator.Push (luaState, val);
			LuaLib.LuaSetTable (luaState, -3);
		}

		string [] FullPathToArray (string fullPath)
		{
			return fullPath.SplitWithEscape ('.', '\\').ToArray ();
		}
		/*
			* Creates a new table as a global variable or as a field
			* inside an existing table
			*/
		public void NewTable (string fullPath)
		{
			string [] path = FullPathToArray (fullPath);
			int oldTop = LuaLib.LuaGetTop (luaState);

			if (path.Length == 1) {
				LuaLib.LuaNewTable (luaState);
				LuaLib.LuaSetGlobal (luaState, fullPath);
			} else {
				LuaLib.LuaGetGlobal (luaState, path [0]);

				for (int i = 1; i < path.Length-1; i++) {
					LuaLib.LuaPushString (luaState, path [i]);
					LuaLib.LuaGetTable (luaState, -2);
				}

				LuaLib.LuaPushString (luaState, path [path.Length - 1]);
				LuaLib.LuaNewTable (luaState);
				LuaLib.LuaSetTable (luaState, -3);
			}

			LuaLib.LuaSetTop (luaState, oldTop);
		}

		public Dictionary<object, object> GetTableDict (LuaTable table)
		{
			var dict = new Dictionary<object, object> ();
			int oldTop = LuaLib.LuaGetTop (luaState);
			translator.Push (luaState, table);
			LuaLib.LuaPushNil (luaState);

			while (LuaLib.LuaNext(luaState, -2) != 0) {
				dict [translator.GetObject (luaState, -2)] = translator.GetObject (luaState, -1);
				LuaLib.LuaSetTop (luaState, -2);
			}

			LuaLib.LuaSetTop (luaState, oldTop);
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
		public int SetDebugHook (EventMasks mask, int count)
		{
			if (hookCallback == null) {
				hookCallback = new LuaHook (Lua.DebugHookCallback);
				return LuaCore.LuaSetHook (luaState, hookCallback, (int)mask, count);
			}

			return -1;
		}

		/// <summary>
		/// Removes the debug hook
		/// </summary>
		/// <returns>see lua docs</returns>
		public int RemoveDebugHook ()
		{
			hookCallback = null;
			return LuaCore.LuaSetHook (luaState, null, 0, 0);
		}

		/// <summary>
		/// Gets the hook mask.
		/// </summary>
		/// <returns>hook mask</returns>
		public EventMasks GetHookMask ()
		{
			return (EventMasks)LuaCore.LuaGetHookMask (luaState);
		}

		/// <summary>
		/// Gets the hook count
		/// </summary>
		/// <returns>see lua docs</returns>
		public int GetHookCount ()
		{
			return LuaCore.LuaGetHookCount (luaState);
		}

			
		/// <summary>
		/// Gets local (see lua docs)
		/// </summary>
		/// <param name = "luaDebug">lua debug structure</param>
		/// <param name = "n">see lua docs</param>
		/// <returns>see lua docs</returns>
		public string GetLocal (LuaDebug luaDebug, int n)
		{
			return LuaCore.LuaGetLocal (luaState, luaDebug, n).ToString ();
		}

		/// <summary>
		/// Sets local (see lua docs)
		/// </summary>
		/// <param name = "luaDebug">lua debug structure</param>
		/// <param name = "n">see lua docs</param>
		/// <returns>see lua docs</returns>
		public string SetLocal (LuaDebug luaDebug, int n)
		{
			return LuaCore.LuaSetLocal (luaState, luaDebug, n).ToString ();
		}

		public int GetStack (int level, ref LuaDebug ar)
		{
			return LuaCore.LuaGetStack (luaState, level,ref ar);
		}

		public int GetInfo (string what, ref LuaDebug ar)
		{
			return LuaCore.LuaGetInfo (luaState, what, ref ar);
		}

		/// <summary>
		/// Gets up value (see lua docs)
		/// </summary>
		/// <param name = "funcindex">see lua docs</param>
		/// <param name = "n">see lua docs</param>
		/// <returns>see lua docs</returns>
		public string GetUpValue (int funcindex, int n)
		{
			return LuaCore.LuaGetUpValue (luaState, funcindex, n).ToString ();
		}

		/// <summary>
		/// Sets up value (see lua docs)
		/// </summary>
		/// <param name = "funcindex">see lua docs</param>
		/// <param name = "n">see lua docs</param>
		/// <returns>see lua docs</returns>
		public string SetUpValue (int funcindex, int n)
		{
			return LuaCore.LuaSetUpValue (luaState, funcindex, n).ToString ();
		}

		/// <summary>
		/// Delegate that is called on lua hook callback
		/// </summary>
		/// <param name = "luaState">lua state</param>
		/// <param name = "luaDebug">Pointer to LuaDebug (lua_debug) structure</param>
		/// 
#if MONOTOUCH
		[MonoTouch.MonoPInvokeCallback (typeof (LuaHook))]
#endif
		[System.Runtime.InteropServices.AllowReversePInvokeCalls]
#if USE_KOPILUA
		static void DebugHookCallback (LuaState luaState, LuaDebug debug)
		{
#else
		static void DebugHookCallback (LuaState luaState, IntPtr luaDebug)
		{	
			LuaDebug debug = (LuaDebug)System.Runtime.InteropServices.Marshal.PtrToStructure (luaDebug, typeof (LuaDebug));
#endif
			ObjectTranslator translator = ObjectTranslatorPool.Instance.Find (luaState);
			Lua lua = translator.Interpreter;
			lua.DebugHookCallbackInternal (luaState, debug);
		}

		private void DebugHookCallbackInternal (LuaState luaState, LuaDebug luaDebug)
		{
			try {
				var temp = DebugHook;

				if (temp != null)
					temp (this, new DebugHookEventArgs (luaDebug));
			} catch (Exception ex) {
				OnHookException (new HookExceptionEventArgs (ex));
			}
		}

		private void OnHookException (HookExceptionEventArgs e)
		{
			var temp = HookException;
			if (temp != null)
				temp (this, e);
		}

		/// <summary>
		/// Pops a value from the lua stack.
		/// </summary>
		/// <returns>Returns the top value from the lua stack.</returns>
		public object Pop ()
		{
			int top = LuaLib.LuaGetTop (luaState);
			return translator.PopValues (luaState, top - 1) [0];
		}

		/// <summary>
		/// Pushes a value onto the lua stack.
		/// </summary>
		/// <param name = "value">Value to push.</param>
		public void Push (object value)
		{
			translator.Push (luaState, value);
		}
		#endregion

		internal void DisposeInternal (int reference)
		{
			if (! CheckNull.IsNull(luaState)) //Fix submitted by Qingrui Li
				LuaLib.LuaUnref (luaState, reference);
		}

		/*
		 * Gets a field of the table corresponding to the provided reference
		 * using rawget (do not use metatables)
		 */
		internal object RawGetObject (int reference, string field)
		{
			int oldTop = LuaLib.LuaGetTop (luaState);
			LuaLib.LuaGetRef (luaState, reference);
			LuaLib.LuaPushString (luaState, field);
			LuaLib.LuaRawGet (luaState, -2);
			object obj = translator.GetObject (luaState, -1);
			LuaLib.LuaSetTop (luaState, oldTop);
			return obj;
		}

		/*
		 * Gets a field of the table or userdata corresponding to the provided reference
		 */
		internal object GetObject (int reference, string field)
		{
			int oldTop = LuaLib.LuaGetTop (luaState);
			LuaLib.LuaGetRef (luaState, reference);
			object returnValue = GetObject (FullPathToArray (field));
			LuaLib.LuaSetTop (luaState, oldTop);
			return returnValue;
		}

		/*
		 * Gets a numeric field of the table or userdata corresponding the the provided reference
		 */
		internal object GetObject (int reference, object field)
		{
			int oldTop = LuaLib.LuaGetTop (luaState);
			LuaLib.LuaGetRef (luaState, reference);
			translator.Push (luaState, field);
			LuaLib.LuaGetTable (luaState, -2);
			object returnValue = translator.GetObject (luaState, -1);
			LuaLib.LuaSetTop (luaState, oldTop);
			return returnValue;
		}

		/*
		 * Sets a field of the table or userdata corresponding the the provided reference
		 * to the provided value
		 */		internal void SetObject (int reference, string field, object val)
		{
			int oldTop = LuaLib.LuaGetTop (luaState);
			LuaLib.LuaGetRef (luaState, reference);
			SetObject (FullPathToArray (field), val);
			LuaLib.LuaSetTop (luaState, oldTop);
		}

		/*
		 * Sets a numeric field of the table or userdata corresponding the the provided reference
		 * to the provided value
		 */
		internal void SetObject (int reference, object field, object val)
		{
			int oldTop = LuaLib.LuaGetTop (luaState);
			LuaLib.LuaGetRef (luaState, reference);
			translator.Push (luaState, field);
			translator.Push (luaState, val);
			LuaLib.LuaSetTable (luaState, -3);
			LuaLib.LuaSetTop (luaState, oldTop);
		}

		public LuaFunction RegisterFunction (string path,MethodBase function /*MethodInfo function*/)
		{
			return RegisterFunction (path, null, function);
		}

		/*
		 * Registers an object's method as a Lua function (global or table field)
		 * The method may have any signature
		 */
		public LuaFunction RegisterFunction (string path, object target, MethodBase function /*MethodInfo function*/)  //CP: Fix for struct constructor by Alexander Kappner (link: http://luaforge.net/forum/forum.php?thread_id = 2859&forum_id = 145)
		{
			// We leave nothing on the stack when we are done
			int oldTop = LuaLib.LuaGetTop (luaState);
			var wrapper = new LuaMethodWrapper (translator, target, function.DeclaringType, function);
			translator.Push (luaState, new LuaNativeFunction (wrapper.invokeFunction));
			this [path] = translator.GetObject (luaState, -1);
			var f = GetFunction (path);
			LuaLib.LuaSetTop (luaState, oldTop);
			return f;
		}

		/*
		 * Compares the two values referenced by ref1 and ref2 for equality
		 */
		internal bool CompareRef (int ref1, int ref2)
		{
			int top = LuaLib.LuaGetTop (luaState);
			LuaLib.LuaGetRef (luaState, ref1);
			LuaLib.LuaGetRef (luaState, ref2);
			int equal = LuaLib.LuaEqual (luaState, -1, -2);
			LuaLib.LuaSetTop (luaState, top);
			return (equal != 0);
		}

		internal void PushCSFunction (LuaNativeFunction function)
		{
			translator.PushFunction (luaState, function);
		}

		#region IDisposable Members
		public virtual void Dispose ()
		{
			if (translator != null) {
				translator.pendingEvents.Dispose ();
				translator = null;
			}

			Close ();
			GC.WaitForPendingFinalizers ();
		}
		#endregion
	}
}