using System;
#if WINDOWS_PHONE || NET_3_5
using System.Collections.Generic;
#else
using System.Collections.Concurrent;
#endif

/*
 * This file is part of NLua.
 * 
 * Copyright (c) 2015 Vinicius Jarina (viniciusjarina@gmail.com)

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
	using LuaCore  = KopiLua.Lua;
	using LuaState = KopiLua.LuaState;
	#else
	using LuaCore  = KeraLua.Lua;
	using LuaState = KeraLua.LuaState;
	#endif

	internal class ObjectTranslatorPool
	{
		private static volatile ObjectTranslatorPool instance = new ObjectTranslatorPool ();		
#if WINDOWS_PHONE || NET_3_5
		private Dictionary<LuaState, ObjectTranslator> translators = new Dictionary<LuaState, ObjectTranslator>();
#else
		private ConcurrentDictionary<LuaState, ObjectTranslator> translators = new ConcurrentDictionary<LuaState, ObjectTranslator>();
#endif

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
		
		public void Add (LuaState luaState, ObjectTranslator translator)
		{
#if WINDOWS_PHONE || NET_3_5
			lock (translators)
				translators.Add (luaState, translator);			
#else
			if (!translators.TryAdd (luaState, translator))
				throw new ArgumentException ("An item with the same key has already been added. ", "luaState");
#endif
		}

		public ObjectTranslator Find (LuaState luaState)
		{
#if WINDOWS_PHONE || NET_3_5
			lock (translators) 
			{
#endif
			ObjectTranslator translator;

			if (!translators.TryGetValue (luaState, out translator))
			{
				LuaState main = LuaCore.LuaNetGetMainState (luaState);

				if (!translators.TryGetValue (main, out translator))
					translator = null;
			}
			
			return translator;
#if WINDOWS_PHONE || NET_3_5
			}
#endif
		}
		
		public void Remove (LuaState luaState)
		{
#if WINDOWS_PHONE || NET_3_5
			lock (translators)
			{
				if (!translators.ContainsKey (luaState))
					return;
			
				translators.Remove (luaState);
			}
#else
			ObjectTranslator translator;
			translators.TryRemove (luaState, out translator);
#endif 
		}
	}
}

