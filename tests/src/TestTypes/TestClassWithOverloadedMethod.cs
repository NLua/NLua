namespace NLuaTest.TestTypes
{
    class TestClassWithOverloadedMethod
    {
        public int CallsToStringFunc { get; set; }
        public int CallsToIntFunc { get; set; }
        public void Func(string param)
        {
            CallsToStringFunc++;
        }

        public void Func(int param)
        {
            CallsToIntFunc++;
        }

    }
}