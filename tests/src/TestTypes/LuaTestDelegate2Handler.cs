namespace NLuaTest.TestTypes
{
    class LuaTestDelegate2Handler : NLua.Method.LuaDelegate
    {
        int CallFunction(int a, out int b)
        {
            object[] args =  { a, 0 };
            object[] inArgs =  { a };
            int[] outArgs =  { 1 };

            object ret = CallFunction(args, inArgs, outArgs);

            b = (int)args[1];
            return (int)ret;
        }
    }
}