
using System;
using System.Collections;

using NLua.Extensions;

using LuaState = KeraLua.Lua;

namespace NLua
{
    public class LuaThread : LuaBase
    {
        public LuaThread(int reference, Lua interpreter): base(reference, interpreter)
        {
        }

        /*
         * Pushes this thread into the Lua stack
         */
        internal void Push(LuaState luaState)
        {
            luaState.GetRef(_Reference);
        }

        public override string ToString()
        {
            return "thread";
        }
    }
}
