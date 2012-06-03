
namespace LuaInterface
{
	using System;
	using System.IO;
	using System.Collections;
	using System.Collections.Specialized;
	using System.Reflection;
	using System.Threading;
	using LuaWrap;

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
	public class Lua : IDisposable
	{
		static string init_luanet =
			"local metatable = {}									\n"+
			"local import_type = luanet.import_type							\n"+
			"local load_assembly = luanet.load_assembly						\n"+
			"											\n"+
			"-- Lookup a .NET identifier component.							\n"+
			"function metatable:__index(key) -- key is e.g. \"Form\"				\n"+
			"    -- Get the fully-qualified name, e.g. \"System.Windows.Forms.Form\"		\n"+
			"    local fqn = ((rawget(self,\".fqn\") and rawget(self,\".fqn\") ..			\n"+
			"		\".\") or \"\") .. key							\n"+
			"											\n"+
			"    -- Try to find either a luanet function or a CLR type				\n"+
			"    local obj = rawget(luanet,key) or import_type(fqn)					\n"+
			"											\n"+
			"    -- If key is neither a luanet function or a CLR type, then it is simply		\n"+
			"    -- an identifier component.							\n"+
			"    if obj == nil then									\n"+
			"		-- It might be an assembly, so we load it too.				\n"+
			"        load_assembly(fqn)								\n"+
			"        obj = { [\".fqn\"] = fqn }							\n"+
			"        setmetatable(obj, metatable)							\n"+
			"    end										\n"+
			"											\n"+
			"    -- Cache this lookup								\n"+
			"    rawset(self, key, obj)								\n"+
			"    return obj										\n"+
			"end											\n"+
			"											\n"+
			"-- A non-type has been called; e.g. foo = System.Foo()					\n"+
			"function metatable:__call(...)								\n"+
			"    error(\"No such type: \" .. rawget(self,\".fqn\"), 2)				\n"+
			"end											\n"+
			"											\n"+
			"-- This is the root of the .NET namespace						\n"+
			"luanet[\".fqn\"] = false								\n"+
			"setmetatable(luanet, metatable)							\n"+
			"											\n"+
			"-- Preload the mscorlib assembly							\n"+
			"luanet.load_assembly(\"mscorlib\")							\n";

		readonly KopiLua.Lua.lua_State luaState;
		ObjectTranslator translator;

		KopiLua.Lua.lua_CFunction panicCallback/*, lockCallback, unlockCallback*/;

		/// <summary>
		/// Used to ensure multiple .net threads all get serialized by this single lock for access to the lua stack/objects
		/// </summary>
		object luaLock = new object();

		public Lua() 
		{
			luaState = KopiLua.Lua.luaL_newstate();	// steffenj: Lua 5.1.1 API change (lua_open is gone)
			//KopiLua.Lua.luaopen_base(luaState);	// steffenj: luaopen_* no longer used
			KopiLua.Lua.luaL_openlibs(luaState);		// steffenj: Lua 5.1.1 API change (luaopen_base is gone, just open all libs right here)
			KopiLua.Lua.lua_pushstring(luaState, "LUAINTERFACE LOADED");
			KopiLua.Lua.lua_pushboolean(luaState, 1);
			KopiLua.Lua.lua_settable(luaState, (int) PseudoIndex.Registry);
			KopiLua.Lua.lua_newtable(luaState);
			KopiLua.Lua.lua_setglobal(luaState, "luanet");
			KopiLua.Lua.lua_pushvalue(luaState, (int)PseudoIndex.Globals);
			KopiLua.Lua.lua_getglobal(luaState, "luanet");
			KopiLua.Lua.lua_pushstring(luaState, "getmetatable");
			KopiLua.Lua.lua_getglobal(luaState, "getmetatable");
			KopiLua.Lua.lua_settable(luaState, -3);
			KopiLua.Lua.lua_replace(luaState, (int)PseudoIndex.Globals);
			translator=new ObjectTranslator(this,luaState);
			KopiLua.Lua.lua_replace(luaState, (int)PseudoIndex.Globals);
			LuaLib.luaL_dostring(luaState, Lua.init_luanet);	// steffenj: lua_dostring renamed to luaL_dostring

			// We need to keep this in a managed reference so the delegate doesn't get garbage collected
			panicCallback = new KopiLua.Lua.lua_CFunction(PanicCallback);
			KopiLua.Lua.lua_atpanic(luaState, panicCallback);

			// KopiLua.Lua.lua_atlock(luaState, lockCallback = new CallbackFunction(LockCallback));
			// KopiLua.Lua.lua_atunlock(luaState, unlockCallback = new CallbackFunction(UnlockCallback));
		}

		/*
		 * CAUTION: LuaInterface.Lua instances can't share the same lua state! 
		 */
		public Lua(KopiLua.Lua.lua_State luaState)
		{
				//IntPtr lState = new IntPtr(luaState);
				KopiLua.Lua.lua_pushstring(luaState, "LUAINTERFACE LOADED");
				KopiLua.Lua.lua_gettable(luaState, (int)PseudoIndex.Registry);
				if(KopiLua.Lua.lua_toboolean(luaState,-1).ToBoolean()) {
					KopiLua.Lua.lua_settop(luaState,-2);
					throw new LuaException("There is already a LuaInterface.Lua instance associated with this Lua state");
				} else {
					KopiLua.Lua.lua_settop(luaState,-2);
					KopiLua.Lua.lua_pushstring(luaState, "LUAINTERFACE LOADED");
					KopiLua.Lua.lua_pushboolean(luaState, 1);
					KopiLua.Lua.lua_settable(luaState, (int)PseudoIndex.Registry);
					this.luaState=luaState;
					KopiLua.Lua.lua_pushvalue(luaState, (int)PseudoIndex.Globals);
					KopiLua.Lua.lua_getglobal(luaState, "luanet");
					KopiLua.Lua.lua_pushstring(luaState, "getmetatable");
					KopiLua.Lua.lua_getglobal(luaState, "getmetatable");
					KopiLua.Lua.lua_settable(luaState, -3);
					KopiLua.Lua.lua_replace(luaState, (int)PseudoIndex.Globals);
					translator=new ObjectTranslator(this, this.luaState);
					KopiLua.Lua.lua_replace(luaState, (int)PseudoIndex.Globals);
					LuaLib.luaL_dostring(luaState, Lua.init_luanet);	// steffenj: lua_dostring renamed to luaL_dostring
				}
		}

		/// <summary>
		/// Called for each lua_lock call 
		/// </summary>
		/// <param name="luaState"></param>
		/// Not yet used
		int LockCallback(KopiLua.Lua.lua_State luaState)
		{
			// Monitor.Enter(luaLock);

			return 0;
		}

		/// <summary>
		/// Called for each lua_unlock call 
		/// </summary>
		/// <param name="luaState"></param>
		/// Not yet used
		int UnlockCallback(KopiLua.Lua.lua_State luaState)
		{
			// Monitor.Exit(luaLock);

			return 0;
		}

		static int PanicCallback(KopiLua.Lua.lua_State luaState)
		{
			// string desc = KopiLua.Lua.lua_tostring(luaState, 1);
			string reason = String.Format("unprotected error in call to Lua API ({0})", KopiLua.Lua.lua_tostring(luaState, -1));

		   //		lua_tostring(L, -1);

			throw new LuaException(reason);
		}



		/// <summary>
		/// Assuming we have a Lua error string sitting on the stack, throw a C# exception out to the user's app
		/// </summary>
		void ThrowExceptionFromError(int oldTop)
		{
			object err = translator.getObject(luaState, -1);
			KopiLua.Lua.lua_settop(luaState, oldTop);

			// If the 'error' on the stack is an actual C# exception, just rethrow it.  Otherwise the value must have started
			// as a true Lua error and is best interpreted as a string - wrap it in a LuaException and rethrow.
			Exception thrown = err as Exception;

			if (thrown == null)
			{
				if (err == null)
					err = "Unknown Lua Error";

				thrown = new LuaException(err.ToString());
			}

			throw thrown;
		}



		/// <summary>
		/// Convert C# exceptions into Lua errors
		/// </summary>
		/// <returns>num of things on stack</returns>
		/// <param name="e">null for no pending exception</param>
		internal int SetPendingException(Exception e)
		{
			Exception caughtExcept = e;

			if (caughtExcept != null)
			{
				translator.throwError(luaState, caughtExcept);
				KopiLua.Lua.lua_pushnil(luaState);

				return 1;
			}
			else
				return 0;
		}


		/*
		 * Excutes a Lua chunk and returns all the chunk's return
		 * values in an array
		 */
		public object[] DoString(string chunk) 
		{
			int oldTop=KopiLua.Lua.lua_gettop(luaState);
			if(LuaLib.luaL_loadbuffer(luaState,chunk,"chunk")== LuaEnum.Ok) 
			{
				if (KopiLua.Lua.lua_pcall(luaState, 0, -1, 0) == 0)
					return translator.popValues(luaState, oldTop);
				else
					ThrowExceptionFromError(oldTop);
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
			int oldTop=KopiLua.Lua.lua_gettop(luaState);
			//Console.WriteLine("aaa: {0}",oldTop);
			if(KopiLua.Lua.luaL_loadfile(luaState,fileName)==0) 
			{
				//Console.WriteLine("aaa2: {0}",KopiLua.Lua.lua_pcall(luaState, 0, -1, 0));
				//Console.WriteLine("zzzzzzzzzzzzzzzzzz");
				if (KopiLua.Lua.lua_pcall(luaState, 0, -1, 0) == 0)
					return translator.popValues(luaState, oldTop);
				else
					ThrowExceptionFromError(oldTop);
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
				object returnValue=null;
				int oldTop=KopiLua.Lua.lua_gettop(luaState);
				string[] path=fullPath.Split(new char[] { '.' });
				KopiLua.Lua.lua_getglobal(luaState,path[0]);
				returnValue=translator.getObject(luaState,-1);
				if(path.Length>1) 
				{
					string[] remainingPath=new string[path.Length-1];
					Array.Copy(path,1,remainingPath,0,path.Length-1);
					returnValue=getObject(remainingPath);
				}
				KopiLua.Lua.lua_settop(luaState,oldTop);
				//Console.WriteLine("get: {0}", returnValue);
				return returnValue;
			}
			set 
			{
				int oldTop=KopiLua.Lua.lua_gettop(luaState);
				string[] path=fullPath.Split(new char[] { '.' });
				//Console.WriteLine("set: {0}", path.Length);
				if(path.Length==1) 
				{
					translator.push(luaState,value);
					KopiLua.Lua.lua_setglobal(luaState,fullPath);
				} 
				else 
				{
					KopiLua.Lua.lua_getglobal(luaState,path[0]);
					string[] remainingPath=new string[path.Length-1];
					Array.Copy(path,1,remainingPath,0,path.Length-1);
					setObject(remainingPath,value);
				}
				KopiLua.Lua.lua_settop(luaState,oldTop);
			}
		}
		/*
		 * Navigates a table in the top of the stack, returning
		 * the value of the specified field
		 */
		internal object getObject(string[] remainingPath) 
		{
			object returnValue=null;
			for(int i=0;i<remainingPath.Length;i++) 
			{
				KopiLua.Lua.lua_pushstring(luaState,remainingPath[i]);
				KopiLua.Lua.lua_gettable(luaState,-2);
				returnValue=translator.getObject(luaState,-1);
				if(returnValue==null) break;	
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
			return CodeGeneration.Instance.GetClassInstance(interfaceType,GetTable(fullPath));
		}
		/*
		 * Gets a function global variable
		 */
		public LuaFunction GetFunction(string fullPath) 
		{
			object obj=this[fullPath];
			return (obj is KopiLua.Lua.lua_CFunction ? new LuaFunction((KopiLua.Lua.lua_CFunction)obj,this) : (LuaFunction)obj);
		}
		/*
		 * Gets a function global variable as a delegate of
		 * type delegateType
		 */
		public Delegate GetFunction(Type delegateType,string fullPath) 
		{
			return CodeGeneration.Instance.GetDelegate(delegateType,GetFunction(fullPath));
		}
		/*
		 * Calls the object as a function with the provided arguments,
		 * returning the function's returned values inside an array
		 */
		internal object[] callFunction(object function,object[] args) 
		{
			return callFunction(function, args, null);
		}


		/*
		 * Calls the object as a function with the provided arguments and
		 * casting returned values to the types in returnTypes before returning
		 * them in an array
		 */
		internal object[] callFunction(object function,object[] args,Type[] returnTypes) 
		{
			int nArgs=0;
			int oldTop=KopiLua.Lua.lua_gettop(luaState);
			if(!KopiLua.Lua.lua_checkstack(luaState,args.Length+6).ToBoolean())
				throw new LuaException("Lua stack overflow");
			translator.push(luaState,function);
			if(args!=null) 
			{
				nArgs=args.Length;
				for(int i=0;i<args.Length;i++) 
				{
					translator.push(luaState,args[i]);
				}
			}
			LuaEnum error = KopiLua.Lua.lua_pcall(luaState, nArgs, -1, 0).ToLuaEnum();
			if (error != LuaEnum.Ok)
				ThrowExceptionFromError(oldTop);

			if(returnTypes != null)
				return translator.popValues(luaState,oldTop,returnTypes);
			else
				return translator.popValues(luaState, oldTop);
		}
		/*
		 * Navigates a table to set the value of one of its fields
		 */
		internal void setObject(string[] remainingPath, object val) 
		{
			for(int i=0; i<remainingPath.Length-1;i++) 
			{
				KopiLua.Lua.lua_pushstring(luaState,remainingPath[i]);
				KopiLua.Lua.lua_gettable(luaState,-2);
			}
			KopiLua.Lua.lua_pushstring(luaState,remainingPath[remainingPath.Length-1]);
			translator.push(luaState,val);
			KopiLua.Lua.lua_settable(luaState,-3);
		}
		/*
		 * Creates a new table as a global variable or as a field
		 * inside an existing table
		 */
		public void NewTable(string fullPath) 
		{
			string[] path=fullPath.Split(new char[] { '.' });
			int oldTop=KopiLua.Lua.lua_gettop(luaState);
			if(path.Length==1) 
			{
				KopiLua.Lua.lua_newtable(luaState);
				KopiLua.Lua.lua_setglobal(luaState,fullPath);
			} 
			else 
			{
				KopiLua.Lua.lua_getglobal(luaState,path[0]);
				for(int i=1; i<path.Length-1;i++) 
				{
					KopiLua.Lua.lua_pushstring(luaState,path[i]);
					KopiLua.Lua.lua_gettable(luaState,-2);
				}
				KopiLua.Lua.lua_pushstring(luaState,path[path.Length-1]);
				KopiLua.Lua.lua_newtable(luaState);
				KopiLua.Lua.lua_settable(luaState,-3);
			}
			KopiLua.Lua.lua_settop(luaState,oldTop);
		}

		public ListDictionary GetTableDict(LuaTable table)
		{
			ListDictionary dict = new ListDictionary();

			int oldTop = KopiLua.Lua.lua_gettop(luaState);
			translator.push(luaState, table);
			KopiLua.Lua.lua_pushnil(luaState);
			// nem biztos hogy jóóóó
			//while (KopiLua.Lua.lua_next(luaState, -2) != 0) 
			while (!KopiLua.Lua.lua_next(luaState, -2).ToBoolean())
			{
				dict[translator.getObject(luaState, -2)] = translator.getObject(luaState, -1);
				KopiLua.Lua.lua_settop(luaState, -2);
			}
			KopiLua.Lua.lua_settop(luaState, oldTop);

			return dict;
		}

		/*
		 * Lets go of a previously allocated reference to a table, function
		 * or userdata
		 */
		internal void dispose(int reference) 
		{
			LuaLib.lua_unref(luaState,reference);
		}
		/*
		 * Gets a field of the table corresponding to the provided reference
		 * using rawget (do not use metatables)
		 */
		internal object rawGetObject(int reference,string field) 
		{
			int oldTop=KopiLua.Lua.lua_gettop(luaState);
			LuaLib.lua_getref(luaState,reference);
			KopiLua.Lua.lua_pushstring(luaState,field);
			KopiLua.Lua.lua_rawget(luaState,-2);
			object obj=translator.getObject(luaState,-1);
			KopiLua.Lua.lua_settop(luaState,oldTop);
			return obj;
		}
		/*
		 * Gets a field of the table or userdata corresponding to the provided reference
		 */
		internal object getObject(int reference,string field) 
		{
			int oldTop=KopiLua.Lua.lua_gettop(luaState);
			LuaLib.lua_getref(luaState,reference);
			object returnValue=getObject(field.Split(new char[] {'.'}));
			KopiLua.Lua.lua_settop(luaState,oldTop);
			return returnValue;
		}
		/*
		 * Gets a numeric field of the table or userdata corresponding the the provided reference
		 */
		internal object getObject(int reference,object field) 
		{
			int oldTop=KopiLua.Lua.lua_gettop(luaState);
			LuaLib.lua_getref(luaState,reference);
			translator.push(luaState,field);
			KopiLua.Lua.lua_gettable(luaState,-2);
			object returnValue=translator.getObject(luaState,-1);
			KopiLua.Lua.lua_settop(luaState,oldTop);
			return returnValue;
		}
		/*
		 * Sets a field of the table or userdata corresponding the the provided reference
		 * to the provided value
		 */
		internal void setObject(int reference, string field, object val) 
		{
			int oldTop=KopiLua.Lua.lua_gettop(luaState);
			LuaLib.lua_getref(luaState,reference);
			setObject(field.Split(new char[] {'.'}),val);
			KopiLua.Lua.lua_settop(luaState,oldTop);
		}
		/*
		 * Sets a numeric field of the table or userdata corresponding the the provided reference
		 * to the provided value
		 */
		internal void setObject(int reference, object field, object val) 
		{
			int oldTop=KopiLua.Lua.lua_gettop(luaState);
			LuaLib.lua_getref(luaState,reference);
			translator.push(luaState,field);
			translator.push(luaState,val);
			KopiLua.Lua.lua_settable(luaState,-3);
			KopiLua.Lua.lua_settop(luaState,oldTop);
		}

		/*
		 * Registers an object's method as a Lua function (global or table field)
		 * The method may have any signature
		 */
			public LuaFunction RegisterFunction(string path, object target,MethodInfo function) 
		{
			// We leave nothing on the stack when we are done
			int oldTop = KopiLua.Lua.lua_gettop(luaState);

			LuaMethodWrapper wrapper=new LuaMethodWrapper(translator,target,function.DeclaringType,function);
			translator.push(luaState,new KopiLua.Lua.lua_CFunction(wrapper.call));

			this[path]=translator.getObject(luaState,-1);
			LuaFunction f = GetFunction(path);

			KopiLua.Lua.lua_settop(luaState, oldTop);

			return f;
		}


		/*
		 * Compares the two values referenced by ref1 and ref2 for equality
		 */
		internal bool compareRef(int ref1, int ref2) 
		{
			int top=KopiLua.Lua.lua_gettop(luaState);
			LuaLib.lua_getref(luaState,ref1);
			LuaLib.lua_getref(luaState,ref2);
			int equal=KopiLua.Lua.lua_equal(luaState,-1,-2);
			KopiLua.Lua.lua_settop(luaState,top);
			return (equal!=0);
		}
		
		internal void pushCSFunction(KopiLua.Lua.lua_CFunction function)
		{
			translator.pushFunction(luaState,function);
		}

		#region IDisposable Members

		public virtual void Dispose()
		{
			if (translator != null)
			{
				translator.pendingEvents.Dispose();

				translator = null;
			}

			System.GC.Collect();
			System.GC.WaitForPendingFinalizers();
		}

		#endregion
	}

	/*
	 * Wrapper class for Lua tables
	 *
	 * Author: Fabio Mascarenhas
	 * Version: 1.0
	 */
	public class LuaTable
	{
		internal int reference;
		private Lua interpreter;
		public LuaTable(int reference, Lua interpreter) 
		{
			this.reference=reference;
			this.interpreter=interpreter;
		}
		~LuaTable() 
		{
			interpreter.dispose(reference);
		}
		/*
		 * Indexer for string fields of the table
		 */
		public object this[string field] 
		{
			get 
			{
				return interpreter.getObject(reference,field);
			}
			set 
			{
				interpreter.setObject(reference,field,value);
			}
		}
		/*
		 * Indexer for numeric fields of the table
		 */
		public object this[object field] 
		{
			get 
			{
				return interpreter.getObject(reference,field);
			}
			set 
			{
				interpreter.setObject(reference,field,value);
			}
		}


		public System.Collections.IEnumerator GetEnumerator()
		{
			return interpreter.GetTableDict(this).GetEnumerator();
		}

		public ICollection Keys 
		{
			get { return interpreter.GetTableDict(this).Keys; }
		}

		public ICollection Values
		{
			get { return interpreter.GetTableDict(this).Values; }
		}

		/*
		 * Gets an string fields of a table ignoring its metatable,
		 * if it exists
		 */
		internal object rawget(string field) 
		{
			return interpreter.rawGetObject(reference,field);
		}

		internal object rawgetFunction(string field) 
		{
			object obj=interpreter.rawGetObject(reference,field);

			if(obj is KopiLua.Lua.lua_CFunction)
				return new LuaFunction((KopiLua.Lua.lua_CFunction)obj,interpreter);
			else
				return obj;
		}
		
		/*
		 * Pushes this table into the Lua stack
		 */
		internal void push(KopiLua.Lua.lua_State luaState) 
		{
			LuaLib.lua_getref(luaState,reference);
		}
		public override string ToString() 
		{
			return "table";
		}
		public override bool Equals(object o) 
		{
			if(o is LuaTable) 
			{
				LuaTable l=(LuaTable)o;
				return interpreter.compareRef(l.reference,this.reference);
			} else return false;
		}
		public override int GetHashCode() 
		{
			return reference;
		}
	}

	public class LuaFunction 
	{
		private Lua interpreter;
		internal KopiLua.Lua.lua_CFunction function;
		internal int reference;

		public LuaFunction(int reference, Lua interpreter) 
		{
			this.reference=reference;
			this.function=null;
			this.interpreter=interpreter;
		}
		
		public LuaFunction(KopiLua.Lua.lua_CFunction function, Lua interpreter) 
		{
			this.reference=0;
			this.function=function;
			this.interpreter=interpreter;
		}

		~LuaFunction() 
		{
			if(reference!=0)
				interpreter.dispose(reference);
		}
		/*
		 * Calls the function casting return values to the types
		 * in returnTypes
		 */
		internal object[] call(object[] args, Type[] returnTypes) 
		{
			return interpreter.callFunction(this,args,returnTypes);
		}
		/*
		 * Calls the function and returns its return values inside
		 * an array
		 */
		public object[] Call(params object[] args) 
		{
			return interpreter.callFunction(this,args);
		}
		/*
		 * Pushes the function into the Lua stack
		 */
		internal void push(KopiLua.Lua.lua_State luaState) 
		{
			if(reference!=0)
				LuaLib.lua_getref(luaState,reference);
			else
				interpreter.pushCSFunction(function);
		}
		public override string ToString() 
		{
			return "function";
		}
		public override bool Equals(object o) 
		{
			if(o is LuaFunction) 
			{
				LuaFunction l=(LuaFunction)o;
				if(this.reference!=0 && l.reference!=0)
					return interpreter.compareRef(l.reference,this.reference);
				else
					return this.function==l.function;
			} 
			else return false;
		}
		public override int GetHashCode() 
		{
			if(reference!=0)
				return reference;
			else
				return function.GetHashCode();
		}
	}

	public class LuaUserData
	{
		internal int reference;
		private Lua interpreter;
		public LuaUserData(int reference, Lua interpreter) 
		{
			this.reference=reference;
			this.interpreter=interpreter;
		}
		~LuaUserData() 
		{
			interpreter.dispose(reference);
		}
		/*
		 * Indexer for string fields of the userdata
		 */
		public object this[string field] 
		{
			get 
			{
				return interpreter.getObject(reference,field);
			}
			set 
			{
				interpreter.setObject(reference,field,value);
			}
		}
		/*
		 * Indexer for numeric fields of the userdata
		 */
		public object this[object field] 
		{
			get 
			{
				return interpreter.getObject(reference,field);
			}
			set 
			{
				interpreter.setObject(reference,field,value);
			}
		}
		/*
		 * Calls the userdata and returns its return values inside
		 * an array
		 */
		public object[] Call(params object[] args) 
		{
			return interpreter.callFunction(this,args);
		}
		/*
		 * Pushes the userdata into the Lua stack
		 */
		internal void push(KopiLua.Lua.lua_State luaState) 
		{
			LuaLib.lua_getref(luaState,reference);
		}
		public override string ToString() 
		{
			return "userdata";
		}
		public override bool Equals(object o) 
		{
			if(o is LuaUserData) 
			{
				LuaUserData l=(LuaUserData)o;
				return interpreter.compareRef(l.reference,this.reference);
			} 
			else return false;
		}
		public override int GetHashCode() 
		{
			return reference;
		}
	}
}