
namespace NLuaTest.TestTypes
{
    public class testClass : Master
    {
        public string strData;
        public int intData;
        public static string read2()
        {
            return "test";
        }

        public static string read(int test)
        {
            return "int-test";
        }
    }
}