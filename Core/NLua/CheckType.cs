/*
 * This file is part of NLua.
 * 
 * Copyright (c) 2014 Vinicius Jarina (viniciusjarina@gmail.com)
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
	using LuaCore  = KopiLua.Lua;
	using LuaState = KopiLua.LuaState;
	#else
	using LuaCore  = KeraLua.Lua;
	using LuaState = KeraLua.LuaState;
	#endif

	/*
	 * Type checking and conversion functions.
	 * 
	 * Author: Fabio Mascarenhas
	 * Version: 1.0
	 */
	sealed class CheckType
	{
		Dictionary<Type, ExtractValue> extractValues = new Dictionary<Type, ExtractValue>();
		ExtractValue extractNetObject;
		ObjectTranslator translator;

		public CheckType (ObjectTranslator translator)
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
			extractValues.Add(GetExtractDictionaryKey(typeof(char[])), new ExtractValue (GetAsCharArray));
			extractValues.Add(GetExtractDictionaryKey(typeof(LuaFunction)), new ExtractValue(GetAsFunction));
			extractValues.Add(GetExtractDictionaryKey(typeof(LuaTable)), new ExtractValue(GetAsTable));
			extractValues.Add(GetExtractDictionaryKey(typeof(LuaUserData)), new ExtractValue(GetAsUserdata));
			extractNetObject = new ExtractValue (GetAsNetObject);		
		}

		/*
		 * Checks if the value at Lua stack index stackPos matches paramType, 
		 * returning a conversion function if it does and null otherwise.
		 */
		internal ExtractValue GetExtractor (ProxyType paramType)
		{
			return GetExtractor (paramType.UnderlyingSystemType);
		}

		internal ExtractValue GetExtractor (Type paramType)
		{
			if (paramType.IsByRef)
				paramType = paramType.GetElementType ();

			var extractKey = GetExtractDictionaryKey(paramType);
			return extractValues.ContainsKey(extractKey) ? extractValues[extractKey] : extractNetObject;
		}

		internal ExtractValue CheckLuaType (LuaState luaState, int stackPos, Type paramType)
		{
			var luatype = LuaLib.LuaType (luaState, stackPos);

			if (paramType.IsByRef)
				paramType = paramType.GetElementType ();

			var underlyingType = Nullable.GetUnderlyingType (paramType);

			if (underlyingType != null)
				paramType = underlyingType;	 // Silently convert nullable types to their non null requics

			var extractKey = GetExtractDictionaryKey (paramType);

			if (paramType.Equals (typeof(object)))
				return extractValues [extractKey];

			//CP: Added support for generic parameters
			if (paramType.IsGenericParameter) {
				if (luatype == LuaTypes.Boolean)
					return extractValues [GetExtractDictionaryKey (typeof(bool))];
				else if (luatype == LuaTypes.String)
					return extractValues[GetExtractDictionaryKey (typeof(string))];
				else if (luatype == LuaTypes.Table)
					return extractValues [GetExtractDictionaryKey (typeof(LuaTable))];
				else if (luatype == LuaTypes.UserData)
					return extractValues [GetExtractDictionaryKey (typeof(object))];
				else if (luatype == LuaTypes.Function)
					return extractValues [GetExtractDictionaryKey (typeof(LuaFunction))];
				else if (luatype == LuaTypes.Number)
					return extractValues [GetExtractDictionaryKey (typeof(double))];
			}
			bool netParamIsString = paramType == typeof (string) || paramType == typeof (char []);
			bool netParamIsNumeric = paramType == typeof (int) ||
									 paramType == typeof (uint) ||
									 paramType == typeof (long) ||
									 paramType == typeof (ulong) ||
									 paramType == typeof (short) ||
									 paramType == typeof (float) ||
									 paramType == typeof (double) ||
									 paramType == typeof (decimal) ||
									 paramType == typeof (byte);

			if (netParamIsNumeric) {
				if (LuaLib.LuaIsNumber (luaState, stackPos) && !netParamIsString)
					return extractValues [extractKey];
			} else if (paramType == typeof(bool)) {
				if (LuaLib.LuaIsBoolean (luaState, stackPos))
					return extractValues [extractKey];
			} else if (netParamIsString) {
				if (LuaLib.LuaNetIsStringStrict (luaState, stackPos))
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
				return new ExtractValue (new DelegateGenerator (translator, paramType).ExtractGenerated);
			else if (paramType.IsInterface() && luatype == LuaTypes.Table)
				return new ExtractValue (new ClassGenerator (translator, paramType).ExtractGenerated);
			else if ((paramType.IsInterface() || paramType.IsClass()) && luatype == LuaTypes.Nil) {
				// kevinh - allow nil to be silently converted to null - extractNetObject will return null when the item ain't found
				return extractNetObject;
			} else if (LuaLib.LuaType (luaState, stackPos) == LuaTypes.Table) {
				if (LuaLib.LuaLGetMetafield (luaState, stackPos, "__index")) {
					object obj = translator.GetNetObject (luaState, -1);
					LuaLib.LuaSetTop (luaState, -2);
					if (obj != null && paramType.IsAssignableFrom (obj.GetType ()))
						return extractNetObject;
				} else
					return null;
			} else {
				object obj = translator.GetNetObject (luaState, stackPos);
				if (obj != null && paramType.IsAssignableFrom (obj.GetType ()))
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
		private object GetAsSbyte (LuaState luaState, int stackPos)
		{
			sbyte retVal = (sbyte)LuaLib.LuaToNumber (luaState, stackPos);
			if (retVal == 0 && !LuaLib.LuaIsNumber (luaState, stackPos))
				return null;

			return retVal;
		}

		private object GetAsByte (LuaState luaState, int stackPos)
		{
			byte retVal = (byte)LuaLib.LuaToNumber (luaState, stackPos);
			if (retVal == 0 && !LuaLib.LuaIsNumber (luaState, stackPos))
				return null;

			return retVal;
		}

		private object GetAsShort (LuaState luaState, int stackPos)
		{
			short retVal = (short)LuaLib.LuaToNumber (luaState, stackPos);
			if (retVal == 0 && !LuaLib.LuaIsNumber (luaState, stackPos))
				return null;

			return retVal;
		}

		private object GetAsUshort (LuaState luaState, int stackPos)
		{
			ushort retVal = (ushort)LuaLib.LuaToNumber (luaState, stackPos);
			if (retVal == 0 && !LuaLib.LuaIsNumber (luaState, stackPos))
				return null;

			return retVal;
		}

		private object GetAsInt (LuaState luaState, int stackPos)
		{
			if (!LuaLib.LuaIsNumber (luaState, stackPos))
				return null;

			int retVal = (int)LuaLib.LuaToNumber (luaState, stackPos);
			return retVal;
		}

		private object GetAsUint (LuaState luaState, int stackPos)
		{
			uint retVal = (uint)LuaLib.LuaToNumber (luaState, stackPos);
			if (retVal == 0 && !LuaLib.LuaIsNumber (luaState, stackPos))
				return null;

			return retVal;
		}

		private object GetAsLong (LuaState luaState, int stackPos)
		{
			long retVal = (long)LuaLib.LuaToNumber (luaState, stackPos);
			if (retVal == 0 && !LuaLib.LuaIsNumber (luaState, stackPos))
				return null;

			return retVal;
		}

		private object GetAsUlong (LuaState luaState, int stackPos)
		{
			ulong retVal = (ulong)LuaLib.LuaToNumber (luaState, stackPos);
			if (retVal == 0 && !LuaLib.LuaIsNumber (luaState, stackPos))
				return null;

			return retVal;
		}

		private object GetAsDouble (LuaState luaState, int stackPos)
		{
			double retVal = LuaLib.LuaToNumber (luaState, stackPos);
			if (retVal == 0 && !LuaLib.LuaIsNumber (luaState, stackPos))
				return null;

			return retVal;
		}

		private object GetAsChar (LuaState luaState, int stackPos)
		{
			char retVal = (char)LuaLib.LuaToNumber (luaState, stackPos);
			if (retVal == 0 && !LuaLib.LuaIsNumber (luaState, stackPos))
				return null;

			return retVal;
		}

		private object GetAsFloat (LuaState luaState, int stackPos)
		{
			float retVal = (float)LuaLib.LuaToNumber (luaState, stackPos);
			if (retVal == 0 && !LuaLib.LuaIsNumber (luaState, stackPos))
				return null;

			return retVal;
		}

		private object GetAsDecimal (LuaState luaState, int stackPos)
		{
			decimal retVal = (decimal)LuaLib.LuaToNumber (luaState, stackPos);
			if (retVal == 0 && !LuaLib.LuaIsNumber (luaState, stackPos))
				return null;

			return retVal;
		}

		private object GetAsBoolean (LuaState luaState, int stackPos)
		{
			return LuaLib.LuaToBoolean (luaState, stackPos);
		}



		private object GetAsCharArray (LuaState luaState, int stackPos)
		{
			if (!LuaLib.LuaNetIsStringStrict (luaState, stackPos))
				return null;
			string retVal = LuaLib.LuaToString (luaState, stackPos).ToString ();
			return retVal.ToCharArray();
		}

		private object GetAsString (LuaState luaState, int stackPos)
		{
			if (!LuaLib.LuaNetIsStringStrict (luaState, stackPos))
				return null;
			string retVal = LuaLib.LuaToString (luaState, stackPos).ToString ();			
			return retVal;
		}

		private object GetAsTable (LuaState luaState, int stackPos)
		{
			return translator.GetTable (luaState, stackPos);
		}

		private object GetAsFunction (LuaState luaState, int stackPos)
		{
			return translator.GetFunction (luaState, stackPos);
		}

		private object GetAsUserdata (LuaState luaState, int stackPos)
		{
			return translator.GetUserData (luaState, stackPos);
		}

		public object GetAsObject (LuaState luaState, int stackPos)
		{
			if (LuaLib.LuaType (luaState, stackPos) == LuaTypes.Table) {
				if (LuaLib.LuaLGetMetafield (luaState, stackPos, "__index")) {
					if (LuaLib.LuaLCheckMetatable (luaState, -1)) {
						LuaLib.LuaInsert (luaState, stackPos);
						LuaLib.LuaRemove (luaState, stackPos + 1);
					} else
						LuaLib.LuaSetTop (luaState, -2);
				}
			}

			object obj = translator.GetObject (luaState, stackPos);
			return obj;
		}

		public object GetAsNetObject (LuaState luaState, int stackPos)
		{
			object obj = translator.GetNetObject (luaState, stackPos);

			if (obj == null && LuaLib.LuaType (luaState, stackPos) == LuaTypes.Table) {
				if (LuaLib.LuaLGetMetafield (luaState, stackPos, "__index")) {
					if (LuaLib.LuaLCheckMetatable (luaState, -1)) {
						LuaLib.LuaInsert (luaState, stackPos);
						LuaLib.LuaRemove (luaState, stackPos + 1);
						obj = translator.GetNetObject (luaState, stackPos);
					} else 
						LuaLib.LuaSetTop (luaState, -2);
				}
			}

			return obj;
		}
	}
}