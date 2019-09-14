﻿using System;
using System.Linq;
using System.Collections;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using KeraLua;

using NLua.Method;
using NLua.Extensions;

#if __IOS__ || __TVOS__ || __WATCHOS__
    using ObjCRuntime;
#endif

using LuaState = KeraLua.Lua;
using LuaNativeFunction = KeraLua.LuaFunction;

namespace NLua
{
    public class MetaFunctions
    {
        public static readonly LuaNativeFunction GcFunction = CollectObject;
        public static readonly LuaNativeFunction IndexFunction  = GetMethod;
        public static readonly LuaNativeFunction NewIndexFunction = SetFieldOrProperty;
        public static readonly LuaNativeFunction BaseIndexFunction  = GetBaseMethod;
        public static readonly LuaNativeFunction ClassIndexFunction  = GetClassMethod;
        public static readonly LuaNativeFunction ClassNewIndexFunction  = SetClassFieldOrProperty;
        public static readonly LuaNativeFunction ExecuteDelegateFunction  = RunFunctionDelegate;
        public static readonly LuaNativeFunction CallConstructorFunction  = CallConstructor;
        public static readonly LuaNativeFunction ToStringFunction = ToStringLua;
        public static readonly LuaNativeFunction CallDelegateFunction = CallDelegate;
        public static readonly LuaNativeFunction CallInvalidFunction  = CallInvalidMethod;

        public static readonly LuaNativeFunction AddFunction = AddLua;
        public static readonly LuaNativeFunction SubtractFunction = SubtractLua;
        public static readonly LuaNativeFunction MultiplyFunction = MultiplyLua;
        public static readonly LuaNativeFunction DivisionFunction = DivideLua;
        public static readonly LuaNativeFunction ModulosFunction = ModLua;
        public static readonly LuaNativeFunction UnaryNegationFunction = UnaryNegationLua;
        public static readonly LuaNativeFunction EqualFunction = EqualLua;
        public static readonly LuaNativeFunction LessThanFunction  = LessThanLua;
        public static readonly LuaNativeFunction LessThanOrEqualFunction = LessThanOrEqualLua;

        readonly Dictionary<object, Dictionary<object, object>> _memberCache = new Dictionary<object, Dictionary<object, object>>();
        readonly ObjectTranslator _translator;

        /*
         * __index metafunction for CLR objects. Implemented in Lua.
         */
        public const string LuaIndexFunction = @"local function a(b,c)local d=getmetatable(b)local e=d.cache[c]if e~=nil then return e else local f,g=get_object_member(b,c)if g then d.cache[c]=f end;return f end end;return a";
            //@"local function index(obj,name)
            //    local meta = getmetatable(obj)
            //    local cached = meta.cache[name]
            //    if cached ~= nil then
            //       return cached
            //    else
            //       local value,isFunc = get_object_member(obj,name)
                   
            //       if isFunc then
            //        meta.cache[name]=value
            //       end
            //       return value
            //     end
            //end
            //return index";

        public MetaFunctions(ObjectTranslator translator)
        {
            _translator = translator;
        }

        /*
         * __call metafunction of CLR delegates, retrieves and calls the delegate.
         */
#if __IOS__ || __TVOS__ || __WATCHOS__
        [MonoPInvokeCallback(typeof(LuaNativeFunction))]
#endif
        private static int RunFunctionDelegate(IntPtr luaState)
        {
            var state = LuaState.FromIntPtr(luaState);
            var translator = ObjectTranslatorPool.Instance.Find(state);
            var func = (LuaNativeFunction)translator.GetRawNetObject(state, 1);
            state.Remove(1);
            return func(luaState);
        }

        /*
         * __gc metafunction of CLR objects.
         */
#if __IOS__ || __TVOS__ || __WATCHOS__
        [MonoPInvokeCallback(typeof(LuaNativeFunction))]
#endif
        private static int CollectObject(IntPtr state)
        {
            var luaState = LuaState.FromIntPtr(state);
            var translator = ObjectTranslatorPool.Instance.Find(luaState);
            return CollectObject(luaState, translator);
        }

        private static int CollectObject(LuaState luaState, ObjectTranslator translator)
        {
            int udata = luaState.RawNetObj(1);

            if (udata != -1)
                translator.CollectObject(udata);

            return 0;
        }

        /*
         * __tostring metafunction of CLR objects.
         */
#if __IOS__ || __TVOS__ || __WATCHOS__
        [MonoPInvokeCallback(typeof(LuaNativeFunction))]
#endif
        private static int ToStringLua(IntPtr state)
        {
            var luaState = LuaState.FromIntPtr(state);
            var translator = ObjectTranslatorPool.Instance.Find(luaState);
            return ToStringLua(luaState, translator);
        }

        private static int ToStringLua(LuaState luaState, ObjectTranslator translator)
        {
            object obj = translator.GetRawNetObject(luaState, 1);

            if (obj != null)
                translator.Push(luaState, obj + ": " + obj.GetHashCode());
            else
                luaState.PushNil();

            return 1;
        }


        /*
         * __add metafunction of CLR objects.
         */
#if __IOS__ || __TVOS__ || __WATCHOS__
        [MonoPInvokeCallback(typeof(LuaNativeFunction))]
#endif
        static int AddLua(IntPtr luaState)
        {
            var state = LuaState.FromIntPtr(luaState);
            var translator = ObjectTranslatorPool.Instance.Find(state);
            return MatchOperator(state, "op_Addition", translator);
        }

        /*
        * __sub metafunction of CLR objects.
        */
#if __IOS__ || __TVOS__ || __WATCHOS__
        [MonoPInvokeCallback(typeof(LuaNativeFunction))]
#endif
        static int SubtractLua(IntPtr luaState)
        {
            var state = LuaState.FromIntPtr(luaState);
            var translator = ObjectTranslatorPool.Instance.Find(state);
            return MatchOperator(state, "op_Subtraction", translator);
        }

        /*
        * __mul metafunction of CLR objects.
        */
#if __IOS__ || __TVOS__ || __WATCHOS__
        [MonoPInvokeCallback(typeof(LuaNativeFunction))]
#endif
        static int MultiplyLua(IntPtr luaState)
        {
            var state = LuaState.FromIntPtr(luaState);
            var translator = ObjectTranslatorPool.Instance.Find(state);
            return MatchOperator(state, "op_Multiply", translator);
        }

        /*
        * __div metafunction of CLR objects.
        */
#if __IOS__ || __TVOS__ || __WATCHOS__
        [MonoPInvokeCallback(typeof(LuaNativeFunction))]
#endif
        static int DivideLua(IntPtr luaState)
        {
            var state = LuaState.FromIntPtr(luaState);
            var translator = ObjectTranslatorPool.Instance.Find(state);
            return MatchOperator(state, "op_Division", translator);
        }

        /*
        * __mod metafunction of CLR objects.
        */
#if __IOS__ || __TVOS__ || __WATCHOS__
        [MonoPInvokeCallback(typeof(LuaNativeFunction))]
#endif
        static int ModLua(IntPtr luaState)
        {
            var state = LuaState.FromIntPtr(luaState);
            var translator = ObjectTranslatorPool.Instance.Find(state);
            return MatchOperator(state, "op_Modulus", translator);
        }

        /*
        * __unm metafunction of CLR objects.
        */
#if __IOS__ || __TVOS__ || __WATCHOS__
        [MonoPInvokeCallback(typeof(LuaNativeFunction))]
#endif
        static int UnaryNegationLua(IntPtr luaState)
        {
            var state = LuaState.FromIntPtr(luaState);
            var translator = ObjectTranslatorPool.Instance.Find(state);
            return UnaryNegationLua(state, translator);
        }

        static int UnaryNegationLua(LuaState luaState, ObjectTranslator translator)
        {
            object obj1 = translator.GetRawNetObject(luaState, 1);

            if (obj1 == null)
            {
                translator.ThrowError(luaState, "Cannot negate a nil object");
                luaState.PushNil();
                return 1;
            }

            Type type = obj1.GetType();
            MethodInfo opUnaryNegation = type.GetMethod("op_UnaryNegation");

            if (opUnaryNegation == null)
            {
                translator.ThrowError(luaState, "Cannot negate object (" + type.Name + " does not overload the operator -)");
                luaState.PushNil();
                return 1;
            }
            obj1 = opUnaryNegation.Invoke(obj1, new [] { obj1 });
            translator.Push(luaState, obj1);
            return 1;
        }


        /*
        * __eq metafunction of CLR objects.
        */
#if __IOS__ || __TVOS__ || __WATCHOS__
        [MonoPInvokeCallback(typeof(LuaNativeFunction))]
#endif
        static int EqualLua(IntPtr luaState)
        {
            var state = LuaState.FromIntPtr(luaState);
            var translator = ObjectTranslatorPool.Instance.Find(state);
            return MatchOperator(state, "op_Equality", translator);
        }

        /*
        * __lt metafunction of CLR objects.
        */
#if __IOS__ || __TVOS__ || __WATCHOS__
        [MonoPInvokeCallback(typeof(LuaNativeFunction))]
#endif
        static int LessThanLua(IntPtr luaState)
        {
            var state = LuaState.FromIntPtr(luaState);
            var translator = ObjectTranslatorPool.Instance.Find(state);
            return MatchOperator(state, "op_LessThan", translator);
        }

        /*
         * __le metafunction of CLR objects.
         */
#if __IOS__ || __TVOS__ || __WATCHOS__
        [MonoPInvokeCallback(typeof(LuaNativeFunction))]
#endif
        static int LessThanOrEqualLua(IntPtr luaState)
        {
            var state = LuaState.FromIntPtr(luaState);
            var translator = ObjectTranslatorPool.Instance.Find(state);
            return MatchOperator(state, "op_LessThanOrEqual", translator);
        }

        /// <summary>
        /// Debug tool to dump the lua stack
        /// </summary>
        /// FIXME, move somewhere else
        public static void DumpStack(ObjectTranslator translator, LuaState luaState)
        {
            int depth = luaState.GetTop();

            Debug.WriteLine("lua stack depth: {0}", depth);

            for (int i = 1; i <= depth; i++)
            {
                var type = luaState.Type(i);
                // we dump stacks when deep in calls, calling typename while the stack is in flux can fail sometimes, so manually check for key types
                string typestr = (type == LuaType.Table) ? "table" : luaState.TypeName(type);
                string strrep = luaState.ToString(i, false);

                if (type == LuaType.UserData)
                {
                    object obj = translator.GetRawNetObject(luaState, i);
                    strrep = obj.ToString();
                }

                Debug.WriteLine("{0}: ({1}) {2}", i, typestr, strrep);
            }
        }

        /*
         * Called by the __index metafunction of CLR objects in case the
         * method is not cached or it is a field/property/event.
         * Receives the object and the member name as arguments and returns
         * either the value of the member or a delegate to call it.
         * If the member does not exist returns nil.
         */
#if __IOS__ || __TVOS__ || __WATCHOS__
        [MonoPInvokeCallback(typeof(LuaNativeFunction))]
#endif
        private static int GetMethod(IntPtr state)
        {
            var luaState = LuaState.FromIntPtr(state);
            var translator = ObjectTranslatorPool.Instance.Find(luaState);
            var instance = translator.MetaFunctionsInstance;
            return instance.GetMethodInternal(luaState);
        }

        private int GetMethodInternal(LuaState luaState)
        {
            object obj = _translator.GetRawNetObject(luaState, 1);

            if (obj == null)
            {
                _translator.ThrowError(luaState, "Trying to index an invalid object reference");
                luaState.PushNil();
                return 1;
            }

            object index = _translator.GetObject(luaState, 2);
            string methodName = index as string; // will be null if not a string arg
            var objType = obj.GetType();
            var proxyType = new ProxyType(objType);

            // Handle the most common case, looking up the method by name. 
            // CP: This will fail when using indexers and attempting to get a value with the same name as a property of the object, 
            // ie: xmlelement['item'] <- item is a property of xmlelement

            if (!string.IsNullOrEmpty(methodName) && IsMemberPresent(proxyType, methodName))
                return GetMember(luaState, proxyType, obj, methodName, BindingFlags.Instance);

            // Try to access by array if the type is right and index is an int (lua numbers always come across as double)
            if (TryAccessByArray(luaState, objType, obj, index))
                return 1;

            int fallback = GetMethodFallback(luaState, objType, obj, index, methodName);
            if (fallback != 0)
                return fallback;

            if (!string.IsNullOrEmpty(methodName))
            {
                return PushInvalidMethodCall(luaState, objType, methodName);
            }

            luaState.PushBoolean(false);
            return 2;
        }

        private int PushInvalidMethodCall(LuaState luaState, Type type, string name)
        {
            var invokeDelegate = CallInvalidFunction;

            SetMemberCache(type, name, invokeDelegate);

            _translator.PushFunction(luaState, invokeDelegate);
            _translator.Push(luaState, false);
            return 2;
        }

        private bool TryAccessByArray(LuaState luaState,
            Type objType,
            object obj,
            object index)
        {
            if (!objType.IsArray)
                return false;

            int intIndex = -1;
            if (index is long l)
                intIndex = (int)l;
            else if (index is double d)
                intIndex = (int)d;

            if (intIndex == -1)
                return false;

            Type type = objType.UnderlyingSystemType;

            if (type == typeof(long[]))
            {
                long[] arr = (long[])obj;
                _translator.Push(luaState, arr[intIndex]);
                return true;
            }
            if (type == typeof(float[]))
            {
                float[] arr = (float[])obj;
                _translator.Push(luaState, arr[intIndex]);
                return true;
            }
            if (type == typeof(double[]))
            {
                double[] arr = (double[])obj;
                _translator.Push(luaState, arr[intIndex]);
                return true;
            }
            if (type == typeof(int[]))
            {
                int[] arr = (int[])obj;
                _translator.Push(luaState, arr[intIndex]);
                return true;
            }
            if (type == typeof(byte[]))
            {
                byte[] arr = (byte[])obj;
                _translator.Push(luaState, arr[intIndex]);
                return true;
            }
            if (type == typeof(short[]))
            {
                short[] arr = (short[])obj;
                _translator.Push(luaState, arr[intIndex]);
                return true;
            }
            if (type == typeof(ushort[]))
            {
                ushort[] arr = (ushort[])obj;
                _translator.Push(luaState, arr[intIndex]);
                return true;
            }
            if (type == typeof(ulong[]))
            {
                ulong[] arr = (ulong[])obj;
                _translator.Push(luaState, arr[intIndex]);
                return true;
            }
            if (type == typeof(uint[]))
            {
                uint[] arr = (uint[])obj;
                _translator.Push(luaState, arr[intIndex]);
                return true;
            }
            if (type == typeof(sbyte[]))
            {
                sbyte[] arr = (sbyte[])obj;
                _translator.Push(luaState, arr[intIndex]);
                return true;
            }

            object[] arrObj = (object[])obj;
            _translator.Push(luaState, arrObj[intIndex]);
            return true;
        }

        private int GetMethodFallback
        (LuaState luaState,
         Type objType,
         object obj,
         object index,
         string methodName)
        {
            object method;
            if (!string.IsNullOrEmpty(methodName) && TryGetExtensionMethod(objType, methodName, out method))
            {
                return PushExtensionMethod(luaState, objType, obj, methodName, method);
            }
            // Try to use get_Item to index into this .net object
            MethodInfo[] methods = objType.GetMethods();

            int res = TryIndexMethods(luaState, methods, obj, index);
            if (res != 0)
                return res;

            // Fallback to GetRuntimeMethods
            methods = objType.GetRuntimeMethods().ToArray();

            res = TryIndexMethods(luaState, methods, obj, index);
            if (res != 0)
                return res;

            res = TryGetValueForKeyMethods(luaState, methods, obj, index);
            if (res != 0)
                return res;

            // Try find explicity interface implementation
            MethodInfo explicitInterfaceMethod = objType.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance).
                                                 FirstOrDefault(m => m.Name == methodName && m.IsPrivate && m.IsVirtual && m.IsFinal);

            if (explicitInterfaceMethod != null)
            {
                var proxyType = new ProxyType(objType);
                var methodWrapper = new LuaMethodWrapper(_translator, obj, proxyType, explicitInterfaceMethod);
                var invokeDelegate = new LuaNativeFunction(methodWrapper.InvokeFunction);

                SetMemberCache(proxyType, methodName, invokeDelegate);

                _translator.PushFunction(luaState, invokeDelegate);
                _translator.Push(luaState, true);
                return 2;
            }

            return 0;
        }

        private int TryGetValueForKeyMethods(LuaState luaState, MethodInfo[] methods, object obj, object index)
        {
            foreach (MethodInfo methodInfo in methods)
            {
                if (methodInfo.Name != "TryGetValueForKey")
                    continue;

                // Check if the signature matches the input
                if (methodInfo.GetParameters().Length != 2)
                    continue;

                ParameterInfo[] actualParams = methodInfo.GetParameters();

                // Get the index in a form acceptable to the getter
                index = _translator.GetAsType(luaState, 2, actualParams[0].ParameterType);

                // If the index type and the parameter doesn't match, just skip it
                if (index == null)
                    break;

                object[] args = new object[2];

                // Just call the indexer - if out of bounds an exception will happen
                args[0] = index;

                try
                {
                    bool found = (bool)methodInfo.Invoke(obj, args);

                    if (!found)
                    {
                        _translator.ThrowError(luaState, "key not found: " + index);
                        luaState.PushNil();
                        return 1;
                    }

                    _translator.Push(luaState, args[1]);
                    return 1;
                }
                catch (TargetInvocationException e)
                {
                    // Provide a more readable description for the common case of key not found
                    if (e.InnerException is KeyNotFoundException)
                        _translator.ThrowError(luaState, "key '" + index + "' not found ");
                    else
                        _translator.ThrowError(luaState, "exception indexing '" + index + "' " + e.Message);

                    luaState.PushNil();
                    return 1;
                }
            }
            return 0;
        }


        private int TryIndexMethods(LuaState luaState, MethodInfo [] methods, object obj, object index)
        {
            foreach (MethodInfo methodInfo in methods)
            {
                if (methodInfo.Name != "get_Item")
                    continue;

                // Check if the signature matches the input
                if (methodInfo.GetParameters().Length != 1)
                    continue;

                ParameterInfo[] actualParams = methodInfo.GetParameters();

                // Get the index in a form acceptable to the getter
                index = _translator.GetAsType(luaState, 2, actualParams[0].ParameterType);

                // If the index type and the parameter doesn't match, just skip it
                if (index == null)
                    break;

                object[] args = new object[1];

                // Just call the indexer - if out of bounds an exception will happen
                args[0] = index;

                try
                {
                    object result = methodInfo.Invoke(obj, args);
                    _translator.Push(luaState, result);
                    return 1;
                }
                catch (TargetInvocationException e)
                {
                    // Provide a more readable description for the common case of key not found
                    if (e.InnerException is KeyNotFoundException)
                        _translator.ThrowError(luaState, "key '" + index + "' not found ");
                    else
                        _translator.ThrowError(luaState, "exception indexing '" + index + "' " + e.Message);

                    luaState.PushNil();
                    return 1;
                }
            }
            return 0;
        }

        /*
         * __index metafunction of base classes (the base field of Lua tables).
         * Adds a prefix to the method name to call the base version of the method.
         */
#if __IOS__ || __TVOS__ || __WATCHOS__
        [MonoPInvokeCallback(typeof(LuaNativeFunction))]
#endif
        private static int GetBaseMethod(IntPtr state)
        {
            var luaState = LuaState.FromIntPtr(state);
            var translator = ObjectTranslatorPool.Instance.Find(luaState);
            var instance = translator.MetaFunctionsInstance;
            return instance.GetBaseMethodInternal(luaState);
        }

        private int GetBaseMethodInternal(LuaState luaState)
        {
            object obj = _translator.GetRawNetObject(luaState, 1);

            if (obj == null)
            {
                _translator.ThrowError(luaState, "Trying to index an invalid object reference");
                luaState.PushNil();
                luaState.PushBoolean(false);
                return 2;
            }

            string methodName = luaState.ToString(2, false);

            if (string.IsNullOrEmpty(methodName))
            {
                luaState.PushNil();
                luaState.PushBoolean(false);
                return 2;
            }

            GetMember(luaState, new ProxyType(obj.GetType()), obj, "__luaInterface_base_" + methodName, BindingFlags.Instance);
            luaState.SetTop(-2);

            if (luaState.Type(-1) == LuaType.Nil)
            {
                luaState.SetTop(-2);
                return GetMember(luaState, new ProxyType(obj.GetType()), obj, methodName, BindingFlags.Instance);
            }

            luaState.PushBoolean(false);
            return 2;
        }

        /// <summary>
        /// Does this method exist as either an instance or static?
        /// </summary>
        /// <param name="objType"></param>
        /// <param name="methodName"></param>
        /// <returns></returns>
        bool IsMemberPresent(ProxyType objType, string methodName)
        {
            object cachedMember = CheckMemberCache(objType, methodName);

            if (cachedMember != null)
                return true;

            var members = objType.GetMember(methodName, BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public);
            return members.Length > 0;
        }

        bool TryGetExtensionMethod(Type type, string name, out object method)
        {
            object cachedMember = CheckMemberCache(type, name);

            if (cachedMember != null)
            {
                method = cachedMember;
                return true;
            }

            MethodInfo methodInfo;
            bool found = _translator.TryGetExtensionMethod(type, name, out methodInfo);
            method = methodInfo;
            return found;
        }

        int PushExtensionMethod(LuaState luaState, Type type, object obj, string name, object method)
        {
            var cachedMember = method as LuaNativeFunction;

            if (cachedMember != null)
            {
                _translator.PushFunction(luaState, cachedMember);
                _translator.Push(luaState, true);
                return 2;
            }

            var methodInfo = (MethodInfo)method;
            var methodWrapper = new LuaMethodWrapper(_translator, obj, new ProxyType(type), methodInfo);
            var invokeDelegate = new LuaNativeFunction(methodWrapper.InvokeFunction);

            SetMemberCache(type, name, invokeDelegate);

            _translator.PushFunction(luaState, invokeDelegate);
            _translator.Push(luaState, true);
            return 2;
        }

        /*
         * Pushes the value of a member or a delegate to call it, depending on the type of
         * the member. Works with static or instance members.
         * Uses reflection to find members, and stores the reflected MemberInfo object in
         * a cache (indexed by the type of the object and the name of the member).
         */
        int GetMember(LuaState luaState, ProxyType objType, object obj, string methodName, BindingFlags bindingType)
        {
            bool implicitStatic = false;
            MemberInfo member = null;
            object cachedMember = CheckMemberCache(objType, methodName);

            if (cachedMember is LuaNativeFunction)
            {
                _translator.PushFunction(luaState, (LuaNativeFunction)cachedMember);
                _translator.Push(luaState, true);
                return 2;
            }
            if (cachedMember != null)
                member = (MemberInfo)cachedMember;
            else
            {
                var members = objType.GetMember(methodName, bindingType | BindingFlags.Public);

                if (members.Length > 0)
                    member = members[0];
                else
                {
                    // If we can't find any suitable instance members, try to find them as statics - but we only want to allow implicit static
                    members = objType.GetMember(methodName, bindingType | BindingFlags.Static | BindingFlags.Public);

                    if (members.Length > 0)
                    {
                        member = members[0];
                        implicitStatic = true;
                    }
                }
            }

            if (member != null)
            {
                if (member.MemberType == MemberTypes.Field)
                {
                    var field = (FieldInfo)member;

                    if (cachedMember == null)
                        SetMemberCache(objType, methodName, member);

                    try
                    {
                        var value = field.GetValue(obj);
                        _translator.Push(luaState, value);
                    }
                    catch
                    {
                        Debug.WriteLine("[Exception] Fail to get field value");
                        luaState.PushNil();
                    }
                }
                else if (member.MemberType == MemberTypes.Property)
                {
                    var property = (PropertyInfo)member;
                    if (cachedMember == null)
                        SetMemberCache(objType, methodName, member);

                    try
                    {
                        object value = property.GetValue(obj, null);
                        _translator.Push(luaState, value);
                    }
                    catch (ArgumentException)
                    {
                        // If we can't find the getter in our class, recurse up to the base class and see
                        // if they can help.
                        if (objType.UnderlyingSystemType != typeof(object))
                            return GetMember(luaState, new ProxyType(objType.UnderlyingSystemType.BaseType), obj, methodName, bindingType);
                        luaState.PushNil();
                    }
                    catch (TargetInvocationException e)
                    {  // Convert this exception into a Lua error
                        ThrowError(luaState, e);
                        luaState.PushNil();
                    }
                }
                else if (member.MemberType == MemberTypes.Event)
                {
                    var eventInfo = (EventInfo)member;
                    if (cachedMember == null)
                        SetMemberCache(objType, methodName, member);

                    _translator.Push(luaState, new RegisterEventHandler(_translator.PendingEvents, obj, eventInfo));
                }
                else if (!implicitStatic)
                {
                    if (member.MemberType == MemberTypes.NestedType && member.DeclaringType != null)
                    {
                        if (cachedMember == null)
                            SetMemberCache(objType, methodName, member);

                        // Find the name of our class
                        string name = member.Name;
                        Type decType = member.DeclaringType;

                        // Build a new long name and try to find the type by name
                        string longName = decType.FullName + "+" + name;
                        var nestedType = _translator.FindType(longName);
                        _translator.PushType(luaState, nestedType);
                    }
                    else
                    {
                        // Member type must be 'method'
                        var methodWrapper = new LuaMethodWrapper(_translator, objType, methodName, bindingType);
                        var wrapper = methodWrapper.InvokeFunction;

                        if (cachedMember == null)
                            SetMemberCache(objType, methodName, wrapper);

                        _translator.PushFunction(luaState, wrapper);
                        _translator.Push(luaState, true);
                        return 2;
                    }
                }
                else
                {
                    // If we reach this point we found a static method, but can't use it in this context because the user passed in an instance
                    _translator.ThrowError(luaState, "Can't pass instance to static method " + methodName);
                    luaState.PushNil();
                }
            }
            else
            {
                if (objType.UnderlyingSystemType != typeof(object))
                    return GetMember(luaState, new ProxyType(objType.UnderlyingSystemType.BaseType), obj, methodName, bindingType);

                // We want to throw an exception because merely returning 'nil' in this case
                // is not sufficient.  valid data members may return nil and therefore there must be some
                // way to know the member just doesn't exist.
                _translator.ThrowError(luaState, "Unknown member name " + methodName);
                luaState.PushNil();
            }

            // Push false because we are NOT returning a function (see luaIndexFunction)
            _translator.Push(luaState, false);
            return 2;
        }

        /*
         * Checks if a MemberInfo object is cached, returning it or null.
         */
        object CheckMemberCache(Type objType, string memberName)
        {
            return CheckMemberCache(new ProxyType(objType), memberName);
        }

        object CheckMemberCache(ProxyType objType, string memberName)
        {
            Dictionary<object, object> members;

            if (!_memberCache.TryGetValue(objType, out members))
                return null;

            object memberValue;

            if (members == null || !members.TryGetValue(memberName, out memberValue))
                return null;

            return memberValue;
        }

        /*
         * Stores a MemberInfo object in the member cache.
         */
        void SetMemberCache(Type objType, string memberName, object member)
        {
            SetMemberCache(new ProxyType(objType), memberName, member);
        }

        void SetMemberCache(ProxyType objType, string memberName, object member)
        {
            Dictionary<object, object> members;
            Dictionary<object, object> memberCacheValue;

            if (_memberCache.TryGetValue(objType, out memberCacheValue))
            {
                members = memberCacheValue;
            }
            else
            {
                members = new Dictionary<object, object>();
                _memberCache[objType] = members;
            }

            members[memberName] = member;
        }

        /*
         * __newindex metafunction of CLR objects. Receives the object,
         * the member name and the value to be stored as arguments. Throws
         * and error if the assignment is invalid.
         */
#if __IOS__ || __TVOS__ || __WATCHOS__
        [MonoPInvokeCallback(typeof(LuaNativeFunction))]
#endif
        private static int SetFieldOrProperty(IntPtr state)
        {
            var luaState = LuaState.FromIntPtr(state);
            var translator = ObjectTranslatorPool.Instance.Find(luaState);
            var instance = translator.MetaFunctionsInstance;
            return instance.SetFieldOrPropertyInternal(luaState);
        }

        private int SetFieldOrPropertyInternal(LuaState luaState)
        {
            object target = _translator.GetRawNetObject(luaState, 1);

            if (target == null)
            {
                _translator.ThrowError(luaState, "trying to index and invalid object reference");
                return 0;
            }

            var type = target.GetType();

            // First try to look up the parameter as a property name
            string detailMessage;
            bool didMember = TrySetMember(luaState, new ProxyType(type), target, BindingFlags.Instance, out detailMessage);

            if (didMember)
                return 0;      // Must have found the property name

            // We didn't find a property name, now see if we can use a [] style this accessor to set array contents
            try
            {
                if (type.IsArray && luaState.IsNumber(2))
                {
                    int index = (int)luaState.ToNumber(2);
                    var arr = (Array)target;
                    object val = _translator.GetAsType(luaState, 3, arr.GetType().GetElementType());
                    arr.SetValue(val, index);
                }
                else
                {
                    // Try to see if we have a this[] accessor
                    var setter = type.GetMethod("set_Item");
                    if (setter != null)
                    {
                        var args = setter.GetParameters();
                        var valueType = args[1].ParameterType;

                        // The new value the user specified 
                        object val = _translator.GetAsType(luaState, 3, valueType);
                        var indexType = args[0].ParameterType;
                        object index = _translator.GetAsType(luaState, 2, indexType);

                        object[] methodArgs = new object[2];

                        // Just call the indexer - if out of bounds an exception will happen
                        methodArgs[0] = index;
                        methodArgs[1] = val;
                        setter.Invoke(target, methodArgs);
                    }
                    else
                        _translator.ThrowError(luaState, detailMessage); // Pass the original message from trySetMember because it is probably best
                }
            }
            catch (Exception e)
            {
                ThrowError(luaState, e);
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
        bool TrySetMember(LuaState luaState, ProxyType targetType, object target, BindingFlags bindingType, out string detailMessage)
        {
            detailMessage = null;   // No error yet

            // If not already a string just return - we don't want to call tostring - which has the side effect of 
            // changing the lua typecode to string
            // Note: We don't use isstring because the standard lua C isstring considers either strings or numbers to
            // be true for isstring.
            if (luaState.Type(2) != LuaType.String)
            {
                detailMessage = "property names must be strings";
                return false;
            }

            // We only look up property names by string
            string fieldName = luaState.ToString(2, false);
            if (string.IsNullOrEmpty(fieldName) || !(char.IsLetter(fieldName[0]) || fieldName[0] == '_'))
            {
                detailMessage = "Invalid property name";
                return false;
            }

            // Find our member via reflection or the cache
            var member = (MemberInfo)CheckMemberCache(targetType, fieldName);
            if (member == null)
            {
                var members = targetType.GetMember(fieldName, bindingType | BindingFlags.Public);

                if (members.Length <= 0)
                {
                    detailMessage = "field or property '" + fieldName + "' does not exist";
                    return false;
                }

                member = members[0];
                SetMemberCache(targetType, fieldName, member);
            }

            if (member.MemberType == MemberTypes.Field)
            {
                var field = (FieldInfo)member;
                object val = _translator.GetAsType(luaState, 3, field.FieldType);

                try
                {
                    field.SetValue(target, val);
                }
                catch (Exception e)
                {
                    ThrowError(luaState, e);
                }

                return true;
            }
            if (member.MemberType == MemberTypes.Property)
            {
                var property = (PropertyInfo)member;
                object val = _translator.GetAsType(luaState, 3, property.PropertyType);

                try
                {
                    property.SetValue(target, val, null);
                }
                catch (Exception e)
                {
                    ThrowError(luaState, e);
                }

                return true;
            }

            detailMessage = "'" + fieldName + "' is not a .net field or property";
            return false;
        }

        /*
         * Writes to fields or properties, either static or instance. Throws an error
         * if the operation is invalid.
         */
        private int SetMember(LuaState luaState, ProxyType targetType, object target, BindingFlags bindingType)
        {
            string detail;
            bool success = TrySetMember(luaState, targetType, target, bindingType, out detail);

            if (!success)
                _translator.ThrowError(luaState, detail);

            return 0;
        }

        /// <summary>
        /// Convert a C# exception into a Lua error
        /// </summary>
        /// <param name="e"></param>
        /// <param name="luaState"></param>
        /// We try to look into the exception to give the most meaningful description
        void ThrowError(LuaState luaState, Exception e)
        {
            // If we got inside a reflection show what really happened
            var te = e as TargetInvocationException;

            if (te != null)
                e = te.InnerException;

            _translator.ThrowError(luaState, e);
        }

        /*
         * __index metafunction of type references, works on static members.
         */
#if __IOS__ || __TVOS__ || __WATCHOS__
        [MonoPInvokeCallback(typeof(LuaNativeFunction))]
#endif
        private static int GetClassMethod(IntPtr state)
        {
            var luaState = LuaState.FromIntPtr(state);
            var translator = ObjectTranslatorPool.Instance.Find(luaState);
            var instance = translator.MetaFunctionsInstance;
            return instance.GetClassMethodInternal(luaState);
        }

        private int GetClassMethodInternal(LuaState luaState)
        {
            var klass = _translator.GetRawNetObject(luaState, 1) as ProxyType;

            if (klass == null)
            {
                _translator.ThrowError(luaState, "Trying to index an invalid type reference");
                luaState.PushNil();
                return 1;
            }
            
            if (luaState.IsNumber(2))
            {
                int size = (int)luaState.ToNumber(2);
                _translator.Push(luaState, Array.CreateInstance(klass.UnderlyingSystemType, size));
                return 1;
            }

            string methodName = luaState.ToString(2, false);

            if (string.IsNullOrEmpty(methodName))
            {
                luaState.PushNil();
                return 1;
            }
            return GetMember(luaState, klass, null, methodName, BindingFlags.Static);
        }

        /*
         * __newindex function of type references, works on static members.
         */
#if __IOS__ || __TVOS__ || __WATCHOS__
        [MonoPInvokeCallback(typeof(LuaNativeFunction))]
#endif
        private static int SetClassFieldOrProperty(IntPtr state)
        {
            var luaState = LuaState.FromIntPtr(state);
            var translator = ObjectTranslatorPool.Instance.Find(luaState);
            var instance = translator.MetaFunctionsInstance;
            return instance.SetClassFieldOrPropertyInternal(luaState);
        }

        private int SetClassFieldOrPropertyInternal(LuaState luaState)
        {
            var target = _translator.GetRawNetObject(luaState, 1) as ProxyType;

            if (target == null)
            {
                _translator.ThrowError(luaState, "trying to index an invalid type reference");
                return 0;
            }

            return SetMember(luaState, target, null, BindingFlags.Static);
        }

        /*
         * __call metafunction of Delegates. 
         */
#if __IOS__ || __TVOS__ || __WATCHOS__
        [MonoPInvokeCallback(typeof(LuaNativeFunction))]
#endif
        static int CallDelegate(IntPtr state)
        {
            var luaState = LuaState.FromIntPtr(state);
            var translator = ObjectTranslatorPool.Instance.Find(luaState);
            var instance = translator.MetaFunctionsInstance;
            return instance.CallDelegateInternal(luaState);
        }

/*
 * LuaNativeFunction called when the method wasn't found
 */
#if __IOS__ || __TVOS__ || __WATCHOS__
        [MonoPInvokeCallback(typeof(LuaNativeFunction))]
#endif
        static int CallInvalidMethod(IntPtr state)
        {
            var luaState = LuaState.FromIntPtr(state);
            var translator = ObjectTranslatorPool.Instance.Find(luaState);
            translator.ThrowError(luaState, "Trying to invoke invalid method");
            luaState.PushNil();
            return 1;
        }

        int CallDelegateInternal(LuaState luaState)
        {
            var del = _translator.GetRawNetObject(luaState, 1) as Delegate;

            if (del == null)
            {
                _translator.ThrowError(luaState, "Trying to invoke a not delegate or callable value");
                luaState.PushNil();
                return 1;
            }

            luaState.Remove(1);

            var validDelegate = new MethodCache();
            MethodBase methodDelegate = del.Method;
            bool isOk = MatchParameters(luaState, methodDelegate, validDelegate, 0);

            if (isOk)
            {
                object result;

                if (methodDelegate.IsStatic)
                    result = methodDelegate.Invoke(null, validDelegate.args);
                else
                    result = methodDelegate.Invoke(del.Target, validDelegate.args);

                _translator.Push(luaState, result);
                return 1;
            }

            _translator.ThrowError(luaState, "Cannot invoke delegate (invalid arguments for  " + methodDelegate.Name + ")");
            luaState.PushNil();
            return 1;
        }

        /*
         * __call metafunction of type references. Searches for and calls
         * a constructor for the type. Returns nil if the constructor is not
         * found or if the arguments are invalid. Throws an error if the constructor
         * generates an exception.
         */
#if __IOS__ || __TVOS__ || __WATCHOS__
        [MonoPInvokeCallback(typeof(LuaNativeFunction))]
#endif
        private static int CallConstructor(IntPtr state)
        {
            var luaState = LuaState.FromIntPtr(state);
            var translator = ObjectTranslatorPool.Instance.Find(luaState);
            var instance = translator.MetaFunctionsInstance;
            return instance.CallConstructorInternal(luaState);
        }

        private int CallConstructorInternal(LuaState luaState)
        {
            var klass = _translator.GetRawNetObject(luaState, 1) as ProxyType;

            if (klass == null)
            {
                _translator.ThrowError(luaState, "Trying to call constructor on an invalid type reference");
                luaState.PushNil();
                return 1;
            }

            var validConstructor = new MethodCache();

            luaState.Remove(1);
            ConstructorInfo[] constructors = klass.UnderlyingSystemType.GetConstructors();

            foreach (var constructor in constructors)
            {
                bool isConstructor = MatchParameters(luaState, constructor, validConstructor, 0);

                if (!isConstructor)
                    continue;

                try
                {
                    _translator.Push(luaState, constructor.Invoke(validConstructor.args));
                }
                catch (TargetInvocationException e)
                {
                    ThrowError(luaState, e);
                    luaState.PushNil();
                }
                catch
                {
                    luaState.PushNil();
                }
                return 1;
            }

            if (klass.UnderlyingSystemType.IsValueType)
            {
                int numLuaParams = luaState.GetTop();
                if (numLuaParams == 0)
                {
                    _translator.Push(luaState, Activator.CreateInstance(klass.UnderlyingSystemType));
                    return 1;
                }
            }

            string constructorName = constructors.Length == 0 ? "unknown" : constructors[0].Name;
            _translator.ThrowError(luaState, string.Format("{0} does not contain constructor({1}) argument match",
                klass.UnderlyingSystemType, constructorName));
            luaState.PushNil();
            return 1;
        }

        static bool IsInteger(double x)
        {
            return Math.Ceiling(x) == x;
        }

        static object GetTargetObject(LuaState luaState, string operation, ObjectTranslator translator)
        {
            Type t;
            object target = translator.GetRawNetObject(luaState, 1);
            if (target != null)
            {
                t = target.GetType();
                if (t.HasMethod(operation))
                    return target;
            }
            target = translator.GetRawNetObject(luaState, 2);
            if (target != null)
            {
                t = target.GetType();
                if (t.HasMethod(operation))
                    return target;
            }
            return null;
        }

        static int MatchOperator(LuaState luaState, string operation, ObjectTranslator translator)
        {
            var validOperator = new MethodCache();

            object target = GetTargetObject(luaState, operation, translator);

            if (target == null)
            {
                translator.ThrowError(luaState, "Cannot call " + operation + " on a nil object");
                luaState.PushNil();
                return 1;
            }

            Type type = target.GetType();
            var operators = type.GetMethods(operation, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

            foreach (var op in operators)
            {
                bool isOk = translator.MatchParameters(luaState, op, validOperator, 0);

                if (!isOk)
                    continue;

                object result;
                if (op.IsStatic)
                    result = op.Invoke(null, validOperator.args);
                else
                    result = op.Invoke(target, validOperator.args);
                translator.Push(luaState, result);
                return 1;
            }

            translator.ThrowError(luaState, "Cannot call (" + operation + ") on object type " + type.Name);
            luaState.PushNil();
            return 1;
        }

        internal Array TableToArray(LuaState luaState, ExtractValue extractValue, Type paramArrayType, ref int startIndex, int count)
        {
            Array paramArray;

            if (count == 0)
                return Array.CreateInstance(paramArrayType, 0);

            var luaParamValue = extractValue(luaState, startIndex);
            startIndex++;

            if (luaParamValue is LuaTable)
            {
                LuaTable table = (LuaTable)luaParamValue;
                IDictionaryEnumerator tableEnumerator = table.GetEnumerator();
                tableEnumerator.Reset();
                paramArray = Array.CreateInstance(paramArrayType, table.Values.Count);

                int paramArrayIndex = 0;

                while (tableEnumerator.MoveNext())
                {
                    object value = tableEnumerator.Value;

                    if (paramArrayType == typeof(object))
                    {
                        if (value != null && value is double && IsInteger((double)value))
                            value = Convert.ToInt32((double)value);
                    }

                    paramArray.SetValue(Convert.ChangeType(value, paramArrayType), paramArrayIndex);
                    paramArrayIndex++;
                }
            }
            else
            {
                paramArray = Array.CreateInstance(paramArrayType, count);

                paramArray.SetValue(luaParamValue, 0);

                for (int i = 1; i < count; i++)
                {
                    var value = extractValue(luaState, startIndex);
                    paramArray.SetValue(value, i);
                    startIndex++;
                }

            }

            return paramArray;

        }

        /*
         * Matches a method against its arguments in the Lua stack. Returns
         * if the match was successful. It it was also returns the information
         * necessary to invoke the method.
         */
        internal bool MatchParameters(LuaState luaState, MethodBase method, MethodCache methodCache, int skipParam)
        {
            var paramInfo = method.GetParameters();
            int currentLuaParam = 1;
            int nLuaParams = luaState.GetTop() - skipParam;
            var paramList = new List<object>();
            var outList = new List<int>();
            var argTypes = new List<MethodArgs>();

            foreach (var currentNetParam in paramInfo)
            {
                if (!currentNetParam.IsIn && currentNetParam.IsOut)  // Skips out params 
                {
                    paramList.Add(null);
                    outList.Add(paramList.Count - 1);
                    continue;
                }  // Type does not match, ignore if the parameter is optional

                ExtractValue extractValue;
                if (IsParamsArray(luaState, nLuaParams, currentLuaParam, currentNetParam, out extractValue))
                {
                    int count = (nLuaParams - currentLuaParam) + 1;
                    Type paramArrayType = currentNetParam.ParameterType.GetElementType();

                    Array paramArray = TableToArray(luaState, extractValue, paramArrayType, ref currentLuaParam, count);
                    paramList.Add(paramArray);
                    int index = paramList.LastIndexOf(paramArray);
                    var methodArg = new MethodArgs();
                    methodArg.Index = index;
                    methodArg.ExtractValue = extractValue;
                    methodArg.IsParamsArray = true;
                    methodArg.ParameterType = paramArrayType;
                    argTypes.Add(methodArg);
                    continue;
                }
                
                if (currentLuaParam > nLuaParams)
                {   // Adds optional parameters
                    if (!currentNetParam.IsOptional)
                        return false;
                    paramList.Add(currentNetParam.DefaultValue);
                    continue;
                }

                if (IsTypeCorrect(luaState, currentLuaParam, currentNetParam, out extractValue))
                {   // Type checking
                    var value = extractValue(luaState, currentLuaParam);
                    paramList.Add(value);
                    int index = paramList.Count - 1;
                    var methodArg = new MethodArgs();
                    methodArg.Index = index;
                    methodArg.ExtractValue = extractValue;
                    methodArg.ParameterType = currentNetParam.ParameterType;
                    argTypes.Add(methodArg);

                    if (currentNetParam.ParameterType.IsByRef)
                        outList.Add(index);

                    currentLuaParam++;
                    continue;
                }

                if (currentNetParam.IsOptional)
                {
                    paramList.Add(currentNetParam.DefaultValue);
                    continue;
                }

                return false;
            }

            if (currentLuaParam != nLuaParams + 1) // Number of parameters does not match
                return false;
            
            methodCache.args = paramList.ToArray();
            methodCache.cachedMethod = method;
            methodCache.outList = outList.ToArray();
            methodCache.argTypes = argTypes.ToArray();

            return true;
        }

        /// <summary>
        /// Returns true if the type is set and assigns the extract value
        /// </summary>
        /// <param name="luaState"></param>
        /// <param name="currentLuaParam"></param>
        /// <param name="currentNetParam"></param>
        /// <param name="extractValue"></param>
        /// <returns></returns>
        private bool IsTypeCorrect(LuaState luaState, int currentLuaParam, ParameterInfo currentNetParam, out ExtractValue extractValue)
        {
            extractValue = _translator.typeChecker.CheckLuaType(luaState, currentLuaParam, currentNetParam.ParameterType);
            return extractValue != null;
        }

        private bool IsParamsArray(LuaState luaState, int nLuaParams, int currentLuaParam, ParameterInfo currentNetParam, out ExtractValue extractValue)
        {
            extractValue = null;

            if (!currentNetParam.GetCustomAttributes(typeof(ParamArrayAttribute), false).Any())
                return false;

            bool isParamArray = nLuaParams < currentLuaParam;

            LuaType  luaType = luaState.Type(currentLuaParam);

            if (luaType == LuaType.Table)
            {
                extractValue = _translator.typeChecker.GetExtractor(typeof(LuaTable));
                if (extractValue != null)
                    return true;
            }
            else
            {
                Type paramElementType = currentNetParam.ParameterType.GetElementType();

                extractValue = _translator.typeChecker.CheckLuaType(luaState, currentLuaParam, paramElementType);

                if (extractValue != null)
                    return true;
            }
            return isParamArray;
        }
    }
}
