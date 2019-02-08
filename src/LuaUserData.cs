
namespace NLua
{
    public class LuaUserData : LuaBase
    {
        public LuaUserData(int reference, Lua interpreter):base(reference)
        {
            _Interpreter = interpreter;
        }

        /*
         * Indexer for string fields of the userdata
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
         * Indexer for numeric fields of the userdata
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

        /*
         * Calls the userdata and returns its return values inside
         * an array
         */
        public object[] Call(params object[] args)
        {
            return _Interpreter.CallFunction(this, args);
        }

        public override string ToString()
        {
            return "userdata";
        }
    }
}