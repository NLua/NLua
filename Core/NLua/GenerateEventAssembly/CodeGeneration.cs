/*
 * This file is part of NLua.
 * 
 * Copyright (c) 2013 Vinicius Jarina (viniciusjarina@gmail.com)
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
using System.Threading;
using System.Reflection;

#if !MONOTOUCH
using System.Reflection.Emit;
#endif
using System.Collections;
using System.Collections.Generic;
using NLua.Method;

namespace NLua
{
	/*
	 * Dynamically generates new types from existing types and
	 * Lua function and table values. Generated types are event handlers, 
	 * delegates, interface implementations and subclasses.
	 * 
	 * Author: Fabio Mascarenhas
	 * Version: 1.0
	 */
	class CodeGeneration
	{
		private Dictionary<Type, LuaClassType> classCollection = new Dictionary<Type, LuaClassType> ();
		private Dictionary<Type, Type> delegateCollection = new Dictionary<Type, Type> ();
		private static readonly CodeGeneration instance = new CodeGeneration ();
		private AssemblyName assemblyName;

#if !MONOTOUCH && !SILVERLIGHT
		private Dictionary<Type, Type> eventHandlerCollection = new Dictionary<Type, Type> ();
		private Type eventHandlerParent = typeof(LuaEventHandler);
		private Type delegateParent = typeof(LuaDelegate);
		private Type classHelper = typeof(LuaClassHelper);
		private AssemblyBuilder newAssembly;
		private ModuleBuilder newModule;
		private int luaClassNumber = 1;
#endif

		static CodeGeneration ()
		{
		}

		private CodeGeneration ()
		{



			// Create an assembly name
			assemblyName = new AssemblyName ();
			assemblyName.Name = "NLua_generatedcode";
			// Create a new assembly with one module.
#if !MONOTOUCH && !SILVERLIGHT
			newAssembly = Thread.GetDomain ().DefineDynamicAssembly (assemblyName, AssemblyBuilderAccess.Run);
			newModule = newAssembly.DefineDynamicModule ("NLua_generatedcode");
#endif

		}

		/*
		 * Singleton instance of the class
		 */
		public static CodeGeneration Instance {
			get { return instance; }
		}
		
		/*
		 *  Generates an event handler that calls a Lua function
		 */
		private Type GenerateEvent (Type eventHandlerType)
		{
#if MONOTOUCH
			throw new NotImplementedException (" Emit not available on MonoTouch ");
#elif SILVERLIGHT
			throw new NotImplementedException(" Emit not available on Silverlight ");
#else
			string typeName;
			lock (this) {
				typeName = "LuaGeneratedClass" + luaClassNumber;
				luaClassNumber++;
			}

			// Define a public class in the assembly, called typeName
			var myType = newModule.DefineType (typeName, TypeAttributes.Public, eventHandlerParent);

			// Defines the handler method. Its signature is void(object, <subclassofEventArgs>)
			var paramTypes = new Type[2];
			paramTypes [0] = typeof(object);
			paramTypes [1] = eventHandlerType;
			var returnType = typeof(void);
			var handleMethod = myType.DefineMethod ("HandleEvent", MethodAttributes.Public | MethodAttributes.HideBySig, returnType, paramTypes);

			// Emits the IL for the method. It loads the arguments
			// and calls the handleEvent method of the base class
			ILGenerator generator = handleMethod.GetILGenerator ();
			generator.Emit (OpCodes.Ldarg_0);
			generator.Emit (OpCodes.Ldarg_1);
			generator.Emit (OpCodes.Ldarg_2);
			var miGenericEventHandler = eventHandlerParent.GetMethod ("handleEvent");
			generator.Emit (OpCodes.Call, miGenericEventHandler);
			// returns
			generator.Emit (OpCodes.Ret);
			// creates the new type
			return myType.CreateType ();
#endif
		}

		/*
		 * Generates a type that can be used for instantiating a delegate
		 * of the provided type, given a Lua function.
		 */
		private Type GenerateDelegate (Type delegateType)
		{
#if MONOTOUCH
			throw new NotImplementedException ("GenerateDelegate is not available on iOS, please register your LuaDelegate type with Lua.RegisterLuaDelegateType( yourDelegate, theLuaDelegateHandler) ");
#elif SILVERLIGHT
			throw new NotImplementedException("GenerateDelegate is not available on Silverlight, please register your LuaDelegate type with Lua.RegisterLuaDelegateType( yourDelegate, theLuaDelegateHandler) ");
#else
			string typeName;
			lock (this) {
				typeName = "LuaGeneratedClass" + luaClassNumber;
				luaClassNumber++;
			}

			// Define a public class in the assembly, called typeName
			var myType = newModule.DefineType (typeName, TypeAttributes.Public, delegateParent);

			// Defines the delegate method with the same signature as the
			// Invoke method of delegateType
			var invokeMethod = delegateType.GetMethod ("Invoke");
			var paramInfo = invokeMethod.GetParameters ();
			var paramTypes = new Type[paramInfo.Length];
			var returnType = invokeMethod.ReturnType;

			// Counts out and ref params, for use later
			int nOutParams = 0;
			int nOutAndRefParams = 0;

			for (int i = 0; i < paramTypes.Length; i++) {
				paramTypes [i] = paramInfo [i].ParameterType;

				if ((!paramInfo [i].IsIn) && paramInfo [i].IsOut)
					nOutParams++;

				if (paramTypes [i].IsByRef)
					nOutAndRefParams++;
			}

			int[] refArgs = new int[nOutAndRefParams];
			var delegateMethod = myType.DefineMethod ("CallFunction", invokeMethod.Attributes, returnType, paramTypes);

			// Generates the IL for the method
			ILGenerator generator = delegateMethod.GetILGenerator ();
			generator.DeclareLocal (typeof(object[])); // original arguments
			generator.DeclareLocal (typeof(object[])); // with out-only arguments removed
			generator.DeclareLocal (typeof(int[])); // indexes of out and ref arguments

			if (!(returnType == typeof(void)))  // return value
				generator.DeclareLocal (returnType);
			else
				generator.DeclareLocal (typeof(object));

			// Initializes local variables
			generator.Emit (OpCodes.Ldc_I4, paramTypes.Length);
			generator.Emit (OpCodes.Newarr, typeof(object));
			generator.Emit (OpCodes.Stloc_0);
			generator.Emit (OpCodes.Ldc_I4, paramTypes.Length - nOutParams);
			generator.Emit (OpCodes.Newarr, typeof(object));
			generator.Emit (OpCodes.Stloc_1);
			generator.Emit (OpCodes.Ldc_I4, nOutAndRefParams);
			generator.Emit (OpCodes.Newarr, typeof(int));
			generator.Emit (OpCodes.Stloc_2);

			// Stores the arguments in the local variables
			for (int iArgs = 0, iInArgs = 0, iOutArgs = 0; iArgs < paramTypes.Length; iArgs++) {
				generator.Emit (OpCodes.Ldloc_0);
				generator.Emit (OpCodes.Ldc_I4, iArgs);
				generator.Emit (OpCodes.Ldarg, iArgs + 1);

				if (paramTypes [iArgs].IsByRef) {
					if (paramTypes [iArgs].GetElementType ().IsValueType) {
						generator.Emit (OpCodes.Ldobj, paramTypes [iArgs].GetElementType ());
						generator.Emit (OpCodes.Box, paramTypes [iArgs].GetElementType ());
					} else
						generator.Emit (OpCodes.Ldind_Ref);
				} else {
					if (paramTypes [iArgs].IsValueType)
						generator.Emit (OpCodes.Box, paramTypes [iArgs]);
				}

				generator.Emit (OpCodes.Stelem_Ref);

				if (paramTypes [iArgs].IsByRef) {
					generator.Emit (OpCodes.Ldloc_2);
					generator.Emit (OpCodes.Ldc_I4, iOutArgs);
					generator.Emit (OpCodes.Ldc_I4, iArgs);
					generator.Emit (OpCodes.Stelem_I4);
					refArgs [iOutArgs] = iArgs;
					iOutArgs++;
				}

				if (paramInfo [iArgs].IsIn || (!paramInfo [iArgs].IsOut)) {
					generator.Emit (OpCodes.Ldloc_1);
					generator.Emit (OpCodes.Ldc_I4, iInArgs);
					generator.Emit (OpCodes.Ldarg, iArgs + 1);

					if (paramTypes [iArgs].IsByRef) {
						if (paramTypes [iArgs].GetElementType ().IsValueType) {
							generator.Emit (OpCodes.Ldobj, paramTypes [iArgs].GetElementType ());
							generator.Emit (OpCodes.Box, paramTypes [iArgs].GetElementType ());
						} else
							generator.Emit (OpCodes.Ldind_Ref);
					} else {
						if (paramTypes [iArgs].IsValueType)
							generator.Emit (OpCodes.Box, paramTypes [iArgs]);
					}

					generator.Emit (OpCodes.Stelem_Ref);
					iInArgs++;
				} 
			}

			// Calls the callFunction method of the base class
			generator.Emit (OpCodes.Ldarg_0);
			generator.Emit (OpCodes.Ldloc_0);
			generator.Emit (OpCodes.Ldloc_1);
			generator.Emit (OpCodes.Ldloc_2);
			var miGenericEventHandler = delegateParent.GetMethod ("callFunction");
			generator.Emit (OpCodes.Call, miGenericEventHandler);

			// Stores return value
			if (returnType == typeof(void)) {
				generator.Emit (OpCodes.Pop);
				generator.Emit (OpCodes.Ldnull);
			} else if (returnType.IsValueType) {
				generator.Emit (OpCodes.Unbox, returnType);
				generator.Emit (OpCodes.Ldobj, returnType);
			} else
				generator.Emit (OpCodes.Castclass, returnType);

			generator.Emit (OpCodes.Stloc_3);

			// Stores new value of out and ref params
			for (int i = 0; i < refArgs.Length; i++) {
				generator.Emit (OpCodes.Ldarg, refArgs [i] + 1);
				generator.Emit (OpCodes.Ldloc_0);
				generator.Emit (OpCodes.Ldc_I4, refArgs [i]);
				generator.Emit (OpCodes.Ldelem_Ref);

				if (paramTypes [refArgs [i]].GetElementType ().IsValueType) {
					generator.Emit (OpCodes.Unbox, paramTypes [refArgs [i]].GetElementType ());
					generator.Emit (OpCodes.Ldobj, paramTypes [refArgs [i]].GetElementType ());
					generator.Emit (OpCodes.Stobj, paramTypes [refArgs [i]].GetElementType ());
				} else {
					generator.Emit (OpCodes.Castclass, paramTypes [refArgs [i]].GetElementType ());
					generator.Emit (OpCodes.Stind_Ref);
				}
			}

			// Returns
			if (!(returnType == typeof(void)))
				generator.Emit (OpCodes.Ldloc_3);

			generator.Emit (OpCodes.Ret);
			return myType.CreateType (); // creates the new type
#endif
		}

		void GetReturnTypesFromClass (Type klass, out Type[][] returnTypes)
		{
			var classMethods = klass.GetMethods ();
			returnTypes = new Type[classMethods.Length][];

			int i = 0;

			foreach (var method in classMethods) {
				if (klass.IsInterface) {
					GetReturnTypesFromMethod (method, out returnTypes [i]);
					i++;
				} else {
					if (!method.IsPrivate && !method.IsFinal && method.IsVirtual) {
						GetReturnTypesFromMethod (method, out returnTypes [i]);
						i++;
					}
				}
			}
		}

		/*
		 * Generates an implementation of klass, if it is an interface, or
		 * a subclass of klass that delegates its virtual methods to a Lua table.
		 */
		public void GenerateClass (Type klass, out Type newType, out Type[][] returnTypes)
		{
#if MONOTOUCH
			throw new NotImplementedException (" Emit not available on MonoTouch ");
#elif SILVERLIGHT
			throw new NotImplementedException (" Emit not available on Silverlight ");
#else
			string typeName;
			lock (this) {
				typeName = "LuaGeneratedClass" + luaClassNumber;
				luaClassNumber++;
			}

			TypeBuilder myType;
			// Define a public class in the assembly, called typeName
			if (klass.IsInterface)
				myType = newModule.DefineType (typeName, TypeAttributes.Public, typeof(object), new Type[] {
					klass,
					typeof(ILuaGeneratedType)
				});
			else
				myType = newModule.DefineType (typeName, TypeAttributes.Public, klass, new Type[] { typeof(ILuaGeneratedType) });
			
			// Field that stores the Lua table
			var luaTableField = myType.DefineField ("__luaInterface_luaTable", typeof(LuaTable), FieldAttributes.Public);
			// Field that stores the return types array
			var returnTypesField = myType.DefineField ("__luaInterface_returnTypes", typeof(Type[][]), FieldAttributes.Public);
			// Generates the constructor for the new type, it takes a Lua table and an array
			// of return types and stores them in the respective fields
			var constructor = myType.DefineConstructor (MethodAttributes.Public, CallingConventions.Standard, new Type[] {
				typeof(LuaTable),
				typeof(Type[][])
			});
			ILGenerator generator = constructor.GetILGenerator ();
			generator.Emit (OpCodes.Ldarg_0);

			if (klass.IsInterface)
				generator.Emit (OpCodes.Call, typeof(object).GetConstructor (Type.EmptyTypes));
			else
				generator.Emit (OpCodes.Call, klass.GetConstructor (Type.EmptyTypes));

			generator.Emit (OpCodes.Ldarg_0);
			generator.Emit (OpCodes.Ldarg_1);
			generator.Emit (OpCodes.Stfld, luaTableField);
			generator.Emit (OpCodes.Ldarg_0);
			generator.Emit (OpCodes.Ldarg_2);
			generator.Emit (OpCodes.Stfld, returnTypesField);
			generator.Emit (OpCodes.Ret);
			// Generates overriden versions of the klass' public virtual methods
			var classMethods = klass.GetMethods ();
			returnTypes = new Type[classMethods.Length][];
			int i = 0;

			foreach (var method in classMethods) {
				if (klass.IsInterface) {
					GenerateMethod (myType, method, MethodAttributes.HideBySig | MethodAttributes.Virtual | MethodAttributes.NewSlot, 
						i, luaTableField, returnTypesField, false, out returnTypes [i]);
					i++;
				} else {
					if (!method.IsPrivate && !method.IsFinal && method.IsVirtual) {
						GenerateMethod (myType, method, (method.Attributes | MethodAttributes.NewSlot) ^ MethodAttributes.NewSlot, i, 
							luaTableField, returnTypesField, true, out returnTypes [i]);
						i++;
					}
				}
			}

			// Generates an implementation of the luaInterfaceGetLuaTable method
			var returnTableMethod = myType.DefineMethod ("LuaInterfaceGetLuaTable", 
				MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual, typeof(LuaTable), new Type[0]);
			myType.DefineMethodOverride (returnTableMethod, typeof(ILuaGeneratedType).GetMethod ("LuaInterfaceGetLuaTable"));
			generator = returnTableMethod.GetILGenerator ();
			generator.Emit (OpCodes.Ldfld, luaTableField);
			generator.Emit (OpCodes.Ret);
			newType = myType.CreateType (); // Creates the type
#endif
		}

		void GetReturnTypesFromMethod (MethodInfo method, out Type[] returnTypes)
		{
			var paramInfo = method.GetParameters ();
			var paramTypes = new Type[paramInfo.Length];
			var returnTypesList = new List<Type> ();
			
			// Counts out and ref parameters, for later use, 
			// and creates the list of return types
			int nOutParams = 0;
			int nOutAndRefParams = 0;
			var returnType = method.ReturnType;
			returnTypesList.Add (returnType);
			
			for (int i = 0; i < paramTypes.Length; i++) {
				paramTypes [i] = paramInfo [i].ParameterType;
#if SILVERLIGHT
				if (paramInfo[i].IsOut) {
#else
				if ((!paramInfo [i].IsIn) && paramInfo [i].IsOut) {
#endif
					nOutParams++;
				}
				
				if (paramTypes [i].IsByRef) {
					returnTypesList.Add (paramTypes [i].GetElementType ());
					nOutAndRefParams++;
				}
			}

			returnTypes = returnTypesList.ToArray ();
		}

#if !MONOTOUCH && !SILVERLIGHT

		/*
		 * Generates an overriden implementation of method inside myType that delegates
		 * to a function in a Lua table with the same name, if the function exists. If it
		 * doesn't the method calls the base method (or does nothing, in case of interface
		 * implementations).
		 */
		private void GenerateMethod (TypeBuilder myType, MethodInfo method, MethodAttributes attributes, int methodIndex,
			FieldInfo luaTableField, FieldInfo returnTypesField, bool generateBase, out Type[] returnTypes)
		{
			var paramInfo = method.GetParameters ();
			var paramTypes = new Type[paramInfo.Length];
			var returnTypesList = new List<Type> ();
			
			// Counts out and ref parameters, for later use, 
			// and creates the list of return types
			int nOutParams = 0;
			int nOutAndRefParams = 0;
			var returnType = method.ReturnType;
			returnTypesList.Add (returnType);
			
			for (int i = 0; i < paramTypes.Length; i++) {
				paramTypes [i] = paramInfo [i].ParameterType;
				if ((!paramInfo [i].IsIn) && paramInfo [i].IsOut)
					nOutParams++;
				
				if (paramTypes [i].IsByRef) {
					returnTypesList.Add (paramTypes [i].GetElementType ());
					nOutAndRefParams++;
				}
			}
			
			int[] refArgs = new int[nOutAndRefParams];
			returnTypes = returnTypesList.ToArray ();

			// Generates a version of the method that calls the base implementation
			// directly, for use by the base field of the table
			if (generateBase) {
				var baseMethod = myType.DefineMethod ("__luaInterface_base_" + method.Name, 
					MethodAttributes.Private | MethodAttributes.NewSlot | MethodAttributes.HideBySig, 
					returnType, paramTypes);
				ILGenerator generatorBase = baseMethod.GetILGenerator ();
				generatorBase.Emit (OpCodes.Ldarg_0);

				for (int i = 0; i < paramTypes.Length; i++)
					generatorBase.Emit (OpCodes.Ldarg, i + 1);

				generatorBase.Emit (OpCodes.Call, method);

				if (returnType == typeof(void))
					generatorBase.Emit (OpCodes.Pop);

				generatorBase.Emit (OpCodes.Ret);
			}

			// Defines the method
			var methodImpl = myType.DefineMethod (method.Name, attributes, returnType, paramTypes);

			// If it's an implementation of an interface tells what method it
			// is overriding
			if (myType.BaseType.Equals (typeof(object)))
				myType.DefineMethodOverride (methodImpl, method);
 
			ILGenerator generator = methodImpl.GetILGenerator ();
			generator.DeclareLocal (typeof(object[])); // original arguments
			generator.DeclareLocal (typeof(object[])); // with out-only arguments removed
			generator.DeclareLocal (typeof(int[])); // indexes of out and ref arguments

			if (!(returnType == typeof(void))) // return value
				generator.DeclareLocal (returnType);
			else
				generator.DeclareLocal (typeof(object));

			// Initializes local variables
			generator.Emit (OpCodes.Ldc_I4, paramTypes.Length);
			generator.Emit (OpCodes.Newarr, typeof(object));
			generator.Emit (OpCodes.Stloc_0);
			generator.Emit (OpCodes.Ldc_I4, paramTypes.Length - nOutParams + 1);
			generator.Emit (OpCodes.Newarr, typeof(object));
			generator.Emit (OpCodes.Stloc_1);
			generator.Emit (OpCodes.Ldc_I4, nOutAndRefParams);
			generator.Emit (OpCodes.Newarr, typeof(int));
			generator.Emit (OpCodes.Stloc_2);
			generator.Emit (OpCodes.Ldloc_1);
			generator.Emit (OpCodes.Ldc_I4_0);
			generator.Emit (OpCodes.Ldarg_0);
			generator.Emit (OpCodes.Ldfld, luaTableField);
			generator.Emit (OpCodes.Stelem_Ref);

			// Stores the arguments into the local variables, as needed
			for (int iArgs = 0, iInArgs = 1, iOutArgs = 0; iArgs < paramTypes.Length; iArgs++) {
				generator.Emit (OpCodes.Ldloc_0);
				generator.Emit (OpCodes.Ldc_I4, iArgs);
				generator.Emit (OpCodes.Ldarg, iArgs + 1);

				if (paramTypes [iArgs].IsByRef) {
					if (paramTypes [iArgs].GetElementType ().IsValueType) {
						generator.Emit (OpCodes.Ldobj, paramTypes [iArgs].GetElementType ());
						generator.Emit (OpCodes.Box, paramTypes [iArgs].GetElementType ());
					} else
						generator.Emit (OpCodes.Ldind_Ref);
				} else {
					if (paramTypes [iArgs].IsValueType)
						generator.Emit (OpCodes.Box, paramTypes [iArgs]);
				}

				generator.Emit (OpCodes.Stelem_Ref);

				if (paramTypes [iArgs].IsByRef) {
					generator.Emit (OpCodes.Ldloc_2);
					generator.Emit (OpCodes.Ldc_I4, iOutArgs);
					generator.Emit (OpCodes.Ldc_I4, iArgs);
					generator.Emit (OpCodes.Stelem_I4);
					refArgs [iOutArgs] = iArgs;
					iOutArgs++;
				}

				if (paramInfo [iArgs].IsIn || (!paramInfo [iArgs].IsOut)) {
					generator.Emit (OpCodes.Ldloc_1);
					generator.Emit (OpCodes.Ldc_I4, iInArgs);
					generator.Emit (OpCodes.Ldarg, iArgs + 1);

					if (paramTypes [iArgs].IsByRef) {
						if (paramTypes [iArgs].GetElementType ().IsValueType) {
							generator.Emit (OpCodes.Ldobj, paramTypes [iArgs].GetElementType ());
							generator.Emit (OpCodes.Box, paramTypes [iArgs].GetElementType ());
						} else
							generator.Emit (OpCodes.Ldind_Ref);
					} else {
						if (paramTypes [iArgs].IsValueType)
							generator.Emit (OpCodes.Box, paramTypes [iArgs]);
					}

					generator.Emit (OpCodes.Stelem_Ref);
					iInArgs++;
				} 
			}

			// Gets the function the method will delegate to by calling
			// the getTableFunction method of class LuaClassHelper
			generator.Emit (OpCodes.Ldarg_0);
			generator.Emit (OpCodes.Ldfld, luaTableField);
			generator.Emit (OpCodes.Ldstr, method.Name);
			generator.Emit (OpCodes.Call, classHelper.GetMethod ("getTableFunction"));
			var lab1 = generator.DefineLabel ();
			generator.Emit (OpCodes.Dup);
			generator.Emit (OpCodes.Brtrue_S, lab1);
			// Function does not exist, call base method
			generator.Emit (OpCodes.Pop);

			if (!method.IsAbstract) {
				generator.Emit (OpCodes.Ldarg_0);

				for (int i = 0; i < paramTypes.Length; i++)
					generator.Emit (OpCodes.Ldarg, i + 1);

				generator.Emit (OpCodes.Call, method);

				if (returnType == typeof(void)) 
					generator.Emit (OpCodes.Pop);

				generator.Emit (OpCodes.Ret);
				generator.Emit (OpCodes.Ldnull);
			} else
				generator.Emit (OpCodes.Ldnull);

			var lab2 = generator.DefineLabel ();
			generator.Emit (OpCodes.Br_S, lab2);
			generator.MarkLabel (lab1);
			// Function exists, call using method callFunction of LuaClassHelper
			generator.Emit (OpCodes.Ldloc_0);
			generator.Emit (OpCodes.Ldarg_0);
			generator.Emit (OpCodes.Ldfld, returnTypesField);
			generator.Emit (OpCodes.Ldc_I4, methodIndex);
			generator.Emit (OpCodes.Ldelem_Ref);
			generator.Emit (OpCodes.Ldloc_1);
			generator.Emit (OpCodes.Ldloc_2);
			generator.Emit (OpCodes.Call, classHelper.GetMethod ("callFunction"));
			generator.MarkLabel (lab2);

			// Stores the function return value
			if (returnType == typeof(void)) {
				generator.Emit (OpCodes.Pop);
				generator.Emit (OpCodes.Ldnull);
			} else if (returnType.IsValueType) {
				generator.Emit (OpCodes.Unbox, returnType);
				generator.Emit (OpCodes.Ldobj, returnType);
			} else
				generator.Emit (OpCodes.Castclass, returnType);

			generator.Emit (OpCodes.Stloc_3);

			// Sets return values of out and ref parameters
			for (int i = 0; i < refArgs.Length; i++) {
				generator.Emit (OpCodes.Ldarg, refArgs [i] + 1);
				generator.Emit (OpCodes.Ldloc_0);
				generator.Emit (OpCodes.Ldc_I4, refArgs [i]);
				generator.Emit (OpCodes.Ldelem_Ref);

				if (paramTypes [refArgs [i]].GetElementType ().IsValueType) {
					generator.Emit (OpCodes.Unbox, paramTypes [refArgs [i]].GetElementType ());
					generator.Emit (OpCodes.Ldobj, paramTypes [refArgs [i]].GetElementType ());
					generator.Emit (OpCodes.Stobj, paramTypes [refArgs [i]].GetElementType ());
				} else {
					generator.Emit (OpCodes.Castclass, paramTypes [refArgs [i]].GetElementType ());
					generator.Emit (OpCodes.Stind_Ref);
				}
			}

			// Returns
			if (!(returnType == typeof(void)))
				generator.Emit (OpCodes.Ldloc_3);

			generator.Emit (OpCodes.Ret);
		}
#endif
		/*
		 * Gets an event handler for the event type that delegates to the eventHandler Lua function.
		 * Caches the generated type.
		 */
		public LuaEventHandler GetEvent (Type eventHandlerType, LuaFunction eventHandler)
		{
#if MONOTOUCH
			throw new NotImplementedException (" Emit not available on MonoTouch ");
#elif SILVERLIGHT
			throw new NotImplementedException (" Emit not available on Silverlight ");
#else
			Type eventConsumerType;

			if (eventHandlerCollection.ContainsKey (eventHandlerType)) 
				eventConsumerType = eventHandlerCollection [eventHandlerType];
			else {
				eventConsumerType = GenerateEvent (eventHandlerType);
				eventHandlerCollection [eventHandlerType] = eventConsumerType;
			}

			var luaEventHandler = (LuaEventHandler)Activator.CreateInstance (eventConsumerType);
			luaEventHandler.handler = eventHandler;
			return luaEventHandler;
#endif
		}

		public void RegisterLuaDelegateType (Type delegateType, Type luaDelegateType)
		{
			delegateCollection [delegateType] = luaDelegateType;
		}

		public void RegisterLuaClassType (Type klass, Type luaClass)
		{
			LuaClassType luaClassType = new LuaClassType ();
			luaClassType.klass = luaClass;
			GetReturnTypesFromClass (klass, out luaClassType.returnTypes);
			classCollection [klass] = luaClassType;
		}
		/*
		 * Gets a delegate with delegateType that calls the luaFunc Lua function
		 * Caches the generated type.
		 */
		public Delegate GetDelegate (Type delegateType, LuaFunction luaFunc)
		{
			var returnTypes = new List<Type> ();
			Type luaDelegateType;

			if (delegateCollection.ContainsKey (delegateType)) 
				luaDelegateType = delegateCollection [delegateType];
			else {
				luaDelegateType = GenerateDelegate (delegateType);
				delegateCollection [delegateType] = luaDelegateType;
			}

			var methodInfo = delegateType.GetMethod ("Invoke");
			returnTypes.Add (methodInfo.ReturnType);

			foreach (ParameterInfo paramInfo in methodInfo.GetParameters()) {
				if (paramInfo.ParameterType.IsByRef)
					returnTypes.Add (paramInfo.ParameterType);
			}

			var luaDelegate = (LuaDelegate)Activator.CreateInstance (luaDelegateType);
			luaDelegate.function = luaFunc;
			luaDelegate.returnTypes = returnTypes.ToArray ();

			return Delegate.CreateDelegate (delegateType, luaDelegate, "CallFunction");
		}

		/*
		 * Gets an instance of an implementation of the klass interface or
		 * subclass of klass that delegates public virtual methods to the
		 * luaTable table.
		 * Caches the generated type.
		 */
		public object GetClassInstance (Type klass, LuaTable luaTable)
		{
			LuaClassType luaClassType;

			if (classCollection.ContainsKey (klass)) 
				luaClassType = classCollection [klass];
			else {
				luaClassType = new LuaClassType ();
				GenerateClass (klass, out luaClassType.klass, out luaClassType.returnTypes);
				classCollection [klass] = luaClassType;
			}

			return Activator.CreateInstance (luaClassType.klass, new object[] {
				luaTable,
				luaClassType.returnTypes
			});
		}
	}
}