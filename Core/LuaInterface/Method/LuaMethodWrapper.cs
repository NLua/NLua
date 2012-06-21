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
using System.Reflection;
using System.Collections.Generic;
using LuaInterface.Exceptions;
using LuaInterface.Extensions;

namespace LuaInterface.Method
{
	using LuaCore = KopiLua.Lua;

	/*
	 * Argument extraction with type-conversion function
	 */
	delegate object ExtractValue(LuaCore.lua_State luaState, int stackPos);

	/*
	 * Wrapper class for methods/constructors accessed from Lua.
	 * 
	 * Author: Fabio Mascarenhas
	 * Version: 1.0
	 */
	class LuaMethodWrapper
	{
		private ObjectTranslator _Translator;
		private MethodBase _Method;
		private MethodCache _LastCalledMethod = new MethodCache();
		private string _MethodName;
		private MemberInfo[] _Members;
		private IReflect _TargetType;
		private ExtractValue _ExtractTarget;
		private object _Target;
		private BindingFlags _BindingType;

		/*
		 * Constructs the wrapper for a known MethodBase instance
		 */
		public LuaMethodWrapper(ObjectTranslator translator, object target, IReflect targetType, MethodBase method)
		{
			_Translator = translator;
			_Target = target;
			_TargetType = targetType;

			if(!targetType.IsNull())
				_ExtractTarget = translator.typeChecker.getExtractor(targetType);

			_Method = method;
			_MethodName = method.Name;

			if(method.IsStatic)
				_BindingType = BindingFlags.Static;
			else
				_BindingType = BindingFlags.Instance;
		}

		/*
		 * Constructs the wrapper for a known method name
		 */
		public LuaMethodWrapper(ObjectTranslator translator, IReflect targetType, string methodName, BindingFlags bindingType)
		{
			_Translator = translator;
			_MethodName = methodName;
			_TargetType = targetType;

			if(!targetType.IsNull())
				_ExtractTarget = translator.typeChecker.getExtractor(targetType);

			_BindingType = bindingType;
			//CP: Removed NonPublic binding search and added IgnoreCase
			_Members = targetType.UnderlyingSystemType.GetMember(methodName, MemberTypes.Method, bindingType | BindingFlags.Public | BindingFlags.IgnoreCase/*|BindingFlags.NonPublic*/);
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
		public int call(LuaCore.lua_State luaState)
		{
			var methodToCall = _Method;
			object targetObject = _Target;
			bool failedCall = true;
			int nReturnValues = 0;

			if(!LuaLib.lua_checkstack(luaState, 5))
				throw new LuaException("Lua stack overflow");

			bool isStatic = (_BindingType & BindingFlags.Static) == BindingFlags.Static;
			SetPendingException(null);

			if(methodToCall.IsNull()) // Method from name
			{
				if(isStatic)
					targetObject = null;
				else
					targetObject = _ExtractTarget(luaState, 1);

				//LuaLib.lua_remove(luaState,1); // Pops the receiver
				if(!_LastCalledMethod.cachedMethod.IsNull()) // Cached?
				{
					int numStackToSkip = isStatic ? 0 : 1; // If this is an instance invoe we will have an extra arg on the stack for the targetObject
					int numArgsPassed = LuaLib.lua_gettop(luaState) - numStackToSkip;

					if(numArgsPassed == _LastCalledMethod.argTypes.Length) // No. of args match?
					{
						if(!LuaLib.lua_checkstack(luaState, _LastCalledMethod.outList.Length + 6))
							throw new LuaException("Lua stack overflow");

						try
						{
							for(int i = 0; i < _LastCalledMethod.argTypes.Length; i++)
							{
								if(_LastCalledMethod.argTypes[i].isParamsArray)
								{
									object luaParamValue = _LastCalledMethod.argTypes[i].extractValue(luaState, i + 1 + numStackToSkip);
									var paramArrayType = _LastCalledMethod.argTypes[i].paramsArrayType;
									Array paramArray;

									if(luaParamValue is LuaTable)
									{
										var table = (LuaTable)luaParamValue;
										paramArray = Array.CreateInstance(paramArrayType, table.Values.Count);

										for(int x = 1; x <= table.Values.Count; x++)
											paramArray.SetValue(Convert.ChangeType(table[x], paramArrayType), x - 1);
									}
									else
									{
										paramArray = Array.CreateInstance(paramArrayType, 1);
										paramArray.SetValue(luaParamValue, 0);
									}

									_LastCalledMethod.args[_LastCalledMethod.argTypes[i].index] = paramArray;
								}
								else
								{
									_LastCalledMethod.args[_LastCalledMethod.argTypes[i].index] =
										_LastCalledMethod.argTypes[i].extractValue(luaState, i + 1 + numStackToSkip);
								}

								if(_LastCalledMethod.args[_LastCalledMethod.argTypes[i].index] == null &&
									!LuaLib.lua_isnil(luaState, i + 1 + numStackToSkip))
									throw new LuaException("argument number " + (i + 1) + " is invalid");
							}

							if((_BindingType & BindingFlags.Static) == BindingFlags.Static)
								_Translator.push(luaState, _LastCalledMethod.cachedMethod.Invoke(null, _LastCalledMethod.args));
							else
							{
								if(_LastCalledMethod.cachedMethod.IsConstructor)
									_Translator.push(luaState, ((ConstructorInfo)_LastCalledMethod.cachedMethod).Invoke(_LastCalledMethod.args));
								else
									_Translator.push(luaState, _LastCalledMethod.cachedMethod.Invoke(targetObject, _LastCalledMethod.args));
							}

							failedCall = false;
						}
						catch(TargetInvocationException e)
						{
							// Failure of method invocation
							return SetPendingException(e.GetBaseException());
						}
						catch(Exception e)
						{
							if(_Members.Length == 1) // Is the method overloaded?
								// No, throw error
								return SetPendingException(e);
						}
					}
				}

				// Cache miss
				if(failedCall)
				{
					// System.Diagnostics.Debug.WriteLine("cache miss on " + methodName);
					// If we are running an instance variable, we can now pop the targetObject from the stack
					if(!isStatic)
					{
						if(targetObject.IsNull())
						{
							_Translator.throwError(luaState, String.Format("instance method '{0}' requires a non null target object", _MethodName));
							LuaLib.lua_pushnil(luaState);
							return 1;
						}

						LuaLib.lua_remove(luaState, 1); // Pops the receiver
					}

					bool hasMatch = false;
					string candidateName = null;

					foreach(var member in _Members)
					{
						candidateName = member.ReflectedType.Name + "." + member.Name;
						var m = (MethodInfo)member;
						bool isMethod = _Translator.matchParameters(luaState, m, ref _LastCalledMethod);

						if(isMethod)
						{
							hasMatch = true;
							break;
						}
					}

					if(!hasMatch)
					{
						string msg = (candidateName == null) ? "invalid arguments to method call" : ("invalid arguments to method: " + candidateName);
						_Translator.throwError(luaState, msg);
						LuaLib.lua_pushnil(luaState);
						return 1;
					}
				}
			}
			else // Method from MethodBase instance 
			{
				if(methodToCall.ContainsGenericParameters)
				{
					/*bool isMethod = */_Translator.matchParameters(luaState, methodToCall, ref _LastCalledMethod);

					if(methodToCall.IsGenericMethodDefinition)
					{
						//need to make a concrete type of the generic method definition
						var typeArgs = new List<Type>();

						foreach(object arg in _LastCalledMethod.args)
							typeArgs.Add(arg.GetType());

						var concreteMethod = (methodToCall as MethodInfo).MakeGenericMethod(typeArgs.ToArray());
						_Translator.push(luaState, concreteMethod.Invoke(targetObject, _LastCalledMethod.args));
						failedCall = false;
					}
					else if(methodToCall.ContainsGenericParameters)
					{
						_Translator.throwError(luaState, "unable to invoke method on generic class as the current method is an open generic method");
						LuaLib.lua_pushnil(luaState);
						return 1;
					}
				}
				else
				{
					if(!methodToCall.IsStatic && !methodToCall.IsConstructor && targetObject == null)
					{
						targetObject = _ExtractTarget(luaState, 1);
						LuaLib.lua_remove(luaState, 1); // Pops the receiver
					}

					if(!_Translator.matchParameters(luaState, methodToCall, ref _LastCalledMethod))
					{
						_Translator.throwError(luaState, "invalid arguments to method call");
						LuaLib.lua_pushnil(luaState);
						return 1;
					}
				}
			}

			if(failedCall)
			{
				if(!LuaLib.lua_checkstack(luaState, _LastCalledMethod.outList.Length + 6))
					throw new LuaException("Lua stack overflow");

				try
				{
					if(isStatic)
						_Translator.push(luaState, _LastCalledMethod.cachedMethod.Invoke(null, _LastCalledMethod.args));
					else
					{
						if(_LastCalledMethod.cachedMethod.IsConstructor)
							_Translator.push(luaState, ((ConstructorInfo)_LastCalledMethod.cachedMethod).Invoke(_LastCalledMethod.args));
						else
							_Translator.push(luaState, _LastCalledMethod.cachedMethod.Invoke(targetObject, _LastCalledMethod.args));
					}
				}
				catch(TargetInvocationException e)
				{
					return SetPendingException(e.GetBaseException());
				}
				catch(Exception e)
				{
					return SetPendingException(e);
				}
			}

			// Pushes out and ref return values
			for(int index = 0; index < _LastCalledMethod.outList.Length; index++)
			{
				nReturnValues++;
				//for(int i=0;i<lastCalledMethod.outList.Length;i++)
				_Translator.push(luaState, _LastCalledMethod.args[_LastCalledMethod.outList[index]]);
			}

			//by isSingle 2010-09-10 11:26:31 
			//Desc: 
			//  if not return void,we need add 1,
			//  or we will lost the function's return value 
			//  when call dotnet function like "int foo(arg1,out arg2,out arg3)" in lua code 
			if(!_LastCalledMethod.IsReturnVoid && nReturnValues > 0)
				nReturnValues++;

			return nReturnValues < 1 ? 1 : nReturnValues;
		}
	}
}