
namespace NLuaTest.TestTypes
{
/*
 * Delegates used for testing Lua function -> delegate translation
 */
    public delegate int TestDelegate1(int a, int b);

    public delegate int TestDelegate2(int a, out int b);

    public delegate void TestDelegate3(int a, ref int b);

    public delegate TestClass TestDelegate4(int a, int b);

    public delegate int TestDelegate5(TestClass a, TestClass b);

    public delegate int TestDelegate6(int a, out TestClass b);

    public delegate void TestDelegate7(int a, ref TestClass b);
}
