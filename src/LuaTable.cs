
using System.Collections;

using NLua.Extensions;

using LuaState = KeraLua.Lua;

namespace NLua
{
    public class LuaTable : LuaBase
    {
        public LuaTable(int reference, Lua interpreter):base(reference)
        {
            _Interpreter = interpreter;
        }

        /*
         * Indexer for string fields of the table
         */
        public object this[string field] {
            get
            {
                return _Interpreter.GetObject(_Reference, field);
            }
            set
            {
                _Interpreter.SetObject(_Reference, field, value);
            }
        }

        /*
         * Indexer for numeric fields of the table
         */
        public object this[object field] {
            get
            {
                return _Interpreter.GetObject(_Reference, field);
            }
            set
            {
                _Interpreter.SetObject(_Reference, field, value);
            }
        }

        public IDictionaryEnumerator GetEnumerator()
        {
            return _Interpreter.GetTableDict(this).GetEnumerator();
        }

        public ICollection Keys => _Interpreter.GetTableDict(this).Keys;


        public ICollection Values => _Interpreter.GetTableDict(this).Values;


        /*
         * Gets an string fields of a table ignoring its metatable,
         * if it exists
         */
        internal object RawGet(string field)
        {
            return _Interpreter.RawGetObject(_Reference, field);
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