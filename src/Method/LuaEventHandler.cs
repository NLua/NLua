namespace NLua.Method
{
    public class LuaEventHandler
    {
        public LuaFunction Handler = null;

        // CP: Fix provided by Ben Bryant for delegates with one param
        // link: http://luaforge.net/forum/message.php?msg_id=9318
        public void HandleEvent(object[] args)
        {
            Handler.Call(args);
        }
    }
}