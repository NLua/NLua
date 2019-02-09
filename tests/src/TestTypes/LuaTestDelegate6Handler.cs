namespace NLuaTest.TestTypes
{
    class LuaTestDelegate6Handler : NLua.Method.LuaDelegate
    {
        int CallFunction(int a, ref TestClass b)
        {
            object[] args = { a, b };
            object[] inArgs =  { a };
            int[] outArgs =  { 1 };

            object ret = CallFunction(args, inArgs, outArgs);

            b = (TestClass)args[1];
            return (int)ret;
        }
    }
}