using System;
using System.Collections.Generic;
using KeraLua;
using NLua.Method;
using NLua.Extensions;

namespace NLua
{
    using LuaState = KeraLua.Lua;
    sealed class CheckType
    {
        readonly Dictionary<Type, ExtractValue> _extractValues = new Dictionary<Type, ExtractValue>();
        readonly ExtractValue _extractNetObject;
        readonly ObjectTranslator _translator;

        public CheckType(ObjectTranslator translator)
        {
            _translator = translator;
            _extractValues.Add(GetExtractDictionaryKey(typeof(object)), GetAsObject);
            _extractValues.Add(GetExtractDictionaryKey(typeof(sbyte)), GetAsSbyte);
            _extractValues.Add(GetExtractDictionaryKey(typeof(byte)), GetAsByte);
            _extractValues.Add(GetExtractDictionaryKey(typeof(short)), GetAsShort);
            _extractValues.Add(GetExtractDictionaryKey(typeof(ushort)), GetAsUshort);
            _extractValues.Add(GetExtractDictionaryKey(typeof(int)), GetAsInt);
            _extractValues.Add(GetExtractDictionaryKey(typeof(uint)), GetAsUint);
            _extractValues.Add(GetExtractDictionaryKey(typeof(long)), GetAsLong);
            _extractValues.Add(GetExtractDictionaryKey(typeof(ulong)), GetAsUlong);
            _extractValues.Add(GetExtractDictionaryKey(typeof(double)), GetAsDouble);
            _extractValues.Add(GetExtractDictionaryKey(typeof(char)), GetAsChar);
            _extractValues.Add(GetExtractDictionaryKey(typeof(float)), GetAsFloat);
            _extractValues.Add(GetExtractDictionaryKey(typeof(decimal)), GetAsDecimal);
            _extractValues.Add(GetExtractDictionaryKey(typeof(bool)), GetAsBoolean);
            _extractValues.Add(GetExtractDictionaryKey(typeof(string)), GetAsString);
            _extractValues.Add(GetExtractDictionaryKey(typeof(char[])), GetAsCharArray);
            _extractValues.Add(GetExtractDictionaryKey(typeof(LuaFunction)), GetAsFunction);
            _extractValues.Add(GetExtractDictionaryKey(typeof(LuaTable)), GetAsTable);
            _extractValues.Add(GetExtractDictionaryKey(typeof(LuaUserData)), GetAsUserdata);
            _extractNetObject = GetAsNetObject;
        }

        /*
         * Checks if the value at Lua stack index stackPos matches paramType, 
         * returning a conversion function if it does and null otherwise.
         */
        internal ExtractValue GetExtractor(ProxyType paramType)
        {
            return GetExtractor(paramType.UnderlyingSystemType);
        }

        internal ExtractValue GetExtractor(Type paramType)
        {
            if (paramType.IsByRef)
                paramType = paramType.GetElementType();

            var extractKey = GetExtractDictionaryKey(paramType);
            return _extractValues.ContainsKey(extractKey) ? _extractValues[extractKey] : _extractNetObject;
        }

        internal ExtractValue CheckLuaType(LuaState luaState, int stackPos, Type paramType)
        {
            LuaType luatype = luaState.Type(stackPos);

            if (paramType.IsByRef)
                paramType = paramType.GetElementType();

            var underlyingType = Nullable.GetUnderlyingType(paramType);

            if (underlyingType != null)
            {
                paramType = underlyingType;  // Silently convert nullable types to their non null requics
            }

            var extractKey = GetExtractDictionaryKey(paramType);

            bool netParamIsNumeric = paramType == typeof(int) ||
                                     paramType == typeof(uint) ||
                                     paramType == typeof(long) ||
                                     paramType == typeof(ulong) ||
                                     paramType == typeof(short) ||
                                     paramType == typeof(ushort) ||
                                     paramType == typeof(float) ||
                                     paramType == typeof(double) ||
                                     paramType == typeof(decimal) ||
                                     paramType == typeof(byte);

            // If it is a nullable
            if (underlyingType != null)
            {
                // null can always be assigned to nullable
                if (luatype == LuaType.Nil)
                {
                    // Return the correct extractor anyways
                    if (netParamIsNumeric || paramType == typeof(bool))
                        return _extractValues[extractKey];
                    return _extractNetObject;
                }
            }

            if (paramType.Equals(typeof(object)))
                return _extractValues[extractKey];

            //CP: Added support for generic parameters
            if (paramType.IsGenericParameter)
            {
                if (luatype == LuaType.Boolean)
                    return _extractValues[GetExtractDictionaryKey(typeof(bool))];
                if (luatype == LuaType.String)
                    return _extractValues[GetExtractDictionaryKey(typeof(string))];
                if (luatype == LuaType.Table)
                    return _extractValues[GetExtractDictionaryKey(typeof(LuaTable))];
                if (luatype == LuaType.UserData)
                    return _extractValues[GetExtractDictionaryKey(typeof(object))];
                if (luatype == LuaType.Function)
                    return _extractValues[GetExtractDictionaryKey(typeof(LuaFunction))];
                if (luatype == LuaType.Number)
                    return _extractValues[GetExtractDictionaryKey(typeof(double))];
            }
            bool netParamIsString = paramType == typeof(string) || paramType == typeof(char[]);

            if (netParamIsNumeric)
            {
                if (luaState.IsNumber(stackPos) && !netParamIsString)
                    return _extractValues[extractKey];
            }
            else if (paramType == typeof(bool))
            {
                if (luaState.IsBoolean(stackPos))
                    return _extractValues[extractKey];
            }
            else if (netParamIsString)
            {
                if (luaState.IsString(stackPos))
                    return _extractValues[extractKey];
                if (luatype == LuaType.Nil)
                    return _extractNetObject; // kevinh - silently convert nil to a null string pointer
            }
            else if (paramType == typeof(LuaTable))
            {
                if (luatype == LuaType.Table || luatype == LuaType.Nil)
                    return _extractValues[extractKey];
            }
            else if (paramType == typeof(LuaUserData))
            {
                if (luatype == LuaType.UserData || luatype == LuaType.Nil)
                    return _extractValues[extractKey];
            }
            else if (paramType == typeof(LuaFunction))
            {
                if (luatype == LuaType.Function || luatype == LuaType.Nil)
                    return _extractValues[extractKey];
            }
            else if (typeof(Delegate).IsAssignableFrom(paramType) && luatype == LuaType.Function)
                return new DelegateGenerator(_translator, paramType).ExtractGenerated;
            else if (paramType.IsInterface && luatype == LuaType.Table)
                return new ClassGenerator(_translator, paramType).ExtractGenerated;
            else if ((paramType.IsInterface || paramType.IsClass) && luatype == LuaType.Nil)
            {
                // kevinh - allow nil to be silently converted to null - extractNetObject will return null when the item ain't found
                return _extractNetObject;
            }
            else if (luaState.Type(stackPos) == LuaType.Table)
            {
                if (luaState.GetMetaField(stackPos, "__index") != LuaType.Nil)
                {
                    object obj = _translator.GetNetObject(luaState, -1);
                    luaState.SetTop(-2);
                    if (obj != null && paramType.IsAssignableFrom(obj.GetType()))
                        return _extractNetObject;
                }
                else
                    return null;
            }
            else
            {
                object obj = _translator.GetNetObject(luaState, stackPos);
                if (obj != null && paramType.IsAssignableFrom(obj.GetType()))
                    return _extractNetObject;
            }

            return null;
        }

        Type GetExtractDictionaryKey(Type targetType)
        {
            return targetType;
        }

        /*
         * The following functions return the value in the Lua stack
         * index stackPos as the desired type if it can, or null
         * otherwise.
         */
        private object GetAsSbyte(LuaState luaState, int stackPos)
        {
            sbyte retVal = (sbyte)luaState.ToNumber(stackPos);
            if (retVal == 0 && !luaState.IsNumber(stackPos))
                return null;

            return retVal;
        }

        private object GetAsByte(LuaState luaState, int stackPos)
        {
            byte retVal = (byte)luaState.ToNumber(stackPos);
            if (retVal == 0 && !luaState.IsNumber(stackPos))
                return null;

            return retVal;
        }

        private object GetAsShort(LuaState luaState, int stackPos)
        {
            short retVal = (short)luaState.ToNumber(stackPos);
            if (retVal == 0 && !luaState.IsNumber(stackPos))
                return null;

            return retVal;
        }

        private object GetAsUshort(LuaState luaState, int stackPos)
        {
            ushort retVal = (ushort)luaState.ToNumber(stackPos);
            if (retVal == 0 && !luaState.IsNumericType(stackPos))
                return null;

            return retVal;
        }

        private object GetAsInt(LuaState luaState, int stackPos)
        {
            if (!luaState.IsNumericType(stackPos))
                return null;

            int retVal = (int)luaState.ToNumber(stackPos);
            return retVal;
        }

        private object GetAsUint(LuaState luaState, int stackPos)
        {
            uint retVal = (uint)luaState.ToNumber(stackPos);
            if (retVal == 0 && !luaState.IsNumericType(stackPos))
                return null;

            return retVal;
        }

        private object GetAsLong(LuaState luaState, int stackPos)
        {
            long retVal = (long)luaState.ToNumber(stackPos);
            if (retVal == 0 && !luaState.IsNumericType(stackPos))
                return null;

            return retVal;
        }

        private object GetAsUlong(LuaState luaState, int stackPos)
        {
            ulong retVal = (ulong)luaState.ToNumber(stackPos);
            if (retVal == 0 && !luaState.IsNumericType(stackPos))
                return null;

            return retVal;
        }

        private object GetAsDouble(LuaState luaState, int stackPos)
        {
            double retVal = luaState.ToNumber(stackPos);
            if (retVal == 0 && !luaState.IsNumericType(stackPos))
                return null;

            return retVal;
        }

        private object GetAsChar(LuaState luaState, int stackPos)
        {
            char retVal = (char)luaState.ToNumber(stackPos);
            if (retVal == 0 && !luaState.IsNumericType(stackPos))
                return null;

            return retVal;
        }

        private object GetAsFloat(LuaState luaState, int stackPos)
        {
            float retVal = (float)luaState.ToNumber(stackPos);
            if (retVal == 0 && !luaState.IsNumericType(stackPos))
                return null;

            return retVal;
        }

        private object GetAsDecimal(LuaState luaState, int stackPos)
        {
            decimal retVal = (decimal)luaState.ToNumber(stackPos);
            if (retVal == 0 && !luaState.IsNumericType(stackPos))
                return null;

            return retVal;
        }

        private object GetAsBoolean(LuaState luaState, int stackPos)
        {
            return luaState.ToBoolean(stackPos);
        }



        private object GetAsCharArray(LuaState luaState, int stackPos)
        {
            if (!luaState.IsString(stackPos))
                return null;
            string retVal = luaState.ToString(stackPos);
            return retVal.ToCharArray();
        }

        private object GetAsString(LuaState luaState, int stackPos)
        {
            if (!luaState.IsString(stackPos))
                return null;
            string retVal = luaState.ToString(stackPos);
            return retVal;
        }

        private object GetAsTable(LuaState luaState, int stackPos)
        {
            return _translator.GetTable(luaState, stackPos);
        }

        private object GetAsFunction(LuaState luaState, int stackPos)
        {
            return _translator.GetFunction(luaState, stackPos);
        }

        private object GetAsUserdata(LuaState luaState, int stackPos)
        {
            return _translator.GetUserData(luaState, stackPos);
        }

        public object GetAsObject(LuaState luaState, int stackPos)
        {
            if (luaState.Type(stackPos) == LuaType.Table)
            {
                if (luaState.GetMetaField(stackPos, "__index") != LuaType.Nil)
                {
                    if (luaState.CheckMetaTable(-1, _translator.Tag))
                    {
                        luaState.Insert(stackPos);
                        luaState.Remove(stackPos + 1);
                    }
                    else
                        luaState.SetTop(-2);
                }
            }

            object obj = _translator.GetObject(luaState, stackPos);
            return obj;
        }

        public object GetAsNetObject(LuaState luaState, int stackPos)
        {
            object obj = _translator.GetNetObject(luaState, stackPos);

            if (obj == null && luaState.Type(stackPos) == LuaType.Table)
            {
                if (luaState.GetMetaField(stackPos, "__index") != LuaType.Nil)
                {
                    if (luaState.CheckMetaTable(-1, _translator.Tag))
                    {
                        luaState.Insert(stackPos);
                        luaState.Remove(stackPos + 1);
                        obj = _translator.GetNetObject(luaState, stackPos);
                    }
                    else
                        luaState.SetTop(-2);
                }
            }

            return obj;
        }
    }
}