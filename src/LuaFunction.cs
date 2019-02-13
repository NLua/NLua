using System;
using KeraLua;

using LuaState = KeraLua.Lua;
using LuaNativeFunction = KeraLua.LuaFunction;

namespace NLua
{
    public class LuaFunction : LuaBase
    {
        internal readonly LuaNativeFunction function;

        public LuaFunction(int reference, Lua interpreter):base(reference, interpreter)
        {
            function = null;
        }

        public LuaFunction(LuaNativeFunction nativeFunction, Lua interpreter):base (0, interpreter)
        {
            function = nativeFunction;
        }

        /*
         * Calls the function casting return values to the types
         * in returnTypes
         */
        internal object[] Call(object[] args, Type[] returnTypes)
        {
            Lua lua;
            if (!TryGet(out lua))
                return null;

            return lua.CallFunction(this, args, returnTypes);
        }

        /*
         * Calls the function and returns its return values inside
         * an array
         */
        public object[] Call(params object[] args)
        {
            Lua lua;
            if (!TryGet(out lua))
                return null;

            return lua.CallFunction(this, args);
        }

        /*
         * Pushes the function into the Lua stack
         */
        internal void Push(LuaState luaState)
        {
            Lua lua;
            if (!TryGet(out lua))
                return;

            if (_Reference != 0)
                luaState.RawGetInteger(LuaRegistry.Index, _Reference);
            else
                lua.PushCSFunction(function);
        }

        public override string ToString()
        {
            return "function";
        }

        public override bool Equals(object o)
        {
            var l = o as LuaFunction;

            if (l == null)
                return false;

            Lua lua;
            if (!TryGet(out lua))
                return false;

            if (_Reference != 0 && l._Reference != 0)
                return lua.CompareRef(l._Reference, _Reference);

            return function == l.function;
        }

        public override int GetHashCode()
        {
            return _Reference != 0 ? _Reference : function.GetHashCode();
        }
    }
}