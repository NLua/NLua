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

		public CheckType (ObjectTranslator translator)
		{
			this.translator = translator;
			extractValues.Add(getExtractDictionaryKey(typeof(object)), new ExtractValue(getAsObject));
			extractValues.Add(getExtractDictionaryKey(typeof(sbyte)), new ExtractValue(getAsSbyte));
			extractValues.Add(getExtractDictionaryKey(typeof(byte)), new ExtractValue(getAsByte));
			extractValues.Add(getExtractDictionaryKey(typeof(short)), new ExtractValue(getAsShort));
			extractValues.Add(getExtractDictionaryKey(typeof(ushort)), new ExtractValue(getAsUshort));
			extractValues.Add(getExtractDictionaryKey(typeof(int)), new ExtractValue(getAsInt));
			extractValues.Add(getExtractDictionaryKey(typeof(uint)), new ExtractValue(getAsUint));
			extractValues.Add(getExtractDictionaryKey(typeof(long)), new ExtractValue(getAsLong));
			extractValues.Add(getExtractDictionaryKey(typeof(ulong)), new ExtractValue(getAsUlong));
			extractValues.Add(getExtractDictionaryKey(typeof(double)), new ExtractValue(getAsDouble));
			extractValues.Add(getExtractDictionaryKey(typeof(char)), new ExtractValue(getAsChar));
			extractValues.Add(getExtractDictionaryKey(typeof(float)), new ExtractValue(getAsFloat));
			extractValues.Add(getExtractDictionaryKey(typeof(decimal)), new ExtractValue(getAsDecimal));
			extractValues.Add(getExtractDictionaryKey(typeof(bool)), new ExtractValue(getAsBoolean));
			extractValues.Add(getExtractDictionaryKey(typeof(string)), new ExtractValue(getAsString));
			extractValues.Add(getExtractDictionaryKey(typeof(LuaFunction)), new ExtractValue(getAsFunction));
			extractValues.Add(getExtractDictionaryKey(typeof(LuaTable)), new ExtractValue(getAsTable));
			extractValues.Add(getExtractDictionaryKey(typeof(LuaUserData)), new ExtractValue(getAsUserdata));
			extractNetObject = new ExtractValue (getAsNetObject);		
		}

		/*
		 * Checks if the value at Lua stack index stackPos matches paramType, 
		 * returning a conversion function if it does and null otherwise.
		 */
		internal ExtractValue getExtractor (IReflect paramType)
		{
			return getExtractor (paramType.UnderlyingSystemType);
		}

		internal ExtractValue getExtractor (Type paramType)
		{
			if (paramType.IsByRef)
				paramType = paramType.GetElementType ();

			var extractKey = getExtractDictionaryKey(paramType);
			return extractValues.ContainsKey(extractKey) ? extractValues[extractKey] : extractNetObject;
		}

		internal ExtractValue checkType (LuaCore.LuaState luaState, int stackPos, Type paramType)
		{
			var luatype = LuaLib.lua_type (luaState, stackPos);

			if (paramType.IsByRef)
				paramType = paramType.GetElementType ();

			var underlyingType = Nullable.GetUnderlyingType (paramType);

			if (!underlyingType.IsNull ())
				paramType = underlyingType;	 // Silently convert nullable types to their non null requics

			var extractKey = getExtractDictionaryKey (paramType);

			if (paramType.Equals (typeof(object)))
				return extractValues [extractKey];

			//CP: Added support for generic parameters
			if (paramType.IsGenericParameter) {
				if (luatype == LuaTypes.Boolean)
					return extractValues [getExtractDictionaryKey (typeof(bool))];
				else if (luatype == LuaTypes.String)
					return extractValues[getExtractDictionaryKey (typeof(string))];
				else if (luatype == LuaTypes.Table)
					return extractValues [getExtractDictionaryKey (typeof(LuaTable))];
				else if (luatype == LuaTypes.UserData)
					return extractValues [getExtractDictionaryKey (typeof(object))];
				else if (luatype == LuaTypes.Function)
					return extractValues [getExtractDictionaryKey (typeof(LuaFunction))];
				else if (luatype == LuaTypes.Number)
					return extractValues [getExtractDictionaryKey (typeof(double))];
			}

			if (LuaLib.lua_isnumber (luaState, stackPos))
				return extractValues [extractKey];

			if (paramType == typeof(bool)) {
				if (LuaLib.lua_isboolean (luaState, stackPos))
					return extractValues [extractKey];
			} else if (paramType == typeof(string)) {
				if (LuaLib.lua_isstring (luaState, stackPos))
					return extractValues [extractKey];
				else if (luatype == LuaTypes.Nil)
					return extractNetObject; // kevinh - silently convert nil to a null string pointer
			} else if (paramType == typeof(LuaTable)) {
				if (luatype == LuaTypes.Table)
					return extractValues [extractKey];
			} else if (paramType == typeof(LuaUserData)) {
				if (luatype == LuaTypes.UserData)
					return extractValues [extractKey];
			} else if (paramType == typeof(LuaFunction)) {
				if (luatype == LuaTypes.Function)
					return extractValues [extractKey];
			} else if (typeof(Delegate).IsAssignableFrom (paramType) && luatype == LuaTypes.Function)
				return new ExtractValue (new DelegateGenerator (translator, paramType).extractGenerated);
			else if (paramType.IsInterface && luatype == LuaTypes.Table)
				return new ExtractValue (new ClassGenerator (translator, paramType).extractGenerated);
			else if ((paramType.IsInterface || paramType.IsClass) && luatype == LuaTypes.Nil) {
				// kevinh - allow nil to be silently converted to null - extractNetObject will return null when the item ain't found
				return extractNetObject;
			} else if (LuaLib.lua_type (luaState, stackPos) == LuaTypes.Table) {
				if (LuaLib.luaL_getmetafield (luaState, stackPos, "__index")) {
					object obj = translator.getNetObject (luaState, -1);
					LuaLib.lua_settop (luaState, -2);
					if (!obj.IsNull () && paramType.IsAssignableFrom (obj.GetType ()))
						return extractNetObject;
				} else
					return null;
			} else {
				object obj = translator.getNetObject (luaState, stackPos);
				if (!obj.IsNull () && paramType.IsAssignableFrom (obj.GetType ()))
					return extractNetObject;
			}

			return null;
		}

#if SILVERLIGHT
		private Type getExtractDictionaryKey(Type targetType)
		{
			return targetType;
		}
#else
		private long getExtractDictionaryKey(Type targetType)
		{
			return targetType.TypeHandle.Value.ToInt64();
		}
#endif

		/*
		 * The following functions return the value in the Lua stack
		 * index stackPos as the desired type if it can, or null
		 * otherwise.
		 */
		private object getAsSbyte (LuaCore.LuaState luaState, int stackPos)
		{
			sbyte retVal = (sbyte)LuaLib.lua_tonumber (luaState, stackPos);
			if (retVal == 0 && !LuaLib.lua_isnumber (luaState, stackPos))
				return null;

			return retVal;
		}

		private object getAsByte (LuaCore.LuaState luaState, int stackPos)
		{
			byte retVal = (byte)LuaLib.lua_tonumber (luaState, stackPos);
			if (retVal == 0 && !LuaLib.lua_isnumber (luaState, stackPos))
				return null;

			return retVal;
		}

		private object getAsShort (LuaCore.LuaState luaState, int stackPos)
		{
			short retVal = (short)LuaLib.lua_tonumber (luaState, stackPos);
			if (retVal == 0 && !LuaLib.lua_isnumber (luaState, stackPos))
				return null;

			return retVal;
		}

		private object getAsUshort (LuaCore.LuaState luaState, int stackPos)
		{
			ushort retVal = (ushort)LuaLib.lua_tonumber (luaState, stackPos);
			if (retVal == 0 && !LuaLib.lua_isnumber (luaState, stackPos))
				return null;

			return retVal;
		}

		private object getAsInt (LuaCore.LuaState luaState, int stackPos)
		{
			int retVal = (int)LuaLib.lua_tonumber (luaState, stackPos);
			if (retVal == 0 && !LuaLib.lua_isnumber (luaState, stackPos))
				return null;

			return retVal;
		}

		private object getAsUint (LuaCore.LuaState luaState, int stackPos)
		{
			uint retVal = (uint)LuaLib.lua_tonumber (luaState, stackPos);
			if (retVal == 0 && !LuaLib.lua_isnumber (luaState, stackPos))
				return null;

			return retVal;
		}

		private object getAsLong (LuaCore.LuaState luaState, int stackPos)
		{
			long retVal = (long)LuaLib.lua_tonumber (luaState, stackPos);
			if (retVal == 0 && !LuaLib.lua_isnumber (luaState, stackPos))
				return null;

			return retVal;
		}

		private object getAsUlong (LuaCore.LuaState luaState, int stackPos)
		{
			ulong retVal = (ulong)LuaLib.lua_tonumber (luaState, stackPos);
			if (retVal == 0 && !LuaLib.lua_isnumber (luaState, stackPos))
				return null;

			return retVal;
		}

		private object getAsDouble (LuaCore.LuaState luaState, int stackPos)
		{
			double retVal = LuaLib.lua_tonumber (luaState, stackPos);
			if (retVal == 0 && !LuaLib.lua_isnumber (luaState, stackPos))
				return null;

			return retVal;
		}

		private object getAsChar (LuaCore.LuaState luaState, int stackPos)
		{
			char retVal = (char)LuaLib.lua_tonumber (luaState, stackPos);
			if (retVal == 0 && !LuaLib.lua_isnumber (luaState, stackPos))
				return null;

			return retVal;
		}

		private object getAsFloat (LuaCore.LuaState luaState, int stackPos)
		{
			float retVal = (float)LuaLib.lua_tonumber (luaState, stackPos);
			if (retVal == 0 && !LuaLib.lua_isnumber (luaState, stackPos))
				return null;

			return retVal;
		}

		private object getAsDecimal (LuaCore.LuaState luaState, int stackPos)
		{
			decimal retVal = (decimal)LuaLib.lua_tonumber (luaState, stackPos);
			if (retVal == 0 && !LuaLib.lua_isnumber (luaState, stackPos))
				return null;

			return retVal;
		}

		private object getAsBoolean (LuaCore.LuaState luaState, int stackPos)
		{
			return LuaLib.lua_toboolean (luaState, stackPos);
		}

		private object getAsString (LuaCore.LuaState luaState, int stackPos)
		{
			string retVal = LuaLib.lua_tostring (luaState, stackPos).ToString ();
			if (retVal == string.Empty && !LuaLib.lua_isstring (luaState, stackPos))
				return null;

			return retVal;
		}

		private object getAsTable (LuaCore.LuaState luaState, int stackPos)
		{
			return translator.getTable (luaState, stackPos);
		}

		private object getAsFunction (LuaCore.LuaState luaState, int stackPos)
		{
			return translator.getFunction (luaState, stackPos);
		}

		private object getAsUserdata (LuaCore.LuaState luaState, int stackPos)
		{
			return translator.getUserData (luaState, stackPos);
		}

		public object getAsObject (LuaCore.LuaState luaState, int stackPos)
		{
			if (LuaLib.lua_type (luaState, stackPos) == LuaTypes.Table) {
				if (LuaLib.luaL_getmetafield (luaState, stackPos, "__index")) {
					if (LuaLib.luaL_checkmetatable (luaState, -1)) {
						LuaLib.lua_insert (luaState, stackPos);
						LuaLib.lua_remove (luaState, stackPos + 1);
					} else
						LuaLib.lua_settop (luaState, -2);
				}
			}

			object obj = translator.getObject (luaState, stackPos);
			return obj;
		}

		public object getAsNetObject (LuaCore.LuaState luaState, int stackPos)
		{
			object obj = translator.getNetObject (luaState, stackPos);

			if (obj.IsNull () && LuaLib.lua_type (luaState, stackPos) == LuaTypes.Table) {
				if (LuaLib.luaL_getmetafield (luaState, stackPos, "__index")) {
					if (LuaLib.luaL_checkmetatable (luaState, -1)) {
						LuaLib.lua_insert (luaState, stackPos);
						LuaLib.lua_remove (luaState, stackPos + 1);
						obj = translator.getNetObject (luaState, stackPos);
					} else 
						LuaLib.lua_settop (luaState, -2);
				}
			}

			return obj;
		}
	}
}