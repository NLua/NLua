namespace NLuaTest.TestTypes
{
    /// <summary>
    /// Normal class containing a generic method
    /// </summary>
    public class TestClassWithGenericMethod
    {
        private object _PassedValue;
        public int x;
        public int y;

        public object PassedValue
        {
            get { return _PassedValue; }
        }

        private bool _GenericMethodSuccess;

        public bool GenericMethodSuccess
        {
            get { return _GenericMethodSuccess; }
        }

        public void GenericMethod<T>(T value)
        {
            _PassedValue = value;
            _GenericMethodSuccess = true;
        }

        public void GenericMethodWithCommonArgs<U>(int x, int y, U value)
        {
            _PassedValue = value;
            this.x = x;
            this.y = y;
            _GenericMethodSuccess = true;
        }

        internal bool Validate<T>(T value)
        {
            return value.Equals(_PassedValue);
        }
    }
}