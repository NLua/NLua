
namespace NLuaTest.TestTypes
{
    class LuaTestDelegate1Handler : NLua.Method.LuaDelegate
    {
        int CallFunction(int a, int b)
        {
            object[] args =  { a, b };
            object[] inArgs =  { a, b };
            int[] outArgs =  { };

            object ret = CallFunction(args, inArgs, outArgs);

            return (int)ret;
        }
    }
}
