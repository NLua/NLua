namespace Mono.LuaInterface 
{
	using System;
	using System.IO;
	using System.Collections;
	using System.Reflection;
    using System.Collections.Generic;
    using System.Diagnostics;
    using LuaWrap;

	/*
	 * Passes objects from the CLR to Lua and vice-versa
	 * 
	 * Author: Fabio Mascarenhas
	 * Version: 1.0
	 */
	public class ObjectTranslator 
	{
		internal CheckType typeChecker;

        // object # to object (FIXME - it should be possible to get object address as an object #)
        public readonly Dictionary<int, object> objects = new Dictionary<int, object>();
        // object to object #
        public readonly Dictionary<object, int> objectsBackMap = new Dictionary<object, int>();
        internal Lua interpreter;
		private MetaFunctions metaFunctions;
		private List<Assembly> assemblies;
		private CallbackFunction registerTableFunction,unregisterTableFunction,getMethodSigFunction,
			getConstructorSigFunction,importTypeFunction,loadAssemblyFunction;

        internal EventHandlerContainer pendingEvents = new EventHandlerContainer();

		public ObjectTranslator(Lua interpreter,IntPtr luaState) 
		{
			this.interpreter=interpreter;
			typeChecker=new CheckType(this);
			metaFunctions=new MetaFunctions(this);
			assemblies=new List<Assembly>();

			importTypeFunction=new CallbackFunction(this.importType);
			loadAssemblyFunction=new CallbackFunction(this.loadAssembly);
			registerTableFunction=new CallbackFunction(this.registerTable);
			unregisterTableFunction=new CallbackFunction(this.unregisterTable);
			getMethodSigFunction=new CallbackFunction(this.getMethodSignature);
			getConstructorSigFunction=new CallbackFunction(this.getConstructorSignature);

			createLuaObjectList(luaState);
			createIndexingMetaFunction(luaState);
			createBaseClassMetatable(luaState);
			createClassMetatable(luaState);
			createFunctionMetatable(luaState);
			setGlobalFunctions(luaState);
		}

		/*
		 * Sets up the list of objects in the Lua side
		 */
		private void createLuaObjectList(IntPtr luaState) 
		{
			LuaLib.lua_pushstring(luaState,"luaNet_objects");
			LuaLib.lua_newtable(luaState);
			LuaLib.lua_newtable(luaState);
			LuaLib.lua_pushstring(luaState,"__mode");
			LuaLib.lua_pushstring(luaState,"v");
			LuaLib.lua_settable(luaState,-3);
			LuaLib.lua_setmetatable(luaState,-2);
			LuaLib.lua_settable(luaState, (int) PseudoIndex.Registry);
		}
		/*
		 * Registers the indexing function of CLR objects
		 * passed to Lua
		 */
		private void createIndexingMetaFunction(IntPtr luaState) 
		{
			LuaLib.lua_pushstring(luaState,"luaNet_indexfunction");
			LuaLib.luaL_dostring(luaState,MetaFunctions.luaIndexFunction);	// steffenj: lua_dostring renamed to luaL_dostring
			//LuaLib.lua_pushstdcallcfunction(luaState,indexFunction);
            LuaLib.lua_rawset(luaState, (int) PseudoIndex.Registry);
		}
		/*
		 * Creates the metatable for superclasses (the base
		 * field of registered tables)
		 */
		private void createBaseClassMetatable(IntPtr luaState) 
		{
			LuaLib.luaL_newmetatable(luaState,"luaNet_searchbase");
			LuaLib.lua_pushstring(luaState,"__gc");
			LuaLib.lua_pushstdcallcfunction(luaState,metaFunctions.gcFunction);
			LuaLib.lua_settable(luaState,-3);
			LuaLib.lua_pushstring(luaState,"__tostring");
			LuaLib.lua_pushstdcallcfunction(luaState,metaFunctions.toStringFunction);
			LuaLib.lua_settable(luaState,-3);
			LuaLib.lua_pushstring(luaState,"__index");
			LuaLib.lua_pushstdcallcfunction(luaState,metaFunctions.baseIndexFunction);
			LuaLib.lua_settable(luaState,-3);
			LuaLib.lua_pushstring(luaState,"__newindex");
			LuaLib.lua_pushstdcallcfunction(luaState,metaFunctions.newindexFunction);
			LuaLib.lua_settable(luaState,-3);
			LuaLib.lua_settop(luaState,-2);
		}
		/*
		 * Creates the metatable for type references
		 */
		private void createClassMetatable(IntPtr luaState) 
		{
			LuaLib.luaL_newmetatable(luaState,"luaNet_class");
			LuaLib.lua_pushstring(luaState,"__gc");
			LuaLib.lua_pushstdcallcfunction(luaState,metaFunctions.gcFunction);
			LuaLib.lua_settable(luaState,-3);
			LuaLib.lua_pushstring(luaState,"__tostring");
			LuaLib.lua_pushstdcallcfunction(luaState,metaFunctions.toStringFunction);
			LuaLib.lua_settable(luaState,-3);
			LuaLib.lua_pushstring(luaState,"__index");
			LuaLib.lua_pushstdcallcfunction(luaState,metaFunctions.classIndexFunction);
			LuaLib.lua_settable(luaState,-3);
			LuaLib.lua_pushstring(luaState,"__newindex");
			LuaLib.lua_pushstdcallcfunction(luaState,metaFunctions.classNewindexFunction);
			LuaLib.lua_settable(luaState,-3);
			LuaLib.lua_pushstring(luaState,"__call");
			LuaLib.lua_pushstdcallcfunction(luaState,metaFunctions.callConstructorFunction);
			LuaLib.lua_settable(luaState,-3);
			LuaLib.lua_settop(luaState,-2);
		}
		/*
		 * Registers the global functions used by LuaInterface
		 */
		private void setGlobalFunctions(IntPtr luaState)
		{
			LuaLib.lua_pushstdcallcfunction(luaState,metaFunctions.indexFunction);
			LuaLib.lua_setglobal(luaState,"get_object_member");
			LuaLib.lua_pushstdcallcfunction(luaState,importTypeFunction);
			LuaLib.lua_setglobal(luaState,"import_type");
			LuaLib.lua_pushstdcallcfunction(luaState,loadAssemblyFunction);
			LuaLib.lua_setglobal(luaState,"load_assembly");
			LuaLib.lua_pushstdcallcfunction(luaState,registerTableFunction);
			LuaLib.lua_setglobal(luaState,"make_object");
			LuaLib.lua_pushstdcallcfunction(luaState,unregisterTableFunction);
			LuaLib.lua_setglobal(luaState,"free_object");
			LuaLib.lua_pushstdcallcfunction(luaState,getMethodSigFunction);
			LuaLib.lua_setglobal(luaState,"get_method_bysig");
			LuaLib.lua_pushstdcallcfunction(luaState,getConstructorSigFunction);
			LuaLib.lua_setglobal(luaState,"get_constructor_bysig");
		}
		/*
		 * Creates the metatable for delegates
		 */
		private void createFunctionMetatable(IntPtr luaState) 
		{
			LuaLib.luaL_newmetatable(luaState,"luaNet_function");
			LuaLib.lua_pushstring(luaState,"__gc");
			LuaLib.lua_pushstdcallcfunction(luaState,metaFunctions.gcFunction);
			LuaLib.lua_settable(luaState,-3);
			LuaLib.lua_pushstring(luaState,"__call");
			LuaLib.lua_pushstdcallcfunction(luaState,metaFunctions.execDelegateFunction);
			LuaLib.lua_settable(luaState,-3);
			LuaLib.lua_settop(luaState,-2);
		}
		/*
		 * Passes errors (argument e) to the Lua interpreter
		 */
		internal void throwError(IntPtr luaState,object e) 
		{
            // If the argument is a mere string, we are free to add extra info to it (as opposed to some private C# exception object or somesuch, which we just pass up)
            if (e is string)
            {
                // We use this to remove anything pushed by luaL_where
                int oldTop = LuaLib.lua_gettop(luaState);

                // Stack frame #1 is our C# wrapper, so not very interesting to the user
                // Stack frame #2 must be the lua code that called us, so that's what we want to use
                LuaLib.luaL_where(luaState, 2);
                object[] curlev = popValues(luaState, oldTop);
                // Debug.WriteLine(curlev);

                if (curlev.Length > 0)
                    e = curlev[0].ToString() + e;
            }

			push(luaState,e);
			LuaLib.lua_error(luaState);
		}
		/*
		 * Implementation of load_assembly. Throws an error
		 * if the assembly is not found.
		 */
		private int loadAssembly(IntPtr luaState) 
		{
			string assemblyName=LuaLib.lua_tostring(luaState,1);
			try 
			{
				Assembly assembly=Assembly.LoadWithPartialName(assemblyName);

                try
                {
                    // If we couldn't find it based on a name, see if we can use it as a filename and find it
                    if (assembly == null)
                        assembly = Assembly.Load(AssemblyName.GetAssemblyName(assemblyName));
                }
                catch (Exception)
                {
                    // ignore - it might not even be a filename
                }

				if(assembly!=null && !assemblies.Contains(assembly))
					assemblies.Add(assembly);
			} 
			catch(Exception e) 
			{
				throwError(luaState,e);
			}
			return 0;
		}
        
        internal Type FindType(string className)
        {
			foreach(Assembly assembly in assemblies) 
			{
				Type klass=assembly.GetType(className);
				if(klass!=null) 
				{
					return klass;
				}
			}
            return null;
        }
            
		/*
		 * Implementation of import_type. Returns nil if the
		 * type is not found.
		 */
		private int importType(IntPtr luaState) 
		{
			string className=LuaLib.lua_tostring(luaState,1);
            Type klass=FindType(className);
            if(klass!=null)
				pushType(luaState,klass);
            else
    			LuaLib.lua_pushnil(luaState);
            return 1;
        }
		/*
		 * Implementation of make_object. Registers a table (first
		 * argument in the stack) as an object subclassing the
		 * type passed as second argument in the stack.
		 */
		private int registerTable(IntPtr luaState) 
		{
			if(LuaLib.lua_type(luaState,1)==LuaType.Table) 
			{
				LuaTable luaTable=getTable(luaState,1);
                string superclassName = LuaLib.lua_tostring(luaState, 2);
                if (superclassName != null)
                {
                    Type klass = FindType(superclassName);
                    if (klass != null)
                    {
                        // Creates and pushes the object in the stack, setting
                        // it as the  metatable of the first argument
                        object obj = CodeGeneration.Instance.GetClassInstance(klass, luaTable);
                        pushObject(luaState, obj, "luaNet_metatable");
                        LuaLib.lua_newtable(luaState);
                        LuaLib.lua_pushstring(luaState, "__index");
                        LuaLib.lua_pushvalue(luaState, -3);
                        LuaLib.lua_settable(luaState, -3);
                        LuaLib.lua_pushstring(luaState, "__newindex");
                        LuaLib.lua_pushvalue(luaState, -3);
                        LuaLib.lua_settable(luaState, -3);
                        LuaLib.lua_setmetatable(luaState, 1);
                        // Pushes the object again, this time as the base field
                        // of the table and with the luaNet_searchbase metatable
                        LuaLib.lua_pushstring(luaState, "base");
                        //int index = addObject(obj);
                        //pushNewObject(luaState, obj, index, "luaNet_searchbase");
						pushNewObject(luaState, obj, 0, "luaNet_searchbase");
                        LuaLib.lua_rawset(luaState, 1);
                    }
                    else
                        throwError(luaState, "register_table: can not find superclass '" + superclassName + "'");
                }
                else
                    throwError(luaState, "register_table: superclass name can not be null");
			} 
			else throwError(luaState,"register_table: first arg is not a table");
			return 0;
		}
		/*
		 * Implementation of free_object. Clears the metatable and the
		 * base field, freeing the created object for garbage-collection
		 */
		private int unregisterTable(IntPtr luaState) 
		{
			try 
			{
				if(LuaLib.lua_getmetatable(luaState,1)!=0) 
				{
					LuaLib.lua_pushstring(luaState,"__index");
					LuaLib.lua_gettable(luaState,-2);
					object obj=getRawNetObject(luaState,-1);
					if(obj==null) throwError(luaState,"unregister_table: arg is not valid table");
					FieldInfo luaTableField=obj.GetType().GetField("__luaInterface_luaTable");
					if(luaTableField==null) throwError(luaState,"unregister_table: arg is not valid table");
					luaTableField.SetValue(obj,null);
					LuaLib.lua_pushnil(luaState);
					LuaLib.lua_setmetatable(luaState,1);
					LuaLib.lua_pushstring(luaState,"base");
					LuaLib.lua_pushnil(luaState);
					LuaLib.lua_settable(luaState,1);
				} 
				else throwError(luaState,"unregister_table: arg is not valid table");
			} 
			catch(Exception e) 
			{
				throwError(luaState,e.Message);
			}
			return 0;
		}
		/*
		 * Implementation of get_method_bysig. Returns nil
		 * if no matching method is not found.
		 */
		private int getMethodSignature(IntPtr luaState) 
		{
			IReflect klass; object target;
			int udata=LuaLib.luanet_checkudata(luaState,1,"luaNet_class");
			if(udata!=-1) 
			{
				klass=(IReflect)objects[udata];
				target=null;
			}
			else 
			{
				target=getRawNetObject(luaState,1);
				if(target==null) 
				{
					throwError(luaState,"get_method_bysig: first arg is not type or object reference");
					LuaLib.lua_pushnil(luaState);
					return 1;
				}
				klass=target.GetType();
			}
			string methodName=LuaLib.lua_tostring(luaState,2);
			Type[] signature=new Type[LuaLib.lua_gettop(luaState)-2];
			for(int i=0;i<signature.Length;i++)
				signature[i]=FindType(LuaLib.lua_tostring(luaState,i+3));
			try 
			{
				MethodInfo method=klass.GetMethod(methodName,BindingFlags.Public | BindingFlags.Static |
                    BindingFlags.Instance | BindingFlags.FlattenHierarchy,null,signature,null);
				pushFunction(luaState,new CallbackFunction((new LuaMethodWrapper(this,target,klass,method)).call));
			} 
			catch(Exception e) 
			{
				throwError(luaState,e);
				LuaLib.lua_pushnil(luaState);
			}
			return 1;
		}
		/*
		 * Implementation of get_constructor_bysig. Returns nil
		 * if no matching constructor is found.
		 */
		private int getConstructorSignature(IntPtr luaState) 
		{
			IReflect klass=null;
			int udata=LuaLib.luanet_checkudata(luaState,1,"luaNet_class");
			if(udata!=-1) 
			{
				klass=(IReflect)objects[udata];
			}
			if(klass==null) 
			{
				throwError(luaState,"get_constructor_bysig: first arg is invalid type reference");
			}
			Type[] signature=new Type[LuaLib.lua_gettop(luaState)-1];
			for(int i=0;i<signature.Length;i++)
				signature[i]=FindType(LuaLib.lua_tostring(luaState,i+2));
			try 
			{
				ConstructorInfo constructor=klass.UnderlyingSystemType.GetConstructor(signature);
				pushFunction(luaState,new CallbackFunction((new LuaMethodWrapper(this,null,klass,constructor)).call));
			} 
			catch(Exception e) 
			{
				throwError(luaState,e);
				LuaLib.lua_pushnil(luaState);
			}
			return 1;
		}
		/*
		 * Pushes a type reference into the stack
		 */
		internal void pushType(IntPtr luaState, Type t) 
		{
			pushObject(luaState,new ProxyType(t),"luaNet_class");
		}
		/*
		 * Pushes a delegate into the stack
		 */
		internal void pushFunction(IntPtr luaState, CallbackFunction func) 
		{
			pushObject(luaState,func,"luaNet_function");
		}
		/*
		 * Pushes a CLR object into the Lua stack as an userdata
		 * with the provided metatable
		 */
		internal void pushObject(IntPtr luaState, object o, string metatable) 
		{
			int index = -1;
			// Pushes nil
			if(o==null) 
			{
				LuaLib.lua_pushnil(luaState);
				return;
			}

			// Object already in the list of Lua objects? Push the stored reference.
            bool found = objectsBackMap.TryGetValue(o, out index);
			if(found) 
			{
				LuaLib.luaL_getmetatable(luaState,"luaNet_objects");
				LuaLib.lua_rawgeti(luaState,-1,index);

                // Note: starting with lua5.1 the garbage collector may remove weak reference items (such as our luaNet_objects values) when the initial GC sweep 
                // occurs, but the actual call of the __gc finalizer for that object may not happen until a little while later.  During that window we might call
                // this routine and find the element missing from luaNet_objects, but collectObject() has not yet been called.  In that case, we go ahead and call collect
                // object here
                // did we find a non nil object in our table? if not, we need to call collect object
                LuaType type = LuaLib.lua_type(luaState, -1);
                if (type != LuaType.Nil)
                {
                    LuaLib.lua_remove(luaState, -2);     // drop the metatable - we're going to leave our object on the stack

                    return;
                }

                // MetaFunctions.dumpStack(this, luaState);
                LuaLib.lua_remove(luaState, -1);    // remove the nil object value
                LuaLib.lua_remove(luaState, -1);    // remove the metatable

                collectObject(o, index);            // Remove from both our tables and fall out to get a new ID
			}
            //index = addObject(o);
//Console.WriteLine("pushObject: {0}", index);
			pushNewObject(luaState,o,0,metatable);
			//pushNewObject(luaState,o,index,metatable);
		}


		/*
		 * Pushes a new object into the Lua stack with the provided
		 * metatable
		 */
		private void pushNewObject(IntPtr luaState,object o,int index,string metatable) 
		{
			if(metatable=="luaNet_metatable") 
			{
				// Gets or creates the metatable for the object's type
				LuaLib.luaL_getmetatable(luaState,o.GetType().AssemblyQualifiedName);

				if(LuaLib.lua_isnil(luaState,-1))
				{
					LuaLib.lua_settop(luaState,-2);
					LuaLib.luaL_newmetatable(luaState,o.GetType().AssemblyQualifiedName);
					LuaLib.lua_pushstring(luaState,"cache");
					LuaLib.lua_newtable(luaState);
					LuaLib.lua_rawset(luaState,-3);
					LuaLib.lua_pushlightuserdata(luaState,LuaLib.luanet_gettag());
					LuaLib.lua_pushnumber(luaState,1);
					LuaLib.lua_rawset(luaState,-3);
					LuaLib.lua_pushstring(luaState,"__index");
					LuaLib.lua_pushstring(luaState,"luaNet_indexfunction");
					LuaLib.lua_rawget(luaState, (int) PseudoIndex.Registry);
					LuaLib.lua_rawset(luaState,-3);
					LuaLib.lua_pushstring(luaState,"__gc");
					LuaLib.lua_pushstdcallcfunction(luaState,metaFunctions.gcFunction);
					LuaLib.lua_rawset(luaState,-3);
					LuaLib.lua_pushstring(luaState,"__tostring");
					LuaLib.lua_pushstdcallcfunction(luaState,metaFunctions.toStringFunction);
					LuaLib.lua_rawset(luaState,-3);
					LuaLib.lua_pushstring(luaState,"__newindex");
					LuaLib.lua_pushstdcallcfunction(luaState,metaFunctions.newindexFunction);
					LuaLib.lua_rawset(luaState,-3);
				}
			}
			else
			{
				LuaLib.luaL_getmetatable(luaState,metatable);
			}

			// Stores the object index in the Lua list and pushes the
			// index into the Lua stack
			LuaLib.luaL_getmetatable(luaState,"luaNet_objects");
			//Console.WriteLine("luaState2:" + luaState);
			nextObj++;
			index = LuaLib.lua_newuserdata(luaState, nextObj).ToInt32();
			addObject(o, index);
			//LuaLib.luanet_newudata(luaState,index);
			//Console.WriteLine("index:"+index);
			LuaLib.lua_pushvalue(luaState,-3);
			LuaLib.lua_remove(luaState,-4);
			LuaLib.lua_setmetatable(luaState,-2);
			LuaLib.lua_pushvalue(luaState,-1);
			LuaLib.lua_rawseti(luaState,-3,index);
			LuaLib.lua_remove(luaState,-2);
		}
		/*
		 * Gets an object from the Lua stack with the desired type, if it matches, otherwise
		 * returns null.
		 */
		internal object getAsType(IntPtr luaState,int stackPos,Type paramType) 
		{
			ExtractValue extractor=typeChecker.checkType(luaState,stackPos,paramType);
			if(extractor!=null) return extractor(luaState,stackPos);
			return null;
		}


        /// <summary>
        /// Given the Lua int ID for an object remove it from our maps
        /// </summary>
        /// <param name="udata"></param>
		internal void collectObject(int udata) 
		{
			object o;
            bool found = objects.TryGetValue(udata, out o);

            // The other variant of collectObject might have gotten here first, in that case we will silently ignore the missing entry
            if (found)
            {
                // Debug.WriteLine("Removing " + o.ToString() + " @ " + udata);

                objects.Remove(udata);
                objectsBackMap.Remove(o);
            }
		}


        /// <summary>
        /// Given an object reference, remove it from our maps
        /// </summary>
        /// <param name="udata"></param>
        void collectObject(object o, int udata)
        {
            // Debug.WriteLine("Removing " + o.ToString() + " @ " + udata);

            objects.Remove(udata);
            objectsBackMap.Remove(o);
        }


        /// <summary>
        /// We want to ensure that objects always have a unique ID
        /// </summary>
        int nextObj = 0;

        int addObject(object obj)
        {
            // New object: inserts it in the list
            int index = nextObj++;

            //Console.WriteLine("Adding " + obj.ToString() + " @ " + index);

            objects[index] = obj;
            objectsBackMap[obj] = index;

            return index;
        }

        int addObject(object obj, int index)
        {
            // New object: inserts it in the list
            //int index = nextObj++;

            //Console.WriteLine("Adding " + obj.ToString() + " @ " + index);

            objects[index] = obj;
            objectsBackMap[obj] = index;

            return index;
        }

		/*
		 * Gets an object from the Lua stack according to its Lua type.
		 */
		internal object getObject(IntPtr luaState,int index) 
		{
			LuaType type=LuaLib.lua_type(luaState,index);

			switch(type) 
			{
				case LuaType.Number:
				{
					return LuaLib.lua_tonumber(luaState,index);
				} 
				case LuaType.String: 
				{
					return LuaLib.lua_tostring(luaState,index);
				} 
				case LuaType.Boolean:
				{
					return LuaLib.lua_toboolean(luaState,index);
				} 
				case LuaType.Table: 
				{
					return getTable(luaState,index);
				} 
				case LuaType.Function:
				{
					return getFunction(luaState,index);
				} 
				case LuaType.UserData:
				{
					int udata=LuaLib.luanet_tonetobject(luaState,index);

					if(udata!=-1)
						return objects[udata];
					else
						return getUserData(luaState,index);
				}
				default:
					return null;
			}
		}
		/*
		 * Gets the table in the index positon of the Lua stack.
		 */
		internal LuaTable getTable(IntPtr luaState,int index) 
		{
			LuaLib.lua_pushvalue(luaState,index);
			return new LuaTable(LuaLib.lua_ref(luaState,1),interpreter);
		}
		/*
		 * Gets the userdata in the index positon of the Lua stack.
		 */
		internal LuaUserData getUserData(IntPtr luaState,int index) 
		{
			LuaLib.lua_pushvalue(luaState,index);
			return new LuaUserData(LuaLib.lua_ref(luaState,1),interpreter);
		}
		/*
		 * Gets the function in the index positon of the Lua stack.
		 */
		internal LuaFunction getFunction(IntPtr luaState,int index) 
		{
			LuaLib.lua_pushvalue(luaState,index);
			return new LuaFunction(LuaLib.lua_ref(luaState,1),interpreter);
		}
		/*
		 * Gets the CLR object in the index positon of the Lua stack. Returns
		 * delegates as Lua functions.
		 */
		internal object getNetObject(IntPtr luaState,int index) 
		{
			int idx=LuaLib.luanet_tonetobject(luaState,index);
			if(idx!=-1)
				return objects[idx];
			else
				return null;
		}
		/*
		 * Gets the CLR object in the index positon of the Lua stack. Returns
		 * delegates as is.
		 */
		internal object getRawNetObject(IntPtr luaState,int index) 
		{
			int udata=LuaLib.luanet_rawnetobj(luaState,index);
			if(udata!=-1) 
			{
				return objects[udata];
			}
			return null;
		}
		/*
		 * Pushes the entire array into the Lua stack and returns the number
		 * of elements pushed.
		 */
		internal int returnValues(IntPtr luaState, object[] returnValues) 
		{
			if(LuaLib.lua_checkstack(luaState,returnValues.Length+5)) 
			{
				for(int i=0;i<returnValues.Length;i++) 
				{
					push(luaState,returnValues[i]);
				}
				return returnValues.Length;
			} else
				return 0;
		}
		/*
		 * Gets the values from the provided index to
		 * the top of the stack and returns them in an array.
		 */
		internal object[] popValues(IntPtr luaState,int oldTop) 
		{
			int newTop=LuaLib.lua_gettop(luaState);
			if(oldTop==newTop) 
			{
				return null;
			} 
			else 
			{
				ArrayList returnValues=new ArrayList();
				for(int i=oldTop+1;i<=newTop;i++) 
				{
					returnValues.Add(getObject(luaState,i));
				}
				LuaLib.lua_settop(luaState,oldTop);
				return returnValues.ToArray();
			}
		}
		/*
		 * Gets the values from the provided index to
		 * the top of the stack and returns them in an array, casting
		 * them to the provided types.
		 */
		internal object[] popValues(IntPtr luaState,int oldTop,Type[] popTypes) 
		{
			int newTop=LuaLib.lua_gettop(luaState);
			if(oldTop==newTop) 
			{
				return null;
			} 
			else 
			{
				int iTypes;
				ArrayList returnValues=new ArrayList();
				if(popTypes[0] == typeof(void))
					iTypes=1;
				else
					iTypes=0;
				for(int i=oldTop+1;i<=newTop;i++) 
				{
					returnValues.Add(getAsType(luaState,i,popTypes[iTypes]));
					iTypes++;
				}
				LuaLib.lua_settop(luaState,oldTop);
				return returnValues.ToArray();
			}
		}

        // kevinh - the following line doesn't work for remoting proxies - they always return a match for 'is'
		// else if(o is ILuaGeneratedType) 
        static bool IsILua(object o)
        {
            if(o is ILuaGeneratedType)
            {
                // Make sure we are _really_ ILuaGenerated
                Type typ = o.GetType();

                return (typ.GetInterface("ILuaGeneratedType") != null);
            }
            else
                return false;
        }

		/*
		 * Pushes the object into the Lua stack according to its type.
		 */
		internal void push(IntPtr luaState, object o) 
		{
			Console.WriteLine("push: {0}, {1}", o, luaState);
			if(o==null) 
			{
				LuaLib.lua_pushnil(luaState);
			}
			else if(o is sbyte || o is byte || o is short || o is ushort ||
				o is int || o is uint || o is long || o is float ||
				o is ulong || o is decimal || o is double) 
			{
				double d=Convert.ToDouble(o);
				LuaLib.lua_pushnumber(luaState,d);
			}
			else if(o is char)
			{
				double d = (char)o;
				LuaLib.lua_pushnumber(luaState,d);
			}
			else if(o is string)
			{
				string str=(string)o;
				LuaLib.lua_pushstring(luaState,str);
			}
			else if(o is bool)
			{
				bool b=(bool)o;
				LuaLib.lua_pushboolean(luaState,b);
			}
			else if(IsILua(o))
			{
				(((ILuaGeneratedType)o).__luaInterface_getLuaTable()).push(luaState);
			}
			else if(o is LuaTable) 
			{
				((LuaTable)o).push(luaState);
			} 
			else if(o is CallbackFunction) 
			{
				pushFunction(luaState,(CallbackFunction)o);
			} 
			else if(o is LuaFunction)
			{
				((LuaFunction)o).push(luaState);
			}
			else 
			{
				pushObject(luaState,o,"luaNet_metatable");
			}
		}
		/*
		 * Checks if the method matches the arguments in the Lua stack, getting
		 * the arguments if it does.
		 */
		internal bool matchParameters(IntPtr luaState,MethodBase method,ref MethodCache methodCache) 
		{
			return metaFunctions.matchParameters(luaState,method,ref methodCache);
		}
	}
}