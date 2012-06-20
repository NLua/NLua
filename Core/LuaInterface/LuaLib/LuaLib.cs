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
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using LuaInterface.Extensions;

namespace LuaInterface
{
	using LuaCore = KopiLua.Lua;

	public static class LuaLib
	{
		private static int tag = 0;

		public static LuaTypes ToLuaTypes(this int type)
		{
			return (LuaTypes)type;
		}

		public static LuaEnums ToLuaEnums(this int lenum)
		{
			return (LuaEnums)lenum;
		}

		public static bool ToBoolean(this int number)
		{
			return number == 1;
		}

		#region Core Library
		/// <summary>
		/// Pushes a C function onto the stack. This function receives a pointer to a C function and pushes onto the stack a Lua value of type function that, when called, invokes the corresponding C function. 
		/// </summary>
		/// <param name="state">
		/// A <see cref="IntPtr"/>
		/// </param>
		/// <param name="fn">
		/// A <see cref="CallbackFunction"/>
		/// </param>
		public static void lua_pushcfunction(LuaCore.lua_State state, LuaCore.lua_CFunction fn)
		{
			LuaCore.lua_pushcclosure(state, fn, 0);
		}
		#endregion

		#region Auxiliary Library
		/// <summary>
		/// Loads and runs the given file.
		/// </summary>
		/// <param name="state">
		/// A <see cref="IntPtr"/>
		/// </param>
		/// <param name="filename">
		/// A <see cref="System.String"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.Boolean"/>
		/// </returns>
		public static bool luaL_dofile(LuaCore.lua_State state, string filename)
		{
			return (LuaCore.luaL_loadfile(state, filename).ToLuaEnums() == LuaEnums.Ok) && (LuaCore.lua_pcall(state, 0, (int)LuaEnums.MultiRet, 0).ToLuaEnums() == LuaEnums.Ok);
		}

		/// <summary>
		/// Loads and runs the given string.
		/// </summary>
		/// <param name="state">
		/// A <see cref="IntPtr"/>
		/// </param>
		/// <param name="chunk">
		/// A <see cref="System.String"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.Boolean"/>
		/// </returns>
		public static bool luaL_dostring(LuaCore.lua_State state, string chunk)
		{
			return (LuaCore.luaL_loadstring(state, chunk).ToLuaEnums() == LuaEnums.Ok) && (LuaCore.lua_pcall(state, 0, (int)LuaEnums.MultiRet, 0).ToLuaEnums() == LuaEnums.Ok);
		}

		public static LuaEnums luaL_loadbuffer(LuaCore.lua_State luaState, string buff, string name)
		{
			var result = LuaCore.luaL_loadbuffer(luaState, buff, (uint)buff.Length, name).ToLuaEnums();
			return result;
		}

		public static bool luaL_checkmetatable(LuaCore.lua_State luaState,int index)
		{
			bool retVal = false;
			Console.WriteLine("v: " + luaState.tt.ToString());

			if(LuaCore.lua_getmetatable(luaState,index) != 0) 
			{
				LuaCore.lua_pushlightuserdata(luaState, tag);
				LuaCore.lua_rawget(luaState, -2);
				retVal = !LuaCore.lua_isnil(luaState, -1);
				LuaCore.lua_settop(luaState, -3);
			}

			return retVal;
		}

		public static int luanet_gettag()
		{
			return tag;
		}

		public static void lua_getref(LuaCore.lua_State luaState, int reference)
		{
			LuaCore.lua_rawgeti(luaState, (int)PseudoIndex.Registry, reference);
		}

		public static void lua_unref(LuaCore.lua_State luaState, int reference) 
		{
			LuaCore.luaL_unref(luaState, (int)PseudoIndex.Registry, reference);
		}

		public static int luanet_rawnetobj(LuaCore.lua_State luaState, int obj)
		{
			int udata = (int)LuaCore.lua_touserdata2(luaState, obj);
			return udata != 0 ? udata : -1;
		}

		public static void lua_pushstdcallcfunction(LuaCore.lua_State luaState, LuaCore.lua_CFunction function)
		{
			lua_pushcfunction(luaState, function);
		}

		public static int checkudata_raw(LuaCore.lua_State luaState, int ud, string tname)
		{
			int p = (int)LuaCore.lua_touserdata2(luaState, ud);

			if(p != 0) 
			{
				/* value is a userdata? */
				if(LuaCore.lua_getmetatable(luaState, ud) != 0) 
				{
					/* does it have a metatable? */
					LuaCore.lua_getfield(luaState, (int)PseudoIndex.Registry, tname);  /* get correct metatable */
					bool isEqual = LuaCore.lua_rawequal(luaState, -1, -2).ToBoolean();

					// NASTY - we need our own version of the lua_pop macro
					// lua_pop(L, 2);  /* remove both metatables */
					LuaCore.lua_settop(luaState, -(2) - 1);

					if(isEqual)   /* does it have the correct mt? */
						return p;
				 }
			}
		  
			return 0;
		}

		public static int luanet_checkudata(LuaCore.lua_State luaState, int ud, string tname)
		{
			int udata = checkudata_raw(luaState, ud, tname);
			return udata != 0 ? udata : -1;
		}

		public static void luanet_newudata(LuaCore.lua_State luaState, int val)
		{
			LuaCore.lua_newuserdata(luaState, (uint)val);
		}

		public static int luanet_tonetobject(LuaCore.lua_State luaState, int index)
		{
			int udata;
			Console.WriteLine("x" + LuaCore.lua_type(luaState, index).ToString());

			if(LuaCore.lua_type(luaState, index).ToLuaTypes() == LuaTypes.UserData)
			{
				if(luaL_checkmetatable(luaState, index)) 
				{
					udata = (int)LuaCore.lua_touserdata2(luaState, index);
					if(udata != 0) 
						return udata; 
				}

				udata = checkudata_raw(luaState, index, "luaNet_class");
				if(udata != 0)
					return udata;

				udata = checkudata_raw(luaState, index, "luaNet_searchbase");
				if(udata != 0)
					return udata;

				udata = checkudata_raw(luaState, index, "luaNet_function");
				if(udata != 0)
					return udata;
			}

			return -1;
		}

		public static int lua_ref(LuaCore.lua_State luaState, int lockRef)
		{
			return lockRef != 0 ? LuaCore.luaL_ref(luaState, (int)PseudoIndex.Registry) : 0;
		}
		#endregion
	}
}