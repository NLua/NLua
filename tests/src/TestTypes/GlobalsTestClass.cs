namespace NLuaTest.TestTypes
{
    class GlobalsTestClass
    {
        public int Property1 { get; set; }

        public int Property2{ get; }

        public int Method1()
        {
            return 1;
        }

        private int Method2()
        {
            return 2;
        }

        public int Method3(int param)
        {
            return param;
        }
    }
}