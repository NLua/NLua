/*
 * This file is part of LuaInterface.
 * 
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
using System.Threading;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using LuaInterface.Event;
using LuaInterface.Method;
using LuaInterface.Exceptions;
using LuaInterface.Extensions;

namespace LuaInterface
{
	using LuaCore = KopiLua.Lua;

	/*
	 * Main class of LuaInterface
	 * Object-oriented wrapper to Lua API
	 *
	 * Author: Fabio Mascarenhas
	 * Version: 1.0
	 * 
	 * // steffenj: important changes in Lua class:
	 * - removed all Open*Lib() functions 
	 * - all libs automatically open in the Lua class constructor (just assign nil to unwanted libs)
	 * */
	[CLSCompliant(true)]
	public class Lua : IDisposable
	{
		#region lua debug functions
		/// <summary>
		/// Event that is raised when an exception occures during a hook call.
		/// </summary>
		/// <author>Reinhard Ostermeier</author>
		public event EventHandler<HookExceptionEventArgs> HookException;
		/// <summary>
		/// Event when lua hook callback is called
		/// </summary>
		/// <remarks>
		/// Is only raised if SetDebugHook is called before.
		/// </remarks>
		/// <author>Reinhard Ostermeier</author>
		public event EventHandler<DebugHookEventArgs> DebugHook;
		/// <summary>
		/// lua hook calback delegate
		/// </summary>
		/// <author>Reinhard Ostermeier</author>
		private LuaCore.lua_Hook hookCallback = null;
		#endregion
		#region Globals auto-complete
		private readonly List<string> globals = new List<string>();
		private bool globalsSorted;
		#endregion
		private /*readonly */ LuaCore.lua_State luaState;
		/// <summary>
		/// True while a script is being executed
		/// </summary>
		public bool IsExecuting { get { return executing; } }
		private LuaCore.lua_CFunction panicCallback;
		private ObjectTranslator translator;
		/// <summary>
		/// Used to ensure multiple .net threads all get serialized by this single lock for access to the lua stack/objects
		/// </summary>
		private object luaLock = new object();
		private bool _StatePassed;
		private bool executing;

		static string init_luanet =
			"local metatable = {}														\n" +
			"local import_type = luanet.import_type										\n" +
			"local load_assembly = luanet.load_assembly									\n" +
			"																			\n" +
			"-- Lookup a .NET identifier component.										\n" +
			"function metatable:__index(key) -- key is e.g. \"Form\"					\n" +
			"	-- Get the fully-qualified name, e.g. \"System.Windows.Forms.Form\"		\n" +
			"	local fqn = ((rawget(self, \".fqn\") and rawget(self, \".fqn\") ..		\n" +
			"		\".\") or \"\") .. key												\n" +
			"																			\n" +
			"	-- Try to find either a luanet function or a CLR type					\n" +
			"	local obj = rawget(luanet, key) or import_type(fqn)						\n" +
			"																			\n" +
			"	-- If key is neither a luanet function or a CLR type, then it is simply	\n" +
			"	-- an identifier component.												\n" +
			"	if obj == nil then														\n" +
			"		-- It might be an assembly, so we load it too.						\n" +
			"		load_assembly(fqn)													\n" +
			"		obj = { [\".fqn\"] = fqn }											\n" +
			"		setmetatable(obj, metatable)										\n" +
			"	end																		\n" +
			"																			\n" +
			"	-- Cache this lookup													\n" +
			"	rawset(self, key, obj)													\n" +
			"	return obj																\n" +
			"end																		\n" +
			"																			\n" +
			"-- A non-type has been called; e.g. foo = System.Foo()						\n" +
			"function metatable:__call(...)												\n" +
			"	error(\"No such type: \" .. rawget(self, \".fqn\"), 2)					\n" +
			"end																		\n" +
			"																			\n" +
			"-- This is the root of the .NET namespace									\n" +
			"luanet[\".fqn\"] = false													\n" +
			"setmetatable(luanet, metatable)											\n" +
			"																			\n" +
			"-- Preload the mscorlib assembly											\n" +
			"luanet.load_assembly(\"mscorlib\")											\n";

		#region Globals auto-complete
		/// <summary>
		/// An alphabetically sorted list of all globals (objects, methods, etc.) externally added to this Lua instance
		/// </summary>
		/// <remarks>Members of globals are also listed. The formatting is optimized for text input auto-completion.</remarks>
		public IEnumerable<string> Globals
		{
			get
			{
				// Only sort list when necessary
				if(!globalsSorted)
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
			luaState = LuaCore.luaL_newstate();	// steffenj: Lua 5.1.1 API change (lua_open is gone)
			//LuaCore.luaopen_base(luaState);	// steffenj: luaopen_* no longer used
			LuaCore.luaL_openlibs(luaState);		// steffenj: Lua 5.1.1 API change (luaopen_base is gone, just open all libs right here)
			LuaCore.lua_pushstring(luaState, "LUAINTERFACE LOADED");
			LuaCore.lua_pushboolean(luaState, 1);
			LuaCore.lua_settable(luaState, (int)PseudoIndex.Registry);
			LuaCore.lua_newtable(luaState);
			LuaCore.lua_setglobal(luaState, "luanet");
			LuaCore.lua_pushvalue(luaState, (int)PseudoIndex.Globals);
			LuaCore.lua_getglobal(luaState, "luanet");
			LuaCore.lua_pushstring(luaState, "getmetatable");
			LuaCore.lua_getglobal(luaState, "getmetatable");
			LuaCore.lua_settable(luaState, -3);
			LuaCore.lua_replace(luaState, (int)PseudoIndex.Globals);
			translator = new ObjectTranslator(this, luaState);
			LuaCore.lua_replace(luaState, (int)PseudoIndex.Globals);
			LuaLib.luaL_dostring(luaState, Lua.init_luanet);	// steffenj: lua_dostring renamed to luaL_dostring

			// We need to keep this in a managed reference so the delegate doesn't get garbage collected
			panicCallback = new LuaCore.lua_CFunction(PanicCallback);
			LuaCore.lua_atpanic(luaState, panicCallback);

			//LuaCore.lua_atlock(luaState, lockCallback = new LuaCore.lua_CFunction(LockCallback));
			//LuaCore.lua_atunlock(luaState, unlockCallback = new LuaCore.lua_CFunction(UnlockCallback));
		}

		/*
			* CAUTION: LuaInterface.Lua instances can't share the same lua state! 
			*/
		public Lua(LuaCore.lua_State luaState)
		{
			LuaCore.lua_State lState = luaState;
			LuaCore.lua_pushstring(lState, "LUAINTERFACE LOADED");
			LuaCore.lua_gettable(lState, (int)PseudoIndex.Registry);
			
			if(LuaCore.lua_toboolean(lState, -1).ToBoolean()) 
			{
				LuaCore.lua_settop(lState, -2);
				throw new LuaException("There is already a LuaInterface.Lua instance associated with this Lua state");
			} 
			else 
			{
				LuaCore.lua_settop(lState, -2);
				LuaCore.lua_pushstring(lState, "LUAINTERFACE LOADED");
				LuaCore.lua_pushboolean(lState, 1);
				LuaCore.lua_settable(lState, (int)PseudoIndex.Registry);
				this.luaState = lState;
				LuaCore.lua_pushvalue(lState, (int)PseudoIndex.Globals);
				LuaCore.lua_getglobal(lState, "luanet");
				LuaCore.lua_pushstring(lState, "getmetatable");
				LuaCore.lua_getglobal(lState, "getmetatable");
				LuaCore.lua_settable(lState, -3);
				LuaCore.lua_replace(lState, (int)PseudoIndex.Globals);
				translator = new ObjectTranslator(this, this.luaState);
				LuaCore.lua_replace(lState, (int)PseudoIndex.Globals);
				LuaLib.luaL_dostring(lState, Lua.init_luanet);	// steffenj: lua_dostring renamed to luaL_dostring
			}
				
			_StatePassed = true;
		}

		/// <summary>
		/// Called for each lua_lock call 
		/// </summary>
		/// <param name = "luaState"></param>
		/// Not yet used
		/*int LockCallback(LuaCore.lua_State luaState)
		{
			// Monitor.Enter(luaLock);
			return 0;
		}*/

		/// <summary>
		/// Called for each lua_unlock call 
		/// </summary>
		/// <param name = "luaState"></param>
		/// Not yet used
		/*int UnlockCallback(LuaCore.lua_State luaState)
		{
			// Monitor.Exit(luaLock);
			return 0;
		}*/

		public void Close()
		{
			if(_StatePassed)
				return;

			//////   if(luaState != LuaCore.lua_State.Zero)
			if(!luaState.IsNull())
				LuaCore.lua_close(luaState);
			//luaState = LuaCore.lua_State.Zero; <- suggested by Christopher Cebulski http://luaforge.net/forum/forum.php?thread_id = 44593&forum_id = 146
		}

		static int PanicCallback(LuaCore.lua_State luaState)
		{
			// string desc = LuaCore.lua_tostring(luaState, 1);
			string reason = string.Format("unprotected error in call to Lua API ({0})", LuaCore.lua_tostring(luaState, -1));
			//		lua_tostring(L, -1);
			throw new LuaException(reason);
		}

		/// <summary>
		/// Assuming we have a Lua error string sitting on the stack, throw a C# exception out to the user's app
		/// </summary>
		/// <exception cref = "LuaScriptException">Thrown if the script caused an exception</exception>
		private void ThrowExceptionFromError(int oldTop)
		{
			object err = translator.getObject(luaState, -1);
			LuaCore.lua_settop(luaState, oldTop);

			// A pre-wrapped exception - just rethrow it (stack trace of InnerException will be preserved)
			var luaEx = err as LuaScriptException;

			if(!luaEx.IsNull())
				throw luaEx;

			// A non-wrapped Lua error (best interpreted as a string) - wrap it and throw it
			if(err.IsNull())
				err = "Unknown Lua Error";

			throw new LuaScriptException(err.ToString(), string.Empty);
		}

		/// <summary>
		/// Convert C# exceptions into Lua errors
		/// </summary>
		/// <returns>num of things on stack</returns>
		/// <param name = "e">null for no pending exception</param>
		internal int SetPendingException(Exception e)
		{
			var caughtExcept = e;

			if(!caughtExcept.IsNull())
			{
				translator.throwError(luaState, caughtExcept);
				LuaCore.lua_pushnil(luaState);
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
			int oldTop = LuaCore.lua_gettop(luaState);
			executing = true;

			try
			{
				if(LuaLib.luaL_loadbuffer(luaState, chunk, name) != 0)
					ThrowExceptionFromError(oldTop);
			}
			finally
			{
				executing = false;
			}

			var result = translator.getFunction(luaState, -1);
			translator.popValues(luaState, oldTop);
			return result;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name = "fileName"></param>
		/// <returns></returns>
		public LuaFunction LoadFile(string fileName)
		{
			int oldTop = LuaCore.lua_gettop(luaState);

			if(LuaCore.luaL_loadfile(luaState, fileName) != 0)
				ThrowExceptionFromError(oldTop);

			var result = translator.getFunction(luaState, -1);
			translator.popValues(luaState, oldTop);
			return result;
		}

		/*
			* Excutes a Lua chunk and returns all the chunk's return
			* values in an array
			*/
		public object[] DoString(string chunk) 
		{
			int oldTop = LuaCore.lua_gettop(luaState);

			if(LuaLib.luaL_loadbuffer(luaState, chunk, "chunk") == 0)
			{
				executing = true;

				try
				{
					if(LuaCore.lua_pcall(luaState, 0, -1, 0) == 0)
						return translator.popValues(luaState, oldTop);
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
		/// Executes a Lua chnk and returns all the chunk's return values in an array.
		/// </summary>
		/// <param name = "chunk">Chunk to execute</param>
		/// <param name = "chunkName">Name to associate with the chunk</param>
		/// <returns></returns>
		public object[] DoString(string chunk, string chunkName)
		{
			int oldTop = LuaCore.lua_gettop(luaState);
			executing = true;

			if(LuaLib.luaL_loadbuffer(luaState, chunk, chunkName) == 0)
			{
				try
				{
					if(LuaCore.lua_pcall(luaState, 0, -1, 0) == 0)
						return translator.popValues(luaState, oldTop);
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
		public object[] DoFile(string fileName) 
		{
			int oldTop = LuaCore.lua_gettop(luaState);

			if(LuaCore.luaL_loadfile(luaState, fileName) == 0) 
			{
				executing = true;

				try
				{
					if(LuaCore.lua_pcall(luaState, 0, -1, 0) == 0)
						return translator.popValues(luaState, oldTop);
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
			* Indexer for global variables from the LuaInterpreter
			* Supports navigation of tables by using . operator
			*/
		public object this[string fullPath]
		{
			get 
			{
				object returnValue = null;
				int oldTop = LuaCore.lua_gettop(luaState);
				string[] path = fullPath.Split(new char[] { '.' });
				LuaCore.lua_getglobal(luaState, path[0]);
				returnValue = translator.getObject(luaState, -1);

				if(path.Length>1) 
				{
					string[] remainingPath = new string[path.Length-1];
					Array.Copy(path, 1, remainingPath, 0, path.Length-1);
					returnValue = getObject(remainingPath);
				}

				LuaCore.lua_settop(luaState, oldTop);
				return returnValue;
			}
			set 
			{
				int oldTop = LuaCore.lua_gettop(luaState);
				string[] path = fullPath.Split(new char[] { '.' });

				if(path.Length == 1) 
				{
					translator.push(luaState, value);
					LuaCore.lua_setglobal(luaState, fullPath);
				} 
				else 
				{
					LuaCore.lua_getglobal(luaState, path[0]);
					string[] remainingPath = new string[path.Length-1];
					Array.Copy(path, 1, remainingPath, 0, path.Length-1);
					setObject(remainingPath, value);
				}

				LuaCore.lua_settop(luaState, oldTop);

				// Globals auto-complete
				if(value.IsNull())
				{
					// Remove now obsolete entries
					globals.Remove(fullPath);
				}
				else
				{
					// Add new entries
					if(!globals.Contains(fullPath))
						registerGlobal(fullPath, value.GetType(), 0);
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
		private void registerGlobal(string path, Type type, int recursionCounter)
		{
			// If the type is a global method, list it directly
			if(type == typeof(LuaCore.lua_CFunction))
			{
				// Format for easy method invocation
				globals.Add(path + "(");
			}
			// If the type is a class or an interface and recursion hasn't been running too long, list the members
			else if((type.IsClass || type.IsInterface) && type != typeof(string) && recursionCounter < 2)
			{
				#region Methods
				foreach(var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
				{
					if(
						// Check that the LuaHideAttribute and LuaGlobalAttribute were not applied
						(method.GetCustomAttributes(typeof(LuaHideAttribute), false).Length == 0) &&
						(method.GetCustomAttributes(typeof(LuaGlobalAttribute), false).Length == 0) &&
						// Exclude some generic .NET methods that wouldn't be very usefull in Lua
						method.Name != "GetType" && method.Name != "GetHashCode" && method.Name != "Equals" &&
						method.Name != "ToString" && method.Name != "Clone" && method.Name != "Dispose" &&
						method.Name != "GetEnumerator" && method.Name != "CopyTo" &&
						!method.Name.StartsWith("get_", StringComparison.Ordinal) &&
						!method.Name.StartsWith("set_", StringComparison.Ordinal) &&
						!method.Name.StartsWith("add_", StringComparison.Ordinal) &&
						!method.Name.StartsWith("remove_", StringComparison.Ordinal))
					{
						// Format for easy method invocation
						string command = path + ":" + method.Name + "(";

						if(method.GetParameters().Length == 0) command += ")";
							globals.Add(command);
					}
				}
				#endregion

				#region Fields
				foreach(var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
				{
					if(
						// Check that the LuaHideAttribute and LuaGlobalAttribute were not applied
						(field.GetCustomAttributes(typeof(LuaHideAttribute), false).Length == 0) &&
						(field.GetCustomAttributes(typeof(LuaGlobalAttribute), false).Length == 0))
					{
						// Go into recursion for members
						registerGlobal(path + "." + field.Name, field.FieldType, recursionCounter + 1);
					}
				}
				#endregion

				#region Properties
				foreach(var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
				{
					if(
						// Check that the LuaHideAttribute and LuaGlobalAttribute were not applied
						(property.GetCustomAttributes(typeof(LuaHideAttribute), false).Length == 0) &&
						(property.GetCustomAttributes(typeof(LuaGlobalAttribute), false).Length == 0)
						// Exclude some generic .NET properties that wouldn't be very usefull in Lua
						&& property.Name != "Item")
					{
						// Go into recursion for members
						registerGlobal(path + "." + property.Name, property.PropertyType, recursionCounter + 1);
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
		internal object getObject(string[] remainingPath) 
		{
			object returnValue = null;

			for(int i = 0; i < remainingPath.Length; i++) 
			{
				LuaCore.lua_pushstring(luaState, remainingPath[i]);
				LuaCore.lua_gettable(luaState, -2);
				returnValue = translator.getObject(luaState, -1);

				if(returnValue.IsNull())
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
			return (string)this[fullPath];
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
			return (obj is LuaCore.lua_CFunction ? new LuaFunction((LuaCore.lua_CFunction)obj, this) : (LuaFunction)obj);
			//return (obj is LuaCore.lua_CFunction ? new LuaFunction((LuaCore.lua_CFunction)obj, this) : /*(LuaFunction)*/new LuaFunction(obj.GetHashCode(), this));
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
		internal object[] callFunction(object function, object[] args) 
		{
			return callFunction(function, args, null);
		}

		/*
			* Calls the object as a function with the provided arguments and
			* casting returned values to the types in returnTypes before returning
			* them in an array
			*/
		internal object[] callFunction(object function, object[] args, Type[] returnTypes) 
		{
			int nArgs = 0;
			int oldTop = LuaCore.lua_gettop(luaState);

			if(!LuaCore.lua_checkstack(luaState, args.Length+6).ToBoolean())
				throw new LuaException("Lua stack overflow");

			translator.push(luaState, function);

			if(!args.IsNull()) 
			{
				nArgs = args.Length;

				for(int i = 0; i < args.Length; i++) 
					translator.push(luaState, args[i]);
			}

			executing = true;

			try
			{
				int error = LuaCore.lua_pcall(luaState, nArgs, -1, 0);
				if(error != 0)
					ThrowExceptionFromError(oldTop);
			}
			finally
			{
				executing = false;
			}

			return !returnTypes.IsNull() ? translator.popValues(luaState, oldTop, returnTypes) : translator.popValues(luaState, oldTop);
		}

		/*
			* Navigates a table to set the value of one of its fields
			*/
		internal void setObject(string[] remainingPath, object val) 
		{
			for(int i = 0; i < remainingPath.Length-1; i++) 
			{
				LuaCore.lua_pushstring(luaState, remainingPath[i]);
				LuaCore.lua_gettable(luaState, -2);
			}

			LuaCore.lua_pushstring(luaState, remainingPath[remainingPath.Length-1]);
			translator.push(luaState, val);
			LuaCore.lua_settable(luaState, -3);
		}

		/*
			* Creates a new table as a global variable or as a field
			* inside an existing table
			*/
		public void NewTable(string fullPath) 
		{
			string[] path = fullPath.Split(new char[] { '.' });
			int oldTop = LuaCore.lua_gettop(luaState);

			if(path.Length == 1) 
			{
				LuaCore.lua_newtable(luaState);
				LuaCore.lua_setglobal(luaState, fullPath);
			} 
			else 
			{
				LuaCore.lua_getglobal(luaState, path[0]);

				for(int i = 1; i < path.Length-1; i++) 
				{
					LuaCore.lua_pushstring(luaState, path[i]);
					LuaCore.lua_gettable(luaState, -2);
				}

				LuaCore.lua_pushstring(luaState, path[path.Length-1]);
				LuaCore.lua_newtable(luaState);
				LuaCore.lua_settable(luaState, -3);
			}

			LuaCore.lua_settop(luaState, oldTop);
		}

		public ListDictionary GetTableDict(LuaTable table)
		{
			var dict = new ListDictionary();
			int oldTop = LuaCore.lua_gettop(luaState);
			translator.push(luaState, table);
			LuaCore.lua_pushnil(luaState);

			while(LuaCore.lua_next(luaState, -2) != 0) 
			{
				dict[translator.getObject(luaState, -2)] = translator.getObject(luaState, -1);
				LuaCore.lua_settop(luaState, -2);
			}

			LuaCore.lua_settop(luaState, oldTop);
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
		/// <author>Reinhard Ostermeier</author>
		/*public int SetDebugHook(EventMasks mask, int count)
		{
			if(hookCallback.IsNull())
			{
			hookCallback = new LuaCore.lua_Hook(DebugHookCallback);
			return LuaCore.lua_sethook(luaState, hookCallback, (int)mask, count);
			}
			return -1;
		}*/

		/// <summary>
		/// Removes the debug hook
		/// </summary>
		/// <returns>see lua docs</returns>
		/// <author>Reinhard Ostermeier</author>
		public int RemoveDebugHook()
		{
			hookCallback = null;
			return LuaCore.lua_sethook(luaState, null, 0, 0);
		}

		/// <summary>
		/// Gets the hook mask.
		/// </summary>
		/// <returns>hook mask</returns>
		/// <author>Reinhard Ostermeier</author>
		public EventMasks GetHookMask()
		{
			return (EventMasks)LuaCore.lua_gethookmask(luaState);
		}

		/// <summary>
		/// Gets the hook count
		/// </summary>
		/// <returns>see lua docs</returns>
		/// <author>Reinhard Ostermeier</author>
		public int GetHookCount()
		{
			return LuaCore.lua_gethookcount(luaState);
		}

		/// <summary>
		/// Gets the stack entry on a given level
		/// </summary>
		/// <param name = "level">level</param>
		/// <param name = "luaDebug">lua debug structure</param>
		/// <returns>Returns true if level was allowed, false if level was invalid.</returns>
		/// <author>Reinhard Ostermeier</author>
		/*public bool GetStack(int level, out LuaDebug luaDebug)
		{
			luaDebug = new LuaDebug();
			LuaCore.lua_State ld = System.Runtime.InteropServices.Marshal.AllocHGlobal(System.Runtime.InteropServices.Marshal.SizeOf(luaDebug));
			System.Runtime.InteropServices.Marshal.StructureToPtr(luaDebug, ld, false);
			try
			{
			return LuaCore.lua_getstack(luaState, level, ld) != 0;
			}
			finally
			{
			luaDebug = (LuaDebug)System.Runtime.InteropServices.Marshal.PtrToStructure(ld, typeof(LuaDebug));
			System.Runtime.InteropServices.Marshal.FreeHGlobal(ld);
			}
		}*/

		/// <summary>
		/// Gets info (see lua docs)
		/// </summary>
		/// <param name = "what">what (see lua docs)</param>
		/// <param name = "luaDebug">lua debug structure</param>
		/// <returns>see lua docs</returns>
		/// <author>Reinhard Ostermeier</author>
		/*public int GetInfo(String what, ref LuaDebug luaDebug)
		{
			LuaCore.lua_State ld = System.Runtime.InteropServices.Marshal.AllocHGlobal(System.Runtime.InteropServices.Marshal.SizeOf(luaDebug));
			System.Runtime.InteropServices.Marshal.StructureToPtr(luaDebug, ld, false);
			try
			{
			return LuaCore.lua_getinfo(luaState, what, ld);
			}
			finally
			{
			luaDebug = (LuaDebug)System.Runtime.InteropServices.Marshal.PtrToStructure(ld, typeof(LuaDebug));
			System.Runtime.InteropServices.Marshal.FreeHGlobal(ld);
			}
		}*/

		/// <summary>
		/// Gets local (see lua docs)
		/// </summary>
		/// <param name = "luaDebug">lua debug structure</param>
		/// <param name = "n">see lua docs</param>
		/// <returns>see lua docs</returns>
		/// <author>Reinhard Ostermeier</author>
		/*public String GetLocal(LuaDebug luaDebug, int n)
		{
			LuaCore.lua_State ld = System.Runtime.InteropServices.Marshal.AllocHGlobal(System.Runtime.InteropServices.Marshal.SizeOf(luaDebug));
			System.Runtime.InteropServices.Marshal.StructureToPtr(luaDebug, ld, false);
			try
			{
			return LuaCore.lua_getlocal(luaState, ld, n);
			}
			finally
			{
			System.Runtime.InteropServices.Marshal.FreeHGlobal(ld);
			}
		}*/

		/// <summary>
		/// Sets local (see lua docs)
		/// </summary>
		/// <param name = "luaDebug">lua debug structure</param>
		/// <param name = "n">see lua docs</param>
		/// <returns>see lua docs</returns>
		/// <author>Reinhard Ostermeier</author>
		/*public String SetLocal(LuaDebug luaDebug, int n)
		{
			LuaCore.lua_State ld = System.Runtime.InteropServices.Marshal.AllocHGlobal(System.Runtime.InteropServices.Marshal.SizeOf(luaDebug));
			System.Runtime.InteropServices.Marshal.StructureToPtr(luaDebug, ld, false);
			try
			{
			return LuaCore.lua_setlocal(luaState, ld, n);
			}
			finally
			{
			System.Runtime.InteropServices.Marshal.FreeHGlobal(ld);
			}
		}*/

		/// <summary>
		/// Gets up value (see lua docs)
		/// </summary>
		/// <param name = "funcindex">see lua docs</param>
		/// <param name = "n">see lua docs</param>
		/// <returns>see lua docs</returns>
		/// <author>Reinhard Ostermeier</author>
		public string GetUpValue(int funcindex, int n)
		{
			return LuaCore.lua_getupvalue(luaState, funcindex, n).ToString();
		}

		/// <summary>
		/// Sets up value (see lua docs)
		/// </summary>
		/// <param name = "funcindex">see lua docs</param>
		/// <param name = "n">see lua docs</param>
		/// <returns>see lua docs</returns>
		/// <author>Reinhard Ostermeier</author>
		public string SetUpValue(int funcindex, int n)
		{
			return LuaCore.lua_setupvalue(luaState, funcindex, n).ToString();
		}

		/// <summary>
		/// Delegate that is called on lua hook callback
		/// </summary>
		/// <param name = "luaState">lua state</param>
		/// <param name = "luaDebug">Pointer to LuaDebug (lua_debug) structure</param>
		/// <author>Reinhard Ostermeier</author>
		/*private void DebugHookCallback(LuaCore.lua_State luaState, LuaCore.lua_Debug luaDebug)
		{
			try
			{
			LuaDebug ld = (LuaDebug)System.Runtime.InteropServices.Marshal.PtrToStructure(luaDebug, typeof(LuaDebug));
			EventHandler<DebugHookEventArgs> temp = DebugHook;
			if(temp != null)
			{
					temp(this, new DebugHookEventArgs(ld));
			}
			}
			catch (Exception ex)
			{
			OnHookException(new HookExceptionEventArgs(ex));
			}
		}*/

		private void OnHookException(HookExceptionEventArgs e)
		{
			var temp = HookException;
			if(!temp.IsNull())
				temp(this, e);
		}

		/// <summary>
		/// Pops a value from the lua stack.
		/// </summary>
		/// <returns>Returns the top value from the lua stack.</returns>
		/// <author>Reinhard Ostermeier</author>
		public object Pop()
		{
			int top = LuaCore.lua_gettop(luaState);
			return translator.popValues(luaState, top - 1)[0];
		}

		/// <summary>
		/// Pushes a value onto the lua stack.
		/// </summary>
		/// <param name = "value">Value to push.</param>
		/// <author>Reinhard Ostermeier</author>
		public void Push(object value)
		{
			translator.push(luaState, value);
		}
		#endregion

		internal void dispose(int reference) 
		{
			///////////// if(luaState != LuaCore.lua_State.Zero)
			if(!luaState.IsNull()) //Fix submitted by Qingrui Li
				LuaLib.lua_unref(luaState, reference);
		}

		/*
		 * Gets a field of the table corresponding to the provided reference
		 * using rawget (do not use metatables)
		 */
		internal object rawGetObject(int reference, string field) 
		{
			int oldTop = LuaCore.lua_gettop(luaState);
			LuaLib.lua_getref(luaState, reference);
			LuaCore.lua_pushstring(luaState, field);
			LuaCore.lua_rawget(luaState, -2);
			object obj = translator.getObject(luaState, -1);
			LuaCore.lua_settop(luaState, oldTop);
			return obj;
		}

		/*
		 * Gets a field of the table or userdata corresponding to the provided reference
		 */
		internal object getObject(int reference, string field) 
		{
			int oldTop = LuaCore.lua_gettop(luaState);
			LuaLib.lua_getref(luaState, reference);
			object returnValue = getObject(field.Split(new char[] {'.'}));
			LuaCore.lua_settop(luaState, oldTop);
			return returnValue;
		}

		/*
		 * Gets a numeric field of the table or userdata corresponding the the provided reference
		 */

		internal object getObject(int reference, object field) 
		{
			int oldTop = LuaCore.lua_gettop(luaState);
			LuaLib.lua_getref(luaState, reference);
			translator.push(luaState, field);
			LuaCore.lua_gettable(luaState, -2);
			object returnValue = translator.getObject(luaState, -1);
			LuaCore.lua_settop(luaState, oldTop);
			return returnValue;
		}

		/*
		 * Sets a field of the table or userdata corresponding the the provided reference
		 * to the provided value
		 */
		internal void setObject(int reference, string field, object val) 
		{
			int oldTop = LuaCore.lua_gettop(luaState);
			LuaLib.lua_getref(luaState, reference);
			setObject(field.Split(new char[] {'.'}), val);
			LuaCore.lua_settop(luaState, oldTop);
		}

		/*
		 * Sets a numeric field of the table or userdata corresponding the the provided reference
		 * to the provided value
		 */
		internal void setObject(int reference, object field, object val) 
		{
			int oldTop = LuaCore.lua_gettop(luaState);
			LuaLib.lua_getref(luaState, reference);
			translator.push(luaState, field);
			translator.push(luaState, val);
			LuaCore.lua_settable(luaState, -3);
			LuaCore.lua_settop(luaState, oldTop);
		}

		/*
		 * Registers an object's method as a Lua function (global or table field)
		 * The method may have any signature
		 */
		public LuaFunction RegisterFunction(string path, object target, MethodBase function /*MethodInfo function*/)  //CP: Fix for struct constructor by Alexander Kappner (link: http://luaforge.net/forum/forum.php?thread_id = 2859&forum_id = 145)
		{
			// We leave nothing on the stack when we are done
			int oldTop = LuaCore.lua_gettop(luaState);
			var wrapper = new LuaMethodWrapper(translator, target, function.DeclaringType, function);
			translator.push(luaState, new LuaCore.lua_CFunction(wrapper.call));
			this[path] = translator.getObject(luaState, -1);
			var f = GetFunction(path);
			LuaCore.lua_settop(luaState, oldTop);
			return f;
		}

		/*
		 * Compares the two values referenced by ref1 and ref2 for equality
		 */
		internal bool compareRef(int ref1, int ref2) 
		{
			int top = LuaCore.lua_gettop(luaState);
			LuaLib.lua_getref(luaState, ref1);
			LuaLib.lua_getref(luaState, ref2);
			int equal = LuaCore.lua_equal(luaState, -1, -2);
			LuaCore.lua_settop(luaState, top);
			return (equal != 0);
		}

		internal void pushCSFunction(LuaCore.lua_CFunction function)
		{
			translator.pushFunction(luaState, function);
		}

		#region IDisposable Members
		public virtual void Dispose()
		{
			if(!translator.IsNull())
			{
				translator.pendingEvents.Dispose();
				translator = null;
			}

			this.Close();
			GC.Collect();
			GC.WaitForPendingFinalizers();
		}
		#endregion
   }
}