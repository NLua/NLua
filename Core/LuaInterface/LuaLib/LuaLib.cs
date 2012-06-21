/*
 * This file is part of LuaInterface.
 * 
 * Copyright (C) 2003-2005 Fabio Mascarenhas de Queiroz.
 * Copyright (C) 2009 Joshua Simmons <simmons.44@gmail.com>
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
using System.IO;
using LuaInterface.Extensions;

namespace LuaInterface
{
	using LuaCore = KopiLua.Lua;

	public class LuaLib
	{
		// Not sure of the purpose of this, but I'm keeping it -kevinh
		private static object tag = 0;

		// steffenj: BEGIN additional Lua API functions new in Lua 5.1
		public static int lua_gc(LuaCore.lua_State luaState, GCOptions what, int data)
		{
			return LuaCore.lua_gc(luaState, (int)what, data);
		}

		public static string lua_typename(LuaCore.lua_State luaState, LuaTypes type)
		{
			return LuaCore.lua_typename(luaState, (int)type).ToString();
		}

		public static string luaL_typename(LuaCore.lua_State luaState, int stackPos)
		{
			return lua_typename(luaState, lua_type(luaState, stackPos));
		}

		public static void luaL_error(LuaCore.lua_State luaState, string message)
		{
			LuaCore.luaL_error(luaState, message);
		}

		public static void luaL_where(LuaCore.lua_State luaState, int level)
		{
			LuaCore.luaL_where(luaState, level);
		}

		// steffenj: BEGIN Lua 5.1.1 API change (lua_open replaced by luaL_newstate)
		public static LuaCore.lua_State luaL_newstate()
		{
			return LuaCore.luaL_newstate();
		}

		// steffenj: BEGIN Lua 5.1.1 API change (new function luaL_openlibs)
		public static void luaL_openlibs(LuaCore.lua_State luaState)
		{
			LuaCore.luaL_openlibs(luaState);
		}

		// steffenj: END Lua 5.1.1 API change (lua_strlen is now lua_objlen)
		// steffenj: BEGIN Lua 5.1.1 API change (lua_dostring is now a macro luaL_dostring)
		public static int luaL_loadstring(LuaCore.lua_State luaState, string chunk)
		{
			return LuaCore.luaL_loadstring(luaState, chunk);
		}

		public static int luaL_dostring(LuaCore.lua_State luaState, string chunk)
		{
			int result = luaL_loadstring(luaState, chunk);
			if(result != 0)
				return result;

			return lua_pcall(luaState, 0, -1, 0);
		}

		/// <summary>DEPRECATED - use luaL_dostring(LuaCore.lua_State luaState, string chunk) instead!</summary>
		public static int lua_dostring(LuaCore.lua_State luaState, string chunk)
		{
			return luaL_dostring(luaState, chunk);
		}

		// steffenj: END Lua 5.1.1 API change (lua_dostring is now a macro luaL_dostring)
		// steffenj: BEGIN Lua 5.1.1 API change (lua_newtable is gone, lua_createtable is new)
		public static void lua_createtable(LuaCore.lua_State luaState, int narr, int nrec)
		{
			LuaCore.lua_createtable(luaState, narr, nrec);
		}

		public static void lua_newtable(LuaCore.lua_State luaState)
		{
			lua_createtable(luaState, 0, 0);
		}

		// steffenj: END Lua 5.1.1 API change (lua_newtable is gone, lua_createtable is new)
		// steffenj: BEGIN Lua 5.1.1 API change (lua_dofile now in LuaLib as luaL_dofile macro)
		public static int luaL_dofile(LuaCore.lua_State luaState, string fileName)
		{
			int result = LuaCore.luaL_loadfile(luaState, fileName);
			if(result != 0)
				return result;

			return LuaCore.lua_pcall(luaState, 0, -1, 0);
		}

		// steffenj: END Lua 5.1.1 API change (lua_dofile now in LuaLib as luaL_dofile)
		public static void lua_getglobal(LuaCore.lua_State luaState, string name) 
		{
			lua_pushstring(luaState, name);
			LuaCore.lua_gettable(luaState, (int)LuaIndexes.Globals);
		}

		public static void lua_setglobal(LuaCore.lua_State luaState, string name)
		{
			lua_pushstring(luaState,name);
			lua_insert(luaState,-2);
			lua_settable(luaState, (int)LuaIndexes.Globals);
		}

		public static void lua_settop(LuaCore.lua_State luaState, int newTop)
		{
			LuaCore.lua_settop(luaState, newTop);
		}

		public static void lua_pop(LuaCore.lua_State luaState, int amount)
		{
			lua_settop(luaState, -(amount) - 1);
		}

		public static void lua_insert(LuaCore.lua_State luaState, int newTop)
		{
			LuaCore.lua_insert(luaState, newTop);
		}

		public static void lua_remove(LuaCore.lua_State luaState, int index)
		{
			LuaCore.lua_remove(luaState, index);
		}

		public static void lua_gettable(LuaCore.lua_State luaState, int index)
		{
			LuaCore.lua_gettable(luaState, index);
		}


		public static void lua_rawget(LuaCore.lua_State luaState, int index)
		{
			LuaCore.lua_rawget(luaState, index);
		}


		public static void lua_settable(LuaCore.lua_State luaState, int index)
		{
			LuaCore.lua_settable(luaState, index);
		}


		public static void lua_rawset(LuaCore.lua_State luaState, int index)
		{
			LuaCore.lua_rawset(luaState, index);
		}


		public static void lua_setmetatable(LuaCore.lua_State luaState, int objIndex)
		{
			LuaCore.lua_setmetatable(luaState, objIndex);
		}


		public static int lua_getmetatable(LuaCore.lua_State luaState, int objIndex)
		{
			return LuaCore.lua_getmetatable(luaState, objIndex);
		}


		public static int lua_equal(LuaCore.lua_State luaState, int index1, int index2)
		{
			return LuaCore.lua_equal(luaState, index1, index2);
		}


		public static void lua_pushvalue(LuaCore.lua_State luaState, int index)
		{
			LuaCore.lua_pushvalue(luaState, index);
		}

		public static void lua_replace(LuaCore.lua_State luaState, int index)
		{
			LuaCore.lua_replace(luaState, index);
		}

		public static int lua_gettop(LuaCore.lua_State luaState)
		{
			return LuaCore.lua_gettop(luaState);
		}

		public static LuaTypes lua_type(LuaCore.lua_State luaState, int index)
		{
			return (LuaTypes) LuaCore.lua_type(luaState, index);
		}

		public static bool lua_isnil(LuaCore.lua_State luaState, int index)
		{
			return lua_type(luaState, index) == LuaTypes.Nil;
		}

		public static bool lua_isnumber(LuaCore.lua_State luaState, int index)
		{
			return lua_type(luaState, index) == LuaTypes.Number;
		}

		public static bool lua_isboolean(LuaCore.lua_State luaState, int index) 
		{
			return lua_type(luaState, index) == LuaTypes.Boolean;
		}

		public static int luaL_ref(LuaCore.lua_State luaState, int registryIndex)
		{
			return LuaCore.luaL_ref(luaState, registryIndex);
		}

		public static int lua_ref(LuaCore.lua_State luaState, int lockRef)
		{
			return lockRef != 0 ? luaL_ref(luaState, (int)LuaIndexes.Registry) : 0;
		}

		public static void lua_rawgeti(LuaCore.lua_State luaState, int tableIndex, int index)
		{
			LuaCore.lua_rawgeti(luaState, tableIndex, index);
		}

		public static void lua_rawseti(LuaCore.lua_State luaState, int tableIndex, int index)
		{
			LuaCore.lua_rawseti(luaState, tableIndex, index);
		}


		public static object lua_newuserdata(LuaCore.lua_State luaState, int size)
		{
			return LuaCore.lua_newuserdata(luaState, (uint)size);
		}

		public static object lua_touserdata(LuaCore.lua_State luaState, int index)
		{
			return LuaCore.lua_touserdata(luaState, index);
		}

		public static void lua_getref(LuaCore.lua_State luaState, int reference)
		{
			lua_rawgeti(luaState, (int)LuaIndexes.Registry, reference);
		}

		public static void lua_unref(LuaCore.lua_State luaState, int reference) 
		{
			LuaCore.luaL_unref(luaState, (int)LuaIndexes.Registry, reference);
		}

		public static bool lua_isstring(LuaCore.lua_State luaState, int index)
		{
			return LuaCore.lua_isstring(luaState, index) != 0;
		}

		public static bool lua_iscfunction(LuaCore.lua_State luaState, int index)
		{
			return LuaCore.lua_iscfunction(luaState, index);
		}

		public static void lua_pushnil(LuaCore.lua_State luaState)
		{
			LuaCore.lua_pushnil(luaState);
		}

		public static void lua_call(LuaCore.lua_State luaState, int nArgs, int nResults)
		{
			LuaCore.lua_call(luaState, nArgs, nResults);
		}

		public static int lua_pcall(LuaCore.lua_State luaState, int nArgs, int nResults, int errfunc)
		{
			return LuaCore.lua_pcall(luaState, nArgs, nResults, errfunc);
		}

		public static LuaCore.lua_CFunction lua_tocfunction(LuaCore.lua_State luaState, int index)
		{
			return LuaCore.lua_tocfunction(luaState, index);
		}

		public static double lua_tonumber(LuaCore.lua_State luaState, int index)
		{
			return LuaCore.lua_tonumber(luaState, index);
		}

		public static bool lua_toboolean(LuaCore.lua_State luaState, int index)
		{
			return LuaCore.lua_toboolean(luaState, index) != 0;
		}

		public static string lua_tostring(LuaCore.lua_State luaState, int index)
		{
#if true
			// FIXME use the same format string as lua i.e. LUA_NUMBER_FMT
			var t = lua_type(luaState, index);

			if(t == LuaTypes.Number)
				return string.Format("{0}", lua_tonumber(luaState, index));
			else if(t == LuaTypes.String)
			{
				uint strlen;
				return LuaCore.lua_tolstring(luaState, index, out strlen).ToString();
			}
			else if(t == LuaTypes.Nil)
				return null;			// treat lua nulls to as C# nulls
			else
				return "0";	// Because luaV_tostring does this
#else
			size_t strlen;

			// Note!  This method will _change_ the representation of the object on the stack to a string.
			// We do not want this behavior so we do the conversion ourselves
			const char *str = LuaCore.lua_tolstring(luaState, index, &strlen);
			if (str)
				return Marshal::PtrToStringAnsi(IntPtr((char *) str), strlen);
			else
				return nullptr;			// treat lua nulls to as C# nulls
#endif
		}

		public static void lua_atpanic(LuaCore.lua_State luaState, LuaCore.lua_CFunction panicf)
		{
			LuaCore.lua_atpanic(luaState, (LuaCore.lua_CFunction)panicf);
		}

		public static void lua_pushstdcallcfunction(LuaCore.lua_State luaState, LuaCore.lua_CFunction function)
		{
			LuaCore.lua_pushcfunction(luaState, function);
		}

		public static void lua_pushnumber(LuaCore.lua_State luaState, double number)
		{
			LuaCore.lua_pushnumber(luaState, number);
		}

		public static void lua_pushboolean(LuaCore.lua_State luaState, bool value)
		{
			LuaCore.lua_pushboolean(luaState, value ? 1 : 0);
		}

		public static void lua_pushstring(LuaCore.lua_State luaState, string str)
		{
			LuaCore.lua_pushstring(luaState, str);
		}

		public static int luaL_newmetatable(LuaCore.lua_State luaState, string meta)
		{
			return LuaCore.luaL_newmetatable(luaState, meta);
		}

		// steffenj: BEGIN Lua 5.1.1 API change (luaL_getmetatable is now a macro using lua_getfield)
		public static void lua_getfield(LuaCore.lua_State luaState, int stackPos, string meta)
		{
			LuaCore.lua_getfield(luaState, stackPos, meta);
		}

		public static void luaL_getmetatable(LuaCore.lua_State luaState, string meta)
		{
			lua_getfield(luaState, (int)LuaIndexes.Registry, meta);
		}

		public static object luaL_checkudata(LuaCore.lua_State luaState, int stackPos, string meta)
		{
			return LuaCore.luaL_checkudata(luaState, stackPos, meta);
		}

		public static bool luaL_getmetafield(LuaCore.lua_State luaState, int stackPos, string field)
		{
			return LuaCore.luaL_getmetafield(luaState, stackPos, field) != 0;
		}

		public static int luaL_loadbuffer(LuaCore.lua_State luaState, string buff, string name)
		{
			return LuaCore.luaL_loadbuffer(luaState, buff, (uint)buff.Length, name);
		}

		public static int luaL_loadfile(LuaCore.lua_State luaState, string filename)
		{
			return LuaCore.luaL_loadfile(luaState, filename);
		}

		public static void lua_error(LuaCore.lua_State luaState)
		{
			LuaCore.lua_error(luaState);
		}

		public static bool lua_checkstack(LuaCore.lua_State luaState,int extra)
		{
			return LuaCore.lua_checkstack(luaState, extra) != 0;
		}

		public static int lua_next(LuaCore.lua_State luaState,int index)
		{
			return LuaCore.lua_next(luaState, index);
		}

		public static void lua_pushlightuserdata(LuaCore.lua_State luaState, object udata)
		{
			LuaCore.lua_pushlightuserdata(luaState, udata);
		}

		public static int luanet_rawnetobj(LuaCore.lua_State luaState,int obj)
		{
			byte[] bytes = lua_touserdata(luaState, obj) as byte[];
			return fourBytesToInt(bytes);
		}

		// Starting with 5.1 the auxlib version of checkudata throws an exception if the type isn't right
		// Instead, we want to run our own version that checks the type and just returns null for failure
		private static object checkudata_raw(LuaCore.lua_State L, int ud, string tname)
		{
			object p = LuaCore.lua_touserdata(L, ud);

			if(p != null) 
			{
				/* value is a userdata? */
				if(LuaCore.lua_getmetatable(L, ud) != 0) 
				{ 
					bool isEqual;

					/* does it have a metatable? */
					LuaCore.lua_getfield(L, (int)LuaIndexes.Registry, tname);  /* get correct metatable */

					isEqual = LuaCore.lua_rawequal(L, -1, -2) != 0;

					// NASTY - we need our own version of the lua_pop macro
					// lua_pop(L, 2);  /* remove both metatables */
					LuaCore.lua_settop(L, -(2) - 1);

					if(isEqual)	/* does it have the correct mt? */
						return p;
				}
			}
		  
			return null;
		}

		public static int luanet_checkudata(LuaCore.lua_State luaState, int ud, string tname)
		{
			object udata = checkudata_raw(luaState, ud, tname);
			return !udata.IsNull() ? fourBytesToInt(udata as byte[]) : -1;
		}

		public static bool luaL_checkmetatable(LuaCore.lua_State luaState,int index)
		{
			bool retVal = false;

			if(lua_getmetatable(luaState, index) != 0) 
			{
				lua_pushlightuserdata(luaState, tag);
				lua_rawget(luaState, -2);
				retVal = !lua_isnil(luaState, -1);
				lua_settop(luaState, -3);
			}

			return retVal;
		}

		public static object luanet_gettag() 
		{
			return tag;
		}

		public static void luanet_newudata(LuaCore.lua_State luaState,int val)
		{
			var userdata = lua_newuserdata(luaState, sizeof(int)) as byte[];
			intToFourBytes(val, userdata);
		}

		public static int luanet_tonetobject(LuaCore.lua_State luaState,int index)
		{
			byte[] udata;

			if(lua_type(luaState, index) == LuaTypes.UserData) 
			{
				if(luaL_checkmetatable(luaState, index)) 
				{
					udata=lua_touserdata(luaState, index) as byte[];
					if(!udata.IsNull())
						return fourBytesToInt(udata);
				}

				udata = checkudata_raw(luaState, index, "luaNet_class") as byte[];
				if(!udata.IsNull())
					return fourBytesToInt(udata);

				udata = checkudata_raw(luaState, index, "luaNet_searchbase") as byte[];
				if(!udata.IsNull())
					return fourBytesToInt(udata);

				udata = checkudata_raw(luaState, index, "luaNet_function") as byte[];
				if(!udata.IsNull())
					return fourBytesToInt(udata);
			}

			return -1;
		}

		private static int fourBytesToInt(byte[] bytes)
		{
			return bytes[0] + (bytes[1] << 8) + (bytes[2] << 16) + (bytes[3] << 24);
		}

		private static void intToFourBytes(int val, byte[] bytes)
		{
			// gfoot: is this really a good idea?
			bytes[0] = (byte)val;
			bytes[1] = (byte)(val >> 8);
			bytes[2] = (byte)(val >> 16);
			bytes[3] = (byte)(val >> 24);
		}
	}
}