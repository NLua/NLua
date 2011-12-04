using System;
using System.Collections.Generic;
using System.Text;

namespace Mono.LuaInterface
{
    /// <summary>
    /// Add a specific type for Lua exceptions (kevinh)
    /// </summary>
    public class LuaException : ApplicationException
    {
        public LuaException(string reason)
            : base(reason)
        {
        }
    }
}
