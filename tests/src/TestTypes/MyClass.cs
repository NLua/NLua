namespace NLuaTest.TestTypes
{
    class MyClass
    {
        public int Func1()
        {
            return 1;
        }

        public T GetValue<T>()
        {
            return default(T);
        }
    }
}