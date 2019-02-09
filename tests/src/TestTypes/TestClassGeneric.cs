namespace NLuaTest.TestTypes
{
    /// <summary>
    /// Generic class with generic and non-generic methods
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class TestClassGeneric<T>
    {
        private object _PassedValue;
        private bool _RegularMethodSuccess;

        public bool RegularMethodSuccess
        {
            get { return _RegularMethodSuccess; }
        }

        private bool _GenericMethodSuccess;

        public bool GenericMethodSuccess
        {
            get { return _GenericMethodSuccess; }
        }

        public void GenericMethod(T value)
        {
            _PassedValue = value;
            _GenericMethodSuccess = true;
        }

        public void RegularMethod()
        {
            _RegularMethodSuccess = true;
        }

        /// <summary>
        /// Returns true if the generic method was successfully passed a matching value
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool Validate(T value)
        {
            return value.Equals(_PassedValue);
        }
    }
}