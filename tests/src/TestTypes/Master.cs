
namespace NLuaTest.TestTypes
{
    public class Master
    {
        public static string read()
        {
            return "test-master";
        }

        public static string read(Parameter test)
        {
            return test.field1;
        }
    }
}