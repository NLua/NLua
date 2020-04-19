namespace NLuaTest.TestTypes
{
    public class TestClass2
    {
        public string teststrval;

        public static int func(int x, int y)
        {
            return x + y;
        }

        public int funcInstance(int x, int y)
        {
            return x + y;
        }

        public int this[int index]
        {
            get { return 3; }
            set { }
        }

        public int this[string index]
        {
            get { return 1; }
            set { }
        }
    }
}