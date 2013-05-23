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
using System.Reflection;
using System.Collections.Generic;
using NLua.Method;
using NLua.Extensions;

namespace NLua
{
#if USE_KOPILUA
    using LuaCore = KopiLua.Lua;
#else
	using LuaCore = KeraLua.Lua;
#endif

    /*
	 * Type checking and conversion functions.
	 * 
	 * Author: Fabio Mascarenhas
	 * Version: 1.0
	 */
    class CheckType
    {
#if SILVERLIGHT
        private Dictionary<Type, ExtractValue> extractValues = new Dictionary<Type, ExtractValue>();
#else
        private Dictionary<long, ExtractValue> extractValues = new Dictionary<long, ExtractValue> ();
#endif
        private ExtractValue extractNetObject;
        private ObjectTranslator translator;

        public CheckType(ObjectTranslator translator)
        {
            this.translator = translator;
#if SILVERLIGHT
            extractValues.Add(typeof(object), new ExtractValue(getAsObject));
            extractValues.Add(typeof(sbyte), new ExtractValue(getAsSbyte));
            extractValues.Add(typeof(byte), new ExtractValue(getAsByte));
            extractValues.Add(typeof(short), new ExtractValue(getAsShort));
            extractValues.Add(typeof(ushort), new ExtractValue(getAsUshort));
            extractValues.Add(typeof(int), new ExtractValue(getAsInt));
            extractValues.Add(typeof(uint), new ExtractValue(getAsUint));
            extractValues.Add(typeof(long), new ExtractValue(getAsLong));
            extractValues.Add(typeof(ulong), new ExtractValue(getAsUlong));
            extractValues.Add(typeof(double), new ExtractValue(getAsDouble));
            extractValues.Add(typeof(char), new ExtractValue(getAsChar));
            extractValues.Add(typeof(float), new ExtractValue(getAsFloat));
            extractValues.Add(typeof(decimal), new ExtractValue(getAsDecimal));
            extractValues.Add(typeof(bool), new ExtractValue(getAsBoolean));
            extractValues.Add(typeof(string), new ExtractValue(getAsString));
            extractValues.Add(typeof(LuaFunction), new ExtractValue(getAsFunction));
            extractValues.Add(typeof(LuaTable), new ExtractValue(getAsTable));
            extractValues.Add(typeof(LuaUserData), new ExtractValue(getAsUserdata));
#else
			extractValues.Add (typeof(object).TypeHandle.Value.ToInt64 (), new ExtractValue (getAsObject));
			extractValues.Add (typeof(sbyte).TypeHandle.Value.ToInt64 (), new ExtractValue (getAsSbyte));
			extractValues.Add (typeof(byte).TypeHandle.Value.ToInt64 (), new ExtractValue (getAsByte));
			extractValues.Add (typeof(short).TypeHandle.Value.ToInt64 (), new ExtractValue (getAsShort));
			extractValues.Add (typeof(ushort).TypeHandle.Value.ToInt64 (), new ExtractValue (getAsUshort));
			extractValues.Add (typeof(int).TypeHandle.Value.ToInt64 (), new ExtractValue (getAsInt));
			extractValues.Add (typeof(uint).TypeHandle.Value.ToInt64 (), new ExtractValue (getAsUint));
			extractValues.Add (typeof(long).TypeHandle.Value.ToInt64 (), new ExtractValue (getAsLong));
			extractValues.Add (typeof(ulong).TypeHandle.Value.ToInt64 (), new ExtractValue (getAsUlong));
			extractValues.Add (typeof(double).TypeHandle.Value.ToInt64 (), new ExtractValue (getAsDouble));
			extractValues.Add (typeof(char).TypeHandle.Value.ToInt64 (), new ExtractValue (getAsChar));
			extractValues.Add (typeof(float).TypeHandle.Value.ToInt64 (), new ExtractValue (getAsFloat));
			extractValues.Add (typeof(decimal).TypeHandle.Value.ToInt64 (), new ExtractValue (getAsDecimal));
			extractValues.Add (typeof(bool).TypeHandle.Value.ToInt64 (), new ExtractValue (getAsBoolean));
			extractValues.Add (typeof(string).TypeHandle.Value.ToInt64 (), new ExtractValue (getAsString));
			extractValues.Add (typeof(LuaFunction).TypeHandle.Value.ToInt64 (), new ExtractValue (getAsFunction));
			extractValues.Add (typeof(LuaTable).TypeHandle.Value.ToInt64 (), new ExtractValue (getAsTable));
			extractValues.Add (typeof(LuaUserData).TypeHandle.Value.ToInt64 (), new ExtractValue (getAsUserdata));
#endif
            extractNetObject = new ExtractValue(getAsNetObject);
        }

        /*
         * Checks if the value at Lua stack index stackPos matches paramType, 
         * returning a conversion function if it does and null otherwise.
         */
        internal ExtractValue getExtractor(IReflect paramType)
        {
            return getExtractor(paramType.UnderlyingSystemType);
        }

        internal ExtractValue getExtractor(Type paramType)
        {
            if (paramType.IsByRef)
                paramType = paramType.GetElementType();

#if SILVERLIGHT
            Type runtimeHandleValue = paramType;
#else
			long runtimeHandleValue = paramType.TypeHandle.Value.ToInt64 ();
#endif
            return extractValues.ContainsKey(runtimeHandleValue) ? extractValues[runtimeHandleValue] : extractNetObject;
        }

        internal ExtractValue checkType(LuaCore.lua_State luaState, int stackPos, Type paramType)
        {
            var luatype = LuaLib.lua_type(luaState, stackPos);

            if (paramType.IsByRef)
                paramType = paramType.GetElementType();

            var underlyingType = Nullable.GetUnderlyingType(paramType);

            if (!underlyingType.IsNull())
                paramType = underlyingType;	 // Silently convert nullable types to their non null requics

#if SILVERLIGHT
            Type runtimeHandleValue = paramType;
#else
			long runtimeHandleValue = paramType.TypeHandle.Value.ToInt64 ();
#endif

            if (paramType.Equals(typeof(object)))
                return extractValues[runtimeHandleValue];

            //CP: Added support for generic parameters
            if (paramType.IsGenericParameter)
            {
#if SILVERLIGHT
                if (luatype == LuaTypes.Boolean)
                    return extractValues[typeof(bool)];
                else if (luatype == LuaTypes.String)
                    return extractValues[typeof(string)];
                else if (luatype == LuaTypes.Table)
                    return extractValues[typeof(LuaTable)];
                else if (luatype == LuaTypes.UserData)
                    return extractValues[typeof(object)];
                else if (luatype == LuaTypes.Function)
                    return extractValues[typeof(LuaFunction)];
                else if (luatype == LuaTypes.Number)
                    return extractValues[typeof(double)];
#else
                if (luatype == LuaTypes.Boolean)
                    return extractValues[typeof(bool).TypeHandle.Value.ToInt64()];
                else if (luatype == LuaTypes.String)
                    return extractValues[typeof(string).TypeHandle.Value.ToInt64()];
                else if (luatype == LuaTypes.Table)
                    return extractValues[typeof(LuaTable).TypeHandle.Value.ToInt64()];
                else if (luatype == LuaTypes.UserData)
                    return extractValues[typeof(object).TypeHandle.Value.ToInt64()];
                else if (luatype == LuaTypes.Function)
                    return extractValues[typeof(LuaFunction).TypeHandle.Value.ToInt64()];
                else if (luatype == LuaTypes.Number)
                    return extractValues[typeof(double).TypeHandle.Value.ToInt64()]; 
#endif
            }

            if (LuaLib.lua_isnumber(luaState, stackPos))
                return extractValues[runtimeHandleValue];

            if (paramType == typeof(bool))
            {
                if (LuaLib.lua_isboolean(luaState, stackPos))
                    return extractValues[runtimeHandleValue];
            }
            else if (paramType == typeof(string))
            {
                if (LuaLib.lua_isstring(luaState, stackPos))
                    return extractValues[runtimeHandleValue];
                else if (luatype == LuaTypes.Nil)
                    return extractNetObject; // kevinh - silently convert nil to a null string pointer
            }
            else if (paramType == typeof(LuaTable))
            {
                if (luatype == LuaTypes.Table)
                    return extractValues[runtimeHandleValue];
            }
            else if (paramType == typeof(LuaUserData))
            {
                if (luatype == LuaTypes.UserData)
                    return extractValues[runtimeHandleValue];
            }
            else if (paramType == typeof(LuaFunction))
            {
                if (luatype == LuaTypes.Function)
                    return extractValues[runtimeHandleValue];
            }
            else if (typeof(Delegate).IsAssignableFrom(paramType) && luatype == LuaTypes.Function)
                return new ExtractValue(new DelegateGenerator(translator, paramType).extractGenerated);
            else if (paramType.IsInterface && luatype == LuaTypes.Table)
                return new ExtractValue(new ClassGenerator(translator, paramType).extractGenerated);
            else if ((paramType.IsInterface || paramType.IsClass) && luatype == LuaTypes.Nil)
            {
                // kevinh - allow nil to be silently converted to null - extractNetObject will return null when the item ain't found
                return extractNetObject;
            }
            else if (LuaLib.lua_type(luaState, stackPos) == LuaTypes.Table)
            {
                if (LuaLib.luaL_getmetafield(luaState, stackPos, "__index"))
                {
                    object obj = translator.getNetObject(luaState, -1);
                    LuaLib.lua_settop(luaState, -2);
                    if (!obj.IsNull() && paramType.IsAssignableFrom(obj.GetType()))
                        return extractNetObject;
                }
                else
                    return null;
            }
            else
            {
                object obj = translator.getNetObject(luaState, stackPos);
                if (!obj.IsNull() && paramType.IsAssignableFrom(obj.GetType()))
                    return extractNetObject;
            }

            return null;
        }

        /*
         * The following functions return the value in the Lua stack
         * index stackPos as the desired type if it can, or null
         * otherwise.
         */
        private object getAsSbyte(LuaCore.lua_State luaState, int stackPos)
        {
            sbyte retVal = (sbyte)LuaLib.lua_tonumber(luaState, stackPos);
            if (retVal == 0 && !LuaLib.lua_isnumber(luaState, stackPos))
                return null;

            return retVal;
        }

        private object getAsByte(LuaCore.lua_State luaState, int stackPos)
        {
            byte retVal = (byte)LuaLib.lua_tonumber(luaState, stackPos);
            if (retVal == 0 && !LuaLib.lua_isnumber(luaState, stackPos))
                return null;

            return retVal;
        }

        private object getAsShort(LuaCore.lua_State luaState, int stackPos)
        {
            short retVal = (short)LuaLib.lua_tonumber(luaState, stackPos);
            if (retVal == 0 && !LuaLib.lua_isnumber(luaState, stackPos))
                return null;

            return retVal;
        }

        private object getAsUshort(LuaCore.lua_State luaState, int stackPos)
        {
            ushort retVal = (ushort)LuaLib.lua_tonumber(luaState, stackPos);
            if (retVal == 0 && !LuaLib.lua_isnumber(luaState, stackPos))
                return null;

            return retVal;
        }

        private object getAsInt(LuaCore.lua_State luaState, int stackPos)
        {
            int retVal = (int)LuaLib.lua_tonumber(luaState, stackPos);
            if (retVal == 0 && !LuaLib.lua_isnumber(luaState, stackPos))
                return null;

            return retVal;
        }

        private object getAsUint(LuaCore.lua_State luaState, int stackPos)
        {
            uint retVal = (uint)LuaLib.lua_tonumber(luaState, stackPos);
            if (retVal == 0 && !LuaLib.lua_isnumber(luaState, stackPos))
                return null;

            return retVal;
        }

        private object getAsLong(LuaCore.lua_State luaState, int stackPos)
        {
            long retVal = (long)LuaLib.lua_tonumber(luaState, stackPos);
            if (retVal == 0 && !LuaLib.lua_isnumber(luaState, stackPos))
                return null;

            return retVal;
        }

        private object getAsUlong(LuaCore.lua_State luaState, int stackPos)
        {
            ulong retVal = (ulong)LuaLib.lua_tonumber(luaState, stackPos);
            if (retVal == 0 && !LuaLib.lua_isnumber(luaState, stackPos))
                return null;

            return retVal;
        }

        private object getAsDouble(LuaCore.lua_State luaState, int stackPos)
        {
            double retVal = LuaLib.lua_tonumber(luaState, stackPos);
            if (retVal == 0 && !LuaLib.lua_isnumber(luaState, stackPos))
                return null;

            return retVal;
        }

        private object getAsChar(LuaCore.lua_State luaState, int stackPos)
        {
            char retVal = (char)LuaLib.lua_tonumber(luaState, stackPos);
            if (retVal == 0 && !LuaLib.lua_isnumber(luaState, stackPos))
                return null;

            return retVal;
        }

        private object getAsFloat(LuaCore.lua_State luaState, int stackPos)
        {
            float retVal = (float)LuaLib.lua_tonumber(luaState, stackPos);
            if (retVal == 0 && !LuaLib.lua_isnumber(luaState, stackPos))
                return null;

            return retVal;
        }

        private object getAsDecimal(LuaCore.lua_State luaState, int stackPos)
        {
            decimal retVal = (decimal)LuaLib.lua_tonumber(luaState, stackPos);
            if (retVal == 0 && !LuaLib.lua_isnumber(luaState, stackPos))
                return null;

            return retVal;
        }

        private object getAsBoolean(LuaCore.lua_State luaState, int stackPos)
        {
            return LuaLib.lua_toboolean(luaState, stackPos);
        }

        private object getAsString(LuaCore.lua_State luaState, int stackPos)
        {
            string retVal = LuaLib.lua_tostring(luaState, stackPos).ToString();
            if (retVal == string.Empty && !LuaLib.lua_isstring(luaState, stackPos))
                return null;

            return retVal;
        }

        private object getAsTable(LuaCore.lua_State luaState, int stackPos)
        {
            return translator.getTable(luaState, stackPos);
        }

        private object getAsFunction(LuaCore.lua_State luaState, int stackPos)
        {
            return translator.getFunction(luaState, stackPos);
        }

        private object getAsUserdata(LuaCore.lua_State luaState, int stackPos)
        {
            return translator.getUserData(luaState, stackPos);
        }

        public object getAsObject(LuaCore.lua_State luaState, int stackPos)
        {
            if (LuaLib.lua_type(luaState, stackPos) == LuaTypes.Table)
            {
                if (LuaLib.luaL_getmetafield(luaState, stackPos, "__index"))
                {
                    if (LuaLib.luaL_checkmetatable(luaState, -1))
                    {
                        LuaLib.lua_insert(luaState, stackPos);
                        LuaLib.lua_remove(luaState, stackPos + 1);
                    }
                    else
                        LuaLib.lua_settop(luaState, -2);
                }
            }

            object obj = translator.getObject(luaState, stackPos);
            return obj;
        }

        public object getAsNetObject(LuaCore.lua_State luaState, int stackPos)
        {
            object obj = translator.getNetObject(luaState, stackPos);

            if (obj.IsNull() && LuaLib.lua_type(luaState, stackPos) == LuaTypes.Table)
            {
                if (LuaLib.luaL_getmetafield(luaState, stackPos, "__index"))
                {
                    if (LuaLib.luaL_checkmetatable(luaState, -1))
                    {
                        LuaLib.lua_insert(luaState, stackPos);
                        LuaLib.lua_remove(luaState, stackPos + 1);
                        obj = translator.getNetObject(luaState, stackPos);
                    }
                    else
                        LuaLib.lua_settop(luaState, -2);
                }
            }

            return obj;
        }
    }
}