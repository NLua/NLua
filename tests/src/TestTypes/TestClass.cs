using System;
using NLua;

namespace NLuaTest.TestTypes
{
    public class TestClass : IFoo1, IFoo2
    {
        public int val;
        private string strVal;

        public long LongValue { get; set; }

        public TestClass()
        {
            val = 0;
        }

        public TestClass(int val)
        {
            this.val = val;
        }

        public TestClass(string val)
        {
            this.strVal = val;
        }

        public static TestClass makeFromString(String str)
        {
            return new TestClass(str);
        }

        bool? nb2 = null;

        public bool? NullableBool
        {
            get { return nb2; }
            set { nb2 = value; }
        }

        TestStruct s = new TestStruct();

        public TestStruct Struct
        {
            get { return s; }
            set { s = (TestStruct)value; }
        }

        public int testval
        {
            get
            {
                return this.val;
            }
            set
            {
                this.val = value;
            }
        }

        public string teststrval
        {
            get
            {
                return this.strVal;
            }
            set
            {
                this.strVal = value;
            }
        }

        public int this[int index]
        {
            get { return 1; }
            set { }
        }

        public int this[string index]
        {
            get { return 1; }
            set { }
        }

        public TimeSpan? NullableMethod(TimeSpan? input)
        {
            return input;
        }

        public int? NullableMethod2(int? input)
        {
            return input;
        }

        public object[] TestLuaFunction(LuaFunction func)
        {
            if (func != null)
            {
                object[]  result = func.Call(1, 2);
                return result;
            }
            return null;
        }

        public int sum(int x, int y)
        {
            return x + y;
        }

        public void setVal(int newVal)
        {
            val = newVal;
        }

        public void setVal(string newVal)
        {
            strVal = newVal;
        }

        public int getVal()
        {
            return val;
        }

        public string getStrVal()
        {
            return strVal;
        }

        public int outVal(out int val)
        {
            val = 5;
            return 3;
        }

        public int outVal(out int val, int val2)
        {
            val = 5;
            return val2;
        }

        public int outVal(int val, ref int val2)
        {
            val2 = val + val2;
            return val;
        }

        public int outValMutiple(int arg, out string arg2, out string arg3)
        {
            arg2 = Guid.NewGuid().ToString();
            arg3 = Guid.NewGuid().ToString();

            return arg;
        }

        public int callDelegate1(TestDelegate1 del)
        {
            return del(2, 3);
        }

        public int callDelegate2(TestDelegate2 del)
        {
            int a = 3;
            int b = del(2, out a);
            return a + b;
        }

        public int callDelegate3(TestDelegate3 del)
        {
            int a = 3;
            del(2, ref a);
            return a;
        }

        public int callDelegate4(TestDelegate4 del)
        {
            return del(2, 3).testval;
        }

        public int callDelegate5(TestDelegate5 del)
        {
            return del(new TestClass(2), new TestClass(3));
        }

        public int callDelegate6(TestDelegate6 del)
        {
            TestClass test = new TestClass();
            int a = del(2, out test);
            return a + test.testval;
        }

        public int callDelegate7(TestDelegate7 del)
        {
            TestClass test = new TestClass(3);
            del(2, ref test);
            return test.testval;
        }

        public int callInterface1(ITest itest)
        {
            return itest.test1(2, 3);
        }

        public int callInterface2(ITest itest)
        {
            int a = 3;
            int b = itest.test2(2, out a);
            return a + b;
        }

        public int callInterface3(ITest itest)
        {
            int a = 3;
            itest.test3(2, ref a);
            //Console.WriteLine(a);
            return a;
        }

        public int callInterface4(ITest itest)
        {
            return itest.test4(2, 3).testval;
        }

        public int callInterface5(ITest itest)
        {
            return itest.test5(new TestClass(2), new TestClass(3));
        }

        public int callInterface6(ITest itest)
        {
            TestClass test = new TestClass();
            int a = itest.test6(2, out test);
            return a + test.testval;
        }

        public int callInterface7(ITest itest)
        {
            TestClass test = new TestClass(3);
            itest.test7(2, ref test);
            return test.testval;
        }

        public int callInterface8(ITest itest)
        {
            itest.intProp = 3;
            return itest.intProp;
        }

        public int callInterface9(ITest itest)
        {
            itest.refProp = new TestClass(3);
            return itest.refProp.testval;
        }

        public void exceptionMethod()
        {
            throw new Exception("exception test");
        }

        public virtual int overridableMethod(int x, int y)
        {
            return x + y;
        }

        public static int callOverridable(TestClass test, int x, int y)
        {
            return test.overridableMethod(x, y);
        }

        int IFoo1.foo()
        {
            return 3;
        }

        public int foo()
        {
            return 5;
        }

        private void _PrivateMethod()
        {
            Console.WriteLine("Private method called");
        }

        public void MethodOverload()
        {
            Console.WriteLine("Method with no params");
        }

        public void MethodOverload(TestClass testClass)
        {
            Console.WriteLine("Method with testclass param");
        }

        public void MethodOverload(int i, int j, int k)
        {
            Console.WriteLine("Overload without out param: " + i + ", " + j + ", " + k);
        }

        public void MethodOverload(int i, int j, out int k)
        {
            k = 5;
            Console.WriteLine("Overload with out param" + i + ", " + j);
        }

        public void Print(object format, params object[] args)
        {
            //just for test,this is not printf implements
            var output = format.ToString() + "\t";
            foreach (var msg in args)
            {
                output += msg.ToString() + "\t";
            }
            Console.WriteLine(output);
        }

        public static int MethodWithParams(int a, params int[] others)
        {
            Console.WriteLine(a);
            int i = 0;
            foreach (int val in others)
            {
                Console.WriteLine(val);
                i++;
            }
            return i;
        }

        public static int MethodWithObjectParams(params object[] others)
        {
            int i = 0;
            foreach (var val in others)
            {
                Console.WriteLine(val);
                i++;
            }
            return i;
        }

        public long MethodWithLong(long param)
        {
            LongValue = param;

            return param;
        }
    }
}
