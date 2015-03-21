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
using System.Reflection;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using NLua.Method;
using NLua.Exceptions;
using NLua.Extensions;

#if MONOTOUCH
	using ObjCRuntime;
#endif

namespace NLua
{
	#if USE_KOPILUA
	using LuaCore  = KopiLua.Lua;
	using LuaState = KopiLua.LuaState;
	using LuaNativeFunction = KopiLua.LuaNativeFunction;
	#else
	using LuaCore  = KeraLua.Lua;
	using LuaState = KeraLua.LuaState;
	using LuaNativeFunction = KeraLua.LuaNativeFunction;
	#endif

	/*
	 * Passes objects from the CLR to Lua and vice-versa
	 * 
	 * Author: Fabio Mascarenhas
	 * Version: 1.0
	 */
	public class ObjectTranslator
	{
		LuaNativeFunction registerTableFunction, unregisterTableFunction, getMethodSigFunction, 
			getConstructorSigFunction, importTypeFunction, loadAssemblyFunction, ctypeFunction, enumFromIntFunction;
		// object to object #
		readonly Dictionary<object, int> objectsBackMap = new Dictionary<object, int> ();
		// object # to object (FIXME - it should be possible to get object address as an object #)
		readonly Dictionary<int, object> objects = new Dictionary<int, object> ();
		internal EventHandlerContainer pendingEvents = new EventHandlerContainer ();
		MetaFunctions metaFunctions;
		List<Assembly> assemblies;
		internal CheckType typeChecker;
		internal Lua interpreter;
		/// <summary>
		/// We want to ensure that objects always have a unique ID
		/// </summary>
		int nextObj = 0;

		public MetaFunctions MetaFunctionsInstance {
			get {
				return metaFunctions;
			}
		}

		public Lua Interpreter {
			get {
				return interpreter;
			}
		}

		public ObjectTranslator (Lua interpreter, LuaState luaState)
		{
			this.interpreter = interpreter;
			typeChecker = new CheckType (this);
			metaFunctions = new MetaFunctions (this);
			assemblies = new List<Assembly> ();

			importTypeFunction = new LuaNativeFunction (ObjectTranslator.ImportType);
			loadAssemblyFunction = new LuaNativeFunction (ObjectTranslator.LoadAssembly);
			registerTableFunction = new LuaNativeFunction (ObjectTranslator.RegisterTable);
			unregisterTableFunction = new LuaNativeFunction (ObjectTranslator.UnregisterTable);
			getMethodSigFunction = new LuaNativeFunction (ObjectTranslator.GetMethodSignature);
			getConstructorSigFunction = new LuaNativeFunction (ObjectTranslator.GetConstructorSignature);
			ctypeFunction = new LuaNativeFunction (ObjectTranslator.CType);
			enumFromIntFunction = new LuaNativeFunction (ObjectTranslator.EnumFromInt);

			CreateLuaObjectList (luaState);
			CreateIndexingMetaFunction (luaState);
			CreateBaseClassMetatable (luaState);
			CreateClassMetatable (luaState);
			CreateFunctionMetatable (luaState);
			SetGlobalFunctions (luaState);
		}

		/*
		 * Sets up the list of objects in the Lua side
		 */
		private void CreateLuaObjectList (LuaState luaState)
		{
			LuaLib.LuaPushString (luaState, "luaNet_objects");
			LuaLib.LuaNewTable (luaState);
			LuaLib.LuaNewTable (luaState);
			LuaLib.LuaPushString (luaState, "__mode");
			LuaLib.LuaPushString (luaState, "v");
			LuaLib.LuaSetTable (luaState, -3);
			LuaLib.LuaSetMetatable (luaState, -2);
			LuaLib.LuaSetTable (luaState, (int)LuaIndexes.Registry);
		}

		/*
		 * Registers the indexing function of CLR objects
		 * passed to Lua
		 */
		private void CreateIndexingMetaFunction (LuaState luaState)
		{
			LuaLib.LuaPushString (luaState, "luaNet_indexfunction");
			LuaLib.LuaLDoString (luaState, MetaFunctions.LuaIndexFunction);
			LuaLib.LuaRawSet (luaState, (int)LuaIndexes.Registry);
		}

		/*
		 * Creates the metatable for superclasses (the base
		 * field of registered tables)
		 */
		private void CreateBaseClassMetatable (LuaState luaState)
		{
			LuaLib.LuaLNewMetatable (luaState, "luaNet_searchbase");
			LuaLib.LuaPushString (luaState, "__gc");
			LuaLib.LuaPushStdCallCFunction (luaState, metaFunctions.GcFunction);
			LuaLib.LuaSetTable (luaState, -3);
			LuaLib.LuaPushString (luaState, "__tostring");
			LuaLib.LuaPushStdCallCFunction (luaState, metaFunctions.ToStringFunction);
			LuaLib.LuaSetTable (luaState, -3);
			LuaLib.LuaPushString (luaState, "__index");
			LuaLib.LuaPushStdCallCFunction (luaState, metaFunctions.BaseIndexFunction);
			LuaLib.LuaSetTable (luaState, -3);
			LuaLib.LuaPushString (luaState, "__newindex");
			LuaLib.LuaPushStdCallCFunction (luaState, metaFunctions.NewIndexFunction);
			LuaLib.LuaSetTable (luaState, -3);
			LuaLib.LuaSetTop (luaState, -2);
		}

		/*
		 * Creates the metatable for type references
		 */
		private void CreateClassMetatable (LuaState luaState)
		{
			LuaLib.LuaLNewMetatable (luaState, "luaNet_class");
			LuaLib.LuaPushString (luaState, "__gc");
			LuaLib.LuaPushStdCallCFunction (luaState, metaFunctions.GcFunction);
			LuaLib.LuaSetTable (luaState, -3);
			LuaLib.LuaPushString (luaState, "__tostring");
			LuaLib.LuaPushStdCallCFunction (luaState, metaFunctions.ToStringFunction);
			LuaLib.LuaSetTable (luaState, -3);
			LuaLib.LuaPushString (luaState, "__index");
			LuaLib.LuaPushStdCallCFunction (luaState, metaFunctions.ClassIndexFunction);
			LuaLib.LuaSetTable (luaState, -3);
			LuaLib.LuaPushString (luaState, "__newindex");
			LuaLib.LuaPushStdCallCFunction (luaState, metaFunctions.ClassNewindexFunction);
			LuaLib.LuaSetTable (luaState, -3);
			LuaLib.LuaPushString (luaState, "__call");
			LuaLib.LuaPushStdCallCFunction (luaState, metaFunctions.CallConstructorFunction);
			LuaLib.LuaSetTable (luaState, -3);
			LuaLib.LuaSetTop (luaState, -2);
		}

		/*
		 * Registers the global functions used by NLua
		 */
		private void SetGlobalFunctions (LuaState luaState)
		{
			LuaLib.LuaPushStdCallCFunction (luaState, metaFunctions.IndexFunction);
			LuaLib.LuaSetGlobal (luaState, "get_object_member");
			LuaLib.LuaPushStdCallCFunction (luaState, importTypeFunction);
			LuaLib.LuaSetGlobal (luaState, "import_type");
			LuaLib.LuaPushStdCallCFunction (luaState, loadAssemblyFunction);
			LuaLib.LuaSetGlobal (luaState, "load_assembly");
			LuaLib.LuaPushStdCallCFunction (luaState, registerTableFunction);
			LuaLib.LuaSetGlobal (luaState, "make_object");
			LuaLib.LuaPushStdCallCFunction (luaState, unregisterTableFunction);
			LuaLib.LuaSetGlobal (luaState, "free_object");
			LuaLib.LuaPushStdCallCFunction (luaState, getMethodSigFunction);
			LuaLib.LuaSetGlobal (luaState, "get_method_bysig");
			LuaLib.LuaPushStdCallCFunction (luaState, getConstructorSigFunction);
			LuaLib.LuaSetGlobal (luaState, "get_constructor_bysig");
			LuaLib.LuaPushStdCallCFunction (luaState,ctypeFunction);
			LuaLib.LuaSetGlobal (luaState,"ctype");
			LuaLib.LuaPushStdCallCFunction (luaState,enumFromIntFunction);
			LuaLib.LuaSetGlobal(luaState,"enum");
		}

		/*
		 * Creates the metatable for delegates
		 */
		private void CreateFunctionMetatable (LuaState luaState)
		{
			LuaLib.LuaLNewMetatable (luaState, "luaNet_function");
			LuaLib.LuaPushString (luaState, "__gc");
			LuaLib.LuaPushStdCallCFunction (luaState, metaFunctions.GcFunction);
			LuaLib.LuaSetTable (luaState, -3);
			LuaLib.LuaPushString (luaState, "__call");
			LuaLib.LuaPushStdCallCFunction (luaState, metaFunctions.ExecuteDelegateFunction);
			LuaLib.LuaSetTable (luaState, -3);
			LuaLib.LuaSetTop (luaState, -2);
		}

		/*
		 * Passes errors (argument e) to the Lua interpreter
		 */
		internal void ThrowError (LuaState luaState, object e)
		{
			// We use this to remove anything pushed by luaL_where
			int oldTop = LuaLib.LuaGetTop (luaState);

			// Stack frame #1 is our C# wrapper, so not very interesting to the user
			// Stack frame #2 must be the lua code that called us, so that's what we want to use
			LuaLib.LuaLWhere (luaState, 1);
			var curlev = PopValues (luaState, oldTop);

			// Determine the position in the script where the exception was triggered
			string errLocation = string.Empty;

			if (curlev.Length > 0)
				errLocation = curlev [0].ToString ();

			string message = e as string;

			if (message != null) {
				// Wrap Lua error (just a string) and store the error location
				e = new LuaScriptException (message, errLocation);
			} else {
				var ex = e as Exception;

				if (ex != null) {
					// Wrap generic .NET exception as an InnerException and store the error location
					e = new LuaScriptException (ex, errLocation);
				}
			}

			Push (luaState, e);
			LuaLib.LuaError (luaState);
		}

		/*
		 * Implementation of load_assembly. Throws an error
		 * if the assembly is not found.
		 */
#if MONOTOUCH
		[MonoPInvokeCallback (typeof (LuaNativeFunction))]
#endif
		private static int LoadAssembly (LuaState luaState)
		{
			var translator = ObjectTranslatorPool.Instance.Find (luaState);
			return translator.LoadAssemblyInternal (luaState);
		}

		private int LoadAssemblyInternal (LuaState luaState)
		{			
			try {
				string assemblyName = LuaLib.LuaToString (luaState, 1).ToString ();
				Assembly assembly = null;
				Exception exception = null;

				try {
#if NETFX_CORE
					assembly = Assembly.Load (new AssemblyName (assemblyName));
#else
					assembly = Assembly.Load (assemblyName);
#endif
				} catch (BadImageFormatException) {
					// The assemblyName was invalid.  It is most likely a path.
				} catch (FileNotFoundException e) {
					exception = e;
				}

#if !SILVERLIGHT && !NETFX_CORE
				if (assembly == null) {
					try {
						assembly = Assembly.Load (AssemblyName.GetAssemblyName (assemblyName));
					} catch (FileNotFoundException e) {
						exception = e;
					}
					if (assembly == null) {

						AssemblyName mscor = assemblies [0].GetName ();
						AssemblyName name = new AssemblyName ();
						name.Name = assemblyName;
						name.CultureInfo = mscor.CultureInfo;
						name.Version = mscor.Version;
						name.SetPublicKeyToken (mscor.GetPublicKeyToken ());
						name.SetPublicKey (mscor.GetPublicKey ());
						assembly = Assembly.Load (name);

						if (assembly != null)
							exception = null;
					}
					if (exception != null)
						ThrowError (luaState, exception);
				}
#endif
				if (assembly != null && !assemblies.Contains (assembly))
					assemblies.Add (assembly);
			} catch (Exception e) {
				ThrowError (luaState, e);
			}

			return 0;
		}
		
		internal Type FindType (string className)
		{
			foreach (var assembly in assemblies) {
				var klass = assembly.GetType (className);

				if (klass != null)
					return klass;
			}
			return null;
		}

		public bool IsExtensionMethodPresent (Type type, string name)
		{
			return GetExtensionMethod (type, name) != null;
		}

		public MethodInfo GetExtensionMethod (Type type, string name)
		{
			return type.GetExtensionMethod (name, assemblies);
		}

		/*
		 * Implementation of import_type. Returns nil if the
		 * type is not found.
		 */
#if MONOTOUCH
		[MonoPInvokeCallback (typeof (LuaNativeFunction))]
#endif
		private static int ImportType (LuaState luaState)
		{
			var translator = ObjectTranslatorPool.Instance.Find (luaState);
			return translator.ImportTypeInternal (luaState);
		}

		private int ImportTypeInternal (LuaState luaState)
		{
			string className = LuaLib.LuaToString (luaState, 1).ToString ();
			var klass = FindType (className);

			if (klass != null)
				PushType (luaState, klass);
			else
				LuaLib.LuaPushNil (luaState);

			return 1;
		}

		/*
		 * Implementation of make_object. Registers a table (first
		 * argument in the stack) as an object subclassing the
		 * type passed as second argument in the stack.
		 */
#if MONOTOUCH
		[MonoPInvokeCallback (typeof (LuaNativeFunction))]
#endif
		private static int RegisterTable (LuaState luaState)
		{
			var translator = ObjectTranslatorPool.Instance.Find (luaState);
			return translator.RegisterTableInternal (luaState);
		}

		private int RegisterTableInternal (LuaState luaState)
		{
			if (LuaLib.LuaType (luaState, 1) == LuaTypes.Table) {
				var luaTable = GetTable (luaState, 1);
				string superclassName = LuaLib.LuaToString (luaState, 2).ToString ();

				if (superclassName != null) {
					var klass = FindType (superclassName);

					if (klass != null) {
						// Creates and pushes the object in the stack, setting
						// it as the  metatable of the first argument
						object obj = CodeGeneration.Instance.GetClassInstance (klass, luaTable);
						PushObject (luaState, obj, "luaNet_metatable");
						LuaLib.LuaNewTable (luaState);
						LuaLib.LuaPushString (luaState, "__index");
						LuaLib.LuaPushValue (luaState, -3);
						LuaLib.LuaSetTable (luaState, -3);
						LuaLib.LuaPushString (luaState, "__newindex");
						LuaLib.LuaPushValue (luaState, -3);
						LuaLib.LuaSetTable (luaState, -3);
						LuaLib.LuaSetMetatable (luaState, 1);
						// Pushes the object again, this time as the base field
						// of the table and with the luaNet_searchbase metatable
						LuaLib.LuaPushString (luaState, "base");
						int index = AddObject (obj);
						PushNewObject (luaState, obj, index, "luaNet_searchbase");
						LuaLib.LuaRawSet (luaState, 1);
					} else
						ThrowError (luaState, "register_table: can not find superclass '" + superclassName + "'");
				} else
					ThrowError (luaState, "register_table: superclass name can not be null");
			} else
				ThrowError (luaState, "register_table: first arg is not a table");

			return 0;
		}

		/*
		 * Implementation of free_object. Clears the metatable and the
		 * base field, freeing the created object for garbage-collection
		 */
#if MONOTOUCH
		[MonoPInvokeCallback (typeof (LuaNativeFunction))]
#endif
		private static int UnregisterTable (LuaState luaState)
		{
			var translator = ObjectTranslatorPool.Instance.Find (luaState);
			return translator.UnregisterTableInternal (luaState);
		}

		private int UnregisterTableInternal (LuaState luaState)
		{
			try {
				if (LuaLib.LuaGetMetatable (luaState, 1) != 0) {
					LuaLib.LuaPushString (luaState, "__index");
					LuaLib.LuaGetTable (luaState, -2);
					object obj = GetRawNetObject (luaState, -1);

					if (obj == null)
						ThrowError (luaState, "unregister_table: arg is not valid table");

					var luaTableField = obj.GetType ().GetField ("__luaInterface_luaTable");

					if (luaTableField == null)
						ThrowError (luaState, "unregister_table: arg is not valid table");

					luaTableField.SetValue (obj, null);
					LuaLib.LuaPushNil (luaState);
					LuaLib.LuaSetMetatable (luaState, 1);
					LuaLib.LuaPushString (luaState, "base");
					LuaLib.LuaPushNil (luaState);
					LuaLib.LuaSetTable (luaState, 1);
				} else
					ThrowError (luaState, "unregister_table: arg is not valid table");
			} catch (Exception e) {
				ThrowError (luaState, e.Message);
			}

			return 0;
		}

		/*
		 * Implementation of get_method_bysig. Returns nil
		 * if no matching method is not found.
		 */
#if MONOTOUCH
		[MonoPInvokeCallback (typeof (LuaNativeFunction))]
#endif
		private static int GetMethodSignature (LuaState luaState)
		{
			var translator = ObjectTranslatorPool.Instance.Find (luaState);
			return translator.GetMethodSignatureInternal (luaState);
		}

		private int GetMethodSignatureInternal (LuaState luaState)
		{
			ProxyType klass;
			object target;
			int udata = LuaLib.LuaNetCheckUData (luaState, 1, "luaNet_class");

			if (udata != -1) {
				klass = (ProxyType)objects [udata];
				target = null;
			} else {
				target = GetRawNetObject (luaState, 1);

				if (target == null) {
					ThrowError (luaState, "get_method_bysig: first arg is not type or object reference");
					LuaLib.LuaPushNil (luaState);
					return 1;
				}

				klass = new ProxyType(target.GetType ());
			}

			string methodName = LuaLib.LuaToString (luaState, 2).ToString ();
			var signature = new Type[LuaLib.LuaGetTop (luaState) - 2];

			for (int i = 0; i < signature.Length; i++)
				signature [i] = FindType (LuaLib.LuaToString (luaState, i + 3).ToString ());

			try {
				var method = klass.GetMethod (methodName, BindingFlags.Public | BindingFlags.Static |
					BindingFlags.Instance, signature);
				PushFunction (luaState, new LuaNativeFunction ((new LuaMethodWrapper (this, target, klass, method)).invokeFunction));
			} catch (Exception e) {
				ThrowError (luaState, e);
				LuaLib.LuaPushNil (luaState);
			}

			return 1;
		}

		/*
		 * Implementation of get_constructor_bysig. Returns nil
		 * if no matching constructor is found.
		 */
#if MONOTOUCH
		[MonoPInvokeCallback (typeof (LuaNativeFunction))]
#endif
		private static int GetConstructorSignature (LuaState luaState)
		{
			var translator = ObjectTranslatorPool.Instance.Find (luaState);
			return translator.GetConstructorSignatureInternal (luaState);
		}

		private int GetConstructorSignatureInternal (LuaState luaState)
		{
			ProxyType klass = null;
			int udata = LuaLib.LuaNetCheckUData (luaState, 1, "luaNet_class");

			if (udata != -1)
				klass = (ProxyType)objects [udata];

			if (klass == null)
				ThrowError (luaState, "get_constructor_bysig: first arg is invalid type reference");

			var signature = new Type[LuaLib.LuaGetTop (luaState) - 1];

			for (int i = 0; i < signature.Length; i++)
				signature [i] = FindType (LuaLib.LuaToString (luaState, i + 2).ToString ());

			try {
				ConstructorInfo constructor = klass.UnderlyingSystemType.GetConstructor (signature);
				PushFunction (luaState, new LuaNativeFunction ((new LuaMethodWrapper (this, null, klass, constructor)).invokeFunction));
			} catch (Exception e) {
				ThrowError (luaState, e);
				LuaLib.LuaPushNil (luaState);
			}

			return 1;
		}

		/*
		 * Pushes a type reference into the stack
		 */
		internal void PushType (LuaState luaState, Type t)
		{
			PushObject (luaState, new ProxyType (t), "luaNet_class");
		}

		/*
		 * Pushes a delegate into the stack
		 */
		internal void PushFunction (LuaState luaState, LuaNativeFunction func)
		{
			PushObject (luaState, func, "luaNet_function");
		}


		/*
		 * Pushes a CLR object into the Lua stack as an userdata
		 * with the provided metatable
		 */
		internal void PushObject (LuaState luaState, object o, string metatable)
		{
			int index = -1;

			// Pushes nil
			if (o == null) {
				LuaLib.LuaPushNil (luaState);
				return;
			}

			// Object already in the list of Lua objects? Push the stored reference.
#if NETFX_CORE
			bool found = (!o.GetType().GetTypeInfo().IsValueType || o.GetType().GetTypeInfo().IsEnum) && objectsBackMap.TryGetValue (o, out index);
#else
            bool found = (!o.GetType().IsValueType || o.GetType().IsEnum) && objectsBackMap.TryGetValue(o, out index);
#endif

			if (found) {
				LuaLib.LuaLGetMetatable (luaState, "luaNet_objects");
				LuaLib.LuaRawGetI (luaState, -1, index);

				// Note: starting with lua5.1 the garbage collector may remove weak reference items (such as our luaNet_objects values) when the initial GC sweep 
				// occurs, but the actual call of the __gc finalizer for that object may not happen until a little while later.  During that window we might call
				// this routine and find the element missing from luaNet_objects, but collectObject() has not yet been called.  In that case, we go ahead and call collect
				// object here
				// did we find a non nil object in our table? if not, we need to call collect object
				var type = LuaLib.LuaType (luaState, -1);
				if (type != LuaTypes.Nil) {
					LuaLib.LuaRemove (luaState, -2);	 // drop the metatable - we're going to leave our object on the stack
					return;
				}

				// MetaFunctions.dumpStack(this, luaState);
				LuaLib.LuaRemove (luaState, -1);	// remove the nil object value
				LuaLib.LuaRemove (luaState, -1);	// remove the metatable
				CollectObject (o, index);			// Remove from both our tables and fall out to get a new ID
			}

			index = AddObject (o);
			PushNewObject (luaState, o, index, metatable);
		}

		/*
		 * Pushes a new object into the Lua stack with the provided
		 * metatable
		 */
		private void PushNewObject (LuaState luaState, object o, int index, string metatable)
		{
			if (metatable == "luaNet_metatable") {
				// Gets or creates the metatable for the object's type
				LuaLib.LuaLGetMetatable (luaState, o.GetType ().AssemblyQualifiedName);

				if (LuaLib.LuaIsNil (luaState, -1)) {
					LuaLib.LuaSetTop (luaState, -2);
					LuaLib.LuaLNewMetatable (luaState, o.GetType ().AssemblyQualifiedName);
					LuaLib.LuaPushString (luaState, "cache");
					LuaLib.LuaNewTable (luaState);
					LuaLib.LuaRawSet (luaState, -3);
					LuaLib.LuaPushLightUserData (luaState, LuaLib.LuaNetGetTag ());
					LuaLib.LuaPushNumber (luaState, 1);
					LuaLib.LuaRawSet (luaState, -3);
					LuaLib.LuaPushString (luaState, "__index");
					LuaLib.LuaPushString (luaState, "luaNet_indexfunction");
					LuaLib.LuaRawGet (luaState, (int)LuaIndexes.Registry);
					LuaLib.LuaRawSet (luaState, -3);
					LuaLib.LuaPushString (luaState, "__gc");
					LuaLib.LuaPushStdCallCFunction (luaState, metaFunctions.GcFunction);
					LuaLib.LuaRawSet (luaState, -3);
					LuaLib.LuaPushString (luaState, "__tostring");
					LuaLib.LuaPushStdCallCFunction (luaState, metaFunctions.ToStringFunction);
					LuaLib.LuaRawSet (luaState, -3);
					LuaLib.LuaPushString (luaState, "__newindex");
					LuaLib.LuaPushStdCallCFunction (luaState, metaFunctions.NewIndexFunction);
					LuaLib.LuaRawSet (luaState, -3);
					// Bind C# operator with Lua metamethods (__add, __sub, __mul)
					RegisterOperatorsFunctions (luaState, o.GetType ());
					RegisterCallMethodForDelegate (luaState, o);
				}
			} else
				LuaLib.LuaLGetMetatable (luaState, metatable);

			// Stores the object index in the Lua list and pushes the
			// index into the Lua stack
			LuaLib.LuaLGetMetatable (luaState, "luaNet_objects");
			LuaLib.LuaNetNewUData (luaState, index);
			LuaLib.LuaPushValue (luaState, -3);
			LuaLib.LuaRemove (luaState, -4);
			LuaLib.LuaSetMetatable (luaState, -2);
			LuaLib.LuaPushValue (luaState, -1);
			LuaLib.LuaRawSetI (luaState, -3, index);
			LuaLib.LuaRemove (luaState, -2);
		}

		void RegisterCallMethodForDelegate (LuaState luaState, object o)
		{
			if (!(o is Delegate))
				return;

			LuaLib.LuaPushString (luaState, "__call");
			LuaLib.LuaPushStdCallCFunction (luaState, metaFunctions.CallDelegateFunction);
			LuaLib.LuaRawSet (luaState, -3);
		}

		void RegisterOperatorsFunctions (LuaState luaState, Type type)
		{
			if (type.HasAdditionOpertator ()) {
				LuaLib.LuaPushString (luaState, "__add");
				LuaLib.LuaPushStdCallCFunction (luaState, metaFunctions.AddFunction);
				LuaLib.LuaRawSet (luaState, -3);
			}
			if (type.HasSubtractionOpertator ()) {
				LuaLib.LuaPushString (luaState, "__sub");
				LuaLib.LuaPushStdCallCFunction (luaState, metaFunctions.SubtractFunction);
				LuaLib.LuaRawSet (luaState, -3);
			}
			if (type.HasMultiplyOpertator ()) {
				LuaLib.LuaPushString (luaState, "__mul");
				LuaLib.LuaPushStdCallCFunction (luaState, metaFunctions.MultiplyFunction);
				LuaLib.LuaRawSet (luaState, -3);
			}
			if (type.HasDivisionOpertator ()) {
				LuaLib.LuaPushString (luaState, "__div");
				LuaLib.LuaPushStdCallCFunction (luaState, metaFunctions.DivisionFunction);
				LuaLib.LuaRawSet (luaState, -3);
			}
			if (type.HasModulusOpertator ()) {
				LuaLib.LuaPushString (luaState, "__mod");
				LuaLib.LuaPushStdCallCFunction (luaState, metaFunctions.ModulosFunction);
				LuaLib.LuaRawSet (luaState, -3);
			}
			if (type.HasUnaryNegationOpertator ()) {
				LuaLib.LuaPushString (luaState, "__unm");
				LuaLib.LuaPushStdCallCFunction (luaState, metaFunctions.UnaryNegationFunction);
				LuaLib.LuaRawSet (luaState, -3);
			}
			if (type.HasEqualityOpertator ()) {
				LuaLib.LuaPushString (luaState, "__eq");
				LuaLib.LuaPushStdCallCFunction (luaState, metaFunctions.EqualFunction);
				LuaLib.LuaRawSet (luaState, -3);
			}
			if (type.HasLessThanOpertator ()) {
				LuaLib.LuaPushString (luaState, "__lt");
				LuaLib.LuaPushStdCallCFunction (luaState, metaFunctions.LessThanFunction);
				LuaLib.LuaRawSet (luaState, -3);
			}
			if (type.HasLessThanOrEqualOpertator ()) {
				LuaLib.LuaPushString (luaState, "__le");
				LuaLib.LuaPushStdCallCFunction (luaState, metaFunctions.LessThanOrEqualFunction);
				LuaLib.LuaRawSet (luaState, -3);
			}
		}

		/*
		 * Gets an object from the Lua stack with the desired type, if it matches, otherwise
		 * returns null.
		 */
		internal object GetAsType (LuaState luaState, int stackPos, Type paramType)
		{
			var extractor = typeChecker.CheckLuaType (luaState, stackPos, paramType);
			return extractor != null ? extractor (luaState, stackPos) : null;
		}

		/// <summary>
		/// Given the Lua int ID for an object remove it from our maps
		/// </summary>
		/// <param name = "udata"></param>
		internal void CollectObject (int udata)
		{
			object o;
			bool found = objects.TryGetValue (udata, out o);

			// The other variant of collectObject might have gotten here first, in that case we will silently ignore the missing entry
			if (found) 
				CollectObject (o, udata);
		}

		/// <summary>
		/// Given an object reference, remove it from our maps
		/// </summary>
		/// <param name = "udata"></param>
		private void CollectObject (object o, int udata)
		{
			objects.Remove (udata);
#if NETFX_CORE
			if (!o.GetType ().GetTypeInfo ().IsValueType || o.GetType().GetTypeInfo().IsEnum)
#else
			if (!o.GetType ().IsValueType || o.GetType().IsEnum)
#endif
				objectsBackMap.Remove (o);
		}

		private int AddObject (object obj)
		{
			// New object: inserts it in the list
			int index = nextObj++;
			objects [index] = obj;
#if NETFX_CORE
			if (!obj.GetType ().GetTypeInfo().IsValueType || obj.GetType ().GetTypeInfo().IsValueType)
#else
            if (!obj.GetType().IsValueType || obj.GetType().IsValueType)
#endif

			objectsBackMap [obj] = index;
			return index;
		}

		/*
		 * Gets an object from the Lua stack according to its Lua type.
		 */
		internal object GetObject (LuaState luaState, int index)
		{
			var type = LuaLib.LuaType (luaState, index);

			switch (type) {
			case LuaTypes.Number:
				{
					return LuaLib.LuaToNumber (luaState, index);
				}
			case LuaTypes.String: 
				{
					return LuaLib.LuaToString (luaState, index);
				}
			case LuaTypes.Boolean:
				{
					return LuaLib.LuaToBoolean (luaState, index);
				}
			case LuaTypes.Table: 
				{
					return GetTable (luaState, index);
				}
			case LuaTypes.Function:
				{
					return GetFunction (luaState, index);
				}
			case LuaTypes.UserData:
				{
					int udata = LuaLib.LuaNetToNetObject (luaState, index);
					return udata != -1 ? objects [udata] : GetUserData (luaState, index);
				}
			default:
				return null;
			}
		}

		/*
		 * Gets the table in the index positon of the Lua stack.
		 */
		internal LuaTable GetTable (LuaState luaState, int index)
		{
			LuaLib.LuaPushValue (luaState, index);
			int reference = LuaLib.LuaRef (luaState, 1);
			if (reference == -1)
				return null;
			return new LuaTable (reference, interpreter);
		}

		/*
		 * Gets the userdata in the index positon of the Lua stack.
		 */
		internal LuaUserData GetUserData (LuaState luaState, int index)
		{
			LuaLib.LuaPushValue (luaState, index);
			int reference = LuaLib.LuaRef (luaState, 1);
			if (reference == -1)
				return null;
			return new LuaUserData(reference, interpreter);
		}

		/*
		 * Gets the function in the index positon of the Lua stack.
		 */
		internal LuaFunction GetFunction (LuaState luaState, int index)
		{
			LuaLib.LuaPushValue (luaState, index);
			int reference = LuaLib.LuaRef (luaState, 1);
			if (reference == -1)
				return null;
			return new LuaFunction (reference, interpreter);
		}

		/*
		 * Gets the CLR object in the index positon of the Lua stack. Returns
		 * delegates as Lua functions.
		 */
		internal object GetNetObject (LuaState luaState, int index)
		{
			int idx = LuaLib.LuaNetToNetObject (luaState, index);
			return idx != -1 ? objects [idx] : null;
		}

		/*
		 * Gets the CLR object in the index position of the Lua stack. Returns
		 * delegates as is.
		 */
		internal object GetRawNetObject (LuaState luaState, int index)
		{
			int udata = LuaLib.LuaNetRawNetObj (luaState, index);
			return udata != -1 ? objects [udata] : null;
		}


		/*
		 * Gets the values from the provided index to
		 * the top of the stack and returns them in an array.
		 */
		internal object[] PopValues (LuaState luaState, int oldTop)
		{
			int newTop = LuaLib.LuaGetTop (luaState);

			if (oldTop == newTop)
				return null;
			else {
				var returnValues = new List<object> ();
				for (int i = oldTop+1; i <= newTop; i++)
					returnValues.Add (GetObject (luaState, i));

				LuaLib.LuaSetTop (luaState, oldTop);
				return returnValues.ToArray ();
			}
		}

		/*
		 * Gets the values from the provided index to
		 * the top of the stack and returns them in an array, casting
		 * them to the provided types.
		 */
		internal object[] PopValues (LuaState luaState, int oldTop, Type[] popTypes)
		{
			int newTop = LuaLib.LuaGetTop (luaState);

			if (oldTop == newTop)
				return null;
			else {
				int iTypes;
				var returnValues = new List<object> ();

				if (popTypes [0] == typeof(void))
					iTypes = 1;
				else
					iTypes = 0;

				for (int i = oldTop+1; i <= newTop; i++) {
					returnValues.Add (GetAsType (luaState, i, popTypes [iTypes]));
					iTypes++;
				}

				LuaLib.LuaSetTop (luaState, oldTop);
				return returnValues.ToArray ();
			}
		}

		// kevinh - the following line doesn't work for remoting proxies - they always return a match for 'is'
		// else if(o is ILuaGeneratedType)
		private static bool IsILua (object o)
		{
			if (o is ILuaGeneratedType) {
				// Make sure we are _really_ ILuaGenerated
				var typ = o.GetType ();
#if NETFX_CORE
				return typ.ImplementInterface ("ILuaGeneratedType");
#else
				return typ.GetInterface ("ILuaGeneratedType", true) != null;
#endif
			} 
			return false;
		}

		/*
		 * Pushes the object into the Lua stack according to its type.
		 */
		internal void Push (LuaState luaState, object o)
		{
			if (o == null)
				LuaLib.LuaPushNil (luaState);
			else if (o is sbyte || o is byte || o is short || o is ushort ||
			         o is int || o is uint || o is long || o is float ||
			         o is ulong || o is decimal || o is double) {
				double d = Convert.ToDouble (o);
				LuaLib.LuaPushNumber (luaState, d);
			} else if (o is char) {
				double d = (char)o;
				LuaLib.LuaPushNumber (luaState, d);
			} else if (o is string) {
				string str = (string)o;
				LuaLib.LuaPushString (luaState, str);
			} else if (o is bool) {
				bool b = (bool)o;
				LuaLib.LuaPushBoolean (luaState, b);
			} else if (IsILua (o))
				(((ILuaGeneratedType)o).LuaInterfaceGetLuaTable ()).Push (luaState);
			else if (o is LuaTable)
				((LuaTable)o).Push (luaState);
			else if (o is LuaNativeFunction)
				PushFunction (luaState, (LuaNativeFunction)o);
			else if (o is LuaFunction)
				((LuaFunction)o).Push (luaState);
			else
				PushObject (luaState, o, "luaNet_metatable");
		}

		/*
		 * Checks if the method matches the arguments in the Lua stack, getting
		 * the arguments if it does.
		 */
		internal bool MatchParameters (LuaState luaState, MethodBase method, ref MethodCache methodCache)
		{
			return metaFunctions.MatchParameters (luaState, method, ref methodCache);
		}
		
		internal Array TableToArray(Func<int, object> luaParamValue, Type paramArrayType, int startIndex, int count) {
			return metaFunctions.TableToArray(luaParamValue,paramArrayType, startIndex, count);
		}

		private Type TypeOf (LuaState luaState, int idx)
		{
			int udata = LuaLib.LuaNetCheckUData (luaState, 1, "luaNet_class");
			if (udata == -1)
				return null;
			
			ProxyType pt = (ProxyType)objects [udata];
			return pt.UnderlyingSystemType;
		}

		static int PushError (LuaState luaState, string msg)
		{
			LuaLib.LuaPushNil (luaState);
			LuaLib.LuaPushString (luaState, msg);
			return 2;
		}

#if MONOTOUCH
		[MonoPInvokeCallback (typeof (LuaNativeFunction))]
#endif
		private static int CType (LuaState luaState)
		{
			var translator = ObjectTranslatorPool.Instance.Find (luaState);
			return translator.CTypeInternal (luaState);
		}

		int CTypeInternal (LuaState luaState)
		{
			Type t = TypeOf (luaState, 1);
			if (t == null)
				return PushError (luaState, "Not a CLR Class");

			PushObject (luaState, t, "luaNet_metatable");
			return 1;
		}

#if MONOTOUCH
		[MonoPInvokeCallback (typeof (LuaNativeFunction))]
#endif
		private static int EnumFromInt (LuaState luaState)
		{
			var translator = ObjectTranslatorPool.Instance.Find (luaState);
			return translator.EnumFromIntInternal (luaState);
		}

		int EnumFromIntInternal (LuaState luaState)
		{
			Type t = TypeOf (luaState, 1);
			if (t == null || !t.IsEnum ())
				return PushError (luaState, "Not an Enum.");

			object res = null;
			LuaTypes lt = LuaLib.LuaType (luaState, 2);
			if (lt == LuaTypes.Number) {
				int ival = (int)LuaLib.LuaToNumber (luaState, 2);
				res = Enum.ToObject (t, ival);
			} else
				if (lt == LuaTypes.String) {
					string sflags = LuaLib.LuaToString (luaState, 2);
					string err = null;
					try {
						res = Enum.Parse (t, sflags, true);
					} catch (ArgumentException e) {
						err = e.Message;
					}
					if (err != null)
						return PushError (luaState, err);
				} else {
					return PushError (luaState, "Second argument must be a integer or a string.");
				}
			PushObject (luaState, res, "luaNet_metatable");
			return 1;
		}
	}
}