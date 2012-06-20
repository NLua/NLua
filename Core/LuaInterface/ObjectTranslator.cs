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
using System.Reflection;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using LuaInterface.Method;
using LuaInterface.Exceptions;
// TODO: Kódot átt kell alakítani a többihez hasonlóan.
namespace LuaInterface 
{
	using LuaCore = KopiLua.Lua;

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
		private LuaCore.lua_CFunction registerTableFunction,unregisterTableFunction,getMethodSigFunction,
			getConstructorSigFunction,importTypeFunction,loadAssemblyFunction;

        internal EventHandlerContainer pendingEvents = new EventHandlerContainer();

		public ObjectTranslator(Lua interpreter,LuaCore.lua_State luaState) 
		{
			this.interpreter=interpreter;
			typeChecker=new CheckType(this);
			metaFunctions=new MetaFunctions(this);
			assemblies=new List<Assembly>();

			importTypeFunction=new LuaCore.lua_CFunction(this.importType);
			loadAssemblyFunction=new LuaCore.lua_CFunction(this.loadAssembly);
			registerTableFunction=new LuaCore.lua_CFunction(this.registerTable);
			unregisterTableFunction=new LuaCore.lua_CFunction(this.unregisterTable);
			getMethodSigFunction=new LuaCore.lua_CFunction(this.getMethodSignature);
			getConstructorSigFunction=new LuaCore.lua_CFunction(this.getConstructorSignature);

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
		private void createLuaObjectList(LuaCore.lua_State luaState) 
		{
			LuaCore.lua_pushstring(luaState,"luaNet_objects");
			LuaCore.lua_newtable(luaState);
			LuaCore.lua_newtable(luaState);
			LuaCore.lua_pushstring(luaState,"__mode");
			LuaCore.lua_pushstring(luaState,"v");
			LuaCore.lua_settable(luaState,-3);
			LuaCore.lua_setmetatable(luaState,-2);
			LuaCore.lua_settable(luaState, (int) PseudoIndex.Registry);
		}
		/*
		 * Registers the indexing function of CLR objects
		 * passed to Lua
		 */
		private void createIndexingMetaFunction(LuaCore.lua_State luaState) 
		{
			LuaCore.lua_pushstring(luaState,"luaNet_indexfunction");
			LuaLib.luaL_dostring(luaState,MetaFunctions.luaIndexFunction);	// steffenj: lua_dostring renamed to luaL_dostring
			//LuaLib.lua_pushstdcallcfunction(luaState,indexFunction);
            LuaCore.lua_rawset(luaState, (int) PseudoIndex.Registry);
		}
		/*
		 * Creates the metatable for superclasses (the base
		 * field of registered tables)
		 */
		private void createBaseClassMetatable(LuaCore.lua_State luaState) 
		{
			LuaCore.luaL_newmetatable(luaState,"luaNet_searchbase");
			LuaCore.lua_pushstring(luaState,"__gc");
			LuaLib.lua_pushstdcallcfunction(luaState,metaFunctions.gcFunction);
			LuaCore.lua_settable(luaState,-3);
			LuaCore.lua_pushstring(luaState,"__tostring");
			LuaLib.lua_pushstdcallcfunction(luaState,metaFunctions.toStringFunction);
			LuaCore.lua_settable(luaState,-3);
			LuaCore.lua_pushstring(luaState,"__index");
			LuaLib.lua_pushstdcallcfunction(luaState,metaFunctions.baseIndexFunction);
			LuaCore.lua_settable(luaState,-3);
			LuaCore.lua_pushstring(luaState,"__newindex");
			LuaLib.lua_pushstdcallcfunction(luaState,metaFunctions.newindexFunction);
			LuaCore.lua_settable(luaState,-3);
			LuaCore.lua_settop(luaState,-2);
		}
		/*
		 * Creates the metatable for type references
		 */
		private void createClassMetatable(LuaCore.lua_State luaState) 
		{
			LuaCore.luaL_newmetatable(luaState,"luaNet_class");
			LuaCore.lua_pushstring(luaState,"__gc");
			LuaLib.lua_pushstdcallcfunction(luaState,metaFunctions.gcFunction);
			LuaCore.lua_settable(luaState,-3);
			LuaCore.lua_pushstring(luaState,"__tostring");
			LuaLib.lua_pushstdcallcfunction(luaState,metaFunctions.toStringFunction);
			LuaCore.lua_settable(luaState,-3);
			LuaCore.lua_pushstring(luaState,"__index");
			LuaLib.lua_pushstdcallcfunction(luaState,metaFunctions.classIndexFunction);
			LuaCore.lua_settable(luaState,-3);
			LuaCore.lua_pushstring(luaState,"__newindex");
			LuaLib.lua_pushstdcallcfunction(luaState,metaFunctions.classNewindexFunction);
			LuaCore.lua_settable(luaState,-3);
			LuaCore.lua_pushstring(luaState,"__call");
			LuaLib.lua_pushstdcallcfunction(luaState,metaFunctions.callConstructorFunction);
			LuaCore.lua_settable(luaState,-3);
			LuaCore.lua_settop(luaState,-2);
		}
		/*
		 * Registers the global functions used by LuaInterface
		 */
		private void setGlobalFunctions(LuaCore.lua_State luaState)
		{
			LuaLib.lua_pushstdcallcfunction(luaState,metaFunctions.indexFunction);
			LuaCore.lua_setglobal(luaState,"get_object_member");
			LuaLib.lua_pushstdcallcfunction(luaState,importTypeFunction);
			LuaCore.lua_setglobal(luaState,"import_type");
			LuaLib.lua_pushstdcallcfunction(luaState,loadAssemblyFunction);
			LuaCore.lua_setglobal(luaState,"load_assembly");
			LuaLib.lua_pushstdcallcfunction(luaState,registerTableFunction);
			LuaCore.lua_setglobal(luaState,"make_object");
			LuaLib.lua_pushstdcallcfunction(luaState,unregisterTableFunction);
			LuaCore.lua_setglobal(luaState,"free_object");
			LuaLib.lua_pushstdcallcfunction(luaState,getMethodSigFunction);
			LuaCore.lua_setglobal(luaState,"get_method_bysig");
			LuaLib.lua_pushstdcallcfunction(luaState,getConstructorSigFunction);
			LuaCore.lua_setglobal(luaState,"get_constructor_bysig");
		}
		/*
		 * Creates the metatable for delegates
		 */
		private void createFunctionMetatable(LuaCore.lua_State luaState) 
		{
			LuaCore.luaL_newmetatable(luaState,"luaNet_function");
			LuaCore.lua_pushstring(luaState,"__gc");
			LuaLib.lua_pushstdcallcfunction(luaState,metaFunctions.gcFunction);
			LuaCore.lua_settable(luaState,-3);
			LuaCore.lua_pushstring(luaState,"__call");
			LuaLib.lua_pushstdcallcfunction(luaState,metaFunctions.execDelegateFunction);
			LuaCore.lua_settable(luaState,-3);
			LuaCore.lua_settop(luaState,-2);
		}
		/*
		 * Passes errors (argument e) to the Lua interpreter
		 */
        internal void throwError(LuaCore.lua_State luaState, object e)
        {
            // We use this to remove anything pushed by luaL_where
            int oldTop = LuaCore.lua_gettop(luaState);

            // Stack frame #1 is our C# wrapper, so not very interesting to the user
            // Stack frame #2 must be the lua code that called us, so that's what we want to use
            LuaCore.luaL_where(luaState, 1);
            object[] curlev = popValues(luaState, oldTop);

            // Determine the position in the script where the exception was triggered
            string errLocation = "";
            if (curlev.Length > 0)
                errLocation = curlev[0].ToString();

            string message = e as string;
            if (message != null)
            {
                // Wrap Lua error (just a string) and store the error location
                e = new LuaScriptException(message, errLocation);
            }
            else
            {
                Exception ex = e as Exception;
                if (ex != null)
                {
                    // Wrap generic .NET exception as an InnerException and store the error location
                    e = new LuaScriptException(ex, errLocation);
                }
            }

            push(luaState, e);
            LuaCore.lua_error(luaState);
        }
		/*
		 * Implementation of load_assembly. Throws an error
		 * if the assembly is not found.
		 */
		private int loadAssembly(LuaCore.lua_State luaState) 
		{            
            try
            {
                string assemblyName=LuaCore.lua_tostring(luaState,1).ToString();

                Assembly assembly = null;

                try
                {
                    assembly = Assembly.Load(assemblyName);
                }
                catch (BadImageFormatException)
                {
                    // The assemblyName was invalid.  It is most likely a path.
                }

                if (assembly == null)
                {
                    assembly = Assembly.Load(AssemblyName.GetAssemblyName(assemblyName));
                }

                if (assembly != null && !assemblies.Contains(assembly))
                {
                    assemblies.Add(assembly);
                }
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
		private int importType(LuaCore.lua_State luaState) 
		{
			string className=LuaCore.lua_tostring(luaState,1).ToString();
            Type klass=FindType(className);
            if(klass!=null)
				pushType(luaState,klass);
            else
    			LuaCore.lua_pushnil(luaState);
            return 1;
        }
		/*
		 * Implementation of make_object. Registers a table (first
		 * argument in the stack) as an object subclassing the
		 * type passed as second argument in the stack.
		 */
		private int registerTable(LuaCore.lua_State luaState) 
		{
			if(LuaCore.lua_type(luaState,1).ToLuaTypes()==LuaTypes.Table) 
			{
				LuaTable luaTable=getTable(luaState,1);
                string superclassName = LuaCore.lua_tostring(luaState, 2).ToString();
                if (superclassName != null)
                {
                    Type klass = FindType(superclassName);
                    if (klass != null)
                    {
                        // Creates and pushes the object in the stack, setting
                        // it as the  metatable of the first argument
                        object obj = CodeGeneration.Instance.GetClassInstance(klass, luaTable);
                        pushObject(luaState, obj, "luaNet_metatable");
                        LuaCore.lua_newtable(luaState);
                        LuaCore.lua_pushstring(luaState, "__index");
                        LuaCore.lua_pushvalue(luaState, -3);
                        LuaCore.lua_settable(luaState, -3);
                        LuaCore.lua_pushstring(luaState, "__newindex");
                        LuaCore.lua_pushvalue(luaState, -3);
                        LuaCore.lua_settable(luaState, -3);
                        LuaCore.lua_setmetatable(luaState, 1);
                        // Pushes the object again, this time as the base field
                        // of the table and with the luaNet_searchbase metatable
                        LuaCore.lua_pushstring(luaState, "base");
                        int index = addObject(obj);
                        pushNewObject(luaState, obj, index, "luaNet_searchbase");
                        LuaCore.lua_rawset(luaState, 1);
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
		private int unregisterTable(LuaCore.lua_State luaState) 
		{
			try 
			{
				if(LuaCore.lua_getmetatable(luaState,1)!=0) 
				{
					LuaCore.lua_pushstring(luaState,"__index");
					LuaCore.lua_gettable(luaState,-2);
					object obj=getRawNetObject(luaState,-1);
					if(obj==null) throwError(luaState,"unregister_table: arg is not valid table");
					FieldInfo luaTableField=obj.GetType().GetField("__luaInterface_luaTable");
					if(luaTableField==null) throwError(luaState,"unregister_table: arg is not valid table");
					luaTableField.SetValue(obj,null);
					LuaCore.lua_pushnil(luaState);
					LuaCore.lua_setmetatable(luaState,1);
					LuaCore.lua_pushstring(luaState,"base");
					LuaCore.lua_pushnil(luaState);
					LuaCore.lua_settable(luaState,1);
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
		private int getMethodSignature(LuaCore.lua_State luaState) 
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
					LuaCore.lua_pushnil(luaState);
					return 1;
				}
				klass=target.GetType();
			}
			string methodName=LuaCore.lua_tostring(luaState,2).ToString();
			Type[] signature=new Type[LuaCore.lua_gettop(luaState)-2];
			for(int i=0;i<signature.Length;i++)
				signature[i]=FindType(LuaCore.lua_tostring(luaState,i+3).ToString());
			try 
			{
                //CP: Added ignore case
				MethodInfo method=klass.GetMethod(methodName,BindingFlags.Public | BindingFlags.Static |
                    BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.IgnoreCase, null, signature, null);
				pushFunction(luaState,new LuaCore.lua_CFunction((new LuaMethodWrapper(this,target,klass,method)).call));
			} 
			catch(Exception e) 
			{
				throwError(luaState,e);
				LuaCore.lua_pushnil(luaState);
			}
			return 1;
		}
		/*
		 * Implementation of get_constructor_bysig. Returns nil
		 * if no matching constructor is found.
		 */
		private int getConstructorSignature(LuaCore.lua_State luaState) 
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
			Type[] signature=new Type[LuaCore.lua_gettop(luaState)-1];
			for(int i=0;i<signature.Length;i++)
				signature[i]=FindType(LuaCore.lua_tostring(luaState,i+2).ToString());
			try 
			{
				ConstructorInfo constructor=klass.UnderlyingSystemType.GetConstructor(signature);
				pushFunction(luaState,new LuaCore.lua_CFunction((new LuaMethodWrapper(this,null,klass,constructor)).call));
			} 
			catch(Exception e) 
			{
				throwError(luaState,e);
				LuaCore.lua_pushnil(luaState);
			}
			return 1;
		}
		/*
		 * Pushes a type reference into the stack
		 */
		internal void pushType(LuaCore.lua_State luaState, Type t) 
		{
			pushObject(luaState,new ProxyType(t),"luaNet_class");
		}
		/*
		 * Pushes a delegate into the stack
		 */
		internal void pushFunction(LuaCore.lua_State luaState, LuaCore.lua_CFunction func) 
		{
			pushObject(luaState,func,"luaNet_function");
		}
		/*
		 * Pushes a CLR object into the Lua stack as an userdata
		 * with the provided metatable
		 */
		internal void pushObject(LuaCore.lua_State luaState, object o, string metatable) 
		{
			int index = -1;
			// Pushes nil
			if(o==null) 
			{
				LuaCore.lua_pushnil(luaState);
				return;
			}

			// Object already in the list of Lua objects? Push the stored reference.
            bool found = objectsBackMap.TryGetValue(o, out index);
			if(found) 
			{
				LuaCore.luaL_getmetatable(luaState,"luaNet_objects");
				LuaCore.lua_rawgeti(luaState,-1,index);

                // Note: starting with lua5.1 the garbage collector may remove weak reference items (such as our luaNet_objects values) when the initial GC sweep 
                // occurs, but the actual call of the __gc finalizer for that object may not happen until a little while later.  During that window we might call
                // this routine and find the element missing from luaNet_objects, but collectObject() has not yet been called.  In that case, we go ahead and call collect
                // object here
                // did we find a non nil object in our table? if not, we need to call collect object
                LuaTypes type = LuaCore.lua_type(luaState, -1).ToLuaTypes();
                if (type != LuaTypes.Nil)
                {
                    LuaCore.lua_remove(luaState, -2);     // drop the metatable - we're going to leave our object on the stack

                    return;
                }

                // MetaFunctions.dumpStack(this, luaState);
                LuaCore.lua_remove(luaState, -1);    // remove the nil object value
                LuaCore.lua_remove(luaState, -1);    // remove the metatable

                collectObject(o, index);            // Remove from both our tables and fall out to get a new ID
			}
            index = addObject(o);

			pushNewObject(luaState,o,index,metatable);
		}


		/*
		 * Pushes a new object into the Lua stack with the provided
		 * metatable
		 */
		private void pushNewObject(LuaCore.lua_State luaState,object o,int index,string metatable) 
		{
			if(metatable=="luaNet_metatable") 
			{
				// Gets or creates the metatable for the object's type
				LuaCore.luaL_getmetatable(luaState,o.GetType().AssemblyQualifiedName);

				if(LuaCore.lua_isnil(luaState,-1))
				{
					LuaCore.lua_settop(luaState,-2);
					LuaCore.luaL_newmetatable(luaState,o.GetType().AssemblyQualifiedName);
					LuaCore.lua_pushstring(luaState,"cache");
					LuaCore.lua_newtable(luaState);
					LuaCore.lua_rawset(luaState,-3);
					LuaCore.lua_pushlightuserdata(luaState,LuaLib.luanet_gettag());
					LuaCore.lua_pushnumber(luaState,1);
					LuaCore.lua_rawset(luaState,-3);
					LuaCore.lua_pushstring(luaState,"__index");
					LuaCore.lua_pushstring(luaState,"luaNet_indexfunction");
					LuaCore.lua_rawget(luaState, (int) PseudoIndex.Registry);
					LuaCore.lua_rawset(luaState,-3);
					LuaCore.lua_pushstring(luaState,"__gc");
					LuaLib.lua_pushstdcallcfunction(luaState,metaFunctions.gcFunction);
					LuaCore.lua_rawset(luaState,-3);
					LuaCore.lua_pushstring(luaState,"__tostring");
					LuaLib.lua_pushstdcallcfunction(luaState,metaFunctions.toStringFunction);
					LuaCore.lua_rawset(luaState,-3);
					LuaCore.lua_pushstring(luaState,"__newindex");
					LuaLib.lua_pushstdcallcfunction(luaState,metaFunctions.newindexFunction);
					LuaCore.lua_rawset(luaState,-3);
				}
			}
			else
			{
				LuaCore.luaL_getmetatable(luaState,metatable);
			}

			// Stores the object index in the Lua list and pushes the
			// index into the Lua stack
			LuaCore.luaL_getmetatable(luaState,"luaNet_objects");
			LuaLib.luanet_newudata(luaState,index);
			LuaCore.lua_pushvalue(luaState,-3);
			LuaCore.lua_remove(luaState,-4);
			LuaCore.lua_setmetatable(luaState,-2);
			LuaCore.lua_pushvalue(luaState,-1);
			LuaCore.lua_rawseti(luaState,-3,index);
			LuaCore.lua_remove(luaState,-2);
		}
		/*
		 * Gets an object from the Lua stack with the desired type, if it matches, otherwise
		 * returns null.
		 */
		internal object getAsType(LuaCore.lua_State luaState,int stackPos,Type paramType) 
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

            // Debug.WriteLine("Adding " + obj.ToString() + " @ " + index);

            objects[index] = obj;
            objectsBackMap[obj] = index;

            return index;
        }



		/*
		 * Gets an object from the Lua stack according to its Lua type.
		 */
		internal object getObject(LuaCore.lua_State luaState,int index) 
		{
			LuaTypes type=LuaCore.lua_type(luaState,index).ToLuaTypes();
			switch(type) 
			{
				case LuaTypes.Number:
				{
					return LuaCore.lua_tonumber(luaState,index);
				} 
				case LuaTypes.String: 
				{
					return LuaCore.lua_tostring(luaState,index);
				} 
				case LuaTypes.Boolean:
				{
					return LuaCore.lua_toboolean(luaState,index);
				} 
				case LuaTypes.Table: 
				{
					return getTable(luaState,index);
				} 
				case LuaTypes.Function:
				{
					return getFunction(luaState,index);
				} 
				case LuaTypes.UserData:
				{
					int udata=LuaLib.luanet_tonetobject(luaState,index);
				Console.WriteLine(udata);
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
		internal LuaTable getTable(LuaCore.lua_State luaState,int index) 
		{
			LuaCore.lua_pushvalue(luaState,index);
			return new LuaTable(LuaLib.lua_ref(luaState,1),interpreter);
		}
		/*
		 * Gets the userdata in the index positon of the Lua stack.
		 */
		internal LuaUserData getUserData(LuaCore.lua_State luaState,int index) 
		{
			LuaCore.lua_pushvalue(luaState,index);
			return new LuaUserData(LuaLib.lua_ref(luaState,1),interpreter);
		}
		/*
		 * Gets the function in the index positon of the Lua stack.
		 */
		internal LuaFunction getFunction(LuaCore.lua_State luaState,int index) 
		{
			LuaCore.lua_pushvalue(luaState,index);
			return new LuaFunction(LuaLib.lua_ref(luaState,1),interpreter);
		}
		/*
		 * Gets the CLR object in the index positon of the Lua stack. Returns
		 * delegates as Lua functions.
		 */
		internal object getNetObject(LuaCore.lua_State luaState,int index) 
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
		internal object getRawNetObject(LuaCore.lua_State luaState,int index) 
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
		internal int returnValues(LuaCore.lua_State luaState, object[] returnValues) 
		{
			if(LuaCore.lua_checkstack(luaState,returnValues.Length+5).ToBoolean()) 
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
		internal object[] popValues(LuaCore.lua_State luaState,int oldTop) 
		{
			int newTop=LuaCore.lua_gettop(luaState);
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
				LuaCore.lua_settop(luaState,oldTop);
				return returnValues.ToArray();
			}
		}
		/*
		 * Gets the values from the provided index to
		 * the top of the stack and returns them in an array, casting
		 * them to the provided types.
		 */
		internal object[] popValues(LuaCore.lua_State luaState,int oldTop,Type[] popTypes) 
		{
			int newTop=LuaCore.lua_gettop(luaState);
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
				LuaCore.lua_settop(luaState,oldTop);
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
		internal void push(LuaCore.lua_State luaState, object o) 
		{
			if(o==null) 
			{
				LuaCore.lua_pushnil(luaState);
			}
			else if(o is sbyte || o is byte || o is short || o is ushort ||
				o is int || o is uint || o is long || o is float ||
				o is ulong || o is decimal || o is double) 
			{
				double d=Convert.ToDouble(o);
				LuaCore.lua_pushnumber(luaState,d);
			}
			else if(o is char)
			{
				double d = (char)o;
				LuaCore.lua_pushnumber(luaState,d);
			}
			else if(o is string)
			{
				string str=(string)o;
				LuaCore.lua_pushstring(luaState,str);
			}
			else if(o is bool)
			{
				bool b=(bool)o;
				LuaCore.lua_pushboolean(luaState, (b == true ? 1 : 0));
			}
			else if(IsILua(o))
			{
				(((ILuaGeneratedType)o).__luaInterface_getLuaTable()).push(luaState);
			}
			else if(o is LuaTable) 
			{
				((LuaTable)o).push(luaState);
			} 
			else if(o is LuaCore.lua_CFunction) 
			{
				pushFunction(luaState,(LuaCore.lua_CFunction)o);
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
		internal bool matchParameters(LuaCore.lua_State luaState,MethodBase method,ref MethodCache methodCache) 
		{
			return metaFunctions.matchParameters(luaState,method,ref methodCache);
		}
	}
}