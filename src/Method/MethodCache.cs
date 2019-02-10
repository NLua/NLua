using System;
using System.Reflection;
using NLua.Extensions;

namespace NLua.Method
{
    class MethodCache
    {
        public MethodCache()
        {
            args = new object[0];
            argTypes = new MethodArgs[0];
            outList = new int[0];
        }
        private MethodBase _cachedMethod;

        public MethodBase cachedMethod {
            get
            {
                return _cachedMethod;
            }
            set
            {
                _cachedMethod = value;
                var mi = value as MethodInfo;

                if (mi != null)
                {
                    IsReturnVoid = mi.ReturnType == typeof(void);
                }
            }
        }

        public bool IsReturnVoid;
        // List or arguments
        public object[] args;
        // Positions of out parameters
        public int[] outList;
        // Types of parameters
        public MethodArgs[] argTypes;
    }
}