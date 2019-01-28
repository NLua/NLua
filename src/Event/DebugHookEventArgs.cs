using System;
using KeraLua;

namespace NLua.Event
{
    /// <summary>
    /// Event args for hook callback event
    /// </summary>
    /// <author>Reinhard Ostermeier</author>
    public class DebugHookEventArgs : EventArgs
    {
        readonly LuaDebug luaDebug;

        public DebugHookEventArgs(LuaDebug luaDebug)
        {
            this.luaDebug = luaDebug;
        }

        public LuaDebug LuaDebug {
            get { return luaDebug; }
        }
    }
}