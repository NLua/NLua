namespace NLuaTest.TestTypes
{
    /// <summary>
    /// Normal class containing a generic method
    /// </summary>
    public class TestClassWithGenericMethod
    {
        private object _PassedValue;

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

        internal bool Validate<T>(T value)
        {
            return value.Equals(_PassedValue);
        }
    }
}