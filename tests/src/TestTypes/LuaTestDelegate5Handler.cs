namespace NLuaTest.TestTypes
{
    class LuaTestDelegate5Handler : NLua.Method.LuaDelegate
    {
        int CallFunction(TestClass a, TestClass b)
        {
            object[] args =  { a, b };
            object[] inArgs =  { a, b };
            int[] outArgs = { };

            object ret = CallFunction(args, inArgs, outArgs);

            return (int)ret;
        }
    }
}