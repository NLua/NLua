using System;
using System.Collections.Generic;
using System.Reflection;
using LuaWrap;

namespace Mono.LuaInterface
{
	/*
	 * Type checking and conversion functions.
	 * 
	 * Author: Fabio Mascarenhas
	 * Version: 1.0
	 */
	class CheckType
	{
		private ObjectTranslator translator;

		ExtractValue extractNetObject;
		Dictionary<long, ExtractValue> extractValues = new Dictionary<long, ExtractValue>();
		
		public CheckType(ObjectTranslator translator) 
		{
            this.translator = translator;

            extractValues.Add(typeof(object).TypeHandle.Value.ToInt64(), new ExtractValue(getAsObject));
            extractValues.Add(typeof(sbyte).TypeHandle.Value.ToInt64(), new ExtractValue(getAsSbyte));
            extractValues.Add(typeof(byte).TypeHandle.Value.ToInt64(), new ExtractValue(getAsByte));
            extractValues.Add(typeof(short).TypeHandle.Value.ToInt64(), new ExtractValue(getAsShort));
            extractValues.Add(typeof(ushort).TypeHandle.Value.ToInt64(), new ExtractValue(getAsUshort));
            extractValues.Add(typeof(int).TypeHandle.Value.ToInt64(), new ExtractValue(getAsInt));
            extractValues.Add(typeof(uint).TypeHandle.Value.ToInt64(), new ExtractValue(getAsUint));
            extractValues.Add(typeof(long).TypeHandle.Value.ToInt64(), new ExtractValue(getAsLong));
            extractValues.Add(typeof(ulong).TypeHandle.Value.ToInt64(), new ExtractValue(getAsUlong));
            extractValues.Add(typeof(double).TypeHandle.Value.ToInt64(), new ExtractValue(getAsDouble));
            extractValues.Add(typeof(char).TypeHandle.Value.ToInt64(), new ExtractValue(getAsChar));
            extractValues.Add(typeof(float).TypeHandle.Value.ToInt64(), new ExtractValue(getAsFloat));
            extractValues.Add(typeof(decimal).TypeHandle.Value.ToInt64(), new ExtractValue(getAsDecimal));
            extractValues.Add(typeof(bool).TypeHandle.Value.ToInt64(), new ExtractValue(getAsBoolean));
            extractValues.Add(typeof(string).TypeHandle.Value.ToInt64(), new ExtractValue(getAsString));
            extractValues.Add(typeof(LuaFunction).TypeHandle.Value.ToInt64(), new ExtractValue(getAsFunction));
            extractValues.Add(typeof(LuaTable).TypeHandle.Value.ToInt64(), new ExtractValue(getAsTable));
            extractValues.Add(typeof(LuaUserData).TypeHandle.Value.ToInt64(), new ExtractValue(getAsUserdata));

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
			if(paramType.IsByRef) paramType=paramType.GetElementType();
			
			long runtimeHandleValue = paramType.TypeHandle.Value.ToInt64();

            if(extractValues.ContainsKey(runtimeHandleValue))
	            return extractValues[runtimeHandleValue];
            else
				return extractNetObject;
		}

		internal ExtractValue checkType(IntPtr luaState,int stackPos,Type paramType) 
		{
            LuaType luatype = LuaLib.lua_type(luaState, stackPos);

			if(paramType.IsByRef) paramType=paramType.GetElementType();

            Type underlyingType = Nullable.GetUnderlyingType(paramType);
            if (underlyingType != null)
            {
                paramType = underlyingType;     // Silently convert nullable types to their non null requics
            }

			long runtimeHandleValue = paramType.TypeHandle.Value.ToInt64();
			
			if (paramType.Equals(typeof(object)))
				return extractValues[runtimeHandleValue];

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
                else if (luatype == LuaType.Nil)
					return extractNetObject; // kevinh - silently convert nil to a null string pointer
            }
            else if (paramType == typeof(LuaTable))
            {
                if (luatype == LuaType.Table)
					return extractValues[runtimeHandleValue];
            }
            else if (paramType == typeof(LuaUserData))
            {
                if (luatype == LuaType.UserData)
					return extractValues[runtimeHandleValue];
            }
            else if (paramType == typeof(LuaFunction))
            {
                if (luatype == LuaType.Function)
					return extractValues[runtimeHandleValue];
            }
            else if (typeof(Delegate).IsAssignableFrom(paramType) && luatype == LuaType.Function)
            {
                return new ExtractValue(new DelegateGenerator(translator, paramType).extractGenerated);
            }
            else if (paramType.IsInterface && luatype == LuaType.Table)
            {
                return new ExtractValue(new ClassGenerator(translator, paramType).extractGenerated);
            }
            else if ((paramType.IsInterface || paramType.IsClass) && luatype == LuaType.Nil)
            {
                // kevinh - allow nil to be silently converted to null - extractNetObject will return null when the item ain't found
                return extractNetObject;
            }
            else if (LuaLib.lua_type(luaState, stackPos) == LuaType.Table)
            {
                if (LuaLib.luaL_getmetafield(luaState, stackPos, "__index"))
                {
                    object obj = translator.getNetObject(luaState, -1);
                    LuaLib.lua_settop(luaState, -2);
                    if (obj != null && paramType.IsAssignableFrom(obj.GetType()))
						return extractNetObject;
                }
                else
					return null;
            }
            else
            {
                object obj = translator.getNetObject(luaState, stackPos);
                if (obj != null && paramType.IsAssignableFrom(obj.GetType()))
					return extractNetObject;
            }

            return null;
		}

		/*
		 * The following functions return the value in the Lua stack
		 * index stackPos as the desired type if it can, or null
		 * otherwise.
		 */
		private object getAsSbyte(IntPtr luaState,int stackPos) 
		{
			sbyte retVal=(sbyte)LuaLib.lua_tonumber(luaState,stackPos);
			if(retVal==0 && !LuaLib.lua_isnumber(luaState,stackPos)) return null;
			return retVal;
		}
		private object getAsByte(IntPtr luaState,int stackPos) 
		{
			byte retVal=(byte)LuaLib.lua_tonumber(luaState,stackPos);
			if(retVal==0 && !LuaLib.lua_isnumber(luaState,stackPos)) return null;
			return retVal;
		}
		private object getAsShort(IntPtr luaState,int stackPos) 
		{
			short retVal=(short)LuaLib.lua_tonumber(luaState,stackPos);
			if(retVal==0 && !LuaLib.lua_isnumber(luaState,stackPos)) return null;
			return retVal;
		}
		private object getAsUshort(IntPtr luaState,int stackPos) 
		{
			ushort retVal=(ushort)LuaLib.lua_tonumber(luaState,stackPos);
			if(retVal==0 && !LuaLib.lua_isnumber(luaState,stackPos)) return null;
			return retVal;
		}
		private object getAsInt(IntPtr luaState,int stackPos) 
		{
			int retVal=(int)LuaLib.lua_tonumber(luaState,stackPos);
			if(retVal==0 && !LuaLib.lua_isnumber(luaState,stackPos)) return null;
			return retVal;
		}
		private object getAsUint(IntPtr luaState,int stackPos) 
		{
			uint retVal=(uint)LuaLib.lua_tonumber(luaState,stackPos);
			if(retVal==0 && !LuaLib.lua_isnumber(luaState,stackPos)) return null;
			return retVal;
		}
		private object getAsLong(IntPtr luaState,int stackPos) 
		{
			long retVal=(long)LuaLib.lua_tonumber(luaState,stackPos);
			if(retVal==0 && !LuaLib.lua_isnumber(luaState,stackPos)) return null;
			return retVal;
		}
		private object getAsUlong(IntPtr luaState,int stackPos) 
		{
			ulong retVal=(ulong)LuaLib.lua_tonumber(luaState,stackPos);
			if(retVal==0 && !LuaLib.lua_isnumber(luaState,stackPos)) return null;
			return retVal;
		}
		private object getAsDouble(IntPtr luaState,int stackPos) 
		{
			double retVal=LuaLib.lua_tonumber(luaState,stackPos);
			if(retVal==0 && !LuaLib.lua_isnumber(luaState,stackPos)) return null;
			return retVal;
		}
		private object getAsChar(IntPtr luaState,int stackPos) 
		{
			char retVal=(char)LuaLib.lua_tonumber(luaState,stackPos);
			if(retVal==0 && !LuaLib.lua_isnumber(luaState,stackPos)) return null;
			return retVal;
		}
		private object getAsFloat(IntPtr luaState,int stackPos) 
		{
			float retVal=(float)LuaLib.lua_tonumber(luaState,stackPos);
			if(retVal==0 && !LuaLib.lua_isnumber(luaState,stackPos)) return null;
			return retVal;
		}
		private object getAsDecimal(IntPtr luaState,int stackPos) 
		{
			decimal retVal=(decimal)LuaLib.lua_tonumber(luaState,stackPos);
			if(retVal==0 && !LuaLib.lua_isnumber(luaState,stackPos)) return null;
			return retVal;
		}
		private object getAsBoolean(IntPtr luaState,int stackPos) 
		{
			return LuaLib.lua_toboolean(luaState,stackPos);
		}
		private object getAsString(IntPtr luaState,int stackPos) 
		{
			string retVal=LuaLib.lua_tostring(luaState,stackPos);
			if(retVal=="" && !LuaLib.lua_isstring(luaState,stackPos)) return null;
			return retVal;
		}
		private object getAsTable(IntPtr luaState,int stackPos) 
		{
			return translator.getTable(luaState,stackPos);
		}
		private object getAsFunction(IntPtr luaState,int stackPos) 
		{
			return translator.getFunction(luaState,stackPos);
		}
		private object getAsUserdata(IntPtr luaState,int stackPos) 
		{
			return translator.getUserData(luaState,stackPos);
		}
		public object getAsObject(IntPtr luaState,int stackPos) 
		{
			if(LuaLib.lua_type(luaState,stackPos)==LuaType.Table) 
			{
				if(LuaLib.luaL_getmetafield(luaState,stackPos,"__index")) 
				{
					if(LuaLib.luaL_checkmetatable(luaState,-1)) 
					{
						LuaLib.lua_insert(luaState,stackPos);
						LuaLib.lua_remove(luaState,stackPos+1);
					} 
					else 
					{
						LuaLib.lua_settop(luaState,-2);
					}
				}
			}
			object obj=translator.getObject(luaState,stackPos);
			return obj;
		}
		public object getAsNetObject(IntPtr luaState,int stackPos) 
		{
			object obj=translator.getNetObject(luaState,stackPos);
			if(obj==null && LuaLib.lua_type(luaState,stackPos)==LuaType.Table) 
			{
				if(LuaLib.luaL_getmetafield(luaState,stackPos,"__index")) 
				{
					if(LuaLib.luaL_checkmetatable(luaState,-1)) 
					{
						LuaLib.lua_insert(luaState,stackPos);
						LuaLib.lua_remove(luaState,stackPos+1);
						obj=translator.getNetObject(luaState,stackPos);
					} 
					else 
					{
						LuaLib.lua_settop(luaState,-2);
					}
				}
			}
			return obj;
		}
	}
}
