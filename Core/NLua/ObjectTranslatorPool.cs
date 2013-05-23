using System;
using System.Collections.Generic;

/*
 * This file is part of NLua.
 * 
 * Copyright (c) 2013 Vinicius Jarina (viniciusjarina@gmail.com)

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

namespace NLua
{
	#if USE_KOPILUA
	using LuaCore = KopiLua.Lua;
	#else
	using LuaCore = KeraLua.Lua;
	#endif

	internal class ObjectTranslatorPool
	{
		private static volatile ObjectTranslatorPool instance = new ObjectTranslatorPool ();		
		private Dictionary<LuaCore.lua_State, ObjectTranslator> translators = new Dictionary<LuaCore.lua_State, ObjectTranslator>();
		
		public static ObjectTranslatorPool Instance
		{
			get
			{
				return instance;
			}
		}
		
		public ObjectTranslatorPool ()
		{
		}
		
		public void Add (LuaCore.lua_State luaState, ObjectTranslator translator)
		{
			translators.Add(luaState , translator);			
		}
		
		public ObjectTranslator Find (LuaCore.lua_State luaState)
		{
			if (!translators.ContainsKey(luaState))
				return null;
			
			return translators [luaState];
		}
		
		public void Remove (LuaCore.lua_State luaState)
		{
			if (!translators.ContainsKey (luaState))
				return;
			
			translators.Remove (luaState);
		}
	}
}

