namespace NLuaTest.TestTypes
{
    public class TestClassWithMethodDefaultParameter
    {
        public int x;
        public void Func(string param1, int param2 = 0, int param3 = 0, string param = null)
        {
            if (param == null)
                x += 1;
            else if (param == "foo")
                x += 2;
            else if (param == "")
                x += 4;
        }

        public void Func2(string param1, int param2 = 0, int param3 = 0, string param = "default")
        {
            if (param == null)
                x += 1;
            else if (param == "foo")
                x += 2;
            else if (param == "default")
                x += 4;
            else if (param == "")
                x += 8;
        }
    }
}