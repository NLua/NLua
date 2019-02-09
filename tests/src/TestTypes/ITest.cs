namespace NLuaTest.TestTypes
{
    public interface ITest
    {
        int intProp
        {
            get;
            set;
        }

        TestClass refProp
        {
            get;
            set;
        }

        int test1(int a, int b);

        int test2(int a, out int b);

        void test3(int a, ref int b);

        TestClass test4(int a, int b);

        int test5(TestClass a, TestClass b);

        int test6(int a, out TestClass b);

        void test7(int a, ref TestClass b);
    }
}