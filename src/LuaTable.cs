
using System;
using System.Collections;

using NLua.Extensions;

using LuaState = KeraLua.Lua;

namespace NLua
{
    public class LuaTable : LuaBase
    {
        public LuaTable(int reference, Lua interpreter): base(reference, interpreter)
        {
        }

        /*
         * Indexer for string fields of the table
         */
        public object this[string field] {
            get
            {
                Lua lua;
                if (!TryGet(out lua))
                    return null;
                return lua.GetObject(_Reference, field);
            }
            set
            {
                Lua lua;
                if (!TryGet(out lua))
                    return;
                lua.SetObject(_Reference, field, value);
            }
        }

        /*
         * Indexer for numeric fields of the table
         */
        public object this[object field] {
            get
            {
                Lua lua;
                if (!TryGet(out lua))
                    return null;

                return lua.GetObject(_Reference, field);
            }
            set
            {
                Lua lua;
                if (!TryGet(out lua))
                    return;

                lua.SetObject(_Reference, field, value);
            }
        }

        public IDictionaryEnumerator GetEnumerator()
        {
            Lua lua;
            if (!TryGet(out lua))
                return null;

            return lua.GetTableDict(this).GetEnumerator();
        }

        public ICollection Keys
        {
            get
            {
                Lua lua;
                if (!TryGet(out lua))
                    return null;

                return lua.GetTableDict(this).Keys;
            }
        }


        public ICollection Values
        {
            get
            {
                Lua lua;
                if (!TryGet(out lua))
                    return new object[0];

                return lua.GetTableDict(this).Values;
            }
        }


        /*
         * Gets an string fields of a table ignoring its metatable,
         * if it exists
         */
        internal object RawGet(string field)
        {
            Lua lua;
            if (!TryGet(out lua))
                return null;

            return lua.RawGetObject(_Reference, field);
        }

        /*
         * Pushes this table into the Lua stack
         */
        internal void Push(LuaState luaState)
        {
            luaState.GetRef(_Reference);
        }

        public override string ToString()
        {
            return "table";
        }
    }
}
