namespace NLuaTest.TestTypes
{
    class LuaTestDelegate4Handler : NLua.Method.LuaDelegate
    {
        TestClass CallFunction(int a, int b)
        {
            object[] args =  { a, b };
            object[] inArgs =  { a, b };
            int[] outArgs = { };

            object ret = CallFunction(args, inArgs, outArgs);

            return (TestClass)ret;
        }
    }
}