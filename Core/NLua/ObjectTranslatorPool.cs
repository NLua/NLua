using System;
using System.Collections.Generic;

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
		
		private static object syncRoot = new object ();
		
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
			syncRoot = new Dictionary<LuaCore.lua_State, ObjectTranslator> ();
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

