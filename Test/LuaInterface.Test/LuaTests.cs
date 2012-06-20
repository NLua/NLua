using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Reflection;
using System.Collections.Generic;
using Xunit;
using LuaInterface.Exceptions;
using LuaInterface.Test.Mock;

namespace LuaInterface.Test
{
    public class LuaTests
    {
        /*
        * Tests capturing an exception
        */
        [Fact]
        public void ThrowException()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("luanet.load_assembly('mscorlib')");
                lua.DoString("luanet.load_assembly('LuaInterface.Test')");
                lua.DoString("TestClass=luanet.import_type('LuaInterface.Test.Mock.TestClass')");
                lua.DoString("test=TestClass()");
                lua.DoString("err,errMsg=pcall(test.exceptionMethod,test)");
                bool err = (bool)lua["err"];
                Exception errMsg = (Exception)lua["errMsg"];
                
                Assert.False(err);
                Assert.NotNull(errMsg.InnerException);                
                Assert.Equal("exception test", errMsg.InnerException.Message);
                
            }
        }

        /*
         * Tests capturing an exception
         */
        [Fact]
        public void ThrowUncaughtException()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("luanet.load_assembly('mscorlib')");
                lua.DoString("luanet.load_assembly('LuaInterface.Test')");
                lua.DoString("TestClass=luanet.import_type('LuaInterface.Test.Mock.TestClass')");
                lua.DoString("test=TestClass()");

                try
                {
                    lua.DoString("test:exceptionMethod()");

                    //failed
                    Assert.True(false);
                }
                catch (Exception e)
                {
                    //passed
                    Assert.True(true);
                }
            }
        }


        /*
         * Tests nullable fields
         */
        [Fact]
        public void TestNullable()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("luanet.load_assembly('mscorlib')");
                lua.DoString("luanet.load_assembly('LuaInterface.Test')");
                lua.DoString("TestClass=luanet.import_type('LuaInterface.Test.Mock.TestClass')");
                lua.DoString("test=TestClass()");

                lua.DoString("val=test.NullableBool");
                
                Assert.Null((object)lua["val"]);

                lua.DoString("test.NullableBool = true");
                lua.DoString("val=test.NullableBool");
                Assert.True((bool)lua["val"]);
            }
        }


        /*
         * Tests structure assignment
         */
        [Fact]
        public void TestStructs()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("luanet.load_assembly('LuaInterface.Test')");
                lua.DoString("TestClass=luanet.import_type('LuaInterface.Test.Mock.TestClass')");
                lua.DoString("test=TestClass()");
                lua.DoString("TestStruct=luanet.import_type('LuaInterface.Test.Mock.TestStruct')");

                lua.DoString("struct=TestStruct(2)");
                lua.DoString("test.Struct = struct");
                lua.DoString("val=test.Struct.val");
                Assert.Equal(2.0d, (double)lua["val"]);
            }
        }

        [Fact]
        public void TestMethodOverloads()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("luanet.load_assembly('mscorlib')");
                lua.DoString("luanet.load_assembly('LuaInterface.Test')");
                lua.DoString("TestClass=luanet.import_type('LuaInterface.Test.Mock.TestClass')");
                lua.DoString("test=TestClass()");
                lua.DoString("test:MethodOverload()");
                lua.DoString("test:MethodOverload(test)");
                lua.DoString("test:MethodOverload(1,1,1)");
                lua.DoString("test:MethodOverload(2,2,i)\r\nprint(i)");
            }
        }

        [Fact]
        public void TestDispose()
        {
            System.GC.Collect();
            long startingMem = System.Diagnostics.Process.GetCurrentProcess().WorkingSet64;

            for (int i = 0; i < 10000; i++)
            {
                using (Lua lua = new Lua())
                {
                    _Calc(lua, i);
                }
            }

            //TODO: make this test assert so that it is useful

            Console.WriteLine("Was using " + startingMem / 1024 / 1024 + "MB, now using: " + System.Diagnostics.Process.GetCurrentProcess().WorkingSet64 / 1024 / 1024 + "MB");
        }

        private void _Calc(Lua lua, int i)
        {
            lua.DoString(
                     "sqrt = math.sqrt;" +
                     "sqr = function(x) return math.pow(x,2); end;" +
                     "log = math.log;" +
                     "log10 = math.log10;" +
                     "exp = math.exp;" +
                     "sin = math.sin;" +
                     "cos = math.cos;" +
                     "tan = math.tan;" +
                     "abs = math.abs;"
                     );

            lua.DoString("function calcVP(a,b) return a+b end");

            LuaFunction lf = lua.GetFunction("calcVP");
            Object[] ret = lf.Call(i, 20);
        }

        [Fact]
        public void TestThreading()
        {
            using (Lua lua = new Lua())
            {
                DoWorkClass doWork = new DoWorkClass();
                lua.RegisterFunction("dowork", doWork, typeof(DoWorkClass).GetMethod("DoWork"));

                bool failureDetected = false;
                int completed = 0;
                int iterations = 500;

                for (int i = 0; i < iterations; i++)
                {
                    ThreadPool.QueueUserWorkItem(new WaitCallback(delegate(object o)
                    {
                        try
                        {
                            lua.DoString("dowork()");
                        }
                        catch
                        {
                            failureDetected = true;
                        }
                        completed++;
                    }));
                }

                while (completed < iterations && !failureDetected)
                    Thread.Sleep(50);

                Assert.False(failureDetected);
            }
        }

        [Fact]
        public void TestPrivateMethod()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("luanet.load_assembly('mscorlib')");
                lua.DoString("luanet.load_assembly('LuaInterface.Test')");
                lua.DoString("TestClass=luanet.import_type('LuaInterface.Test.Mock.TestClass')");
                lua.DoString("test=TestClass()");
                
                try
                {
                    lua.DoString("test:_PrivateMethod()");
                }
                catch
                {
                    Assert.True(true);
                    return;
                }

                Assert.True(false);
            }
        }

        /*
         * Tests functions
         */
        [Fact]
        public void TestFunctions()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("luanet.load_assembly('mscorlib')");
                lua.DoString("luanet.load_assembly('LuaInterface.Test')");
                lua.RegisterFunction("p", null, typeof(System.Console).GetMethod("WriteLine", new Type[] { typeof(String) }));

                /// Lua command that works (prints to console)
                lua.DoString("p('Foo')");

                /// Yet this works...
                lua.DoString("string.gsub('some string', '(%w+)', function(s) p(s) end)");

                /// This fails if you don't fix Lua5.1 lstrlib.c/add_value to treat LUA_TUSERDATA the same as LUA_FUNCTION
                lua.DoString("string.gsub('some string', '(%w+)', p)");
            }
        }


        /*
         * Tests making an object from a Lua table and calling one of
         * methods the table overrides.
         */
        [Fact]
        public void LuaTableOverridedMethod()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("luanet.load_assembly('LuaInterface.Test')");
                lua.DoString("TestClass=luanet.import_type('LuaInterface.Test.Mock.TestClass')");
                lua.DoString("test={}");
                lua.DoString("function test:overridableMethod(x,y) return x*y; end");
                lua.DoString("luanet.make_object(test,'LuaInterface.Test.Mock.TestClass')");
                lua.DoString("a=TestClass.callOverridable(test,2,3)");
                int a = (int)lua.GetNumber("a");
                lua.DoString("luanet.free_object(test)");
                Assert.Equal(6, a);
            }
        }


        /*
         * Tests making an object from a Lua table and calling a method
         * the table does not override.
         */
        [Fact]
        public void LuaTableInheritedMethod()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("luanet.load_assembly('LuaInterface.Test')");
                lua.DoString("TestClass=luanet.import_type('LuaInterface.Test.Mock.TestClass')");
                lua.DoString("test={}");
                lua.DoString("function test:overridableMethod(x,y) return x*y; end");
                lua.DoString("luanet.make_object(test,'LuaInterface.Test.Mock.TestClass')");
                lua.DoString("test:setVal(3)");
                lua.DoString("a=test.testval");
                int a = (int)lua.GetNumber("a");
                lua.DoString("luanet.free_object(test)");

                Assert.Equal(3, a);

                //Console.WriteLine("interface returned: "+a);
            }
        }


        /// <summary>
        /// Basic multiply method which expects 2 floats
        /// </summary>
        /// <param name="val"></param>
        /// <param name="val2"></param>
        /// <returns></returns>
        private float _TestException(float val, float val2)
        {
            return val * val2;
        }


        [Fact]
        public void TestEventException()
        {
            using (Lua lua = new Lua())
            {
                //Register a C# function
                MethodInfo testException = this.GetType().GetMethod("_TestException", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Instance, null, new Type[] { typeof(float), typeof(float) }, null);
                lua.RegisterFunction("Multiply", this, testException);

                //create the lua event handler code for the entity
                //includes the bad code!
                lua.DoString("function OnClick(sender, eventArgs)\r\n" +
                              "--Multiply expects 2 floats, but instead receives 2 strings\r\n" +
                              "Multiply(asd, we)\r\n" +
                            "end");

                //create the lua event handler code for the entity
                //good code
                //lua.DoString("function OnClick(sender, eventArgs)\r\n" +
                //              "--Multiply expects 2 floats\r\n" +
                //              "Multiply(2, 50)\r\n" +
                //            "end");

                //Create the event handler script
                lua.DoString("function SubscribeEntity(e)\r\ne.Clicked:Add(OnClick)\r\nend");

                //Create the entity object
                Entity entity = new Entity();

                //Register the entity object with the event handler inside lua
                LuaFunction lf = lua.GetFunction("SubscribeEntity");
                lf.Call(new object[1] { entity });

                try
                {
                    //Cause the event to be fired
                    entity.Click();

                    //failed
                    Assert.True(false);
                }
                catch (LuaException e)
                {
                    //passed
                    Assert.True(true);
                }
            }
        }

        [Fact]
        public void TestExceptionWithChunkOverload()
        {
            using (Lua lua = new Lua())
            {
                try
                {
                    lua.DoString("thiswillthrowanerror", "MyChunk");
                }
                catch (Exception e)
                {
                    Assert.True(e.Message.StartsWith("[string \"MyChunk\"]"));
                }
            }
        }

        [Fact]
        public void TestGenerics()
        {
            //Im not sure support for generic classes is possible to implement, see: http://msdn.microsoft.com/en-us/library/system.reflection.methodinfo.containsgenericparameters.aspx
            //specifically the line that says: "If the ContainsGenericParameters property returns true, the method cannot be invoked"

            //TestClassGeneric<string> genericClass = new TestClassGeneric<string>();

            //lua.RegisterFunction("genericMethod", genericClass, typeof(TestClassGeneric<>).GetMethod("GenericMethod"));
            //lua.RegisterFunction("regularMethod", genericClass, typeof(TestClassGeneric<>).GetMethod("RegularMethod"));

            using (Lua lua = new Lua())
            {
                TestClassWithGenericMethod classWithGenericMethod = new TestClassWithGenericMethod();

                lua.RegisterFunction("genericMethod2", classWithGenericMethod, typeof(TestClassWithGenericMethod).GetMethod("GenericMethod"));

                try
                {
                    lua.DoString("genericMethod2(100)");
                }
                catch { }

                Assert.True(classWithGenericMethod.GenericMethodSuccess);
                Assert.True(classWithGenericMethod.Validate<double>(100)); //note the gotcha: numbers are all being passed to generic methods as doubles                    

                try
                {
                    lua.DoString("luanet.load_assembly('LuaInterface.Test')");
                    lua.DoString("TestClass=luanet.import_type('LuaInterface.Test.Mock.TestClass')");
                    lua.DoString("test=TestClass(56)");
                    lua.DoString("genericMethod2(test)");
                }
                catch { }

                Assert.True(classWithGenericMethod.GenericMethodSuccess);
                Assert.Equal(56, (classWithGenericMethod.PassedValue as TestClass).val);
            }
        }


        [Fact]
        public void RegisterFunctionStressTest()
        {
            const int Count = 200;  // it seems to work with 41

            using (Lua lua = new Lua())
            {
                LuaFunction fc = null;
                MyClass t = new MyClass();

                for (int i = 1; i < Count - 1; ++i)
                {
                    fc = lua.RegisterFunction("func" + i, t, typeof(MyClass).GetMethod("Func1"));
                }
                fc = lua.RegisterFunction("func" + (Count - 1), t, typeof(MyClass).GetMethod("Func1"));

                lua.DoString("print(func1())");
            }
        }

        [Fact]
        public void TestMultipleOutParameters()
        {
            using (Lua lua = new Lua())
            {
                TestClass t1 = new TestClass();
                lua["netobj"] = t1;
                lua.DoString("a,b,c=netobj:outValMutiple(2)");
                int a = (int)lua.GetNumber("a");
                string b = (string)lua.GetString("b");
                string c = (string)lua.GetString("c");
                
                Assert.Equal(2, a);                
                Assert.NotNull(b);
                Assert.NotNull(c);
            }
        }

        [Fact]
        public void TestLoadStringLeak()
        {
            //Test to prevent stack overflow
            //See: http://code.google.com/p/luainterface/issues/detail?id=5

            //number of iterations to test
            int count = 10000;


            using (Lua lua = new Lua())
            {
                for (int i = 0; i < count; i++)
                {
                    lua.LoadString("abc = 'def'", string.Empty);
                }
            }

            //any thrown exceptions cause the test run to fail
        }

        [Fact]
        public void TestLoadFileLeak()
        {
            //Test to prevent stack overflow
            //See: http://code.google.com/p/luainterface/issues/detail?id=5

            //number of iterations to test
            int count = 10000;


            using (Lua lua = new Lua())
            {
                for (int i = 0; i < count; i++)
                {
                    lua.LoadFile(Environment.CurrentDirectory + System.IO.Path.DirectorySeparatorChar + "test.lua");
                }
            }

            //any thrown exceptions cause the test run to fail
        }

        [Fact]
        public void TestRegisterFunction()
        {
            using (Lua lua = new Lua())
            {

                lua.RegisterFunction("func1", null, typeof(TestClass2).GetMethod("func"));
                object[] vals1 = lua.GetFunction("func1").Call(2, 3);

                Assert.Equal(5.0f, Convert.ToSingle(vals1[0]));

                TestClass2 obj = new TestClass2();
                lua.RegisterFunction("func2", obj, typeof(TestClass2).GetMethod("funcInstance"));
                vals1 = lua.GetFunction("func2").Call(2, 3);

                Assert.Equal(5.0f, Convert.ToSingle(vals1[0]));
            }
        }

        /*
		 * Tests if DoString is correctly returning values
		 */
        [Fact]
        public void DoString()
        {
            using (Lua lua = new Lua())
            {
                object[] res = lua.DoString("a=2\nreturn a,3");
                //Console.WriteLine("a="+res[0]+", b="+res[1]);
                Assert.Equal(res[0], 2d);
                Assert.Equal(res[1], 3d);
            }
        }
        /*
         * Tests getting of global numeric variables
         */
        [Fact]
        public void GetGlobalNumber()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("a=2");
                double num = lua.GetNumber("a");
                //Console.WriteLine("a="+num);
                Assert.Equal(num, 2d);
            }
        }
        /*
         * Tests setting of global numeric variables
         */
        [Fact]
        public void SetGlobalNumber()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("a=2");
                lua["a"] = 3;
                double num = lua.GetNumber("a");
                //Console.WriteLine("a="+num);
                Assert.Equal(num, 3d);
            }
        }
        /*
         * Tests getting of numeric variables from tables
         * by specifying variable path
         */
        [Fact]
        public void GetNumberInTable()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("a={b={c=2}}");
                double num = lua.GetNumber("a.b.c");
                //Console.WriteLine("a.b.c="+num);
                Assert.Equal(num, 2d);
            }
        }
        /*
         * Tests setting of numeric variables from tables
         * by specifying variable path
         */
        [Fact]
        public void SetNumberInTable()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("a={b={c=2}}");
                lua["a.b.c"] = 3;
                double num = lua.GetNumber("a.b.c");
                //Console.WriteLine("a.b.c="+num);
                Assert.Equal(num, 3d);
            }
        }
        /*
         * Tests getting of global string variables
         */
        [Fact]
        public void GetGlobalString()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("a=\"test\"");
                string str = lua.GetString("a");
                //Console.WriteLine("a="+str);
                Assert.Equal(str, "test");
            }
        }
        /*
         * Tests setting of global string variables
         */
        [Fact]
        public void SetGlobalString()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("a=\"test\"");
                lua["a"] = "new test";
                string str = lua.GetString("a");
                //Console.WriteLine("a="+str);
                Assert.Equal(str, "new test");
            }
        }
        /*
         * Tests getting of string variables from tables
         * by specifying variable path
         */
        [Fact]
        public void GetStringInTable()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("a={b={c=\"test\"}}");
                string str = lua.GetString("a.b.c");
                //Console.WriteLine("a.b.c="+str);
                Assert.Equal(str, "test");
            }
        }
        /*
         * Tests setting of string variables from tables
         * by specifying variable path
         */
        [Fact]
        public void SetStringInTable()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("a={b={c=\"test\"}}");
                lua["a.b.c"] = "new test";
                string str = lua.GetString("a.b.c");
                //Console.WriteLine("a.b.c="+str);
                Assert.Equal(str, "new test");
            }
        }
        /*
         * Tests getting and setting of global table variables
         */
        [Fact]
        public void GetAndSetTable()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("a={b={c=2}}\nb={c=3}");
                LuaTable tab = lua.GetTable("b");
                lua["a.b"] = tab;
                double num = lua.GetNumber("a.b.c");
                //Console.WriteLine("a.b.c="+num);
                Assert.Equal(num, 3d);
            }
        }
        /*
         * Tests getting of numeric field of a table
         */
        [Fact]
        public void GetTableNumericField1()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("a={b={c=2}}");
                LuaTable tab = lua.GetTable("a.b");
                double num = (double)tab["c"];
                //Console.WriteLine("a.b.c="+num);
                Assert.Equal(num, 2d);
            }
        }
        /*
         * Tests getting of numeric field of a table
         * (the field is inside a subtable)
         */
        [Fact]
        public void GetTableNumericField2()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("a={b={c=2}}");
                LuaTable tab = lua.GetTable("a");
                double num = (double)tab["b.c"];
                //Console.WriteLine("a.b.c="+num);
                Assert.Equal(num, 2d);
            }
        }
        /*
         * Tests setting of numeric field of a table
         */
        [Fact]
        public void SetTableNumericField1()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("a={b={c=2}}");
                LuaTable tab = lua.GetTable("a.b");
                tab["c"] = 3;
                double num = lua.GetNumber("a.b.c");
                //Console.WriteLine("a.b.c="+num);
                Assert.Equal(num, 3d);
            }
        }
        /*
         * Tests setting of numeric field of a table
         * (the field is inside a subtable)
         */
        [Fact]
        public void SetTableNumericField2()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("a={b={c=2}}");
                LuaTable tab = lua.GetTable("a");
                tab["b.c"] = 3;
                double num = lua.GetNumber("a.b.c");
                //Console.WriteLine("a.b.c="+num);
                Assert.Equal(num, 3d);
            }
        }
        /*
         * Tests getting of string field of a table
         */
        [Fact]
        public void GetTableStringField1()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("a={b={c=\"test\"}}");
                LuaTable tab = lua.GetTable("a.b");
                string str = (string)tab["c"];
                //Console.WriteLine("a.b.c="+str);
                Assert.Equal(str, "test");
            }
        }
        /*
         * Tests getting of string field of a table
         * (the field is inside a subtable)
         */
        [Fact]
        public void GetTableStringField2()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("a={b={c=\"test\"}}");
                LuaTable tab = lua.GetTable("a");
                string str = (string)tab["b.c"];
                //Console.WriteLine("a.b.c="+str);
                Assert.Equal(str, "test");
            }
        }
        /*
         * Tests setting of string field of a table
         */
        [Fact]
        public void SetTableStringField1()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("a={b={c=\"test\"}}");
                LuaTable tab = lua.GetTable("a.b");
                tab["c"] = "new test";
                string str = lua.GetString("a.b.c");
                //Console.WriteLine("a.b.c="+str);
                Assert.Equal(str, "new test");
            }
        }
        /*
         * Tests setting of string field of a table
         * (the field is inside a subtable)
         */
        [Fact]
        public void SetTableStringField2()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("a={b={c=\"test\"}}");
                LuaTable tab = lua.GetTable("a");
                tab["b.c"] = "new test";
                string str = lua.GetString("a.b.c");
                //Console.WriteLine("a.b.c="+str);
                Assert.Equal(str, "new test");
            }
        }
        /*
         * Tests calling of a global function with zero arguments
         */
        [Fact]
        public void CallGlobalFunctionNoArgs()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("a=2\nfunction f()\na=3\nend");
                lua.GetFunction("f").Call();
                double num = lua.GetNumber("a");
                //Console.WriteLine("a="+num);
                Assert.Equal(num, 3d);
            }
        }
        /*
         * Tests calling of a global function with one argument
         */
        [Fact]
        public void CallGlobalFunctionOneArg()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("a=2\nfunction f(x)\na=a+x\nend");
                lua.GetFunction("f").Call(1);
                double num = lua.GetNumber("a");
                //Console.WriteLine("a="+num);
                Assert.Equal(num, 3d);
            }
        }
        /*
         * Tests calling of a global function with two arguments
         */
        [Fact]
        public void CallGlobalFunctionTwoArgs()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("a=2\nfunction f(x,y)\na=x+y\nend");
                lua.GetFunction("f").Call(1, 3);
                double num = lua.GetNumber("a");
                //Console.WriteLine("a="+num);
                Assert.Equal(num, 4d);
            }
        }
        /*
         * Tests calling of a global function that returns one value
         */
        [Fact]
        public void CallGlobalFunctionOneReturn()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("function f(x)\nreturn x+2\nend");
                object[] ret = lua.GetFunction("f").Call(3);
                //Console.WriteLine("ret="+ret[0]);
                Assert.Equal(1, ret.Length);
                Assert.Equal(5, (double)ret[0]);
            }
        }
        /*
         * Tests calling of a global function that returns two values
         */
        [Fact]
        public void CallGlobalFunctionTwoReturns()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("function f(x,y)\nreturn x,x+y\nend");
                object[] ret = lua.GetFunction("f").Call(3, 2);
                //Console.WriteLine("ret="+ret[0]+","+ret[1]);
                Assert.Equal(2, ret.Length);
                Assert.Equal(3, (double)ret[0]);
                Assert.Equal(5, (double)ret[1]);
            }
        }
        /*
         * Tests calling of a function inside a table
         */
        [Fact]
        public void CallTableFunctionTwoReturns()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("a={}\nfunction a.f(x,y)\nreturn x,x+y\nend");
                object[] ret = lua.GetFunction("a.f").Call(3, 2);
                //Console.WriteLine("ret="+ret[0]+","+ret[1]);
                Assert.Equal(2, ret.Length);
                Assert.Equal(3, (double)ret[0]);
                Assert.Equal(5, (double)ret[1]);
            }
        }
        /*
         * Tests setting of a global variable to a CLR object value
         */
        [Fact]
        public void SetGlobalObject()
        {
            using (Lua lua = new Lua())
            {
                TestClass t1 = new TestClass();
                t1.testval = 4;
                lua["netobj"] = t1;
                object o = lua["netobj"];
                TestClass t2 = (TestClass)lua["netobj"];
                Assert.Equal(t2.testval, 4);
                Assert.True(t1 == t2);
            }
        }
        ///*
        // * Tests if CLR object is being correctly collected by Lua
        // */
        //[Fact]
        //public void GarbageCollection()
        //{
        //    using (Lua lua = new Lua())
        //    {
        //        TestClass t1 = new TestClass();
        //        t1.testval = 4;
        //        lua["netobj"] = t1;
        //        TestClass t2 = (TestClass)lua["netobj"];
        //        Assert.True(lua[0] != null);
        //        lua.DoString("netobj=nil;collectgarbage();");
        //        Assert.True(lua.translator.objects[0] == null);
        //    }
        //}
        /*
         * Tests setting of a table field to a CLR object value
         */
        [Fact]
        public void SetTableObjectField1()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("a={b={c=\"test\"}}");
                LuaTable tab = lua.GetTable("a.b");
                TestClass t1 = new TestClass();
                t1.testval = 4;
                tab["c"] = t1;
                TestClass t2 = (TestClass)lua["a.b.c"];
                //Console.WriteLine("a.b.c="+t2.testval);
                Assert.Equal(t2.testval, 4);
                Assert.True(t1 == t2);
            }
        }
        /*
         * Tests reading and writing of an object's field
         */
        [Fact]
        public void AccessObjectField()
        {
            using (Lua lua = new Lua())
            {
                TestClass t1 = new TestClass();
                t1.val = 4;
                lua["netobj"] = t1;
                lua.DoString("var=netobj.val");
                double var = (double)lua["var"];
                //Console.WriteLine("value from Lua="+var);
                Assert.Equal(4, var);
                lua.DoString("netobj.val=3");
                Assert.Equal(3, t1.val);
                //Console.WriteLine("new val (from Lua)="+t1.val);
            }
        }
        /*
         * Tests reading and writing of an object's non-indexed
         * property
         */
        [Fact]
        public void AccessObjectProperty()
        {
            using (Lua lua = new Lua())
            {
                TestClass t1 = new TestClass();
                t1.testval = 4;
                lua["netobj"] = t1;
                lua.DoString("var=netobj.testval");
                double var = (double)lua["var"];
                //Console.WriteLine("value from Lua="+var);
                Assert.Equal(4, var);
                lua.DoString("netobj.testval=3");
                Assert.Equal(3, t1.testval);
                //Console.WriteLine("new val (from Lua)="+t1.testval);
            }
        }
        /*
         * Tests calling of an object's method with no overloads
         */
        [Fact]
        public void CallObjectMethod()
        {
            using (Lua lua = new Lua())
            {
                TestClass t1 = new TestClass();
                t1.testval = 4;
                lua["netobj"] = t1;
                lua.DoString("netobj:setVal(3)");
                Assert.Equal(3, t1.testval);
                //Console.WriteLine("new val(from C#)="+t1.testval);
                lua.DoString("val=netobj:getVal()");
                int val = (int)lua.GetNumber("val");
                Assert.Equal(3, val);
                //Console.WriteLine("new val(from Lua)="+val);
            }
        }
        /*
         * Tests calling of an object's method with overloading
         */
        [Fact]
        public void CallObjectMethodByType()
        {
            using (Lua lua = new Lua())
            {
                TestClass t1 = new TestClass();
                lua["netobj"] = t1;
                lua.DoString("netobj:setVal('str')");
                Assert.Equal("str", t1.getStrVal());
                //Console.WriteLine("new val(from C#)="+t1.getStrVal());
            }
        }
        /*
         * Tests calling of an object's method with no overloading
         * and out parameters
         */
        [Fact]
        public void CallObjectMethodOutParam()
        {
            using (Lua lua = new Lua())
            {
                TestClass t1 = new TestClass();
                lua["netobj"] = t1;
                lua.DoString("a,b=netobj:outVal()");
                int a = (int)lua.GetNumber("a");
                int b = (int)lua.GetNumber("b");
                Assert.Equal(3, a);
                Assert.Equal(5, b);
                //Console.WriteLine("function returned (from lua)="+a+","+b);
            }
        }
        /*
         * Tests calling of an object's method with overloading and
         * out params
         */
        [Fact]
        public void CallObjectMethodOverloadedOutParam()
        {
            using (Lua lua = new Lua())
            {
                TestClass t1 = new TestClass();
                lua["netobj"] = t1;
                lua.DoString("a,b=netobj:outVal(2)");
                int a = (int)lua.GetNumber("a");
                int b = (int)lua.GetNumber("b");
                Assert.Equal(2, a);
                Assert.Equal(5, b);
                //Console.WriteLine("function returned (from lua)="+a+","+b);
            }
        }
        /*
         * Tests calling of an object's method with ref params
         */
        [Fact]
        public void CallObjectMethodByRefParam()
        {
            using (Lua lua = new Lua())
            {
                TestClass t1 = new TestClass();
                lua["netobj"] = t1;
                lua.DoString("a,b=netobj:outVal(2,3)");
                int a = (int)lua.GetNumber("a");
                int b = (int)lua.GetNumber("b");
                Assert.Equal(2, a);
                Assert.Equal(5, b);
                //Console.WriteLine("function returned (from lua)="+a+","+b);
            }
        }
        /*
         * Tests calling of two versions of an object's method that have
         * the same name and signature but implement different interfaces
         */
        [Fact]
        public void CallObjectMethodDistinctInterfaces()
        {
            using (Lua lua = new Lua())
            {
                TestClass t1 = new TestClass();
                lua["netobj"] = t1;
                lua.DoString("a=netobj:foo()");
                lua.DoString("b=netobj['LuaInterface.Test.Mock.IFoo1.foo'](netobj)");
                int a = (int)lua.GetNumber("a");
                int b = (int)lua.GetNumber("b");
                Assert.Equal(5, a);
                Assert.Equal(3, b);
                //Console.WriteLine("function returned (from lua)="+a+","+b);
            }
        }
        /*
         * Tests instantiating an object with no-argument constructor
         */
        [Fact]
        public void CreateNetObjectNoArgsCons()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("luanet.load_assembly(\"LuaInterface.Test\")");
                lua.DoString("TestClass=luanet.import_type(\"LuaInterface.Test.Mock.TestClass\")");
                lua.DoString("test=TestClass()");
                lua.DoString("test:setVal(3)");
                object[] res = lua.DoString("return test");
                TestClass test = (TestClass)res[0];
                //Console.WriteLine("returned: "+test.testval);
                Assert.Equal(3, test.testval);
            }
        }
        /*
         * Tests instantiating an object with one-argument constructor
         */
        [Fact]
        public void CreateNetObjectOneArgCons()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("luanet.load_assembly(\"LuaInterface.Test\")");
                lua.DoString("TestClass=luanet.import_type(\"LuaInterface.Test.Mock.TestClass\")");
                lua.DoString("test=TestClass(3)");
                object[] res = lua.DoString("return test");
                TestClass test = (TestClass)res[0];
                //Console.WriteLine("returned: "+test.testval);
                Assert.Equal(3, test.testval);
            }
        }
        /*
         * Tests instantiating an object with overloaded constructor
         */
        [Fact]
        public void CreateNetObjectOverloadedCons()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("luanet.load_assembly(\"LuaInterface.Test\")");
                lua.DoString("TestClass=luanet.import_type(\"LuaInterface.Test.Mock.TestClass\")");
                lua.DoString("test=TestClass('str')");
                object[] res = lua.DoString("return test");
                TestClass test = (TestClass)res[0];
                //Console.WriteLine("returned: "+test.getStrVal());
                Assert.Equal("str", test.getStrVal());
            }
        }
        /*
         * Tests getting item of a CLR array
         */
        [Fact]
        public void ReadArrayField()
        {
            using (Lua lua = new Lua())
            {
                string[] arr = new string[] { "str1", "str2", "str3" };
                lua["netobj"] = arr;
                lua.DoString("val=netobj[1]");
                string val = lua.GetString("val");
                Assert.Equal("str2", val);
                //Console.WriteLine("new val(from array to Lua)="+val);
            }
        }
        /*
         * Tests setting item of a CLR array
         */
        [Fact]
        public void WriteArrayField()
        {
            using (Lua lua = new Lua())
            {
                string[] arr = new string[] { "str1", "str2", "str3" };
                lua["netobj"] = arr;
                lua.DoString("netobj[1]='test'");
                Assert.Equal("test", arr[1]);
                //Console.WriteLine("new val(from Lua to array)="+arr[1]);
            }
        }
        /*
         * Tests creating a new CLR array
         */
        [Fact]
        public void CreateArray()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("luanet.load_assembly(\"LuaInterface.Test\")");
                lua.DoString("TestClass=luanet.import_type(\"LuaInterface.Test.Mock.TestClass\")");
                lua.DoString("arr=TestClass[3]");
                lua.DoString("for i=0,2 do arr[i]=TestClass(i+1) end");
                TestClass[] arr = (TestClass[])lua["arr"];
                Assert.Equal(arr[1].testval, 2);
            }
        }
        /*
         * Tests passing a Lua function to a delegate
         * with value-type arguments
         */
        [Fact]
        public void LuaDelegateValueTypes()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("luanet.load_assembly('LuaInterface.Test')");
                lua.DoString("TestClass=luanet.import_type('LuaInterface.Test.Mock.TestClass')");
                lua.DoString("test=TestClass()");
                lua.DoString("function func(x,y) return x+y; end");
                lua.DoString("test=TestClass()");
                lua.DoString("a=test:callDelegate1(func)");
                int a = (int)lua.GetNumber("a");
                Assert.Equal(5, a);
                //Console.WriteLine("delegate returned: "+a);
            }
        }
        /*
         * Tests passing a Lua function to a delegate
         * with value-type arguments and out params
         */
        [Fact]
        public void LuaDelegateValueTypesOutParam()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("luanet.load_assembly('LuaInterface.Test')");
                lua.DoString("TestClass=luanet.import_type('LuaInterface.Test.Mock.TestClass')");
                lua.DoString("test=TestClass()");
                lua.DoString("function func(x) return x,x*2; end");
                lua.DoString("test=TestClass()");
                lua.DoString("a=test:callDelegate2(func)");
                int a = (int)lua.GetNumber("a");
                Assert.Equal(6, a);
                //Console.WriteLine("delegate returned: "+a);
            }
        }
        /*
         * Tests passing a Lua function to a delegate
         * with value-type arguments and ref params
         */
        [Fact]
        public void LuaDelegateValueTypesByRefParam()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("luanet.load_assembly('LuaInterface.Test')");
                lua.DoString("TestClass=luanet.import_type('LuaInterface.Test.Mock.TestClass')");
                lua.DoString("test=TestClass()");
                lua.DoString("function func(x,y) return x+y; end");
                lua.DoString("test=TestClass()");
                lua.DoString("a=test:callDelegate3(func)");
                int a = (int)lua.GetNumber("a");
                Assert.Equal(5, a);
                //Console.WriteLine("delegate returned: "+a);
            }
        }
        /*
         * Tests passing a Lua function to a delegate
         * with value-type arguments that returns a reference type
         */
        [Fact]
        public void LuaDelegateValueTypesReturnReferenceType()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("luanet.load_assembly('LuaInterface.Test')");
                lua.DoString("TestClass=luanet.import_type('LuaInterface.Test.Mock.TestClass')");
                lua.DoString("test=TestClass()");
                lua.DoString("function func(x,y) return TestClass(x+y); end");
                lua.DoString("test=TestClass()");
                lua.DoString("a=test:callDelegate4(func)");
                int a = (int)lua.GetNumber("a");
                Assert.Equal(5, a);
                //Console.WriteLine("delegate returned: "+a);
            }
        }
        /*
         * Tests passing a Lua function to a delegate
         * with reference type arguments
         */
        [Fact]
        public void LuaDelegateReferenceTypes()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("luanet.load_assembly('LuaInterface.Test')");
                lua.DoString("TestClass=luanet.import_type('LuaInterface.Test.Mock.TestClass')");
                lua.DoString("test=TestClass()");
                lua.DoString("function func(x,y) return x.testval+y.testval; end");
                lua.DoString("test=TestClass()");
                lua.DoString("a=test:callDelegate5(func)");
                int a = (int)lua.GetNumber("a");
                Assert.Equal(5, a);
                //Console.WriteLine("delegate returned: "+a);
            }
        }
        /*
         * Tests passing a Lua function to a delegate
         * with reference type arguments and an out param
         */
        [Fact]
        public void LuaDelegateReferenceTypesOutParam()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("luanet.load_assembly('LuaInterface.Test')");
                lua.DoString("TestClass=luanet.import_type('LuaInterface.Test.Mock.TestClass')");
                lua.DoString("test=TestClass()");
                lua.DoString("function func(x) return x,TestClass(x*2); end");
                lua.DoString("test=TestClass()");
                lua.DoString("a=test:callDelegate6(func)");
                int a = (int)lua.GetNumber("a");
                Assert.Equal(6, a);
                //Console.WriteLine("delegate returned: "+a);
            }
        }
        /*
         * Tests passing a Lua function to a delegate
         * with reference type arguments and a ref param
         */
        [Fact]
        public void LuaDelegateReferenceTypesByRefParam()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("luanet.load_assembly('LuaInterface.Test')");
                lua.DoString("TestClass=luanet.import_type('LuaInterface.Test.Mock.TestClass')");
                lua.DoString("test=TestClass()");
                lua.DoString("function func(x,y) return TestClass(x+y.testval); end");
                lua.DoString("a=test:callDelegate7(func)");
                int a = (int)lua.GetNumber("a");
                Assert.Equal(5, a);
                //Console.WriteLine("delegate returned: "+a);
            }
        }
        /*
         * Tests passing a Lua table as an interface and
         * calling one of its methods with value-type params
         */
        [Fact]
        public void LuaInterfaceValueTypes()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("luanet.load_assembly('LuaInterface.Test')");
                lua.DoString("TestClass=luanet.import_type('LuaInterface.Test.Mock.TestClass')");
                lua.DoString("test=TestClass()");
                lua.DoString("itest={}");
                lua.DoString("function itest:test1(x,y) return x+y; end");
                lua.DoString("test=TestClass()");
                lua.DoString("a=test:callInterface1(itest)");
                int a = (int)lua.GetNumber("a");
                Assert.Equal(5, a);
                //Console.WriteLine("interface returned: "+a);
            }
        }
        /*
         * Tests passing a Lua table as an interface and
         * calling one of its methods with value-type params
         * and an out param
         */
        [Fact]
        public void LuaInterfaceValueTypesOutParam()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("luanet.load_assembly('LuaInterface.Test')");
                lua.DoString("TestClass=luanet.import_type('LuaInterface.Test.Mock.TestClass')");
                lua.DoString("test=TestClass()");
                lua.DoString("itest={}");
                lua.DoString("function itest:test2(x) return x,x*2; end");
                lua.DoString("test=TestClass()");
                lua.DoString("a=test:callInterface2(itest)");
                int a = (int)lua.GetNumber("a");
                Assert.Equal(6, a);
                //Console.WriteLine("interface returned: "+a);
            }
        }
        /*
         * Tests passing a Lua table as an interface and
         * calling one of its methods with value-type params
         * and a ref param
         */
        [Fact]
        public void LuaInterfaceValueTypesByRefParam()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("luanet.load_assembly('LuaInterface.Test')");
                lua.DoString("TestClass=luanet.import_type('LuaInterface.Test.Mock.TestClass')");
                lua.DoString("test=TestClass()");
                lua.DoString("itest={}");
                lua.DoString("function itest:test3(x,y) return x+y; end");
                lua.DoString("test=TestClass()");
                lua.DoString("a=test:callInterface3(itest)");
                int a = (int)lua.GetNumber("a");
                Assert.Equal(5, a);
                //Console.WriteLine("interface returned: "+a);
            }
        }
        /*
         * Tests passing a Lua table as an interface and
         * calling one of its methods with value-type params
         * returning a reference type param
         */
        [Fact]
        public void LuaInterfaceValueTypesReturnReferenceType()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("luanet.load_assembly('LuaInterface.Test')");
                lua.DoString("TestClass=luanet.import_type('LuaInterface.Test.Mock.TestClass')");
                lua.DoString("test=TestClass()");
                lua.DoString("itest={}");
                lua.DoString("function itest:test4(x,y) return TestClass(x+y); end");
                lua.DoString("test=TestClass()");
                lua.DoString("a=test:callInterface4(itest)");
                int a = (int)lua.GetNumber("a");
                Assert.Equal(5, a);
                //Console.WriteLine("interface returned: "+a);
            }
        }
        /*
         * Tests passing a Lua table as an interface and
         * calling one of its methods with reference type params
         */
        [Fact]
        public void LuaInterfaceReferenceTypes()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("luanet.load_assembly('LuaInterface.Test')");
                lua.DoString("TestClass=luanet.import_type('LuaInterface.Test.Mock.TestClass')");
                lua.DoString("test=TestClass()");
                lua.DoString("itest={}");
                lua.DoString("function itest:test5(x,y) return x.testval+y.testval; end");
                lua.DoString("test=TestClass()");
                lua.DoString("a=test:callInterface5(itest)");
                int a = (int)lua.GetNumber("a");
                Assert.Equal(5, a);
                //Console.WriteLine("interface returned: "+a);
            }
        }
        /*
         * Tests passing a Lua table as an interface and
         * calling one of its methods with reference type params
         * and an out param
         */
        [Fact]
        public void LuaInterfaceReferenceTypesOutParam()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("luanet.load_assembly('LuaInterface.Test')");
                lua.DoString("TestClass=luanet.import_type('LuaInterface.Test.Mock.TestClass')");
                lua.DoString("test=TestClass()");
                lua.DoString("itest={}");
                lua.DoString("function itest:test6(x) return x,TestClass(x*2); end");
                lua.DoString("test=TestClass()");
                lua.DoString("a=test:callInterface6(itest)");
                int a = (int)lua.GetNumber("a");
                Assert.Equal(6, a);
                //Console.WriteLine("interface returned: "+a);
            }
        }
        /*
         * Tests passing a Lua table as an interface and
         * calling one of its methods with reference type params
         * and a ref param
         */
        [Fact]
        public void LuaInterfaceReferenceTypesByRefParam()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("luanet.load_assembly('LuaInterface.Test')");
                lua.DoString("TestClass=luanet.import_type('LuaInterface.Test.Mock.TestClass')");
                lua.DoString("test=TestClass()");
                lua.DoString("itest={}");
                lua.DoString("function itest:test7(x,y) return TestClass(x+y.testval); end");
                lua.DoString("a=test:callInterface7(itest)");
                int a = (int)lua.GetNumber("a");
                Assert.Equal(5, a);
                //Console.WriteLine("interface returned: "+a);
            }
        }
        /*
         * Tests passing a Lua table as an interface and
         * accessing one of its value-type properties
         */
        [Fact]
        public void LuaInterfaceValueProperty()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("luanet.load_assembly('LuaInterface.Test')");
                lua.DoString("TestClass=luanet.import_type('LuaInterface.Test.Mock.TestClass')");
                lua.DoString("test=TestClass()");
                lua.DoString("itest={}");
                lua.DoString("function itest:get_intProp() return itest.int_prop; end");
                lua.DoString("function itest:set_intProp(val) itest.int_prop=val; end");
                lua.DoString("a=test:callInterface8(itest)");
                int a = (int)lua.GetNumber("a");
                Assert.Equal(3, a);
                //Console.WriteLine("interface returned: "+a);
            }
        }
        /*
         * Tests passing a Lua table as an interface and
         * accessing one of its reference type properties
         */
        [Fact]
        public void LuaInterfaceReferenceProperty()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("luanet.load_assembly('LuaInterface.Test')");
                lua.DoString("TestClass=luanet.import_type('LuaInterface.Test.Mock.TestClass')");
                lua.DoString("test=TestClass()");
                lua.DoString("itest={}");
                lua.DoString("function itest:get_refProp() return TestClass(itest.int_prop); end");
                lua.DoString("function itest:set_refProp(val) itest.int_prop=val.testval; end");
                lua.DoString("a=test:callInterface9(itest)");
                int a = (int)lua.GetNumber("a");
                Assert.Equal(3, a);
                //Console.WriteLine("interface returned: "+a);
            }
        }


        /*
         * Tests making an object from a Lua table and calling the base
         * class version of one of the methods the table overrides.
         */
        [Fact]
        public void LuaTableBaseMethod()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("luanet.load_assembly('LuaInterface.Test')");
                lua.DoString("TestClass=luanet.import_type('LuaInterface.Test.Mock.TestClass')");
                lua.DoString("test={}");
                lua.DoString("function test:overridableMethod(x,y) return 2*self.base:overridableMethod(x,y); end");
                lua.DoString("luanet.make_object(test,'LuaInterface.Test.Mock.TestClass')");
                lua.DoString("a=TestClass:callOverridable(test,2,3)");
                int a = (int)lua.GetNumber("a");
                lua.DoString("luanet.free_object(test)");
                Assert.Equal(10, a);
                //Console.WriteLine("interface returned: "+a);
            }
        }
        /*
         * Tests getting an object's method by its signature
         * (from object)
         */
        [Fact]
        public void GetMethodBySignatureFromObj()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("luanet.load_assembly('mscorlib')");
                lua.DoString("luanet.load_assembly('LuaInterface.Test')");
                lua.DoString("TestClass=luanet.import_type('LuaInterface.Test.Mock.TestClass')");
                lua.DoString("test=TestClass()");
                lua.DoString("setMethod=luanet.get_method_bysig(test,'setVal','System.String')");
                lua.DoString("setMethod('test')");
                TestClass test = (TestClass)lua["test"];
                Assert.Equal("test", test.getStrVal());
                //Console.WriteLine("interface returned: "+test.getStrVal());
            }
        }
        /*
         * Tests getting an object's method by its signature
         * (from type)
         */
        [Fact]
        public void GetMethodBySignatureFromType()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("luanet.load_assembly('mscorlib')");
                lua.DoString("luanet.load_assembly('LuaInterface.Test')");
                lua.DoString("TestClass=luanet.import_type('LuaInterface.Test.Mock.TestClass')");
                lua.DoString("test=TestClass()");
                lua.DoString("setMethod=luanet.get_method_bysig(TestClass,'setVal','System.String')");
                lua.DoString("setMethod(test,'test')");
                TestClass test = (TestClass)lua["test"];
                Assert.Equal("test", test.getStrVal());
                //Console.WriteLine("interface returned: "+test.getStrVal());
            }
        }
        /*
         * Tests getting a type's method by its signature
         */
        [Fact]
        public void GetStaticMethodBySignature()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("luanet.load_assembly('mscorlib')");
                lua.DoString("luanet.load_assembly('LuaInterface.Test')");
                lua.DoString("TestClass=luanet.import_type('LuaInterface.Test.Mock.TestClass')");
                lua.DoString("make_method=luanet.get_method_bysig(TestClass,'makeFromString','System.String')");
                lua.DoString("test=make_method('test')");
                TestClass test = (TestClass)lua["test"];
                Assert.Equal("test", test.getStrVal());
                //Console.WriteLine("interface returned: "+test.getStrVal());
            }
        }
        /*
         * Tests getting an object's constructor by its signature
         */
        [Fact]
        public void GetConstructorBySignature()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("luanet.load_assembly('mscorlib')");
                lua.DoString("luanet.load_assembly('LuaInterface.Test')");
                lua.DoString("TestClass=luanet.import_type('LuaInterface.Test.Mock.TestClass')");
                lua.DoString("test_cons=luanet.get_constructor_bysig(TestClass,'System.String')");
                lua.DoString("test=test_cons('test')");
                TestClass test = (TestClass)lua["test"];
                Assert.Equal("test", test.getStrVal());
                //Console.WriteLine("interface returned: "+test.getStrVal());
            }
        }

    }
}
