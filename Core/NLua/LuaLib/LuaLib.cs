/*
 * This file is part of NLua.
 * 
 * Copyright (c) 2013 Vinicius Jarina (viniciusjarina@gmail.com)
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
using NLua.Extensions;

namespace NLua
{
	#if USE_KOPILUA
	using LuaCore = KopiLua.Lua;
	#else
	using LuaCore = KeraLua.Lua;
	#endif

	public class LuaLib
	{
		// steffenj: BEGIN additional Lua API functions new in Lua 5.1
		public static int lua_gc (LuaCore.LuaState luaState, GCOptions what, int data)
		{
			return LuaCore.LuaGC (luaState, (int)what, data);
		}

		public static string lua_typename (LuaCore.LuaState luaState, LuaTypes type)
		{
			return LuaCore.LuaTypeName (luaState, (int)type).ToString ();
		}

		public static string luaL_typename (LuaCore.LuaState luaState, int stackPos)
		{
			return lua_typename (luaState, lua_type (luaState, stackPos));
		}

		public static void luaL_error (LuaCore.LuaState luaState, string message)
		{
			LuaCore.LuaLError (luaState, message);
		}

		public static void luaL_where (LuaCore.LuaState luaState, int level)
		{
			LuaCore.LuaLWhere (luaState, level);
		}

		// steffenj: BEGIN Lua 5.1.1 API change (lua_open replaced by luaL_newstate)
		public static LuaCore.LuaState luaL_newstate ()
		{
			return LuaCore.LuaLNewState ();
		}

		// steffenj: BEGIN Lua 5.1.1 API change (new function luaL_openlibs)
		public static void luaL_openlibs (LuaCore.LuaState luaState)
		{
			LuaCore.LuaLOpenLibs (luaState);
		}

		// steffenj: END Lua 5.1.1 API change (lua_strlen is now lua_objlen)
		// steffenj: BEGIN Lua 5.1.1 API change (lua_dostring is now a macro luaL_dostring)
		public static int luaL_loadstring (LuaCore.LuaState luaState, string chunk)
		{
			return LuaCore.LuaLLoadString (luaState, chunk);
		}

		public static int luaL_loadstring (LuaCore.LuaState luaState, byte[] chunk)
		{
			return LuaCore.LuaLLoadString (luaState, chunk);
		}

		public static int luaL_dostring (LuaCore.LuaState luaState, string chunk)
		{
			int result = luaL_loadstring (luaState, chunk);
			if (result != 0)
				return result;

			return lua_pcall (luaState, 0, -1, 0);
		}

		public static int luaL_dostring (LuaCore.LuaState luaState, byte[] chunk)
		{
			int result = luaL_loadstring (luaState, chunk);
			if (result != 0)
				return result;
			
			return lua_pcall (luaState, 0, -1, 0);
		}
		
		/// <summary>DEPRECATED - use luaL_dostring(LuaCore.LuaState luaState, string chunk) instead!</summary>
		public static int lua_dostring (LuaCore.LuaState luaState, string chunk)
		{
			return luaL_dostring (luaState, chunk);
		}

		// steffenj: END Lua 5.1.1 API change (lua_dostring is now a macro luaL_dostring)
		// steffenj: BEGIN Lua 5.1.1 API change (lua_newtable is gone, lua_createtable is new)
		public static void lua_createtable (LuaCore.LuaState luaState, int narr, int nrec)
		{
			LuaCore.LuaCreateTable (luaState, narr, nrec);
		}

		public static void lua_newtable (LuaCore.LuaState luaState)
		{
			lua_createtable (luaState, 0, 0);
		}

		// steffenj: END Lua 5.1.1 API change (lua_newtable is gone, lua_createtable is new)
		// steffenj: BEGIN Lua 5.1.1 API change (lua_dofile now in LuaLib as luaL_dofile macro)
		public static int luaL_dofile (LuaCore.LuaState luaState, string fileName)
		{
			int result = LuaCore.LuaNetLoadFile (luaState, fileName);
			if (result != 0)
				return result;

			return LuaCore.LuaNetPCall (luaState, 0, -1, 0);
		}

		public static void lua_getglobal (LuaCore.LuaState luaState, string name)
		{
			LuaCore.LuaNetGetGlobal (luaState, name);
		}

		public static void lua_setglobal (LuaCore.LuaState luaState, string name)
		{
			LuaCore.LuaNetSetGlobal (luaState, name);
		}

		public static void lua_settop (LuaCore.LuaState luaState, int newTop)
		{
			LuaCore.LuaSetTop (luaState, newTop);
		}

		public static void lua_pop (LuaCore.LuaState luaState, int amount)
		{
			lua_settop (luaState, -(amount) - 1);
		}

		public static void lua_insert (LuaCore.LuaState luaState, int newTop)
		{
			LuaCore.LuaInsert (luaState, newTop);
		}

		public static void lua_remove (LuaCore.LuaState luaState, int index)
		{
			LuaCore.LuaRemove (luaState, index);
		}

		public static void lua_gettable (LuaCore.LuaState luaState, int index)
		{
			LuaCore.LuaGetTable (luaState, index);
		}

		public static void lua_rawget (LuaCore.LuaState luaState, int index)
		{
			LuaCore.LuaRawGet (luaState, index);
		}

		public static void lua_settable (LuaCore.LuaState luaState, int index)
		{
			LuaCore.LuaSetTable (luaState, index);
		}

		public static void lua_rawset (LuaCore.LuaState luaState, int index)
		{
			LuaCore.LuaRawSet (luaState, index);
		}

		public static void lua_setmetatable (LuaCore.LuaState luaState, int objIndex)
		{
			LuaCore.LuaSetMetatable (luaState, objIndex);
		}

		public static int lua_getmetatable (LuaCore.LuaState luaState, int objIndex)
		{
			return LuaCore.LuaGetMetatable (luaState, objIndex);
		}

		public static int lua_equal (LuaCore.LuaState luaState, int index1, int index2)
		{
			return LuaCore.LuaNetEqual (luaState, index1, index2);
		}

		public static void lua_pushvalue (LuaCore.LuaState luaState, int index)
		{
			LuaCore.LuaPushValue (luaState, index);
		}

		public static void lua_replace (LuaCore.LuaState luaState, int index)
		{
			LuaCore.LuaReplace (luaState, index);
		}

		public static int lua_gettop (LuaCore.LuaState luaState)
		{
			return LuaCore.LuaGetTop (luaState);
		}

		public static LuaTypes lua_type (LuaCore.LuaState luaState, int index)
		{
			return (LuaTypes)LuaCore.LuaType (luaState, index);
		}

		public static bool lua_isnil (LuaCore.LuaState luaState, int index)
		{
			return lua_type (luaState, index) == LuaTypes.Nil;
		}

		public static bool lua_isnumber (LuaCore.LuaState luaState, int index)
		{
			return lua_type (luaState, index) == LuaTypes.Number;
		}

		public static bool lua_isboolean (LuaCore.LuaState luaState, int index)
		{
			return lua_type (luaState, index) == LuaTypes.Boolean;
		}

		public static int luaL_ref (LuaCore.LuaState luaState, int registryIndex)
		{
			return LuaCore.LuaLRef (luaState, registryIndex);
		}

		public static int lua_ref (LuaCore.LuaState luaState, int lockRef)
		{
			return lockRef != 0 ? luaL_ref (luaState, (int)LuaIndexes.Registry) : 0;
		}

		public static void lua_rawgeti (LuaCore.LuaState luaState, int tableIndex, int index)
		{
			LuaCore.LuaRawGetI (luaState, tableIndex, index);
		}

		public static void lua_rawseti (LuaCore.LuaState luaState, int tableIndex, int index)
		{
			LuaCore.LuaRawSetI (luaState, tableIndex, index);
		}

		public static object lua_newuserdata (LuaCore.LuaState luaState, int size)
		{
			return LuaCore.LuaNewUserData (luaState, (uint)size);
		}

		public static object lua_touserdata (LuaCore.LuaState luaState, int index)
		{
			return LuaCore.LuaToUserData (luaState, index);
		}

		public static void lua_getref (LuaCore.LuaState luaState, int reference)
		{
			lua_rawgeti (luaState, (int)LuaIndexes.Registry, reference);
		}

		public static void lua_unref (LuaCore.LuaState luaState, int reference)
		{
			LuaCore.LuaLUnref (luaState, (int)LuaIndexes.Registry, reference);
		}

		public static bool lua_isstring (LuaCore.LuaState luaState, int index)
		{
			return LuaCore.LuaIsString (luaState, index) != 0;
		}

		public static bool lua_iscfunction (LuaCore.LuaState luaState, int index)
		{
			return LuaCore.LuaIsCFunction (luaState, index);
		}

		public static void lua_pushnil (LuaCore.LuaState luaState)
		{
			LuaCore.LuaPushNil (luaState);
		}

		public static void lua_call (LuaCore.LuaState luaState, int nArgs, int nResults)
		{
			LuaCore.LuaCall (luaState, nArgs, nResults);
		}

		public static void lua_pushstdcallcfunction (LuaCore.LuaState luaState, LuaCore.LuaNativeFunction function)
		{
			LuaCore.LuaPushStdCallCFunction (luaState, function);
		}

		public static int lua_pcall (LuaCore.LuaState luaState, int nArgs, int nResults, int errfunc)
		{
			return LuaCore.LuaNetPCall (luaState, nArgs, nResults, errfunc);
		}

		public static LuaCore.LuaNativeFunction lua_tocfunction (LuaCore.LuaState luaState, int index)
		{
			return LuaCore.LuaToCFunction (luaState, index);
		}

		public static double lua_tonumber (LuaCore.LuaState luaState, int index)
		{
			return LuaCore.LuaNetToNumber (luaState, index);
		}

		public static bool lua_toboolean (LuaCore.LuaState luaState, int index)
		{
			return LuaCore.LuaToBoolean (luaState, index) != 0;
		}

		public static string lua_tostring (LuaCore.LuaState luaState, int index)
		{
#if true
			// FIXME use the same format string as lua i.e. LUA_NUMBER_FMT
			var t = lua_type (luaState, index);

			if (t == LuaTypes.Number)
				return string.Format ("{0}", lua_tonumber (luaState, index));
			else if (t == LuaTypes.String) {
				uint strlen;
				// Changed 2013-05-18 by Dirk Weltz
				// Changed because binary chunks, which are also transfered as strings
				// get corrupted by conversion to strings because of the encoding.
				// So we use the ToString method with string length, so it could be checked,
				// if string is a binary chunk and if, could transfered to string without
				// encoding.
				return LuaCore.LuaToLString (luaState, index, out strlen).ToString ((int)strlen);
			} else if (t == LuaTypes.Nil)
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

		public static void lua_atpanic (LuaCore.LuaState luaState, LuaCore.LuaNativeFunction panicf)
		{
			LuaCore.LuaAtPanic (luaState, (LuaCore.LuaNativeFunction)panicf);
		}


		public static void lua_pushnumber (LuaCore.LuaState luaState, double number)
		{
			LuaCore.LuaPushNumber (luaState, number);
		}

		public static void lua_pushboolean (LuaCore.LuaState luaState, bool value)
		{
			LuaCore.LuaPushBoolean (luaState, value ? 1 : 0);
		}

		public static void lua_pushstring (LuaCore.LuaState luaState, string str)
		{
			LuaCore.LuaPushString (luaState, str);
		}

		public static int luaL_newmetatable (LuaCore.LuaState luaState, string meta)
		{
			return LuaCore.LuaLNewMetatable (luaState, meta);
		}

		// steffenj: BEGIN Lua 5.1.1 API change (luaL_getmetatable is now a macro using lua_getfield)
		public static void lua_getfield (LuaCore.LuaState luaState, int stackPos, string meta)
		{
			LuaCore.LuaGetField (luaState, stackPos, meta);
		}

		public static void luaL_getmetatable (LuaCore.LuaState luaState, string meta)
		{
			lua_getfield (luaState, (int)LuaIndexes.Registry, meta);
		}

		public static object luaL_checkudata (LuaCore.LuaState luaState, int stackPos, string meta)
		{
			return LuaCore.LuaLCheckUData (luaState, stackPos, meta);
		}

		public static bool luaL_getmetafield (LuaCore.LuaState luaState, int stackPos, string field)
		{
			return LuaCore.LuaLGetMetafield (luaState, stackPos, field) != 0;
		}

		public static int luaL_loadbuffer (LuaCore.LuaState luaState, string buff, string name)
		{
			return LuaCore.LuaNetLoadBuffer (luaState, buff, (uint)buff.Length, name);
		}

		public static int luaL_loadbuffer (LuaCore.LuaState luaState, byte [] buff, string name)
		{
			return LuaCore.LuaNetLoadBuffer (luaState, buff, (uint)buff.Length, name);
		}

		public static int luaL_loadfile (LuaCore.LuaState luaState, string filename)
		{
			return LuaCore.LuaNetLoadFile (luaState, filename);
		}

		public static bool luaL_checkmetatable (LuaCore.LuaState luaState, int index)
		{
			return LuaCore.LuaLCheckMetatable (luaState, index);
		}

		public static int luanet_registryindex ()
		{
			return LuaCore.LuaNetRegistryIndex ();
		}

		public static int luanet_tonetobject (LuaCore.LuaState luaState, int index)
		{
			return LuaCore.LuaNetToNetObject (luaState, index);
		}

		public static void luanet_newudata (LuaCore.LuaState luaState, int val)
		{
			LuaCore.LuaNetNewUData (luaState, val);
		}

		public static int luanet_rawnetobj (LuaCore.LuaState luaState, int obj)
		{
			return LuaCore.LuaNetRawNetObj (luaState, obj);
		}

		public static int luanet_checkudata (LuaCore.LuaState luaState, int ud, string tname)
		{
			return LuaCore.LuaNetCheckUData (luaState, ud, tname);
		}

		public static void lua_error (LuaCore.LuaState luaState)
		{
			LuaCore.LuaError (luaState);
		}

		public static bool lua_checkstack (LuaCore.LuaState luaState, int extra)
		{
			return LuaCore.LuaCheckStack (luaState, extra) != 0;
		}

		public static int lua_next (LuaCore.LuaState luaState, int index)
		{
			return LuaCore.LuaNext (luaState, index);
		}

		public static void lua_pushlightuserdata (LuaCore.LuaState luaState, LuaCore.LuaTag udata)
		{
			LuaCore.LuaPushLightUserData (luaState, udata.Tag);
		}

		public static LuaCore.LuaTag luanet_gettag ()
		{
			return LuaCore.LuaNetGetTag ();
		}

		public static void luanet_pushglobaltable (LuaCore.LuaState luaState)
		{
			LuaCore.LuaNetPushGlobalTable (luaState);
		}

		public static void luanet_popglobaltable (LuaCore.LuaState luaState)
		{
			LuaCore.LuaNetPopGlobalTable (luaState);
		}

	}
}