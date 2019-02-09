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
        internal LuaNativeFunction InvokeFunction;

        readonly ObjectTranslator _translator;
        readonly MethodBase _method;

        readonly ExtractValue _extractTarget;
        readonly object _target;
        readonly bool _isStatic;

        readonly string _methodName;
        readonly MethodInfo[] _members;

        MethodCache _lastCalledMethod;


        /*
         * Constructs the wrapper for a known MethodBase instance
         */
        public LuaMethodWrapper(ObjectTranslator translator, object target, ProxyType targetType, MethodBase method)
        {
            InvokeFunction = Call;
            _translator = translator;
            _target = target;
            _extractTarget = translator.typeChecker.GetExtractor(targetType);

            _method = method;
            _methodName = method.Name;
            _isStatic = method.IsStatic;
        }

        /*
         * Constructs the wrapper for a known method name
         */
        public LuaMethodWrapper(ObjectTranslator translator, ProxyType targetType, string methodName, BindingFlags bindingType)
        {
            InvokeFunction = Call;

            _translator = translator;
            _methodName = methodName;
            _extractTarget = translator.typeChecker.GetExtractor(targetType);

            _isStatic = (bindingType & BindingFlags.Static) == BindingFlags.Static;
            _members = GetMethodsRecursively(targetType.UnderlyingSystemType,
                methodName,
                bindingType | BindingFlags.Public);
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
            return _translator.interpreter.SetPendingException(e);
        }

        /*
         * Calls the method. Receives the arguments from the Lua stack
         * and returns values in it.
         */
        int Call(IntPtr state)
        {
            var luaState = LuaState.FromIntPtr(state);

            MethodBase methodToCall = _method;
            object targetObject = _target;

            bool failedCall = true;
            int nReturnValues = 0;

            if (!luaState.CheckStack(5))
                throw new LuaException("Lua stack overflow");

            bool isStatic = _isStatic;
            SetPendingException(null);

            if (methodToCall == null)
            {   // Method from name
                if (isStatic)
                    targetObject = null;
                else
                    targetObject = _extractTarget(luaState, 1);

                if (_lastCalledMethod.cachedMethod != null)
                { // Cached?
                    int numStackToSkip = isStatic ? 0 : 1; // If this is an instance invoe we will have an extra arg on the stack for the targetObject
                    int numArgsPassed = luaState.GetTop() - numStackToSkip;
                    MethodBase method = _lastCalledMethod.cachedMethod;

                    if (numArgsPassed == _lastCalledMethod.argTypes.Length)
                    { // No. of args match?
                        if (!luaState.CheckStack(_lastCalledMethod.outList.Length + 6))
                            throw new LuaException("Lua stack overflow");

                        object[] args = _lastCalledMethod.args;

                        try
                        {
                            for (int i = 0; i < _lastCalledMethod.argTypes.Length; i++)
                            {
                                MethodArgs type = _lastCalledMethod.argTypes[i];

                                int index = i + 1 + numStackToSkip;


                                if (_lastCalledMethod.argTypes[i].IsParamsArray)
                                {
                                    int count = _lastCalledMethod.argTypes.Length - i;
                                    Array paramArray = _translator.TableToArray(luaState, type.ExtractValue, type.ParamsArrayType, index, count);
                                    args[_lastCalledMethod.argTypes[i].Index] = paramArray;
                                }
                                else
                                {
                                    args[type.Index] = type.ExtractValue(luaState, index);
                                }

                                if (_lastCalledMethod.args[_lastCalledMethod.argTypes[i].Index] == null &&
                                    !luaState.IsNil(i + 1 + numStackToSkip))
                                    throw new LuaException(string.Format("Argument number {0} is invalid", (i + 1)));
                            }

                            if (_isStatic)
                                _translator.Push(luaState, method.Invoke(null, _lastCalledMethod.args));
                            else
                            {
                                if (method.IsConstructor)
                                    _translator.Push(luaState, ((ConstructorInfo)method).Invoke(_lastCalledMethod.args));
                                else
                                    _translator.Push(luaState, method.Invoke(targetObject, _lastCalledMethod.args));
                            }

                            failedCall = false;
                        }
                        catch (TargetInvocationException e)
                        {
                            // Failure of method invocation
                            if (_translator.interpreter.UseTraceback) e.GetBaseException().Data["Traceback"] = _translator.interpreter.GetDebugTraceback();
                            return SetPendingException(e.GetBaseException());
                        }
                        catch (Exception e)
                        {
                            if (_members.Length == 1) // Is the method overloaded?
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
                            _translator.ThrowError(luaState, string.Format("instance method '{0}' requires a non null target object", _methodName));
                            luaState.PushNil();
                            return 1;
                        }

                        luaState.Remove(1); // Pops the receiver
                    }

                    bool hasMatch = false;
                    string candidateName = null;

                    foreach (var member in _members)
                    {
                        candidateName = member.ReflectedType.Name + "." + member.Name;
                        bool isMethod = _translator.MatchParameters(luaState, member, ref _lastCalledMethod);

                        if (isMethod)
                        {
                            hasMatch = true;
                            break;
                        }
                    }

                    if (!hasMatch)
                    {
                        string msg = (candidateName == null) ? "Invalid arguments to method call" : ("Invalid arguments to method: " + candidateName);
                        _translator.ThrowError(luaState, msg);
                        luaState.PushNil();
                        return 1;
                    }
                }
            }
            else
            {   // Method from MethodBase instance
                if (methodToCall.ContainsGenericParameters)
                {
                    _translator.MatchParameters(luaState, methodToCall, ref _lastCalledMethod);

                    if (methodToCall.IsGenericMethodDefinition)
                    {
                        //need to make a concrete type of the generic method definition
                        var typeArgs = new List<Type>();

                        foreach (object arg in _lastCalledMethod.args)
                            typeArgs.Add(arg.GetType());

                        var concreteMethod = ((MethodInfo)methodToCall).MakeGenericMethod(typeArgs.ToArray());
                        _translator.Push(luaState, concreteMethod.Invoke(targetObject, _lastCalledMethod.args));
                        failedCall = false;
                    }
                    else if (methodToCall.ContainsGenericParameters)
                    {
                        _translator.ThrowError(luaState, "Unable to invoke method on generic class as the current method is an open generic method");
                        luaState.PushNil();
                        return 1;
                    }
                }
                else
                {
                    if (!methodToCall.IsStatic && !methodToCall.IsConstructor && targetObject == null)
                    {
                        targetObject = _extractTarget(luaState, 1);
                        luaState.Remove(1); // Pops the receiver
                    }

                    if (!_translator.MatchParameters(luaState, methodToCall, ref _lastCalledMethod))
                    {
                        _translator.ThrowError(luaState, "Invalid arguments to method call");
                        luaState.PushNil();
                        return 1;
                    }
                }
            }

            if (failedCall)
            {
                if (!luaState.CheckStack(_lastCalledMethod.outList.Length + 6))
                    throw new LuaException("Lua stack overflow");

                try
                {
                    if (isStatic)
                        _translator.Push(luaState, _lastCalledMethod.cachedMethod.Invoke(null, _lastCalledMethod.args));
                    else
                    {
                        if (_lastCalledMethod.cachedMethod.IsConstructor)
                            _translator.Push(luaState, ((ConstructorInfo)_lastCalledMethod.cachedMethod).Invoke(_lastCalledMethod.args));
                        else
                            _translator.Push(luaState, _lastCalledMethod.cachedMethod.Invoke(targetObject, _lastCalledMethod.args));
                    }
                }
                catch (TargetInvocationException e)
                {
                    if (_translator.interpreter.UseTraceback) e.GetBaseException().Data["Traceback"] = _translator.interpreter.GetDebugTraceback();
                    return SetPendingException(e.GetBaseException());
                }
                catch (Exception e)
                {
                    return SetPendingException(e);
                }
            }

            // Pushes out and ref return values
            for (int index = 0; index < _lastCalledMethod.outList.Length; index++)
            {
                nReturnValues++;
                _translator.Push(luaState, _lastCalledMethod.args[_lastCalledMethod.outList[index]]);
            }


            //  If not return void,we need add 1,
            //  or we will lost the function's return value 
            //  when call dotnet function like "int foo(arg1,out arg2,out arg3)" in Lua code 
            if (!_lastCalledMethod.IsReturnVoid && nReturnValues > 0)
                nReturnValues++;

            return nReturnValues < 1 ? 1 : nReturnValues;
        }
    }
}