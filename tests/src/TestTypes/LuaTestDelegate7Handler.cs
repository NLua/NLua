namespace NLuaTest.TestTypes
{
    class LuaTestDelegate7Handler : NLua.Method.LuaDelegate
    {
        void CallFunction(int a, ref TestClass b)
        {
            object[] args =  { a, b };
            object[] inArgs =  { a, b };
            int[] outArgs =  { 1 };

            CallFunction(args, inArgs, outArgs);

            b = (TestClass)args[1];
        }
    }
}