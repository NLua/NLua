/*
 * This file is part of NLua.
 * Copyright (C) 2014 Vinicius Jarina.
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
using System.Linq;
using System.IO;
using System.Collections;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NLua.Method;
using NLua.Extensions;

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
	 * Functions used in the metatables of userdata representing
	 * CLR objects
	 * 
	 */
	public class MetaFunctions
	{
		public LuaNativeFunction GcFunction { get; private set; }
		public LuaNativeFunction IndexFunction { get; private set; }
		public LuaNativeFunction NewIndexFunction { get; private set; }
		public LuaNativeFunction BaseIndexFunction { get; private set; }
		public LuaNativeFunction ClassIndexFunction { get; private set; }
		public LuaNativeFunction ClassNewindexFunction { get; private set; }
		public LuaNativeFunction ExecuteDelegateFunction { get; private set; }
		public LuaNativeFunction CallConstructorFunction { get; private set; }
		public LuaNativeFunction ToStringFunction { get; private set; }
		 
		public LuaNativeFunction AddFunction { get; private set; }
		public LuaNativeFunction SubtractFunction { get; private set; }
		public LuaNativeFunction MultiplyFunction { get; private set; }
		public LuaNativeFunction DivisionFunction { get; private set; }
		public LuaNativeFunction ModulosFunction { get; private set; }
		public LuaNativeFunction UnaryNegationFunction { get; private set; }
		public LuaNativeFunction EqualFunction { get; private set; }
		public LuaNativeFunction LessThanFunction { get; private set; }
		public LuaNativeFunction LessThanOrEqualFunction { get; private set; }

		Dictionary<object, object> memberCache = new Dictionary<object, object> ();
		ObjectTranslator translator;

		/*
		 * __index metafunction for CLR objects. Implemented in Lua.
		 */
		static string luaIndexFunction =
			@"local function index(obj,name)
			    local meta = getmetatable(obj)
			    local cached = meta.cache[name]
			    if cached ~= nil then
			       return cached
			    else
			       local value,isFunc = get_object_member(obj,name)
			       
			       if isFunc then
					meta.cache[name]=value
			       end
			       return value
			     end
		    end
		    return index";
		public static string LuaIndexFunction {
			get { return luaIndexFunction; }
		}
		public MetaFunctions (ObjectTranslator translator)
		{
			this.translator = translator;
			GcFunction = new LuaNativeFunction (MetaFunctions.CollectObject);
			ToStringFunction = new LuaNativeFunction (MetaFunctions.ToStringLua);
			IndexFunction = new LuaNativeFunction (MetaFunctions.GetMethod);
			NewIndexFunction = new LuaNativeFunction (MetaFunctions.SetFieldOrProperty);
			BaseIndexFunction = new LuaNativeFunction (MetaFunctions.GetBaseMethod);
			CallConstructorFunction = new LuaNativeFunction (MetaFunctions.CallConstructor);
			ClassIndexFunction = new LuaNativeFunction (MetaFunctions.GetClassMethod);
			ClassNewindexFunction = new LuaNativeFunction (MetaFunctions.SetClassFieldOrProperty);
			ExecuteDelegateFunction = new LuaNativeFunction (MetaFunctions.RunFunctionDelegate);
			AddFunction = new LuaNativeFunction (MetaFunctions.AddLua);
			SubtractFunction = new LuaNativeFunction (MetaFunctions.SubtractLua);
			MultiplyFunction = new LuaNativeFunction (MetaFunctions.MultiplyLua);
			DivisionFunction = new LuaNativeFunction (MetaFunctions.DivideLua);
			ModulosFunction = new LuaNativeFunction (MetaFunctions.ModLua);
			UnaryNegationFunction = new LuaNativeFunction (MetaFunctions.UnaryNegationLua);
			EqualFunction = new LuaNativeFunction (MetaFunctions.EqualLua);
			LessThanFunction = new LuaNativeFunction (MetaFunctions.LessThanLua);
			LessThanOrEqualFunction = new LuaNativeFunction (MetaFunctions.LessThanOrEqualLua);
		}

		/*
		 * __call metafunction of CLR delegates, retrieves and calls the delegate.
		 */
#if MONOTOUCH
		[MonoTouch.MonoPInvokeCallback (typeof (LuaNativeFunction))]
#endif
		[System.Runtime.InteropServices.AllowReversePInvokeCalls]
		private static int RunFunctionDelegate (LuaState luaState)
		{
			var translator = ObjectTranslatorPool.Instance.Find (luaState);
			return RunFunctionDelegate (luaState, translator);
		}

		private static int RunFunctionDelegate (LuaState luaState, ObjectTranslator translator)
		{
			LuaNativeFunction func = (LuaNativeFunction)translator.GetRawNetObject (luaState, 1);
			LuaLib.LuaRemove (luaState, 1);
			return func (luaState);
		}

		/*
		 * __gc metafunction of CLR objects.
		 */
#if MONOTOUCH
		[MonoTouch.MonoPInvokeCallback (typeof (LuaNativeFunction))]
#endif
		[System.Runtime.InteropServices.AllowReversePInvokeCalls]
		private static int CollectObject (LuaState luaState)
		{
			var translator = ObjectTranslatorPool.Instance.Find (luaState);
			return CollectObject (luaState, translator);
		}

		private static int CollectObject (LuaState luaState, ObjectTranslator translator)
		{
			int udata = LuaLib.LuaNetRawNetObj (luaState, 1);

			if (udata != -1)
				translator.CollectObject (udata);

			return 0;
		}

		/*
		 * __tostring metafunction of CLR objects.
		 */
#if MONOTOUCH
		[MonoTouch.MonoPInvokeCallback (typeof (LuaNativeFunction))]
#endif
		[System.Runtime.InteropServices.AllowReversePInvokeCalls]
		private static int ToStringLua (LuaState luaState)
		{
			var translator = ObjectTranslatorPool.Instance.Find (luaState);
			return ToStringLua (luaState, translator);
		}

		private static int ToStringLua (LuaState luaState, ObjectTranslator translator)
		{
			object obj = translator.GetRawNetObject (luaState, 1);

			if (obj != null)
				translator.Push (luaState, obj.ToString () + ": " + obj.GetHashCode ().ToString());
			else
				LuaLib.LuaPushNil (luaState);

			return 1;
		}


/*
 * __add metafunction of CLR objects.
 */
#if MONOTOUCH
		[MonoTouch.MonoPInvokeCallback (typeof (LuaNativeFunction))]
#endif
		[System.Runtime.InteropServices.AllowReversePInvokeCalls]
		static int AddLua (LuaState luaState)
		{
			var translator = ObjectTranslatorPool.Instance.Find (luaState);
			return MatchOperator (luaState, "op_Addition", translator);
		}
	
		/*
		* __sub metafunction of CLR objects.
		*/
#if MONOTOUCH
		[MonoTouch.MonoPInvokeCallback (typeof (LuaNativeFunction))]
#endif
		[System.Runtime.InteropServices.AllowReversePInvokeCalls]
		static int SubtractLua (LuaState luaState)
		{
			var translator = ObjectTranslatorPool.Instance.Find (luaState);
			return MatchOperator (luaState, "op_Subtraction", translator);
		}

		/*
		* __mul metafunction of CLR objects.
		*/
#if MONOTOUCH
		[MonoTouch.MonoPInvokeCallback (typeof (LuaNativeFunction))]
#endif
		[System.Runtime.InteropServices.AllowReversePInvokeCalls]
		static int MultiplyLua (LuaState luaState)
		{
			var translator = ObjectTranslatorPool.Instance.Find (luaState);
			return MatchOperator (luaState, "op_Multiply", translator);
		}
		
		/*
		* __div metafunction of CLR objects.
		*/
#if MONOTOUCH
		[MonoTouch.MonoPInvokeCallback (typeof (LuaNativeFunction))]
#endif
		[System.Runtime.InteropServices.AllowReversePInvokeCalls]
		static int DivideLua (LuaState luaState)
		{
			var translator = ObjectTranslatorPool.Instance.Find (luaState);
			return MatchOperator (luaState, "op_Division", translator);
		}

		/*
		* __mod metafunction of CLR objects.
		*/
#if MONOTOUCH
		[MonoTouch.MonoPInvokeCallback (typeof (LuaNativeFunction))]
#endif
		[System.Runtime.InteropServices.AllowReversePInvokeCalls]
		static int ModLua (LuaState luaState)
		{
			var translator = ObjectTranslatorPool.Instance.Find (luaState);
			return MatchOperator (luaState, "op_Modulus", translator);
		}
	
		/*
		* __unm metafunction of CLR objects.
		*/
#if MONOTOUCH
		[MonoTouch.MonoPInvokeCallback (typeof (LuaNativeFunction))]
#endif
		[System.Runtime.InteropServices.AllowReversePInvokeCalls]
		static int UnaryNegationLua (LuaState luaState)
		{
			var translator = ObjectTranslatorPool.Instance.Find (luaState);
			return UnaryNegationLua (luaState, translator);
		}
		static int UnaryNegationLua (LuaState luaState, ObjectTranslator translator)
		{
			object obj1 = translator.GetRawNetObject (luaState, 1);

			if (obj1 == null) {
				translator.ThrowError (luaState, "Cannot negate a nil object");
				LuaLib.LuaPushNil (luaState);
				return 1;
			}

			Type type = obj1.GetType ();
			MethodInfo opUnaryNegation = type.GetMethod ("op_UnaryNegation");

			if (opUnaryNegation == null) {
				translator.ThrowError (luaState, "Cannot negate object (" + type.Name + " does not overload the operator -)");
				LuaLib.LuaPushNil (luaState);
				return 1;
			}
			obj1 = opUnaryNegation.Invoke (obj1, new object [] { obj1 });
			translator.Push (luaState, obj1);
			return 1;
		}


		/*
		* __eq metafunction of CLR objects.
		*/
#if MONOTOUCH
		[MonoTouch.MonoPInvokeCallback (typeof (LuaNativeFunction))]
#endif
		[System.Runtime.InteropServices.AllowReversePInvokeCalls]
		static int EqualLua (LuaState luaState)
		{
			var translator = ObjectTranslatorPool.Instance.Find (luaState);
			return MatchOperator (luaState, "op_Equality", translator);
		}

		/*
		* __lt metafunction of CLR objects.
		*/
#if MONOTOUCH
		[MonoTouch.MonoPInvokeCallback (typeof (LuaNativeFunction))]
#endif
		[System.Runtime.InteropServices.AllowReversePInvokeCalls]
		static int LessThanLua (LuaState luaState)
		{
			var translator = ObjectTranslatorPool.Instance.Find (luaState);
			return MatchOperator (luaState, "op_LessThan", translator);
		}
		
		/*
		 * __le metafunction of CLR objects.
		 */
#if MONOTOUCH
		[MonoTouch.MonoPInvokeCallback (typeof (LuaNativeFunction))]
#endif
		[System.Runtime.InteropServices.AllowReversePInvokeCalls]
		static int LessThanOrEqualLua (LuaState luaState)
		{
			var translator = ObjectTranslatorPool.Instance.Find (luaState);
			return MatchOperator (luaState, "op_LessThanOrEqual", translator);
		}		

		/// <summary>
		/// Debug tool to dump the lua stack
		/// </summary>
		/// FIXME, move somewhere else
		public static void DumpStack (ObjectTranslator translator, LuaState luaState)
		{
			int depth = LuaLib.LuaGetTop (luaState);

#if WINDOWS_PHONE
			Debug.WriteLine("lua stack depth: {0}", depth);
#elif UNITY_3D
			UnityEngine.Debug.Log(string.Format("lua stack depth: {0}", depth));
#elif !SILVERLIGHT
			Debug.Print ("lua stack depth: {0}", depth);
#endif

			for (int i = 1; i <= depth; i++) {
				var type = LuaLib.LuaType (luaState, i);
				// we dump stacks when deep in calls, calling typename while the stack is in flux can fail sometimes, so manually check for key types
				string typestr = (type == LuaTypes.Table) ? "table" : LuaLib.LuaTypeName (luaState, type);
				string strrep = LuaLib.LuaToString (luaState, i).ToString ();

				if (type == LuaTypes.UserData) {
					object obj = translator.GetRawNetObject (luaState, i);
					strrep = obj.ToString ();
				}

#if WINDOWS_PHONE
                Debug.WriteLine("{0}: ({1}) {2}", i, typestr, strrep);
#elif UNITY_3D
			UnityEngine.Debug.Log(string.Format("{0}: ({1}) {2}", i, typestr, strrep));
#elif !SILVERLIGHT
				Debug.Print ("{0}: ({1}) {2}", i, typestr, strrep);
#endif
			}
		}

		/*
		 * Called by the __index metafunction of CLR objects in case the
		 * method is not cached or it is a field/property/event.
		 * Receives the object and the member name as arguments and returns
		 * either the value of the member or a delegate to call it.
		 * If the member does not exist returns nil.
		 */
#if MONOTOUCH
		[MonoTouch.MonoPInvokeCallback (typeof (LuaNativeFunction))]
#endif
		[System.Runtime.InteropServices.AllowReversePInvokeCalls]
		private static int GetMethod (LuaState luaState)
		{
			var translator = ObjectTranslatorPool.Instance.Find (luaState);
			var instance = translator.MetaFunctionsInstance;
			return instance.GetMethodInternal (luaState);
		}

		private int GetMethodInternal (LuaState luaState)
		{
			object obj = translator.GetRawNetObject (luaState, 1);

			if (obj == null) {
				translator.ThrowError (luaState, "trying to index an invalid object reference");
				LuaLib.LuaPushNil (luaState);
				return 1;
			}

			object index = translator.GetObject (luaState, 2);
			//var indexType = index.GetType();
			string methodName = index as string;		// will be null if not a string arg
			var objType = obj.GetType ();

			// Handle the most common case, looking up the method by name. 

			// CP: This will fail when using indexers and attempting to get a value with the same name as a property of the object, 
			// ie: xmlelement['item'] <- item is a property of xmlelement
			try {
				if (!string.IsNullOrEmpty(methodName) && IsMemberPresent (objType, methodName))
					return GetMember (luaState, objType, obj, methodName, BindingFlags.Instance);
			} catch {
			}
			
			// Try to access by array if the type is right and index is an int (lua numbers always come across as double)
			if (objType.IsArray && index is double) {
				int intIndex = (int)((double)index);

				if (objType.UnderlyingSystemType == typeof(float[])) {
					float[] arr = ((float[])obj);
					translator.Push (luaState, arr [intIndex]);
				} else if (objType.UnderlyingSystemType == typeof(double[])) {
					double[] arr = ((double[])obj);
					translator.Push (luaState, arr [intIndex]);
				} else if (objType.UnderlyingSystemType == typeof(int[])) {
					int[] arr = ((int[])obj);
					translator.Push (luaState, arr [intIndex]);
				} else {
					object[] arr = (object[])obj;
					translator.Push (luaState, arr [intIndex]);
				}
			} else {

				if (!string.IsNullOrEmpty (methodName) && IsExtensionMethodPresent (objType, methodName)) {
					return GetExtensionMethod (luaState, objType, obj, methodName);
				}
				// Try to use get_Item to index into this .net object
				var methods = objType.GetMethods ();

				foreach (var mInfo in methods) {
					if (mInfo.Name == "get_Item") {
						//check if the signature matches the input
						if (mInfo.GetParameters ().Length == 1) {
							var getter = mInfo;
							var actualParms = (getter != null) ? getter.GetParameters () : null;

							if (actualParms == null || actualParms.Length != 1) {
								translator.ThrowError (luaState, "method not found (or no indexer): " + index);
								LuaLib.LuaPushNil (luaState);
							} else {
								// Get the index in a form acceptable to the getter
								index = translator.GetAsType (luaState, 2, actualParms [0].ParameterType);
								object[] args = new object[1];

								// Just call the indexer - if out of bounds an exception will happen
								args [0] = index;

								try {
									object result = getter.Invoke (obj, args);
									translator.Push (luaState, result);
								} catch (TargetInvocationException e) {
									// Provide a more readable description for the common case of key not found
									if (e.InnerException is KeyNotFoundException)
										translator.ThrowError (luaState, "key '" + index + "' not found ");
									else
										translator.ThrowError (luaState, "exception indexing '" + index + "' " + e.Message);

									LuaLib.LuaPushNil (luaState);
								}
							}
						}
					}
				}
			}

			LuaLib.LuaPushBoolean (luaState, false);
			return 2;
		}

		/*
		 * __index metafunction of base classes (the base field of Lua tables).
		 * Adds a prefix to the method name to call the base version of the method.
		 */
#if MONOTOUCH
		[MonoTouch.MonoPInvokeCallback (typeof (LuaNativeFunction))]
#endif
		[System.Runtime.InteropServices.AllowReversePInvokeCalls]
		private static int GetBaseMethod (LuaState luaState)
		{
			var translator = ObjectTranslatorPool.Instance.Find (luaState);
			var instance = translator.MetaFunctionsInstance;
			return instance.GetBaseMethodInternal (luaState);
		}

		private int GetBaseMethodInternal (LuaState luaState)
		{
			object obj = translator.GetRawNetObject (luaState, 1);

			if (obj == null) {
				translator.ThrowError (luaState, "trying to index an invalid object reference");
				LuaLib.LuaPushNil (luaState);
				LuaLib.LuaPushBoolean (luaState, false);
				return 2;
			}

			string methodName = LuaLib.LuaToString (luaState, 2).ToString ();

			if (string.IsNullOrEmpty(methodName)) {
				LuaLib.LuaPushNil (luaState);
				LuaLib.LuaPushBoolean (luaState, false);
				return 2;
			}

			GetMember (luaState, obj.GetType (), obj, "__luaInterface_base_" + methodName, BindingFlags.Instance);
			LuaLib.LuaSetTop (luaState, -2);

			if (LuaLib.LuaType (luaState, -1) == LuaTypes.Nil) {
				LuaLib.LuaSetTop (luaState, -2);
				return GetMember (luaState, obj.GetType (), obj, methodName, BindingFlags.Instance);
			}

			LuaLib.LuaPushBoolean (luaState, false);
			return 2;
		}

		/// <summary>
		/// Does this method exist as either an instance or static?
		/// </summary>
		/// <param name="objType"></param>
		/// <param name="methodName"></param>
		/// <returns></returns>
		bool IsMemberPresent (IReflect objType, string methodName)
		{
			object cachedMember = CheckMemberCache (memberCache, objType, methodName);

			if (cachedMember != null)
				return true;

			var members = objType.GetMember (methodName, BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public);
			return (members.Length > 0);
		}

		bool IsExtensionMethodPresent (Type type, string name)
		{
			object cachedMember = CheckMemberCache (memberCache, type, name);

			if (cachedMember != null)
				return true;

			return translator.IsExtensionMethodPresent (type, name);
		}

		int GetExtensionMethod (LuaState luaState, Type type, object obj, string name)
		{
			object cachedMember = CheckMemberCache (memberCache, type, name);

			if (cachedMember != null && cachedMember is LuaNativeFunction) {
					translator.PushFunction (luaState, (LuaNativeFunction)cachedMember);
					translator.Push (luaState, true);
					return 2;
			}

			MethodInfo methodInfo = translator.GetExtensionMethod (type, name);
			var wrapper = new LuaNativeFunction ((new LuaMethodWrapper (translator, obj, type, methodInfo)).invokeFunction);

			SetMemberCache (memberCache, type, name, wrapper);

			translator.PushFunction (luaState, wrapper);
			translator.Push (luaState, true);
			return 2;			
		}

		/*
		 * Pushes the value of a member or a delegate to call it, depending on the type of
		 * the member. Works with static or instance members.
		 * Uses reflection to find members, and stores the reflected MemberInfo object in
		 * a cache (indexed by the type of the object and the name of the member).
		 */
		int GetMember (LuaState luaState, IReflect objType, object obj, string methodName, BindingFlags bindingType)
		{
			bool implicitStatic = false;
			MemberInfo member = null;
			object cachedMember = CheckMemberCache (memberCache, objType, methodName);

			if (cachedMember is LuaNativeFunction) {
				translator.PushFunction (luaState, (LuaNativeFunction)cachedMember);
				translator.Push (luaState, true);
				return 2;
			} else if (cachedMember != null)
				member = (MemberInfo)cachedMember;
			else {
				var members = objType.GetMember (methodName, bindingType | BindingFlags.Public);

				if (members.Length > 0)
					member = members [0];
				else {
					// If we can't find any suitable instance members, try to find them as statics - but we only want to allow implicit static
					members = objType.GetMember (methodName, bindingType | BindingFlags.Static | BindingFlags.Public);

					if (members.Length > 0) {
						member = members [0];
						implicitStatic = true;
					}
				}
			}

			if (member != null) {
				if (member.MemberType == MemberTypes.Field) {
					var field = (FieldInfo)member;

					if (cachedMember == null)
						SetMemberCache (memberCache, objType, methodName, member);

					try {
						translator.Push (luaState, field.GetValue (obj));
					} catch {
						LuaLib.LuaPushNil (luaState);
					}
				} else if (member.MemberType == MemberTypes.Property) {
					var property = (PropertyInfo)member;
					if (cachedMember == null)
						SetMemberCache (memberCache, objType, methodName, member);

					try {
						object val = property.GetValue (obj, null);
						translator.Push (luaState, val);
					} catch (ArgumentException) {
						// If we can't find the getter in our class, recurse up to the base class and see
						// if they can help.
						if (objType is Type && !(((Type)objType) == typeof(object)))
							return GetMember (luaState, ((Type)objType).BaseType, obj, methodName, bindingType);
						else
							LuaLib.LuaPushNil (luaState);
					} catch (TargetInvocationException e) {  // Convert this exception into a Lua error
						ThrowError (luaState, e);
						LuaLib.LuaPushNil (luaState);
					}
				} else if (member.MemberType == MemberTypes.Event) {
					var eventInfo = (EventInfo)member;
					if (cachedMember == null)
						SetMemberCache (memberCache, objType, methodName, member);

					translator.Push (luaState, new RegisterEventHandler (translator.pendingEvents, obj, eventInfo));
				} else if (!implicitStatic) {
					if (member.MemberType == MemberTypes.NestedType) {
						// kevinh - added support for finding nested types
						// cache us
						if (cachedMember == null)
							SetMemberCache (memberCache, objType, methodName, member);

						// Find the name of our class
						string name = member.Name;
						var dectype = member.DeclaringType;

						// Build a new long name and try to find the type by name
						string longname = dectype.FullName + "+" + name;
						var nestedType = translator.FindType (longname);
						translator.PushType (luaState, nestedType);
					} else {
						// Member type must be 'method'
						var wrapper = new LuaNativeFunction ((new LuaMethodWrapper (translator, objType, methodName, bindingType)).invokeFunction);

						if (cachedMember == null)
							SetMemberCache (memberCache, objType, methodName, wrapper);

						translator.PushFunction (luaState, wrapper);
						translator.Push (luaState, true);
						return 2;
					}
				} else {
					// If we reach this point we found a static method, but can't use it in this context because the user passed in an instance
					translator.ThrowError (luaState, "can't pass instance to static method " + methodName);
					LuaLib.LuaPushNil (luaState);
				}
			} else {
				// kevinh - we want to throw an exception because meerly returning 'nil' in this case
				// is not sufficient.  valid data members may return nil and therefore there must be some
				// way to know the member just doesn't exist.
				translator.ThrowError (luaState, "unknown member name " + methodName);
				LuaLib.LuaPushNil (luaState);
			}

			// push false because we are NOT returning a function (see luaIndexFunction)
			translator.Push (luaState, false);
			return 2;
		}

		/*
		 * Checks if a MemberInfo object is cached, returning it or null.
		 */
		private object CheckMemberCache (Dictionary<object, object> memberCache, IReflect objType, string memberName)
		{
			object members = null;

			if (memberCache.TryGetValue(objType, out members))
			{
				var membersDict = members as Dictionary<object, object>;

				object memberValue = null;

				if (members != null && membersDict.TryGetValue(memberName, out memberValue))
				{
					return memberValue;
				}
			}

			return null;
		}

		/*
		 * Stores a MemberInfo object in the member cache.
		 */
		private void SetMemberCache (Dictionary<object, object> memberCache, IReflect objType, string memberName, object member)
		{
			Dictionary<object, object> members = null;
			object memberCacheValue = null;

			if (memberCache.TryGetValue(objType, out memberCacheValue)) {
				members = (Dictionary<object, object>)memberCacheValue;
			} else {
				members = new Dictionary<object, object>();
				memberCache[objType] = members;
			}

			members [memberName] = member;
		}

		/*
		 * __newindex metafunction of CLR objects. Receives the object,
		 * the member name and the value to be stored as arguments. Throws
		 * and error if the assignment is invalid.
		 */
#if MONOTOUCH
		[MonoTouch.MonoPInvokeCallback (typeof (LuaNativeFunction))]
#endif
		[System.Runtime.InteropServices.AllowReversePInvokeCalls]
		private static int SetFieldOrProperty (LuaState luaState)
		{
			var translator = ObjectTranslatorPool.Instance.Find (luaState);
			var instance = translator.MetaFunctionsInstance;
			return instance.SetFieldOrPropertyInternal (luaState);
		}

		private int SetFieldOrPropertyInternal (LuaState luaState)
 		{
			object target = translator.GetRawNetObject (luaState, 1);

			if (target == null) {
				translator.ThrowError (luaState, "trying to index and invalid object reference");
				return 0;
			}

			var type = target.GetType ();

			// First try to look up the parameter as a property name
			string detailMessage;
			bool didMember = TrySetMember (luaState, type, target, BindingFlags.Instance, out detailMessage);

			if (didMember)
				return 0;	   // Must have found the property name

			// We didn't find a property name, now see if we can use a [] style this accessor to set array contents
			try {
				if (type.IsArray && LuaLib.LuaIsNumber (luaState, 2)) {
					int index = (int)LuaLib.LuaToNumber (luaState, 2);
					var arr = (Array)target;
					object val = translator.GetAsType (luaState, 3, arr.GetType ().GetElementType ());
					arr.SetValue (val, index);
				} else {
					// Try to see if we have a this[] accessor
					var setter = type.GetMethod ("set_Item");
					if (setter != null) {
						var args = setter.GetParameters ();
						var valueType = args [1].ParameterType;

						// The new val ue the user specified 
						object val = translator.GetAsType (luaState, 3, valueType);
						var indexType = args [0].ParameterType;
						object index = translator.GetAsType (luaState, 2, indexType);

						object[] methodArgs = new object[2];

						// Just call the indexer - if out of bounds an exception will happen
						methodArgs [0] = index;
						methodArgs [1] = val;
						setter.Invoke (target, methodArgs);
					} else
						translator.ThrowError (luaState, detailMessage); // Pass the original message from trySetMember because it is probably best
				}
#if !SILVERLIGHT
			} catch (SEHException) {
				// If we are seeing a C++ exception - this must actually be for Lua's private use.  Let it handle it
				throw;
#endif
			} catch (Exception e) {
				ThrowError (luaState, e);
			}

			return 0;
		}

		/// <summary>
		/// Tries to set a named property or field
		/// </summary>
		/// <param name="luaState"></param>
		/// <param name="targetType"></param>
		/// <param name="target"></param>
		/// <param name="bindingType"></param>
		/// <returns>false if unable to find the named member, true for success</returns>
		private bool TrySetMember (LuaState luaState, IReflect targetType, object target, BindingFlags bindingType, out string detailMessage)
		{
			detailMessage = null;   // No error yet

			// If not already a string just return - we don't want to call tostring - which has the side effect of 
			// changing the lua typecode to string
			// Note: We don't use isstring because the standard lua C isstring considers either strings or numbers to
			// be true for isstring.
			if (LuaLib.LuaType (luaState, 2) != LuaTypes.String) {
				detailMessage = "property names must be strings";
				return false;
			}

			// We only look up property names by string
			string fieldName = LuaLib.LuaToString (luaState, 2).ToString ();
			if (fieldName == null || fieldName.Length < 1 || !(char.IsLetter (fieldName [0]) || fieldName [0] == '_')) {
				detailMessage = "invalid property name";
				return false;
			}

			// Find our member via reflection or the cache
			var member = (MemberInfo)CheckMemberCache (memberCache, targetType, fieldName);
			if (member == null) {
				var members = targetType.GetMember (fieldName, bindingType | BindingFlags.Public);

				if (members.Length > 0) {
					member = members [0];
					SetMemberCache (memberCache, targetType, fieldName, member);
				} else {
					detailMessage = "field or property '" + fieldName + "' does not exist";
					return false;
				}
			}

			if (member.MemberType == MemberTypes.Field) {
				var field = (FieldInfo)member;
				object val = translator.GetAsType (luaState, 3, field.FieldType);

				try {
					field.SetValue (target, val);
				} catch (Exception e) {
					ThrowError (luaState, e);
				}

				// We did a call
				return true;
			} else if (member.MemberType == MemberTypes.Property) {
				var property = (PropertyInfo)member;
				object val = translator.GetAsType (luaState, 3, property.PropertyType);

				try {
					property.SetValue (target, val, null);
				} catch (Exception e) {
					ThrowError (luaState, e);
				}

				// We did a call
				return true;
			}

			detailMessage = "'" + fieldName + "' is not a .net field or property";
			return false;
		}

		/*
		 * Writes to fields or properties, either static or instance. Throws an error
		 * if the operation is invalid.
		 */
		private int SetMember (LuaState luaState, IReflect targetType, object target, BindingFlags bindingType)
		{
			string detail;
			bool success = TrySetMember (luaState, targetType, target, bindingType, out detail);

			if (!success)
				translator.ThrowError (luaState, detail);

			return 0;
		}

		/// <summary>
		/// Convert a C# exception into a Lua error
		/// </summary>
		/// <param name="e"></param>
		/// We try to look into the exception to give the most meaningful description
		void ThrowError (LuaState luaState, Exception e)
		{
			// If we got inside a reflection show what really happened
			var te = e as TargetInvocationException;

			if (te != null)
				e = te.InnerException;

			translator.ThrowError (luaState, e);
		}

		/*
		 * __index metafunction of type references, works on static members.
		 */
#if MONOTOUCH
		[MonoTouch.MonoPInvokeCallback (typeof (LuaNativeFunction))]
#endif
		[System.Runtime.InteropServices.AllowReversePInvokeCalls]
		private static int GetClassMethod (LuaState luaState)
		{
			var translator = ObjectTranslatorPool.Instance.Find (luaState);
			var instance = translator.MetaFunctionsInstance;
			return instance.GetClassMethodInternal (luaState);
		}

		private int GetClassMethodInternal (LuaState luaState)
		{
			IReflect klass;
			object obj = translator.GetRawNetObject (luaState, 1);

			if (obj == null || !(obj is IReflect)) {
				translator.ThrowError (luaState, "trying to index an invalid type reference");
				LuaLib.LuaPushNil (luaState);
				return 1;
			} else
				klass = (IReflect)obj;

			if (LuaLib.LuaIsNumber (luaState, 2)) {
				int size = (int)LuaLib.LuaToNumber (luaState, 2);
				translator.Push (luaState, Array.CreateInstance (klass.UnderlyingSystemType, size));
				return 1;
			} else {
				string methodName = LuaLib.LuaToString (luaState, 2).ToString ();

				if (string.IsNullOrEmpty(methodName)) {
					LuaLib.LuaPushNil (luaState);
					return 1;
				}
				else
					return GetMember (luaState, klass, null, methodName, BindingFlags.FlattenHierarchy | BindingFlags.Static);
			}
		}

		/*
		 * __newindex function of type references, works on static members.
		 */
#if MONOTOUCH
		[MonoTouch.MonoPInvokeCallback (typeof (LuaNativeFunction))]
#endif
		[System.Runtime.InteropServices.AllowReversePInvokeCalls]
		private static int SetClassFieldOrProperty (LuaState luaState)
		{
			var translator = ObjectTranslatorPool.Instance.Find (luaState);
			var instance = translator.MetaFunctionsInstance;
			return instance.SetClassFieldOrPropertyInternal (luaState);
		}

		private int SetClassFieldOrPropertyInternal (LuaState luaState)
		{
			IReflect target;
			object obj = translator.GetRawNetObject (luaState, 1);

			if (obj == null || !(obj is IReflect)) {
				translator.ThrowError (luaState, "trying to index an invalid type reference");
				return 0;
			} else
				target = (IReflect)obj;

			return SetMember (luaState, target, null, BindingFlags.FlattenHierarchy | BindingFlags.Static);
		}

		/*
		 * __call metafunction of type references. Searches for and calls
		 * a constructor for the type. Returns nil if the constructor is not
		 * found or if the arguments are invalid. Throws an error if the constructor
		 * generates an exception.
		 */
#if MONOTOUCH
		[MonoTouch.MonoPInvokeCallback (typeof (LuaNativeFunction))]
#endif
		[System.Runtime.InteropServices.AllowReversePInvokeCalls]
		private static int CallConstructor (LuaState luaState)
		{
			var translator = ObjectTranslatorPool.Instance.Find (luaState);
			var instance = translator.MetaFunctionsInstance;
			return instance.CallConstructorInternal (luaState);
		}

		private int CallConstructorInternal (LuaState luaState)
		{
			var validConstructor = new MethodCache ();
			IReflect klass;
			object obj = translator.GetRawNetObject (luaState, 1);

			if (obj == null || !(obj is IReflect)) {
				translator.ThrowError (luaState, "trying to call constructor on an invalid type reference");
				LuaLib.LuaPushNil (luaState);
				return 1;
			} else
				klass = (IReflect)obj;

			LuaLib.LuaRemove (luaState, 1);
			var constructors = klass.UnderlyingSystemType.GetConstructors ();

			foreach (var constructor in constructors) {
				bool isConstructor = MatchParameters (luaState, constructor, ref validConstructor);

				if (isConstructor) {
					try {
						translator.Push (luaState, constructor.Invoke (validConstructor.args));
					} catch (TargetInvocationException e) {
						ThrowError (luaState, e);
						LuaLib.LuaPushNil (luaState);
					} catch {
						LuaLib.LuaPushNil (luaState);
					}

					return 1;
				}
			}

			string constructorName = (constructors.Length == 0) ? "unknown" : constructors [0].Name;
			translator.ThrowError (luaState, String.Format ("{0} does not contain constructor({1}) argument match",
				klass.UnderlyingSystemType, constructorName));
			LuaLib.LuaPushNil (luaState);
			return 1;
		}
		static bool IsInteger(double x) {
			return Math.Ceiling(x) == x;	
		}

		static object GetTargetObject (LuaState luaState, string operation, ObjectTranslator translator)
		{
			Type t;
			object target = translator.GetRawNetObject (luaState, 1);
			if (target != null) {
				t = target.GetType ();
				if (t.HasMethod (operation))
					return target;
			}
			target = translator.GetRawNetObject (luaState, 2);
			if (target != null) {
				t = target.GetType ();
				if (t.HasMethod (operation))
					return target;
			}
			return null;
		}
		
		static int MatchOperator (LuaState luaState, string operation, ObjectTranslator translator)
		{
			var validOperator = new MethodCache ();

			object target = GetTargetObject (luaState, operation, translator);

			if (target == null) {
				translator.ThrowError (luaState, "Cannot call " + operation + " on a nil object");
				LuaLib.LuaPushNil (luaState);
				return 1;
			}

			Type type = target.GetType ();
			var operators = type.GetMethods (operation, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

			foreach (var op in operators) {
				bool isOk = translator.MatchParameters (luaState, op, ref validOperator);

				if (!isOk)
					continue;

				object result;
				if (op.IsStatic)
					result = op.Invoke (null, validOperator.args);
				else
					result = op.Invoke (target, validOperator.args);
				translator.Push (luaState, result);
				return 1;
			}

			translator.ThrowError (luaState, "Cannot call (" + operation + ") on object type " + type.Name);
			LuaLib.LuaPushNil (luaState);
			return 1;
		}



		internal Array TableToArray (Func<int, object> luaParamValueExtractor, Type paramArrayType, int startIndex, int count)
		{
			Array paramArray;

			if (count == 0)
				return Array.CreateInstance (paramArrayType, 0);

			var luaParamValue = luaParamValueExtractor (startIndex);

			if (luaParamValue is LuaTable) {
				LuaTable table = (LuaTable)luaParamValue;
				IDictionaryEnumerator tableEnumerator = table.GetEnumerator ();
				tableEnumerator.Reset ();
				paramArray = Array.CreateInstance (paramArrayType, table.Values.Count);

				int paramArrayIndex = 0;

				while (tableEnumerator.MoveNext ()) {

					object value = tableEnumerator.Value;

					if (paramArrayType == typeof (object)) {
						if (value != null && value.GetType () == typeof (double) && IsInteger ((double)value))
							value = Convert.ToInt32 ((double)value);
					}
#if SILVERLIGHT
					paramArray.SetValue (Convert.ChangeType (value, paramArrayType, System.Globalization.CultureInfo.InvariantCulture), paramArrayIndex);
#else
					paramArray.SetValue (Convert.ChangeType (value, paramArrayType), paramArrayIndex);
#endif
					paramArrayIndex++;
				}
			} else {

				paramArray = Array.CreateInstance (paramArrayType, count);

				paramArray.SetValue (luaParamValue, 0);

				for (int i = 1; i < count; i++) {
					startIndex++;
					var value = luaParamValueExtractor (startIndex);
					paramArray.SetValue (value, i);
				}
			}

			return paramArray;

		}
		
		/*
		 * Matches a method against its arguments in the Lua stack. Returns
		 * if the match was successful. It it was also returns the information
		 * necessary to invoke the method.
		 */
		internal bool MatchParameters (LuaState luaState, MethodBase method, ref MethodCache methodCache)
		{
			ExtractValue extractValue;
			bool isMethod = true;
			var paramInfo = method.GetParameters ();
			int currentLuaParam = 1;
			int nLuaParams = LuaLib.LuaGetTop (luaState);
			var paramList = new List<object> ();
			var outList = new List<int> ();
			var argTypes = new List<MethodArgs> ();

			foreach (var currentNetParam in paramInfo) {
#if !SILVERLIGHT
				if (!currentNetParam.IsIn && currentNetParam.IsOut)  // Skips out params 
#else
				if (currentNetParam.IsOut)  // Skips out params
#endif
				{					
					paramList.Add (null);
					outList.Add (paramList.LastIndexOf (null));
				}  else if (IsTypeCorrect (luaState, currentLuaParam, currentNetParam, out extractValue)) {  // Type checking
					var value = extractValue (luaState, currentLuaParam);
					paramList.Add (value);
					int index = paramList.LastIndexOf (value);
					var methodArg = new MethodArgs ();
					methodArg.index = index;
					methodArg.extractValue = extractValue;
					argTypes.Add (methodArg);

					if (currentNetParam.ParameterType.IsByRef)
						outList.Add (index);

					currentLuaParam++;
				}  // Type does not match, ignore if the parameter is optional
				else if (IsParamsArray (luaState, currentLuaParam, currentNetParam, out extractValue)) {

					var paramArrayType = currentNetParam.ParameterType.GetElementType ();

					Func<int, object> extractDelegate = (currentParam) => {
						currentLuaParam ++;
						return extractValue (luaState, currentParam);
					};
					int count = (nLuaParams - currentLuaParam) + 1;
					Array paramArray = TableToArray (extractDelegate, paramArrayType, currentLuaParam, count);

					paramList.Add (paramArray);
					int index = paramList.LastIndexOf (paramArray);
					var methodArg = new MethodArgs ();
					methodArg.index = index;
					methodArg.extractValue = extractValue;
					methodArg.isParamsArray = true;
					methodArg.paramsArrayType = paramArrayType;
					argTypes.Add (methodArg);

				} else if (currentLuaParam > nLuaParams) { // Adds optional parameters
					if (currentNetParam.IsOptional)
						paramList.Add (currentNetParam.DefaultValue);
					else {
						isMethod = false;
						break;
					}
				} else if (currentNetParam.IsOptional)
					paramList.Add (currentNetParam.DefaultValue);
				else {  // No match
					isMethod = false;
					break;
				}
			}

			if (currentLuaParam != nLuaParams + 1) // Number of parameters does not match
				isMethod = false;
			if (isMethod) {
				methodCache.args = paramList.ToArray ();
				methodCache.cachedMethod = method;
				methodCache.outList = outList.ToArray ();
				methodCache.argTypes = argTypes.ToArray ();
			}
			return isMethod;
		}

		/// <summary>
		/// CP: Fix for operator overloading failure
		/// Returns true if the type is set and assigns the extract value
		/// </summary>
		/// <param name="luaState"></param>
		/// <param name="currentLuaParam"></param>
		/// <param name="currentNetParam"></param>
		/// <param name="extractValue"></param>
		/// <returns></returns>
		private bool IsTypeCorrect (LuaState luaState, int currentLuaParam, ParameterInfo currentNetParam, out ExtractValue extractValue)
		{
			try {
				return (extractValue = translator.typeChecker.CheckLuaType (luaState, currentLuaParam, currentNetParam.ParameterType)) != null;
			} catch {
				extractValue = null;
				Debug.WriteLine ("Type wasn't correct");
				return false;
			}
		}

		private bool IsParamsArray (LuaState luaState, int currentLuaParam, ParameterInfo currentNetParam, out ExtractValue extractValue)
		{
			extractValue = null;

			if (currentNetParam.GetCustomAttributes (typeof(ParamArrayAttribute), false).Length > 0) {
				LuaTypes luaType;

				try {
					luaType = LuaLib.LuaType (luaState, currentLuaParam);
				} catch (Exception ex) {
					Debug.WriteLine ("Could not retrieve lua type while attempting to determine params Array Status.");
					Debug.WriteLine (ex.Message);
					extractValue = null;
					return false;
				}

				if (luaType == LuaTypes.Table) {
					try {
						extractValue = translator.typeChecker.GetExtractor (typeof(LuaTable));
					} catch (Exception/* ex*/) {
						Debug.WriteLine ("An error occurred during an attempt to retrieve a LuaTable extractor while checking for params array status.");
					}

					if (extractValue != null) {
						return true;
					}
				} else {
					var paramElementType = currentNetParam.ParameterType.GetElementType ();

					try {
						extractValue = translator.typeChecker.CheckLuaType (luaState, currentLuaParam, paramElementType);
					} catch (Exception/* ex*/) {
						Debug.WriteLine (string.Format ("An error occurred during an attempt to retrieve an extractor ({0}) while checking for params array status.", paramElementType.FullName));
					}

					if (extractValue != null) {
						return true;
					}
				}
			}

			Debug.WriteLine ("Type wasn't Params object.");
			return false;
		}
	}
}