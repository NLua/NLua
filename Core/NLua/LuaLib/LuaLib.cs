/*
 * This file is part of NLua.
 * 
 * Copyright (c) 2014 Vinicius Jarina (viniciusjarina@gmail.com)
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
	using LuaCore  = KopiLua.Lua;
	using LuaState = KopiLua.LuaState;
	using LuaTag = KopiLua.LuaTag;
	using LuaNativeFunction = KopiLua.LuaNativeFunction;
	#else
	using LuaCore  = KeraLua.Lua;
	using LuaState = KeraLua.LuaState;
	using LuaTag = KeraLua.LuaTag;
	using LuaNativeFunction = KeraLua.LuaNativeFunction;
	#endif



	public class LuaLib
	{
		public static int LuaGC (LuaState luaState, GCOptions what, int data)
		{
			return LuaCore.LuaGC (luaState, (int)what, data);
		}

		public static string LuaTypeName (LuaState luaState, LuaTypes type)
		{
			return LuaCore.LuaTypeName (luaState, (int)type).ToString ();
		}

		public static string LuaLTypeName (LuaState luaState, int stackPos)
		{
			return LuaTypeName (luaState, LuaType (luaState, stackPos));
		}

		public static void LuaLError (LuaState luaState, string message)
		{
			LuaCore.LuaLError (luaState, message);
		}

		public static void LuaLWhere (LuaState luaState, int level)
		{
			LuaCore.LuaLWhere (luaState, level);
		}

		public static LuaState LuaLNewState ()
		{
			return LuaCore.LuaLNewState ();
		}

		public static void LuaLOpenLibs (LuaState luaState)
		{
			LuaCore.LuaLOpenLibs (luaState);
		}

		public static int LuaLLoadString (LuaState luaState, string chunk)
		{
			return LuaCore.LuaLLoadString (luaState, chunk);
		}

		public static int LuaLLoadString (LuaState luaState, byte[] chunk)
		{
			return LuaCore.LuaLLoadString (luaState, chunk);
		}

		public static int LuaLDoString (LuaState luaState, string chunk)
		{
			int result = LuaLLoadString (luaState, chunk);
			if (result != 0)
				return result;

			return LuaPCall (luaState, 0, -1, 0);
		}

		public static int LuaLDoString (LuaState luaState, byte[] chunk)
		{
			int result = LuaLLoadString (luaState, chunk);
			if (result != 0)
				return result;
			
			return LuaPCall (luaState, 0, -1, 0);
		}
		
		public static void LuaCreateTable (LuaState luaState, int narr, int nrec)
		{
			LuaCore.LuaCreateTable (luaState, narr, nrec);
		}

		public static void LuaNewTable (LuaState luaState)
		{
			LuaCreateTable (luaState, 0, 0);
		}

		public static int LuaLDoFile (LuaState luaState, string fileName)
		{
			int result = LuaCore.LuaNetLoadFile (luaState, fileName);
			if (result != 0)
				return result;

			return LuaCore.LuaNetPCall (luaState, 0, -1, 0);
		}

		public static void LuaGetGlobal (LuaState luaState, string name)
		{
			LuaCore.LuaNetGetGlobal (luaState, name);
		}

		public static void LuaSetGlobal (LuaState luaState, string name)
		{
			LuaCore.LuaNetSetGlobal (luaState, name);
		}

		public static void LuaSetTop (LuaState luaState, int newTop)
		{
			LuaCore.LuaSetTop (luaState, newTop);
		}

		public static void LuaPop (LuaState luaState, int amount)
		{
			LuaSetTop (luaState, -(amount) - 1);
		}

		public static void LuaInsert (LuaState luaState, int newTop)
		{
			LuaCore.LuaInsert (luaState, newTop);
		}

		public static void LuaRemove (LuaState luaState, int index)
		{
			LuaCore.LuaRemove (luaState, index);
		}

		public static void LuaGetTable (LuaState luaState, int index)
		{
			LuaCore.LuaGetTable (luaState, index);
		}

		public static void LuaRawGet (LuaState luaState, int index)
		{
			LuaCore.LuaRawGet (luaState, index);
		}

		public static void LuaSetTable (LuaState luaState, int index)
		{
			LuaCore.LuaSetTable (luaState, index);
		}

		public static void LuaRawSet (LuaState luaState, int index)
		{
			LuaCore.LuaRawSet (luaState, index);
		}

		public static void LuaSetMetatable (LuaState luaState, int objIndex)
		{
			LuaCore.LuaSetMetatable (luaState, objIndex);
		}

		public static int LuaGetMetatable (LuaState luaState, int objIndex)
		{
			return LuaCore.LuaGetMetatable (luaState, objIndex);
		}

		public static int LuaEqual (LuaState luaState, int index1, int index2)
		{
			return LuaCore.LuaNetEqual (luaState, index1, index2);
		}

		public static void LuaPushValue (LuaState luaState, int index)
		{
			LuaCore.LuaPushValue (luaState, index);
		}

		public static void LuaReplace (LuaState luaState, int index)
		{
			LuaCore.LuaReplace (luaState, index);
		}

		public static int LuaGetTop (LuaState luaState)
		{
			return LuaCore.LuaGetTop (luaState);
		}

		public static LuaTypes LuaType (LuaState luaState, int index)
		{
			return (LuaTypes)LuaCore.LuaType (luaState, index);
		}

		public static bool LuaIsNil (LuaState luaState, int index)
		{
			return LuaType (luaState, index) == LuaTypes.Nil;
		}

		public static bool LuaIsNumber (LuaState luaState, int index)
		{
			return LuaType (luaState, index) == LuaTypes.Number;
		}

		public static bool LuaIsBoolean (LuaState luaState, int index)
		{
			return LuaType (luaState, index) == LuaTypes.Boolean;
		}

		public static int LuaLRef (LuaState luaState, int registryIndex)
		{
			return LuaCore.LuaLRef (luaState, registryIndex);
		}

		public static int LuaRef (LuaState luaState, int lockRef)
		{
			return lockRef != 0 ? LuaLRef (luaState, (int)LuaIndexes.Registry) : 0;
		}

		public static void LuaRawGetI (LuaState luaState, int tableIndex, int index)
		{
			LuaCore.LuaRawGetI (luaState, tableIndex, index);
		}

		public static void LuaRawSetI (LuaState luaState, int tableIndex, int index)
		{
			LuaCore.LuaRawSetI (luaState, tableIndex, index);
		}

		public static object LuaNewUserData (LuaState luaState, int size)
		{
			return LuaCore.LuaNewUserData (luaState, (uint)size);
		}

		public static object LuaToUserData (LuaState luaState, int index)
		{
			return LuaCore.LuaToUserData (luaState, index);
		}

		public static void LuaGetRef (LuaState luaState, int reference)
		{
			LuaRawGetI (luaState, (int)LuaIndexes.Registry, reference);
		}

		public static void LuaUnref (LuaState luaState, int reference)
		{
			LuaCore.LuaLUnref (luaState, (int)LuaIndexes.Registry, reference);
		}

		public static bool LuaIsString (LuaState luaState, int index)
		{
			return LuaCore.LuaIsString (luaState, index) != 0;
		}

		public static bool LuaNetIsStringStrict (LuaState luaState, int index)
		{
			return LuaCore.LuaNetIsStringStrict (luaState, index) != 0;
		}

		public static bool LuaIsCFunction (LuaState luaState, int index)
		{
			return LuaCore.LuaIsCFunction (luaState, index);
		}

		public static void LuaPushNil (LuaState luaState)
		{
			LuaCore.LuaPushNil (luaState);
		}

		public static void LuaPushStdCallCFunction (LuaState luaState, LuaNativeFunction function)
		{
			LuaCore.LuaPushStdCallCFunction (luaState, function);
		}

		public static int LuaPCall (LuaState luaState, int nArgs, int nResults, int errfunc)
		{
			return LuaCore.LuaNetPCall (luaState, nArgs, nResults, errfunc);
		}

		public static LuaNativeFunction LuaToCFunction (LuaState luaState, int index)
		{
			return LuaCore.LuaToCFunction (luaState, index);
		}

		public static double LuaToNumber (LuaState luaState, int index)
		{
			return LuaCore.LuaNetToNumber (luaState, index);
		}

		public static bool LuaToBoolean (LuaState luaState, int index)
		{
			return LuaCore.LuaToBoolean (luaState, index) != 0;
		}

		public static string LuaToString (LuaState luaState, int index)
		{
			// FIXME use the same format string as lua i.e. LUA_NUMBER_FMT
			var t = LuaType (luaState, index);

			if (t == LuaTypes.Number)
				return string.Format ("{0}", LuaToNumber (luaState, index));
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
		}

		public static void LuaAtPanic (LuaState luaState, LuaNativeFunction panicf)
		{
			LuaCore.LuaAtPanic (luaState, (LuaNativeFunction)panicf);
		}


		public static void LuaPushNumber (LuaState luaState, double number)
		{
			LuaCore.LuaPushNumber (luaState, number);
		}

		public static void LuaPushBoolean (LuaState luaState, bool value)
		{
			LuaCore.LuaPushBoolean (luaState, value ? 1 : 0);
		}

		public static void LuaPushString (LuaState luaState, string str)
		{
			LuaCore.LuaPushString (luaState, str);
		}

		public static int LuaLNewMetatable (LuaState luaState, string meta)
		{
			return LuaCore.LuaLNewMetatable (luaState, meta);
		}

		public static void LuaGetField (LuaState luaState, int stackPos, string meta)
		{
			LuaCore.LuaGetField (luaState, stackPos, meta);
		}

		public static void LuaLGetMetatable (LuaState luaState, string meta)
		{
			LuaGetField (luaState, (int)LuaIndexes.Registry, meta);
		}

		public static object LuaLCheckUData (LuaState luaState, int stackPos, string meta)
		{
			return LuaCore.LuaLCheckUData (luaState, stackPos, meta);
		}

		public static bool LuaLGetMetafield (LuaState luaState, int stackPos, string field)
		{
			return LuaCore.LuaLGetMetafield (luaState, stackPos, field) != 0;
		}

		public static int LuaLLoadBuffer (LuaState luaState, string buff, string name)
		{
			return LuaCore.LuaNetLoadBuffer (luaState, buff, (uint)0, name);
		}

		public static int LuaLLoadBuffer (LuaState luaState, byte [] buff, string name)
		{
			return LuaCore.LuaNetLoadBuffer (luaState, buff, (uint)buff.Length, name);
		}

		public static int LuaLLoadFile (LuaState luaState, string filename)
		{
			return LuaCore.LuaNetLoadFile (luaState, filename);
		}

		public static bool LuaLCheckMetatable (LuaState luaState, int index)
		{
			return LuaCore.LuaLCheckMetatable (luaState, index);
		}

		public static int LuaNetRegistryIndex ()
		{
			return LuaCore.LuaNetRegistryIndex ();
		}

		public static int LuaNetToNetObject (LuaState luaState, int index)
		{
			return LuaCore.LuaNetToNetObject (luaState, index);
		}

		public static void LuaNetNewUData (LuaState luaState, int val)
		{
			LuaCore.LuaNetNewUData (luaState, val);
		}

		public static int LuaNetRawNetObj (LuaState luaState, int obj)
		{
			return LuaCore.LuaNetRawNetObj (luaState, obj);
		}

		public static int LuaNetCheckUData (LuaState luaState, int ud, string tname)
		{
			return LuaCore.LuaNetCheckUData (luaState, ud, tname);
		}

		public static void LuaError (LuaState luaState)
		{
			LuaCore.LuaError (luaState);
		}

		public static bool LuaCheckStack (LuaState luaState, int extra)
		{
			return LuaCore.LuaCheckStack (luaState, extra) != 0;
		}

		public static int LuaNext (LuaState luaState, int index)
		{
			return LuaCore.LuaNext (luaState, index);
		}

		public static void LuaPushLightUserData (LuaState luaState, LuaTag udata)
		{
			LuaCore.LuaPushLightUserData (luaState, udata.Tag);
		}

		public static LuaTag LuaNetGetTag ()
		{
			return LuaCore.LuaNetGetTag ();
		}

		public static void LuaNetPushGlobalTable (LuaState luaState)
		{
			LuaCore.LuaNetPushGlobalTable (luaState);
		}

		public static void LuaNetPopGlobalTable (LuaState luaState)
		{
			LuaCore.LuaNetPopGlobalTable (luaState);
		}
	}
}