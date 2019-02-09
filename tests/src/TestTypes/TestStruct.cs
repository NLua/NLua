namespace NLuaTest.TestTypes
{
    /// <summary>
    /// test structure passing
    /// </summary>
    public struct TestStruct
    {
        public TestStruct(float val)
        {
            v = val;
        }

        public float v;

        public float val
        {
            get { return v; }
            set { v = value; }
        }
    }
}