using System;

namespace NLua
{
    /// <summary>
    /// Marks a method, field or property to be hidden from Lua
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class LuaHideAttribute : Attribute
    {
    }
}