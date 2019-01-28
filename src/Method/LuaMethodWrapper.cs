using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;

using NLua.Exceptions;
using NLua.Extensions;

using LuaState = KeraLua.Lua;
using LuaNativeFunction = KeraLua.LuaFunction;

namespace NLua.Method
{
    /*
     * Argument extraction with type-conversion function
     */
    delegate object ExtractValue(LuaState luaState, int stackPos);

    /*
     * Wrapper class for methods/constructors accessed from Lua.
     * 
     */
    class LuaMethodWrapper
    {
        internal LuaNativeFunction invokeFunction;
        ObjectTranslator _Translator;
        MethodBase _Method;
        MethodCache _LastCalledMethod = new MethodCache();
        string _MethodName;
        MemberInfo[] _Members;
        ExtractValue _ExtractTarget;
        object _Target;
        bool _IsStatic;

        /*
         * Constructs the wrapper for a known MethodBase instance
         */
        public LuaMethodWrapper(ObjectTranslator translator, object target, ProxyType targetType, MethodBase method)
        {
            invokeFunction = Call;
            _Translator = translator;
            _Target = target;

            if (targetType != null)
                _ExtractTarget = translator.typeChecker.GetExtractor(targetType);

            _Method = method;
            _MethodName = method.Name;
            _IsStatic = method.IsStatic;
        }

        /*
         * Constructs the wrapper for a known method name
         */
        public LuaMethodWrapper(ObjectTranslator translator, ProxyType targetType, string methodName, BindingFlags bindingType)
        {
            invokeFunction = Call;

            _Translator = translator;
            _MethodName = methodName;

            if (targetType != null)
                _ExtractTarget = translator.typeChecker.GetExtractor(targetType);

            _IsStatic = (bindingType & BindingFlags.Static) == BindingFlags.Static;
            _Members = GetMethodsRecursively(targetType.UnderlyingSystemType, methodName, bindingType | BindingFlags.Public);
        }

        MethodInfo[] GetMethodsRecursively(Type type, string methodName, BindingFlags bindingType)
        {
            if (type == typeof(object))
                return type.GetMethods(methodName, bindingType);

            var methods = type.GetMethods(methodName, bindingType);
            var baseMethods = GetMethodsRecursively(type.BaseType, methodName, bindingType);

            return methods.Concat(baseMethods).ToArray();
        }

        /// <summary>
        /// Convert C# exceptions into Lua errors
        /// </summary>
        /// <returns>num of things on stack</returns>
        /// <param name="e">null for no pending exception</param>
        int SetPendingException(Exception e)
        {
            return _Translator.interpreter.SetPendingException(e);
        }

        /*
         * Calls the method. Receives the arguments from the Lua stack
         * and returns values in it.
         */
        int Call(IntPtr state)
        {
            var luaState = LuaState.FromIntPtr(state);
            var methodToCall = _Method;
            object targetObject = _Target;
            bool failedCall = true;
            int nReturnValues = 0;

            if (!luaState.CheckStack(5))
                throw new LuaException("Lua stack overflow");

            bool isStatic = _IsStatic;
            SetPendingException(null);

            if (methodToCall == null)
            {   // Method from name
                if (isStatic)
                    targetObject = null;
                else
                    targetObject = _ExtractTarget(luaState, 1);

                if (_LastCalledMethod.cachedMethod != null)
                { // Cached?
                    int numStackToSkip = isStatic ? 0 : 1; // If this is an instance invoe we will have an extra arg on the stack for the targetObject
                    int numArgsPassed = luaState.GetTop() - numStackToSkip;
                    MethodBase method = _LastCalledMethod.cachedMethod;

                    if (numArgsPassed == _LastCalledMethod.argTypes.Length)
                    { // No. of args match?
                        if (!luaState.CheckStack(_LastCalledMethod.outList.Length + 6))
                            throw new LuaException("Lua stack overflow");

                        object[] args = _LastCalledMethod.args;

                        try
                        {
                            for (int i = 0; i < _LastCalledMethod.argTypes.Length; i++)
                            {
                                MethodArgs type = _LastCalledMethod.argTypes[i];

                                int index = i + 1 + numStackToSkip;

                                Func<int, object> valueExtractor = currentParam => {
                                    return type.extractValue(luaState, currentParam);
                                };

                                if (_LastCalledMethod.argTypes[i].isParamsArray)
                                {
                                    int count = _LastCalledMethod.argTypes.Length - i;
                                    Array paramArray = _Translator.TableToArray(valueExtractor, type.paramsArrayType, index, count);
                                    args[_LastCalledMethod.argTypes[i].index] = paramArray;
                                }
                                else
                                {
                                    args[type.index] = valueExtractor(index);
                                }

                                if (_LastCalledMethod.args[_LastCalledMethod.argTypes[i].index] == null &&
                                    !luaState.IsNil(i + 1 + numStackToSkip))
                                    throw new LuaException(string.Format("Argument number {0} is invalid", (i + 1)));
                            }

                            if (_IsStatic)
                                _Translator.Push(luaState, method.Invoke(null, _LastCalledMethod.args));
                            else
                            {
                                if (method.IsConstructor)
                                    _Translator.Push(luaState, ((ConstructorInfo)method).Invoke(_LastCalledMethod.args));
                                else
                                    _Translator.Push(luaState, method.Invoke(targetObject, _LastCalledMethod.args));
                            }

                            failedCall = false;
                        }
                        catch (TargetInvocationException e)
                        {
                            // Failure of method invocation
                            if (_Translator.interpreter.UseTraceback) e.GetBaseException().Data["Traceback"] = _Translator.interpreter.GetDebugTraceback();
                            return SetPendingException(e.GetBaseException());
                        }
                        catch (Exception e)
                        {
                            if (_Members.Length == 1) // Is the method overloaded?
                                                     // No, throw error
                                return SetPendingException(e);
                        }
                    }
                }

                // Cache miss
                if (failedCall)
                {
                    // System.Diagnostics.Debug.WriteLine("cache miss on " + methodName);
                    // If we are running an instance variable, we can now pop the targetObject from the stack
                    if (!isStatic)
                    {
                        if (targetObject == null)
                        {
                            _Translator.ThrowError(luaState, string.Format("instance method '{0}' requires a non null target object", _MethodName));
                            luaState.PushNil();
                            return 1;
                        }

                        luaState.Remove(1); // Pops the receiver
                    }

                    bool hasMatch = false;
                    string candidateName = null;

                    foreach (var member in _Members)
                    {
                        candidateName = member.ReflectedType.Name + "." + member.Name;
                        var m = (MethodInfo)member;
                        bool isMethod = _Translator.MatchParameters(luaState, m, ref _LastCalledMethod);

                        if (isMethod)
                        {
                            hasMatch = true;
                            break;
                        }
                    }

                    if (!hasMatch)
                    {
                        string msg = (candidateName == null) ? "Invalid arguments to method call" : ("Invalid arguments to method: " + candidateName);
                        _Translator.ThrowError(luaState, msg);
                        luaState.PushNil();
                        return 1;
                    }
                }
            }
            else
            {   // Method from MethodBase instance
                if (methodToCall.ContainsGenericParameters)
                {
                    _Translator.MatchParameters(luaState, methodToCall, ref _LastCalledMethod);

                    if (methodToCall.IsGenericMethodDefinition)
                    {
                        //need to make a concrete type of the generic method definition
                        var typeArgs = new List<Type>();

                        foreach (object arg in _LastCalledMethod.args)
                            typeArgs.Add(arg.GetType());

                        var concreteMethod = ((MethodInfo)methodToCall).MakeGenericMethod(typeArgs.ToArray());
                        _Translator.Push(luaState, concreteMethod.Invoke(targetObject, _LastCalledMethod.args));
                        failedCall = false;
                    }
                    else if (methodToCall.ContainsGenericParameters)
                    {
                        _Translator.ThrowError(luaState, "Unable to invoke method on generic class as the current method is an open generic method");
                        luaState.PushNil();
                        return 1;
                    }
                }
                else
                {
                    if (!methodToCall.IsStatic && !methodToCall.IsConstructor && targetObject == null)
                    {
                        targetObject = _ExtractTarget(luaState, 1);
                        luaState.Remove(1); // Pops the receiver
                    }

                    if (!_Translator.MatchParameters(luaState, methodToCall, ref _LastCalledMethod))
                    {
                        _Translator.ThrowError(luaState, "Invalid arguments to method call");
                        luaState.PushNil();
                        return 1;
                    }
                }
            }

            if (failedCall)
            {
                if (!luaState.CheckStack(_LastCalledMethod.outList.Length + 6))
                    throw new LuaException("Lua stack overflow");

                try
                {
                    if (isStatic)
                        _Translator.Push(luaState, _LastCalledMethod.cachedMethod.Invoke(null, _LastCalledMethod.args));
                    else
                    {
                        if (_LastCalledMethod.cachedMethod.IsConstructor)
                            _Translator.Push(luaState, ((ConstructorInfo)_LastCalledMethod.cachedMethod).Invoke(_LastCalledMethod.args));
                        else
                            _Translator.Push(luaState, _LastCalledMethod.cachedMethod.Invoke(targetObject, _LastCalledMethod.args));
                    }
                }
                catch (TargetInvocationException e)
                {
                    if (_Translator.interpreter.UseTraceback) e.GetBaseException().Data["Traceback"] = _Translator.interpreter.GetDebugTraceback();
                    return SetPendingException(e.GetBaseException());
                }
                catch (Exception e)
                {
                    return SetPendingException(e);
                }
            }

            // Pushes out and ref return values
            for (int index = 0; index < _LastCalledMethod.outList.Length; index++)
            {
                nReturnValues++;
                _Translator.Push(luaState, _LastCalledMethod.args[_LastCalledMethod.outList[index]]);
            }


            //  If not return void,we need add 1,
            //  or we will lost the function's return value 
            //  when call dotnet function like "int foo(arg1,out arg2,out arg3)" in Lua code 
            if (!_LastCalledMethod.IsReturnVoid && nReturnValues > 0)
                nReturnValues++;

            return nReturnValues < 1 ? 1 : nReturnValues;
        }
    }
}