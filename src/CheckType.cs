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
        Dictionary<Type, ExtractValue> extractValues = new Dictionary<Type, ExtractValue>();
        ExtractValue extractNetObject;
        ObjectTranslator translator;

        public CheckType(ObjectTranslator translator)
        {
            this.translator = translator;
            extractValues.Add(GetExtractDictionaryKey(typeof(object)), new ExtractValue(GetAsObject));
            extractValues.Add(GetExtractDictionaryKey(typeof(sbyte)), new ExtractValue(GetAsSbyte));
            extractValues.Add(GetExtractDictionaryKey(typeof(byte)), new ExtractValue(GetAsByte));
            extractValues.Add(GetExtractDictionaryKey(typeof(short)), new ExtractValue(GetAsShort));
            extractValues.Add(GetExtractDictionaryKey(typeof(ushort)), new ExtractValue(GetAsUshort));
            extractValues.Add(GetExtractDictionaryKey(typeof(int)), new ExtractValue(GetAsInt));
            extractValues.Add(GetExtractDictionaryKey(typeof(uint)), new ExtractValue(GetAsUint));
            extractValues.Add(GetExtractDictionaryKey(typeof(long)), new ExtractValue(GetAsLong));
            extractValues.Add(GetExtractDictionaryKey(typeof(ulong)), new ExtractValue(GetAsUlong));
            extractValues.Add(GetExtractDictionaryKey(typeof(double)), new ExtractValue(GetAsDouble));
            extractValues.Add(GetExtractDictionaryKey(typeof(char)), new ExtractValue(GetAsChar));
            extractValues.Add(GetExtractDictionaryKey(typeof(float)), new ExtractValue(GetAsFloat));
            extractValues.Add(GetExtractDictionaryKey(typeof(decimal)), new ExtractValue(GetAsDecimal));
            extractValues.Add(GetExtractDictionaryKey(typeof(bool)), new ExtractValue(GetAsBoolean));
            extractValues.Add(GetExtractDictionaryKey(typeof(string)), new ExtractValue(GetAsString));
            extractValues.Add(GetExtractDictionaryKey(typeof(char[])), new ExtractValue(GetAsCharArray));
            extractValues.Add(GetExtractDictionaryKey(typeof(LuaFunction)), new ExtractValue(GetAsFunction));
            extractValues.Add(GetExtractDictionaryKey(typeof(LuaTable)), new ExtractValue(GetAsTable));
            extractValues.Add(GetExtractDictionaryKey(typeof(LuaUserData)), new ExtractValue(GetAsUserdata));
            extractNetObject = new ExtractValue(GetAsNetObject);
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
            return extractValues.ContainsKey(extractKey) ? extractValues[extractKey] : extractNetObject;
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
                        return extractValues[extractKey];
                    return extractNetObject;
                }
            }

            if (paramType.Equals(typeof(object)))
                return extractValues[extractKey];

            //CP: Added support for generic parameters
            if (paramType.IsGenericParameter)
            {
                if (luatype == LuaType.Boolean)
                    return extractValues[GetExtractDictionaryKey(typeof(bool))];
                else if (luatype == LuaType.String)
                    return extractValues[GetExtractDictionaryKey(typeof(string))];
                else if (luatype == LuaType.Table)
                    return extractValues[GetExtractDictionaryKey(typeof(LuaTable))];
                else if (luatype == LuaType.UserData)
                    return extractValues[GetExtractDictionaryKey(typeof(object))];
                else if (luatype == LuaType.Function)
                    return extractValues[GetExtractDictionaryKey(typeof(LuaFunction))];
                else if (luatype == LuaType.Number)
                    return extractValues[GetExtractDictionaryKey(typeof(double))];
            }
            bool netParamIsString = paramType == typeof(string) || paramType == typeof(char[]);

            if (netParamIsNumeric)
            {
                if (luaState.IsNumber(stackPos) && !netParamIsString)
                    return extractValues[extractKey];
            }
            else if (paramType == typeof(bool))
            {
                if (luaState.IsBoolean(stackPos))
                    return extractValues[extractKey];
            }
            else if (netParamIsString)
            {
                if (luaState.IsString(stackPos))
                    return extractValues[extractKey];
                else if (luatype == LuaType.Nil)
                    return extractNetObject; // kevinh - silently convert nil to a null string pointer
            }
            else if (paramType == typeof(LuaTable))
            {
                if (luatype == LuaType.Table || luatype == LuaType.Nil)
                    return extractValues[extractKey];
            }
            else if (paramType == typeof(LuaUserData))
            {
                if (luatype == LuaType.UserData || luatype == LuaType.Nil)
                    return extractValues[extractKey];
            }
            else if (paramType == typeof(LuaFunction))
            {
                if (luatype == LuaType.Function || luatype == LuaType.Nil)
                    return extractValues[extractKey];
            }
            else if (typeof(Delegate).IsAssignableFrom(paramType) && luatype == LuaType.Function)
                return new ExtractValue(new DelegateGenerator(translator, paramType).ExtractGenerated);
            else if (paramType.IsInterface && luatype == LuaType.Table)
                return new ExtractValue(new ClassGenerator(translator, paramType).ExtractGenerated);
            else if ((paramType.IsInterface || paramType.IsClass) && luatype == LuaType.Nil)
            {
                // kevinh - allow nil to be silently converted to null - extractNetObject will return null when the item ain't found
                return extractNetObject;
            }
            else if (luaState.Type(stackPos) == LuaType.Table)
            {
                if (luaState.GetMetaField(stackPos, "__index") != LuaType.Nil)
                {
                    object obj = translator.GetNetObject(luaState, -1);
                    luaState.SetTop(-2);
                    if (obj != null && paramType.IsAssignableFrom(obj.GetType()))
                        return extractNetObject;
                }
                else
                    return null;
            }
            else
            {
                object obj = translator.GetNetObject(luaState, stackPos);
                if (obj != null && paramType.IsAssignableFrom(obj.GetType()))
                    return extractNetObject;
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
            return translator.GetTable(luaState, stackPos);
        }

        private object GetAsFunction(LuaState luaState, int stackPos)
        {
            return translator.GetFunction(luaState, stackPos);
        }

        private object GetAsUserdata(LuaState luaState, int stackPos)
        {
            return translator.GetUserData(luaState, stackPos);
        }

        public object GetAsObject(LuaState luaState, int stackPos)
        {
            if (luaState.Type(stackPos) == LuaType.Table)
            {
                if (luaState.GetMetaField(stackPos, "__index") != LuaType.Nil)
                {
                    if (luaState.CheckMetaTable(-1, translator.Tag))
                    {
                        luaState.Insert(stackPos);
                        luaState.Remove(stackPos + 1);
                    }
                    else
                        luaState.SetTop(-2);
                }
            }

            object obj = translator.GetObject(luaState, stackPos);
            return obj;
        }

        public object GetAsNetObject(LuaState luaState, int stackPos)
        {
            object obj = translator.GetNetObject(luaState, stackPos);

            if (obj == null && luaState.Type(stackPos) == LuaType.Table)
            {
                if (luaState.GetMetaField(stackPos, "__index") != LuaType.Nil)
                {
                    if (luaState.CheckMetaTable(-1, translator.Tag))
                    {
                        luaState.Insert(stackPos);
                        luaState.Remove(stackPos + 1);
                        obj = translator.GetNetObject(luaState, stackPos);
                    }
                    else
                        luaState.SetTop(-2);
                }
            }

            return obj;
        }
    }
}