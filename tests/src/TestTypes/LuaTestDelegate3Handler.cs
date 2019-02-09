namespace NLuaTest.TestTypes
{
    class LuaTestDelegate3Handler : NLua.Method.LuaDelegate
    {
        void CallFunction(int a, ref int b)
        {
            object[] args =  { a, b };
            object[] inArgs =  { a, b };
            int[] outArgs =  { 1 };

            CallFunction(args, inArgs, outArgs);

            b = (int)args[1];
        }
    }
}