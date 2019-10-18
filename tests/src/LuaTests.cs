using System;
using System.Text;
using System.Reflection;
using System.Threading;
using KeraLua;
using NLua;
using NLua.Exceptions;

using LoadFileTests;
using NLuaTest.TestTypes;



using NUnit.Framework;
using Lua = NLua.Lua;
using LuaFunction = NLua.LuaFunction;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;

// ReSharper disable StringLiteralTypo

namespace NLuaTest
{
    [TestFixture]
    public class LuaTests
    {
        public static readonly char UnicodeChar = '\uE007';
        public static string UnicodeString => Convert.ToString(UnicodeChar);
        public static string UnicodeStringRussian => "Файл";

        /*
        * Tests capturing an exception
        */
        [Test]
        public void ThrowException()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("luanet.load_assembly('mscorlib')");
                lua.DoString("luanet.load_assembly('NLuaTest', 'NLuaTest.TestTypes')");
                lua.DoString("TestClass = luanet.import_type('NLuaTest.TestTypes.TestClass')");
                lua.DoString("test = TestClass()");
                lua.DoString("err,errMsg = pcall(test.exceptionMethod,test)");
                bool err = (bool)lua["err"];

                var errMsg = (Exception)lua["errMsg"];
                Assert.AreEqual(false, err);
                Assert.AreNotEqual(null, errMsg.InnerException);
                Assert.AreEqual("exception test", errMsg.InnerException.Message);
            }
        }

        /*
        * Tests passing a LuaFunction
        */
        [Test]
        public void CallLuaFunction()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("function someFunc(v1,v2) return v1 + v2 end");
                lua["funcObject"] = lua.GetFunction("someFunc");

                //lua.DoString("luanet.load_assembly('mscorlib')");
                lua.DoString("luanet.load_assembly('NLuaTest', 'NLuaTest.TestTypes')");
                lua.DoString("TestClass=luanet.import_type('NLuaTest.TestTypes.TestClass')");
                lua.DoString("b = TestClass():TestLuaFunction(funcObject)[0]");
                Assert.AreEqual(3, lua["b"]);
                lua.DoString("a = TestClass():TestLuaFunction(nil)");
                Assert.AreEqual(null, lua["a"]);
            }
        }

        /*
        * Tests capturing an exception
        */
        [Test]
        public void ThrowUncaughtException()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("luanet.load_assembly('mscorlib')");
                lua.DoString("luanet.load_assembly('NLuaTest', 'NLuaTest.TestTypes')");
                lua.DoString("TestClass=luanet.import_type('NLuaTest.TestTypes.TestClass')");
                lua.DoString("test=TestClass()");

                try
                {
                    lua.DoString("test:exceptionMethod()");
                    //failed
                    Assert.AreEqual(false, true);
                }
                catch (Exception)
                {
                    //passed
                    Assert.AreEqual(true, true);
                }
            }
        }


        /*
        * Tests nullable fields
        */
        [Test]
        public void TestNullable()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("luanet.load_assembly('mscorlib')");
                lua.DoString("luanet.load_assembly('NLuaTest', 'NLuaTest.TestTypes')");
                lua.DoString("TestClass=luanet.import_type('NLuaTest.TestTypes.TestClass')");
                lua.DoString("test=TestClass()");
                lua.DoString("val=test.NullableBool");
                Assert.AreEqual(null, (object)lua["val"]);
                lua.DoString("test.NullableBool = true");
                lua.DoString("val=test.NullableBool");
                Assert.AreEqual(true, (bool)lua["val"]);
            }
        }

        /*
        * Tests structure assignment
        */
        [Test]
        public void TestStructs()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("luanet.load_assembly('NLuaTest', 'NLuaTest.TestTypes')");
                lua.DoString("TestClass=luanet.import_type('NLuaTest.TestTypes.TestClass')");
                lua.DoString("test=TestClass()");
                lua.DoString("TestStruct=luanet.import_type('NLuaTest.TestTypes.TestStruct')");
                lua.DoString("struct=TestStruct(2)");
                lua.DoString("test.Struct = struct");
                lua.DoString("val=test.Struct.val");
                Assert.AreEqual(2.0d, (double)lua["val"]);
            }
        }

        /*
        * Tests structure creation via the default constructor
        */
        [Test]
        public void TestStructDefaultConstructor()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("luanet.load_assembly('NLuaTest', 'NLuaTest.TestTypes')");
                lua.DoString("TestStruct=luanet.import_type('NLuaTest.TestTypes.TestStruct')");
                lua.DoString("struct=TestStruct()");
                Assert.AreEqual(new TestStruct(), (TestStruct)lua["struct"]);
            }
        }

        [Test]
        public void TestStructHashesEqual()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("luanet.load_assembly('NLuaTest', 'NLuaTest.TestTypes')");
                lua.DoString("TestStruct=luanet.import_type('NLuaTest.TestTypes.TestStruct')");
                lua.DoString("struct1=TestStruct(0)");
                lua.DoString("struct2=TestStruct(0)");
                lua.DoString("struct2.val=1");
                Assert.AreEqual(0, (double)lua["struct1.val"]);
            }
        }

        [Test]
        public void TestEnumEqual()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("luanet.load_assembly('NLuaTest', 'NLuaTest.TestTypes')");
                lua.DoString("TestEnum=luanet.import_type('NLuaTest.TestTypes.TestEnum')");
                lua.DoString("enum1=TestEnum.ValueA");
                lua.DoString("enum2=TestEnum.ValueB");
                Assert.AreEqual(true, (bool)lua.DoString("return enum1 ~= enum2")[0]);
                Assert.AreEqual(false, (bool)lua.DoString("return enum1 == enum2")[0]);
            }
        }

        [Test]
        public void TestMethodOverloads()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("luanet.load_assembly('mscorlib')");
                lua.DoString("luanet.load_assembly('NLuaTest', 'NLuaTest.TestTypes')");
                lua.DoString("TestClass=luanet.import_type('NLuaTest.TestTypes.TestClass')");
                lua.DoString("test=TestClass()");
                lua.DoString("test:MethodOverload()");
                lua.DoString("test:MethodOverload(test)");
                lua.DoString("test:MethodOverload(test)");
                lua.DoString("test:MethodOverload(1,1,1)");
                lua.DoString("i = test:MethodOverload(2,2)\r\nprint(i)");
                int i = (int) lua.GetNumber("i");
                Assert.AreEqual(5, i, "#1");
            }
        }

        [Test]
        public void TestDispose()
        {
            GC.Collect();
            long startingMem = System.Diagnostics.Process.GetCurrentProcess().WorkingSet64;

            for (int i = 0; i < 300; i++)
            {
                using (Lua lua = new Lua())
                {
                    _Calc(lua, i);
                }
            }

            long endMem = System.Diagnostics.Process.GetCurrentProcess().WorkingSet64;
            Console.WriteLine("Was using " + startingMem / 1024 / 1024 + "MB, now using: " + endMem  / 1024 / 1024 + "MB");
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
        }

        [Test]
        public void TestThreading()
        {
            using (Lua lua = new Lua())
            {
                object lua_locker = new object();
                DoWorkClass doWork = new DoWorkClass();
                lua.RegisterFunction("dowork", doWork, typeof(DoWorkClass).GetMethod("DoWork"));
                bool failureDetected = false;
                int completed = 0;
                int iterations = 10;

                for (int i = 0; i < iterations; i++)
                {
                    ThreadPool.QueueUserWorkItem(new WaitCallback(delegate (object o)
                    {
                        try
                        {
                            lock (lua_locker)
                            {
                                lua.DoString("dowork()");
                            }
                        }
                        catch (Exception e)
                        {
                            Console.Write(e);
                            failureDetected = true;
                        }

                        completed++;
                    }));
                }

                while (completed < iterations && !failureDetected)
                    Thread.Sleep(50);

                Assert.AreEqual(false, failureDetected);
            }
        }

        [Test]
        public void TestPrivateMethod()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("luanet.load_assembly('mscorlib')");
                lua.DoString("luanet.load_assembly('NLuaTest', 'NLuaTest.TestTypes')");
                lua.DoString("TestClass=luanet.import_type('NLuaTest.TestTypes.TestClass')");
                lua.DoString("test=TestClass()");

                try
                {
                    lua.DoString("test:_PrivateMethod()");
                }
                catch
                {
                    Assert.AreEqual(true, true);
                    return;
                }

                Assert.AreEqual(true, false);
            }
        }

        /*
        * Tests functions
        */
        [Test]
        public void TestFunctions()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("luanet.load_assembly('mscorlib')");
                lua.DoString("luanet.load_assembly('NLuaTest', 'NLuaTest.TestTypes')");
                lua.RegisterFunction("p", null, typeof(Console).GetMethod("WriteLine", new [] { typeof(string) }));
                lua.DoString("p('Foo')");
                // Yet this works...
                lua.DoString("string.gsub('some string', '(%w+)', function(s) p(s) end)");
            }
        }


        /*
        * Tests making an object from a Lua table and calling one of
        * methods the table overrides.
        */
        [Test]
        public void LuaTableOverridedMethod()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("luanet.load_assembly('NLuaTest', 'NLuaTest.TestTypes')");
                lua.DoString("TestClass=luanet.import_type('NLuaTest.TestTypes.TestClass')");
                lua.DoString("test={}");
                lua.DoString("function test:overridableMethod(x,y) return x*y; end");
                lua.DoString("luanet.make_object(test,'NLuaTest.TestTypes.TestClass')");
                lua.DoString("a=TestClass.callOverridable(test,2,3)");
                int a = (int)lua.GetNumber("a");
                lua.DoString("luanet.free_object(test)");
                Assert.AreEqual(6, a);
            }
        }


        /*
        * Tests making an object from a Lua table and calling a method
        * the table does not override.
        */
        [Test]
        public void LuaTableInheritedMethod()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("luanet.load_assembly('NLuaTest', 'NLuaTest.TestTypes')");
                lua.DoString("TestClass=luanet.import_type('NLuaTest.TestTypes.TestClass')");
                lua.DoString("test={}");
                lua.DoString("function test:overridableMethod(x,y) return x*y; end");
                lua.DoString("luanet.make_object(test,'NLuaTest.TestTypes.TestClass')");
                lua.DoString("test:setVal(3)");
                lua.DoString("a=test.testval");
                int a = (int)lua.GetNumber("a");
                lua.DoString("luanet.free_object(test)");
                Assert.AreEqual(3, a);
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

        

        [Test]
        public void TestEventException()
        {
            using (Lua lua = new Lua())
            {
                //Register a C# function
                MethodInfo testException = GetType().GetMethod("_TestException", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Instance, null, new Type[] {
                                typeof(float),
                                typeof(float)
                        }, null);
                lua.RegisterFunction("Multiply", this, testException);
                lua.RegisterLuaDelegateType(typeof(EventHandler<EventArgs>), typeof(LuaEventArgsHandler));
                //create the lua event handler code for the entity
                //includes the bad code!
                lua.DoString("function OnClick(sender, eventArgs)\r\n" +
                    "--Multiply expects 2 floats, but instead receives 2 strings\r\n" +
                    "Multiply(asd, es)\r\n" +
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
                lf.Call(entity);

                try
                {
                    //Cause the event to be fired
                    entity.Click();
                    //failed
                    Assert.AreEqual(true, false);
                }
                catch (LuaException)
                {
                    //passed
                    Assert.AreEqual(true, true);
                }
            }
        }

        [Test]
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
                    Assert.AreEqual(true, e.Message.StartsWith("[string \"MyChunk\"]"));
                }
            }
        }

        [Test]
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

                ////////////////////////////////////////////////////////////////////////////
                /// ////////////////////////////////////////////////////////////////////////
                ///  IMPORTANT: Use generic method with the type you will call or generic methods will fail with iOS
                /// ////////////////////////////////////////////////////////////////////////
                classWithGenericMethod.GenericMethod<double>(99.0);
                classWithGenericMethod.GenericMethod<TestClass>(new TestClass(99));
                ////////////////////////////////////////////////////////////////////////////
                /// ////////////////////////////////////////////////////////////////////////

                lua.RegisterFunction("genericMethod2", classWithGenericMethod, typeof(TestClassWithGenericMethod).GetMethod("GenericMethod"));

                try
                {
                    lua.DoString("genericMethod2(100)");
                }
                catch
                {
                }

                Assert.AreEqual(true, classWithGenericMethod.GenericMethodSuccess);
                Assert.AreEqual(true, classWithGenericMethod.Validate<double>(100)); //note the gotcha: numbers are all being passed to generic methods as doubles

                try
                {
                    lua.DoString("luanet.load_assembly('NLuaTest', 'NLuaTest.TestTypes')");
                    lua.DoString("TestClass=luanet.import_type('NLuaTest.TestTypes.TestClass')");
                    lua.DoString("test=TestClass(56)");
                    lua.DoString("genericMethod2(test)");
                }
                catch
                {
                }

                Assert.AreEqual(true, classWithGenericMethod.GenericMethodSuccess);
                Assert.AreEqual(56, (classWithGenericMethod.PassedValue as TestTypes.TestClass).val);
            }
        }

        [Test]
        public void RegisterFunctionStressTest()
        {
            const int Count = 200;  // it seems to work with 41
            using (Lua lua = new Lua())
            {
                MyClass t = new MyClass();

                for (int i = 1; i < Count - 1; ++i)
                {
                    lua.RegisterFunction("func" + i, t, typeof(MyClass).GetMethod("Func1"));
                }

                lua.RegisterFunction("func" + (Count - 1), t, typeof(MyClass).GetMethod("Func1"));
                lua.DoString("print(func1())");
            }
        }

        [Test]
        public void TestMultipleOutParameters()
        {
            using (Lua lua = new Lua())
            {
                TestTypes.TestClass t1 = new TestTypes.TestClass();
                lua["netobj"] = t1;
                lua.DoString("a,b,c=netobj:outValMutiple(2)");
                int a = (int)lua.GetNumber("a");
                string b = lua.GetString("b");
                string c = lua.GetString("c");

                Assert.AreEqual(2, a);
                Assert.AreNotEqual(null, b);
                Assert.AreNotEqual(null, c);
            }
        }

        [Test]
        public void TestLoadStringLeak()
        {
            //Test to prevent stack overflow
            //See: http://code.google.com/p/nlua/issues/detail?id=5
            //number of iterations to test
            int count = 1000;
            using (Lua lua = new Lua())
            {
                for (int i = 0; i < count; i++)
                {
                    lua.LoadString("abc = 'def'", string.Empty);
                }
            }
            //any thrown exceptions cause the test run to fail
        }



        [Test]
        public void TestLoadFileLeak()
        {
            //Test to prevent stack overflow
            //See: http://code.google.com/p/luainterface/issues/detail?id=5
            //number of iterations to test
            int count = 1000;
            string file = LoadLuaFile.GetScriptsPath("test.lua");

            using (Lua lua = new Lua())
            {
                for (int i = 0; i < count; i++)
                {
                    lua.LoadFile(file);
                }
            }
            //any thrown exceptions cause the test run to fail
        }

        [Test]
        public void TestRegisterFunction()
        {
            using (Lua lua = new Lua())
            {
                lua.RegisterFunction("func1", null, typeof(TestClass2).GetMethod("func"));
                object[] vals1 = lua.GetFunction("func1").Call(2, 3);
                Assert.AreEqual(5.0f, Convert.ToSingle(vals1[0]));
                TestClass2 obj = new TestClass2();
                lua.RegisterFunction("func2", obj, typeof(TestClass2).GetMethod("funcInstance"));
                vals1 = lua.GetFunction("func2").Call(2, 3);
                Assert.AreEqual(5.0f, Convert.ToSingle(vals1[0]));
            }
        }

        /*
         * Tests passing a null object as a parameter to a
         * method that accepts a nullable.
         */
        [Test]
        public void TestNullableParameter()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("luanet.load_assembly('NLuaTest', 'NLuaTest.TestTypes')");
                lua.DoString("TestClass=luanet.import_type('NLuaTest.TestTypes.TestClass')");
                lua.DoString("test=TestClass()");
                lua.DoString("a = test:NullableMethod(nil)");
                Assert.AreEqual(null, lua["a"]);
                lua["timeVal"] = TimeSpan.FromSeconds(5);
                lua.DoString("b = test:NullableMethod(timeVal)");
                Assert.AreEqual(TimeSpan.FromSeconds(5), lua["b"]);
                lua.DoString("d = test:NullableMethod2(2)");
                Assert.AreEqual(2, lua["d"]);
                lua.DoString("c = test:NullableMethod2(nil)");
                Assert.AreEqual(null, lua["c"]);
            }
        }

        /*
        * Tests if DoString is correctly returning values
        */
        [Test]
        public void DoString()
        {
            using (Lua lua = new Lua())
            {
                object[] res = lua.DoString("a=2\nreturn a,3");
                //Console.WriteLine("a="+res[0]+", b="+res[1]);
                Assert.AreEqual(res[0], 2d);
                Assert.AreEqual(res[1], 3d);
            }
        }
        /*
        * Tests getting of global numeric variables
        */
        [Test]
        public void GetGlobalNumber()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("a=2");
                double num = lua.GetNumber("a");
                //Console.WriteLine("a="+num);
                Assert.AreEqual(num, 2d);
            }
        }
        /*
        * Tests setting of global numeric variables
        */
        [Test]
        public void SetGlobalNumber()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("a=2");
                lua["a"] = 3;
                double num = lua.GetNumber("a");

                Assert.AreEqual(num, 3d);
            }
        }
        /*
        * Tests getting of numeric variables from tables
        * by specifying variable path
        */
        [Test]
        public void GetNumberInTable()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("a={b={c=2}}");
                double num = lua.GetNumber("a.b.c");
                //Console.WriteLine("a.b.c="+num);
                Assert.AreEqual(num, 2d);
            }
        }
        /*
        * Tests setting of numeric variables from tables
        * by specifying variable path
        */
        [Test]
        public void SetNumberInTable()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("a={b={c=2}}");
                lua["a.b.c"] = 3;
                double num = lua.GetNumber("a.b.c");
                //Console.WriteLine("a.b.c="+num);
                Assert.AreEqual(num, 3d);
            }
        }
        /*
        * Tests getting of global string variables
        */
        [Test]
        public void GetGlobalString()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("a=\"test\"");
                string str = lua.GetString("a");
                //Console.WriteLine("a="+str);
                Assert.AreEqual(str, "test");
            }
        }
        /*
        * Tests setting of global string variables
        */
        [Test]
        public void SetGlobalString()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("a=\"test\"");
                lua["a"] = "new test";
                string str = lua.GetString("a");
                //Console.WriteLine("a="+str);
                Assert.AreEqual(str, "new test");
            }
        }
        /*
        * Tests getting of string variables from tables
        * by specifying variable path
        */
        [Test]
        public void GetStringInTable()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("a={b={c=\"test\"}}");
                string str = lua.GetString("a.b.c");
                Assert.AreEqual(str, "test");
            }
        }
        /*
        * Tests setting of string variables from tables
        * by specifying variable path
        */
        [Test]
        public void SetStringInTable()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("a={b={c=\"test\"}}");
                lua["a.b.c"] = "new test";
                string str = lua.GetString("a.b.c");
                Assert.AreEqual(str, "new test");
            }
        }
        /*
        * Tests getting and setting of global table variables
        */
        [Test]
        public void GetAndSetTable()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("a={b={c=2}}\nb={c=3}");
                LuaTable tab = lua.GetTable("b");
                lua["a.b"] = tab;
                double num = lua.GetNumber("a.b.c");
                Assert.AreEqual(num, 3d);
            }
        }
        /*
        * Tests getting of numeric field of a table
        */
        [Test]
        public void GetTableNumericField1()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("a={b={c=2}}");
                LuaTable tab = lua.GetTable("a.b");
                long num = (long)tab["c"];
                Assert.AreEqual(2L, num);
            }
        }
        /*
        * Tests getting of numeric field of a table
        * (the field is inside a subtable)
        */
        [Test]
        public void GetTableNumericField2()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("a={b={c=2}}");
                LuaTable tab = lua.GetTable("a");
                long num = (long)tab["b.c"];
                Assert.AreEqual(2L, num);
            }
        }
        /*
        * Tests setting of numeric field of a table
        */
        [Test]
        public void SetTableNumericField1()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("a={b={c=2}}");
                LuaTable tab = lua.GetTable("a.b");
                tab["c"] = 3;
                double num = lua.GetNumber("a.b.c");
                Assert.AreEqual(num, 3d);
            }
        }
        /*
        * Tests setting of numeric field of a table
        * (the field is inside a subtable)
        */
        [Test]
        public void SetTableNumericField2()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("a={b={c=2}}");
                LuaTable tab = lua.GetTable("a");
                tab["b.c"] = 3;
                double num = lua.GetNumber("a.b.c");
                Assert.AreEqual(num, 3d);
            }
        }
        /*
        * Tests getting of string field of a table
        */
        [Test]
        public void GetTableStringField1()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("a={b={c=\"test\"}}");
                LuaTable tab = lua.GetTable("a.b");
                string str = (string)tab["c"];
                Assert.AreEqual(str, "test");
            }
        }
        /*
        * Tests getting of string field of a table
        * (the field is inside a subtable)
        */
        [Test]
        public void GetTableStringField2()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("a={b={c=\"test\"}}");
                LuaTable tab = lua.GetTable("a");
                string str = (string)tab["b.c"];
                Assert.AreEqual(str, "test");
            }
        }
        /*
        * Tests setting of string field of a table
        */
        [Test]
        public void SetTableStringField1()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("a={b={c=\"test\"}}");
                LuaTable tab = lua.GetTable("a.b");
                tab["c"] = "new test";
                string str = lua.GetString("a.b.c");
                Assert.AreEqual(str, "new test");
            }
        }
        /*
        * Tests setting of string field of a table
        * (the field is inside a subtable)
        */
        [Test]
        public void SetTableStringField2()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("a={b={c=\"test\"}}");
                LuaTable tab = lua.GetTable("a");
                tab["b.c"] = "new test";
                string str = lua.GetString("a.b.c");
                Assert.AreEqual(str, "new test");
            }
        }
        /*
        * Tests calling of a global function with zero arguments
        */
        [Test]
        public void CallGlobalFunctionNoArgs()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("a=2\nfunction f()\na=3\nend");
                lua.GetFunction("f").Call();
                int num = lua.GetInteger("a");
                Assert.AreEqual(num, 3);
            }
        }
        /*
        * Tests calling of a global function with one argument
        */
        [Test]
        public void CallGlobalFunctionOneArg()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("a=2\nfunction f(x)\na=a+x\nend");
                lua.GetFunction("f").Call(1);
                double num = lua.GetNumber("a");
                Assert.AreEqual(num, 3d);
            }
        }
        /*
        * Tests calling of a global function with two arguments
        */
        [Test]
        public void CallGlobalFunctionTwoArgs()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("a=2\nfunction f(x,y)\na=x+y\nend");
                lua.GetFunction("f").Call(1, 3);
                double num = lua.GetNumber("a");
                Assert.AreEqual(num, 4d);
            }
        }
        /*
        * Tests calling of a global function that returns one value
        */
        [Test]
        public void CallGlobalFunctionOneReturn()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("function f(x)\nreturn x+2\nend");
                object[] ret = lua.GetFunction("f").Call(3);

                Assert.AreEqual(1, ret.Length);
                Assert.AreEqual(5, ret[0]);
            }
        }
        /*
        * Tests calling of a global function that returns two values
        */
        [Test]
        public void CallGlobalFunctionTwoReturns()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("function f(x,y)\nreturn x,x+y\nend");
                object[] ret = lua.GetFunction("f").Call(3, 2.5);

                Assert.AreEqual(2, ret.Length);
                Assert.AreEqual(3, (long)ret[0]);
                Assert.AreEqual(5.5, (double)ret[1]);
            }
        }
        /*
        * Tests calling of a function inside a table
        */
        [Test]
        public void CallTableFunctionTwoReturns()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("a={}\nfunction a.f(x,y)\nreturn x,x+y\nend");
                object[] ret = lua.GetFunction("a.f").Call(3, 2);

                Assert.AreEqual(2, ret.Length);
                Assert.AreEqual(3, ret[0]);
                Assert.AreEqual(5, ret[1]);
            }
        }
        /*
        * Tests setting of a global variable to a CLR object value
        */
        [Test]
        public void SetGlobalObject()
        {
            using (Lua lua = new Lua())
            {
                var t1 = new TestTypes.TestClass();
                t1.testval = 4;
                lua["netobj"] = t1;
                object o = lua["netobj"];
                Assert.AreEqual(true, o is TestTypes.TestClass);
                var t2 = (TestTypes.TestClass)lua["netobj"];
                Assert.AreEqual(t2.testval, 4);
                Assert.AreEqual(t1, t2);
            }
        }
        ///*
        // * Tests if CLR object is being correctly collected by Lua
        // */
        [Test]
        public void GarbageCollection()
        {
            using (Lua lua = new Lua())
            {
                TestClass t1 = new TestClass();
                lua["netobj"] = t1;
                TestClass t2 = (TestClass)lua["netobj"];
                Assert.IsNotNull(t2);
                lua.DoString("netobj=nil;collectgarbage();");
                t2 = (TestClass)lua["netobj"];
                Assert.IsNull(t2);
            }
        }
        /*
        * Tests setting of a table field to a CLR object value
        */
        [Test]
        public void SetTableObjectField1()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("a={b={c=\"test\"}}");
                LuaTable tab = lua.GetTable("a.b");
                var t1 = new TestTypes.TestClass();
                t1.testval = 4;
                tab["c"] = t1;
                var t2 = (TestTypes.TestClass)lua["a.b.c"];

                Assert.AreEqual(4, t2.testval);
                Assert.AreEqual(t1, t2);
            }
        }
        /*
        * Tests reading and writing of an object's field
        */
        [Test]
        public void AccessObjectField()
        {
            using (Lua lua = new Lua())
            {
                var t1 = new TestTypes.TestClass();
                t1.val = 4;
                lua["netobj"] = t1;
                lua.DoString("var=netobj.val");
                double var = (double)lua["var"];

                Assert.AreEqual(4, var);
                lua.DoString("netobj.val=3");
                Assert.AreEqual(3, t1.val);
            }
        }
        /*
        * Tests reading and writing of an object's non-indexed
        * property
        */
        [Test]
        public void AccessObjectProperty()
        {
            using (Lua lua = new Lua())
            {
                var t1 = new TestTypes.TestClass();
                t1.testval = 4;
                lua["netobj"] = t1;
                lua.DoString("var=netobj.testval");
                double var = (double)lua["var"];

                Assert.AreEqual(4, var);
                lua.DoString("netobj.testval=3");
                Assert.AreEqual(3, t1.testval);
            }
        }

        [Test]
        public void AccessObjectStringProperty()
        {
            using (Lua lua = new Lua())
            {
                var t1 = new TestTypes.TestClass();
                t1.teststrval = "This is a string test";
                lua["netobj"] = t1;
                lua.DoString("var=netobj.teststrval");
                string var = (string)lua["var"];

                Assert.AreEqual("This is a string test", var);
                lua.DoString("netobj.teststrval='Another String'");
                Assert.AreEqual("Another String", t1.teststrval);
            }
        }
        /*
        * Tests calling of an object's method with no overloads
        */
        [Test]
        public void CallObjectMethod()
        {
            using (Lua lua = new Lua())
            {
                TestTypes.TestClass t1 = new TestTypes.TestClass();
                t1.testval = 4;
                lua["netobj"] = t1;
                lua.DoString("netobj:setVal(3)");
                Assert.AreEqual(3, t1.testval);

                lua.DoString("val=netobj:getVal()");
                int val = (int)lua.GetNumber("val");
                Assert.AreEqual(3, val);
            }
        }
        /*
        * Tests calling of an object's method with overloading
        */
        [Test]
        public void CallObjectMethodByType()
        {
            using (Lua lua = new Lua())
            {
                var t1 = new TestTypes.TestClass();
                lua["netobj"] = t1;
                lua.DoString("netobj:setVal('str')");
                Assert.AreEqual("str", t1.getStrVal());
            }
        }
        /*
        * Tests calling of an object's method with no overloading
        * and out parameters
        */
        [Test]
        public void CallObjectMethodOutParam()
        {
            using (Lua lua = new Lua())
            {
                TestTypes.TestClass t1 = new TestTypes.TestClass();
                lua["netobj"] = t1;
                lua.DoString("a,b=netobj:outVal()");
                int a = (int)lua.GetNumber("a");
                int b = (int)lua.GetNumber("b");

                Assert.AreEqual(3, a);
                Assert.AreEqual(5, b);
            }
        }
        /*
        * Tests calling of an object's method with overloading and
        * out params
        */
        [Test]
        public void CallObjectMethodOverloadedOutParam()
        {
            using (Lua lua = new Lua())
            {
                var t1 = new TestTypes.TestClass();
                lua["netobj"] = t1;
                lua.DoString("a,b=netobj:outVal(2)");
                int a = (int)lua.GetNumber("a");
                int b = (int)lua.GetNumber("b");
                Assert.AreEqual(2, a);
                Assert.AreEqual(5, b);
            }
        }
        /*
        * Tests calling of an object's method with ref params
        */
        [Test]
        public void CallObjectMethodByRefParam()
        {
            using (Lua lua = new Lua())
            {
                var t1 = new TestTypes.TestClass();
                lua["netobj"] = t1;
                lua.DoString("a,b=netobj:outVal(2,3)");
                int a = (int)lua.GetNumber("a");
                int b = (int)lua.GetNumber("b");

                Assert.AreEqual(2, a);
                Assert.AreEqual(5, b);
            }
        }
        /*
        * Tests calling of two versions of an object's method that have
        * the same name and signature but implement different interfaces
        */
        [Test]
        public void CallObjectMethodDistinctInterfaces()
        {
            using (Lua lua = new Lua())
            {
                var t1 = new TestClass();
                lua["netobj"] = t1;
                lua.DoString("a=netobj:foo()");
                lua.DoString("b=netobj['NLuaTest.TestTypes.IFoo1.foo']()");
                int a = (int)lua.GetNumber("a");
                int b = (int)lua.GetNumber("b");

                Assert.AreEqual(5, a, "#1");
                Assert.AreEqual(5, t1.foo(), "#1.1");
                IFoo1 foo1 = t1;
                Assert.AreEqual(3, b, "#2");
                Assert.AreEqual(3, foo1.foo(), "#2.1");
            }
        }
        /*
        * Tests instantiating an object with no-argument constructor
        */
        [Test]
        public void CreateNetObjectNoArgsCons()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("luanet.load_assembly(\"NLuaTest\")");
                lua.DoString("TestClass=luanet.import_type(\"NLuaTest.TestTypes.TestClass\")");
                lua.DoString("test=TestClass()");
                lua.DoString("test:setVal(3)");
                object[] res = lua.DoString("return test");
                var test = (TestTypes.TestClass)res[0];

                Assert.AreEqual(3, test.testval);
            }
        }
        /*
        * Tests instantiating an object with one-argument constructor
        */
        [Test]
        public void CreateNetObjectOneArgCons()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("luanet.load_assembly(\"NLuaTest\")");
                lua.DoString("TestClass=luanet.import_type(\"NLuaTest.TestTypes.TestClass\")");
                lua.DoString("test=TestClass(3)");
                object[] res = lua.DoString("return test");
                var test = (TestTypes.TestClass)res[0];

                Assert.AreEqual(3, test.testval);
            }
        }
        /*
        * Tests instantiating an object with overloaded constructor
        */
        [Test]
        public void CreateNetObjectOverloadedCons()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("luanet.load_assembly(\"NLuaTest\")");
                lua.DoString("TestClass=luanet.import_type(\"NLuaTest.TestTypes.TestClass\")");
                lua.DoString("test=TestClass('str')");
                object[] res = lua.DoString("return test");
                var test = (TestTypes.TestClass)res[0];

                Assert.AreEqual("str", test.getStrVal());
            }
        }
        /*
        * Tests getting item of a CLR array
        */
        [Test]
        public void ReadArrayField()
        {
            using (Lua lua = new Lua())
            {
                string[] arr = { "str1", "str2", "str3" };
                lua["netobj"] = arr;
                lua.DoString("val=netobj[1]");
                string val = lua.GetString("val");

                Assert.AreEqual("str2", val);
            }
        }
        /*G
        * Tests setting item of a CLR array
        */
        [Test]
        public void WriteArrayField()
        {
            using (Lua lua = new Lua())
            {
                string[] arr = { "str1", "str2", "str3" };
                lua["netobj"] = arr;
                lua.DoString("netobj[1]='test'");
                Assert.AreEqual("test", arr[1]);
            }
        }
        /*
        * Tests creating a new CLR array
        */
        [Test]
        public void CreateArray()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("luanet.load_assembly(\"NLuaTest\")");
                lua.DoString("TestClass=luanet.import_type(\"NLuaTest.TestTypes.TestClass\")");
                lua.DoString("arr=TestClass[3]");
                lua.DoString("for i=0,2 do arr[i]=TestClass(i+1) end");
                TestTypes.TestClass[] arr = (TestTypes.TestClass[])lua["arr"];
                Assert.AreEqual(arr[1].testval, 2);
            }
        }
        /*
        * Tests passing a Lua function to a delegate
        * with value-type arguments
        */
        [Test]
        public void LuaDelegateValueTypes()
        {
            using (Lua lua = new Lua())
            {
                lua.RegisterLuaDelegateType(typeof(TestDelegate1), typeof(LuaTestDelegate1Handler));
                lua.DoString("luanet.load_assembly('NLuaTest', 'NLuaTest.TestTypes')");
                lua.DoString("TestClass=luanet.import_type('NLuaTest.TestTypes.TestClass')");
                lua.DoString("test=TestClass()");
                lua.DoString("function func(x,y) return x+y; end");
                lua.DoString("test=TestClass()");
                lua.DoString("a=test:callDelegate1(func)");
                int a = (int)lua.GetNumber("a");

                Assert.AreEqual(5, a);
            }
        }
        /*
        * Tests passing a Lua function to a delegate
        * with value-type arguments and out params
        */
        [Test]
        public void LuaDelegateValueTypesOutParam()
        {
            using (Lua lua = new Lua())
            {
                lua.RegisterLuaDelegateType(typeof(TestDelegate2), typeof(LuaTestDelegate2Handler));
                lua.DoString("luanet.load_assembly('NLuaTest', 'NLuaTest.TestTypes')");
                lua.DoString("TestClass=luanet.import_type('NLuaTest.TestTypes.TestClass')");
                lua.DoString("test=TestClass()");
                lua.DoString("function func(x) return x,x*2; end");
                lua.DoString("test=TestClass()");
                lua.DoString("a=test:callDelegate2(func)");
                int a = (int)lua.GetNumber("a");

                Assert.AreEqual(6, a);
            }
        }
        /*
        * Tests passing a Lua function to a delegate
        * with value-type arguments and ref params
        */
        [Test]
        public void LuaDelegateValueTypesByRefParam()
        {
            using (Lua lua = new Lua())
            {
                lua.RegisterLuaDelegateType(typeof(TestDelegate3), typeof(LuaTestDelegate3Handler));
                lua.DoString("luanet.load_assembly('NLuaTest', 'NLuaTest.TestTypes')");
                lua.DoString("TestClass=luanet.import_type('NLuaTest.TestTypes.TestClass')");
                lua.DoString("test=TestClass()");
                lua.DoString("function func(x,y) return x+y; end");
                lua.DoString("test=TestClass()");
                lua.DoString("a=test:callDelegate3(func)");
                int a = (int)lua.GetNumber("a");

                Assert.AreEqual(5, a);
            }
        }
        /*
        * Tests passing a Lua function to a delegate
        * with value-type arguments that returns a reference type
        */
        [Test]
        public void LuaDelegateValueTypesReturnReferenceType()
        {
            using (Lua lua = new Lua())
            {
                lua.RegisterLuaDelegateType(typeof(TestDelegate4), typeof(LuaTestDelegate4Handler));
                lua.DoString("luanet.load_assembly('NLuaTest', 'NLuaTest.TestTypes')");
                lua.DoString("TestClass=luanet.import_type('NLuaTest.TestTypes.TestClass')");
                lua.DoString("test=TestClass()");
                lua.DoString("function func(x,y) return TestClass(x+y); end");
                lua.DoString("test=TestClass()");
                lua.DoString("a=test:callDelegate4(func)");
                int a = (int)lua.GetNumber("a");

                Assert.AreEqual(5, a);
            }
        }
        /*
        * Tests passing a Lua function to a delegate
        * with reference type arguments
        */
        [Test]
        public void LuaDelegateReferenceTypes()
        {
            using (Lua lua = new Lua())
            {
                lua.RegisterLuaDelegateType(typeof(TestDelegate5), typeof(LuaTestDelegate5Handler));
                lua.DoString("luanet.load_assembly('NLuaTest', 'NLuaTest.TestTypes')");
                lua.DoString("TestClass=luanet.import_type('NLuaTest.TestTypes.TestClass')");
                lua.DoString("test=TestClass()");
                lua.DoString("function func(x,y) return x.testval+y.testval; end");
                lua.DoString("a=test:callDelegate5(func)");
                int a = (int)lua.GetNumber("a");

                Assert.AreEqual(5, a);
            }
        }
        /*
        * Tests passing a Lua function to a delegate
        * with reference type arguments and an out param
        */
        [Test]
        public void LuaDelegateReferenceTypesOutParam()
        {
            using (Lua lua = new Lua())
            {
                lua.RegisterLuaDelegateType(typeof(TestDelegate6), typeof(LuaTestDelegate6Handler));
                lua.DoString("luanet.load_assembly('NLuaTest', 'NLuaTest.TestTypes')");
                lua.DoString("TestClass=luanet.import_type('NLuaTest.TestTypes.TestClass')");
                lua.DoString("test=TestClass()");
                lua.DoString("function func(x) return x,TestClass(x*2); end");
                lua.DoString("test=TestClass()");
                lua.DoString("a=test:callDelegate6(func)");
                int a = (int)lua.GetNumber("a");

                Assert.AreEqual(6, a);
            }
        }
        /*
        * Tests passing a Lua function to a delegate
        * with reference type arguments and a ref param
        */
        [Test]
        public void LuaDelegateReferenceTypesByRefParam()
        {
            using (Lua lua = new Lua())
            {
                lua.RegisterLuaDelegateType(typeof(TestDelegate7), typeof(LuaTestDelegate7Handler));
                lua.DoString("luanet.load_assembly('NLuaTest', 'NLuaTest.TestTypes')");
                lua.DoString("TestClass=luanet.import_type('NLuaTest.TestTypes.TestClass')");
                lua.DoString("test=TestClass()");
                lua.DoString("function func(x,y) return TestClass(x+y.testval); end");
                lua.DoString("a=test:callDelegate7(func)");
                int a = (int)lua.GetNumber("a");

                Assert.AreEqual(5, a);
            }
        }


        /*
        * Tests passing a Lua table as an interface and
        * calling one of its methods with value-type params
        */
        [Test]
        public void NLuaAAValueTypes()
        {
            using (Lua lua = new Lua())
            {
                lua.RegisterLuaClassType(typeof(ITest), typeof(LuaITestClassHandler));
                lua.DoString("luanet.load_assembly('NLuaTest', 'NLuaTest.TestTypes')");
                lua.DoString("TestClass=luanet.import_type('NLuaTest.TestTypes.TestClass')");
                lua.DoString("test=TestClass()");
                lua.DoString("itest={}");
                lua.DoString("function itest:test1(x,y) return x+y; end");
                lua.DoString("test=TestClass()");
                lua.DoString("a=test:callInterface1(itest)");
                int a = (int)lua.GetNumber("a");

                Assert.AreEqual(5, a);
            }
        }
        /*
        * Tests passing a Lua table as an interface and
        * calling one of its methods with value-type params
        * and an out param
        */
        [Test]
        public void NLuaValueTypesOutParam()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("luanet.load_assembly('NLuaTest', 'NLuaTest.TestTypes')");
                lua.DoString("TestClass=luanet.import_type('NLuaTest.TestTypes.TestClass')");
                lua.DoString("test=TestClass()");
                lua.DoString("itest={}");
                lua.DoString("function itest:test2(x) return x,x*2; end");
                lua.DoString("test=TestClass()");
                lua.DoString("a=test:callInterface2(itest)");
                int a = (int)lua.GetNumber("a");

                Assert.AreEqual(6, a);
            }
        }
        /*
        * Tests passing a Lua table as an interface and
        * calling one of its methods with value-type params
        * and a ref param
        */
        [Test]
        public void NLuaValueTypesByRefParam()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("luanet.load_assembly('NLuaTest', 'NLuaTest.TestTypes')");
                lua.DoString("TestClass=luanet.import_type('NLuaTest.TestTypes.TestClass')");
                lua.DoString("test=TestClass()");
                lua.DoString("itest={}");
                lua.DoString("function itest:test3(x,y) return x+y; end");
                lua.DoString("test=TestClass()");
                lua.DoString("a=test:callInterface3(itest)");
                int a = (int)lua.GetNumber("a");

                Assert.AreEqual(5, a);
            }
        }
        /*
        * Tests passing a Lua table as an interface and
        * calling one of its methods with value-type params
        * returning a reference type param
        */
        [Test]
        public void NLuaValueTypesReturnReferenceType()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("luanet.load_assembly('NLuaTest', 'NLuaTest.TestTypes')");
                lua.DoString("TestClass=luanet.import_type('NLuaTest.TestTypes.TestClass')");
                lua.DoString("test=TestClass()");
                lua.DoString("itest={}");
                lua.DoString("function itest:test4(x,y) return TestClass(x+y); end");
                lua.DoString("test=TestClass()");
                lua.DoString("a=test:callInterface4(itest)");
                int a = (int)lua.GetNumber("a");

                Assert.AreEqual(5, a);
            }
        }
        /*
        * Tests passing a Lua table as an interface and
        * calling one of its methods with reference type params
        */
        [Test]
        public void NLuaReferenceTypes()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("luanet.load_assembly('NLuaTest', 'NLuaTest.TestTypes')");
                lua.DoString("TestClass=luanet.import_type('NLuaTest.TestTypes.TestClass')");
                lua.DoString("test=TestClass()");
                lua.DoString("itest={}");
                lua.DoString("function itest:test5(x,y) return x.testval+y.testval; end");
                lua.DoString("test=TestClass()");
                lua.DoString("a=test:callInterface5(itest)");
                int a = (int)lua.GetNumber("a");

                Assert.AreEqual(5, a);
            }
        }
        /*
        * Tests passing a Lua table as an interface and
        * calling one of its methods with reference type params
        * and an out param
        */
        [Test]
        public void NLuaReferenceTypesOutParam()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("luanet.load_assembly('NLuaTest', 'NLuaTest.TestTypes')");
                lua.DoString("TestClass=luanet.import_type('NLuaTest.TestTypes.TestClass')");
                lua.DoString("test=TestClass()");
                lua.DoString("itest={}");
                lua.DoString("function itest:test6(x) return x,TestClass(x*2); end");
                lua.DoString("test=TestClass()");
                lua.DoString("a=test:callInterface6(itest)");
                int a = (int)lua.GetNumber("a");

                Assert.AreEqual(6, a);
            }
        }
        /*
        * Tests passing a Lua table as an interface and
        * calling one of its methods with reference type params
        * and a ref param
        */
        [Test]
        public void NLuaReferenceTypesByRefParam()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("luanet.load_assembly('NLuaTest', 'NLuaTest.TestTypes')");
                lua.DoString("TestClass=luanet.import_type('NLuaTest.TestTypes.TestClass')");
                lua.DoString("test=TestClass()");
                lua.DoString("itest={}");
                lua.DoString("function itest:test7(x,y) return TestClass(x+y.testval); end");
                lua.DoString("a=test:callInterface7(itest)");
                int a = (int)lua.GetNumber("a");
                Assert.AreEqual(5, a);
                //Console.WriteLine("interface returned: "+a);
            }
        }


       /*
        * Tests passing a Lua table as an interface and
        * accessing one of its value-type properties
        */
        [Test]
        public void NLuaValueProperty()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("luanet.load_assembly('NLuaTest', 'NLuaTest.TestTypes')");
                lua.DoString("TestClass=luanet.import_type('NLuaTest.TestTypes.TestClass')");
                lua.DoString("test=TestClass()");
                lua.DoString("itest={}");
                lua.DoString("function itest:get_intProp() return itest.int_prop; end");
                lua.DoString("function itest:set_intProp(val) itest.int_prop=val; end");
                lua.DoString("a=test:callInterface8(itest)");
                int a = (int)lua.GetNumber("a");

                Assert.AreEqual(3, a);
            }
        }
        /*
        * Tests passing a Lua table as an interface and
        * accessing one of its reference type properties
        */
        [Test]
        public void NLuaReferenceProperty()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("luanet.load_assembly('NLuaTest', 'NLuaTest.TestTypes')");
                lua.DoString("TestClass=luanet.import_type('NLuaTest.TestTypes.TestClass')");
                lua.DoString("test=TestClass()");
                lua.DoString("itest={}");
                lua.DoString("function itest:get_refProp() return TestClass(itest.int_prop); end");
                lua.DoString("function itest:set_refProp(val) itest.int_prop=val.testval; end");
                lua.DoString("a=test:callInterface9(itest)");
                int a = (int)lua.GetNumber("a");

                Assert.AreEqual(3, a);
            }
        }


        /*
        * Tests making an object from a Lua table and calling the base
        * class version of one of the methods the table overrides.
        */
        [Test]
        public void LuaTableBaseMethod()
        {
            using (Lua lua = new Lua())
            {
                lua.RegisterLuaClassType(typeof(TestTypes.TestClass), typeof(LuaTestClassHandler));
                lua.DoString("luanet.load_assembly('NLuaTest', 'NLuaTest.TestTypes')");
                lua.DoString("TestClass=luanet.import_type('NLuaTest.TestTypes.TestClass')");
                lua.DoString("test={}");
                lua.DoString("function test:overridableMethod(x,y) print(self[base]); return 6 end");
                lua.DoString("luanet.make_object(test,'NLuaTest.TestTypes.TestClass')");
                lua.DoString("a=TestClass.callOverridable(test,2,3)");
                int a = (int)lua.GetNumber("a");
                lua.DoString("luanet.free_object(test)");
                Assert.AreEqual(6, a);
                //                 lua.DoString("luanet.load_assembly('NLuaTest', 'NLuaTest.TestTypes')");
                //                 lua.DoString("TestClass=luanet.import_type('NLuaTest.TestTypes.TestClass')");
                //                 lua.DoString("test={}");
                //
                //                 lua.DoString("luanet.make_object(test,'NLuaTest.TestTypes.TestClass')");
                //                              lua.DoString ("function test.overridableMethod(test,x,y) return 2*test.base.overridableMethod(test,x,y); end");
                //                 lua.DoString("a=TestClass.callOverridable(test,2,3)");
                //                 int a = (int)lua.GetNumber("a");
                //                 lua.DoString("luanet.free_object(test)");
                //                 Assert.AreEqual(10, a);
                //Console.WriteLine("interface returned: "+a);
            }
        }
        /*
        * Tests getting an object's method by its signature
        * (from object)
        */
        [Test]
        public void GetMethodBySignatureFromObj()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("luanet.load_assembly('mscorlib')");
                lua.DoString("luanet.load_assembly('NLuaTest', 'NLuaTest.TestTypes')");
                lua.DoString("TestClass=luanet.import_type('NLuaTest.TestTypes.TestClass')");
                lua.DoString("test=TestClass()");
                lua.DoString("setMethod=luanet.get_method_bysig(test,'setVal','System.String')");
                lua.DoString("setMethod('test')");
                TestTypes.TestClass test = (TestTypes.TestClass)lua["test"];

                Assert.AreEqual("test", test.getStrVal());
            }
        }
        /*
        * Tests getting an object's method by its signature
        * (from type)
        */
        [Test]
        public void GetMethodBySignatureFromType()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("luanet.load_assembly('mscorlib')");
                lua.DoString("luanet.load_assembly('NLuaTest', 'NLuaTest.TestTypes')");
                lua.DoString("TestClass=luanet.import_type('NLuaTest.TestTypes.TestClass')");
                lua.DoString("test=TestClass()");
                lua.DoString("setMethod=luanet.get_method_bysig(TestClass,'setVal','System.String')");
                lua.DoString("setMethod(test,'test')");
                var test = (TestTypes.TestClass)lua["test"];

                Assert.AreEqual("test", test.getStrVal());
            }
        }
        /*
        * Tests getting a type's method by its signature
        */
        [Test]
        public void GetStaticMethodBySignature()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("luanet.load_assembly('mscorlib')");
                lua.DoString("luanet.load_assembly('NLuaTest', 'NLuaTest.TestTypes')");
                lua.DoString("TestClass=luanet.import_type('NLuaTest.TestTypes.TestClass')");
                lua.DoString("make_method=luanet.get_method_bysig(TestClass,'makeFromString','System.String')");
                lua.DoString("test=make_method('test')");
                TestTypes.TestClass test = (TestTypes.TestClass)lua["test"];

                Assert.AreEqual("test", test.getStrVal());
            }
        }
        /*
        * Tests getting an object's constructor by its signature
        */
        [Test]
        public void GetConstructorBySignature()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("luanet.load_assembly('mscorlib')");
                lua.DoString("luanet.load_assembly('NLuaTest', 'NLuaTest.TestTypes')");
                lua.DoString("TestClass=luanet.import_type('NLuaTest.TestTypes.TestClass')");
                lua.DoString("test_cons=luanet.get_constructor_bysig(TestClass,'System.String')");
                lua.DoString("test=test_cons('test')");
                var test = (TestClass)lua["test"];

                Assert.AreEqual("test", test.getStrVal());
            }
        }

        [Test]
        public void TestVarargs()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("luanet.load_assembly('mscorlib')");
                lua.DoString("luanet.load_assembly('NLuaTest', 'NLuaTest.TestTypes')");
                lua.DoString("TestClass=luanet.import_type('NLuaTest.TestTypes.TestClass')");
                lua.DoString("test=TestClass()");
                lua.DoString("test:Print('this will pass')");
                lua.DoString("test:Print('this will ','fail')");
            }
        }

        [Test]
        public void TestCtype()
        {
            using (Lua lua = new Lua())
            {
                lua.LoadCLRPackage();
                lua.DoString("import'System'");
                var x = lua.DoString("return luanet.ctype(String)")[0];
                Assert.AreEqual(x, typeof(String), "#1 String ctype test");
            }
        }

        [Test]
        public void TestPrintChars()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString(@"print(""waüäq?=()[&]ß"")");
                Assert.IsTrue(true);
            }
        }

        [Test]
        public void TestUnicodeChars()
        {
            using (Lua lua = new Lua())
            {
                lua.State.Encoding = Encoding.UTF8;
                
                lua.LoadCLRPackage();
                lua.DoString("import('NLuaTest')");
                lua.DoString("res = LuaTests.UnicodeString");
                string res = (string)lua["res"];

                Assert.AreEqual(LuaTests.UnicodeString, res);
            }
        }

        [Test]
        public void TestUnicodeCharsInDoString()
        {
            using (Lua lua = new Lua())
            {
                lua.State.Encoding = Encoding.UTF8;

                lua.DoString("res = 'Файл'");
                string res = (string)lua["res"];

                Assert.AreEqual(LuaTests.UnicodeStringRussian, res);
            }
        }

        [Test]
        public void TestCoroutine()
        {
            using (Lua lua = new Lua())
            {
                lua.LoadCLRPackage();
                lua.RegisterFunction("func1", null, typeof(TestClass2).GetMethod("func"));
                lua.DoString(@"function yielder() 
                                a=1; 
                                coroutine.yield();
                                func1(3,2);
                                coroutine.yield();
                                a=2;
                                coroutine.yield();
                             end
                             co_routine = coroutine.create(yielder);
                             while coroutine.resume(co_routine) do end;");

                double num = lua.GetNumber("a");
                Assert.AreEqual(num, 2d);
            }
        }

        [Test]
        public void TestDebugHook()
        {
            int[] lines = { 1, 2, 1, 3 };
            int line = 0;

            using (var lua = new Lua())
            {
                lua.DebugHook += (sender, args) =>
                {
                    Assert.AreEqual(args.LuaDebug.CurrentLine, lines[line]);
                    line++;
                };
                lua.SetDebugHook(KeraLua.LuaHookMask.Line, 0);

                lua.DoString(@"function testing_hooks() return 10 end
                            val = testing_hooks() 
                            val = val + 1");
            }
        }

        [Test]
        public void TestKeyWithDots()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString(@"g_dot = {} 
                             g_dot['key.with.dot'] = 42");

                Assert.AreEqual(42, (int)(double)lua["g_dot.key\\.with\\.dot"]);
            }
        }

        [Test]
        public void TestOperatorAdd()
        {
            using (Lua lua = new Lua())
            {
                var a = new System.Numerics.Complex(10, 0);
                var b = new System.Numerics.Complex(0, 3);
                var x = a + b;

                lua["a"] = a;
                lua["b"] = b;
                var res = lua.DoString(@"return a + b")[0];
                Assert.AreEqual(x, res);
            }
        }

        [Test]
        public void TestOperatorMinus()
        {
            using (Lua lua = new Lua())
            {
                var a = new System.Numerics.Complex(10, 0);
                var b = new System.Numerics.Complex(0, 3);
                var x = a - b;

                lua["a"] = a;
                lua["b"] = b;
                var res = lua.DoString(@"return a - b")[0];
                Assert.AreEqual(x, res);
            }
        }

        [Test]
        public void TestOperatorMultiply()
        {
            using (Lua lua = new Lua())
            {
                var a = new System.Numerics.Complex(10, 0);
                var b = new System.Numerics.Complex(0, 3);
                var x = a * b;

                lua["a"] = a;
                lua["b"] = b;
                var res = lua.DoString(@"return a * b")[0];
                Assert.AreEqual(x, res);
            }
        }

        [Test]
        public void TestOperatorEqual()
        {
            using (Lua lua = new Lua())
            {
                var a = new System.Numerics.Complex(10, 0);
                var b = new System.Numerics.Complex(0, 3);
                var x = a == b;

                lua["a"] = a;
                lua["b"] = b;
                var res = lua.DoString(@"return a == b")[0];
                Assert.AreEqual(x, res);
            }
        }

        [Test]
        public void TestOperatorNotEqual()
        {
            using (Lua lua = new Lua())
            {
                var a = new System.Numerics.Complex(10, 0);
                var b = new System.Numerics.Complex(0, 3);
                var x = a != b;

                lua["a"] = a;
                lua["b"] = b;
                var res = lua.DoString(@"return a ~= b")[0];
                Assert.AreEqual(x, res);
            }
        }

        [Test]
        public void TestUnaryMinus()
        {
            using (Lua lua = new Lua())
            {

                lua.LoadCLRPackage();
                lua.DoString(@" import ('System.Numerics')
                              c = Complex (10, 5) 
                              c = -c ");

                var expected = new System.Numerics.Complex(10, 5);
                expected = -expected;

                var res = lua["c"];
                Assert.AreEqual(expected, res);
            }
        }

        [Test]
        public void TestCaseFields()
        {
            using (Lua lua = new Lua())
            {
                lua.LoadCLRPackage();

                lua.DoString(@" import ('NLuaTest', 'NLuaTest.TestTypes')
                              x = TestCaseName()
                              name  = x.name;
                              name2 = x.Name;
                              Name = x.Name;
                              Name2 = x.name");

                Assert.AreEqual("name", lua["name"]);
                Assert.AreEqual("**name**", lua["name2"]);
                Assert.AreEqual("**name**", lua["Name"]);
                Assert.AreEqual("name", lua["Name2"]);
            }
        }

        [Test]
        public void TestStaticOperators()
        {
            using (Lua lua = new Lua())
            {
                lua.LoadCLRPackage();

                lua.DoString(@" import ('NLuaTest', 'NLuaTest.TestTypes')
                              v = Vector()
                              v.x = 10
                              v.y = 3
                              v = v*2 ");

                var v = (Vector)lua["v"];

                Assert.AreEqual(20, v.x, "#1");
                Assert.AreEqual(6, v.y, "#2");

                lua.DoString(@" x = 2 * v");
                var x = (Vector)lua["x"];

                Assert.AreEqual(40, x.x, "#3");
                Assert.AreEqual(12, x.y, "#4");
            }
        }

        [Test]
        public void TestExtensionMethods()
        {
            using (Lua lua = new Lua())
            {
                lua.LoadCLRPackage();

                lua.DoString(@" import ('NLuaTest', 'NLuaTest.TestTypes')
                              v = Vector()
                              v.x = 10
                              v.y = 3
                              v = v*2 ");

                var v = (Vector)lua["v"];

                double len = v.Length();
                lua.DoString(" v:Length() ");
                lua.DoString(@" len2 = v:Length()");
                double len2 = (double)lua["len2"];
                Assert.AreEqual(len, len2, "#1");
            }
        }

        [Test]
        public void TestInexistentExtensionMethods()
        {
            using (Lua lua = new Lua())
            {
                lua.LoadCLRPackage();

                lua.DoString(@" import ('NLuaTest', 'NLuaTest.TestTypes')
                              v = Vector()
                              v.x = 10
                              v.y = 3
                              v = v*2 ");

                var sw = new Stopwatch();
                sw.Start();
                try
                {
                    for(int i = 0; i < 1000; i++)
                        lua.DoString($" v:Lengthx{i}() ");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
                sw.Stop();
                long time1 = sw.ElapsedMilliseconds;

                var sw2 = new Stopwatch();
                sw2.Start();
                try
                {
                    for (int i = 0; i < 1000; i++)
                        lua.DoString($" v:Lengthx{i}() ");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
                sw2.Stop();
                long time2 = sw2.ElapsedMilliseconds;

                // .NET Core keeps failing on this :/ but at least is never >
                Assert.True(time2 <= time1, "#1 t1:" + time1 + "t2:" + time2);
            }
        }

        [Test]
        public void TestBaseClassExtensionMethods()
        {
            using (Lua lua = new Lua())
            {
                lua.LoadCLRPackage();

                lua.DoString(@" import ('NLuaTest', 'NLuaTest.TestTypes')
                              p = Employee()
                              p.firstName = 'Paulo'
                              p.occupation = 'Programmer'");

                var p = (Person)lua["p"];

                string name = p.GetFirstName();
                lua.DoString(" p:GetFirstName() ");
                lua.DoString(@" name2 = p:GetFirstName()");
                string name2 = (string)lua["name2"];
                Assert.AreEqual(name, name2, "#1");
            }
        }

        [Test]
        public void TestOverloadedMethods()
        {
            using (Lua lua = new Lua())
            {
                var obj = new TestClassWithOverloadedMethod();
                lua["obj"] = obj;
                lua.DoString(@" 
                                obj:Func (10)
                                obj:Func ('10')
                                obj:Func (10)
                                obj:Func ('10')
                                obj:Func (10)
                                ");
                Assert.AreEqual(3, obj.CallsToIntFunc, "#integer");
                Assert.AreEqual(2, obj.CallsToStringFunc, "#string");

                obj.CallsToIntFunc = 0;
                obj.CallsToStringFunc = 0;

                lua.DoString(@" 
                                obj:Func2('foo','10')
                                obj:Func2('foo', 10)
                                obj:Func2('foo','10')
                                obj:Func2('foo',10)
                                obj:Func2('foo','10')
                                ");

                Assert.AreEqual(2, obj.CallsToIntFunc, "#integer");
                Assert.AreEqual(3, obj.CallsToStringFunc, "#string");
            }
        }



        [Test]
        public void TestGetStack()
        {
            using (Lua lua = new Lua())
            {
                lua.LoadCLRPackage();
                m_lua = lua;
                lua.DoString(@" 
                                import ('NLuaTest')
                                function f1 ()
                                     f2 ()
                                 end
                                 
                                function f2()
                                    f3()
                                end

                                function f3()
                                    LuaTests.Func()
                                end
                                
                                f1 ()
                                ");
            }
            m_lua = null;
        }

        public static void Func()
        {

            //string expected = "[0] func:-1 -- <unknown> [func]\n[1] f3:12 -- <unknown> [f3]\n[2] f2:8 -- <unknown> [f2]\n[3] f1:4 -- <unknown> [f1]\n[4] :15 --  []\n";
            var info = new KeraLua.LuaDebug();

            int level = 0;
            var sb = new StringBuilder();
            while (m_lua.GetStack(level, ref info) != 0)
            {
                m_lua.GetInfo("nSl", ref info);
                string name = "<unknow>";
                if (!string.IsNullOrEmpty(info.Name))
                    name = info.Name;

                sb.AppendFormat("[{0}] {1}:{2} -- {3} [{4}]\n",
                    level, info.ShortSource, info.CurrentLine,
                    name, info.NameWhat);
                ++level;
            }
            string x = sb.ToString();
            Assert.True(!string.IsNullOrEmpty(x));
        }

        [Test]
        public void TestCallImplicitBaseMethod()
        {
            using (var l = new Lua())
            {
                l.LoadCLRPackage();
                l.DoString("import ('NLuaTest', 'NLuaTest.TestTypes')");
                l.DoString("res = testClass.read() ");
                string res = (string)l["res"];
                Assert.AreEqual(testClass.read(), res);
            }
        }

        [Test]
        public void TestPushLuaFunctionWhenReadingDelegateProperty()
        {
            bool called = false;
            var _model = new DefaultElementModel();
            _model.DrawMe = (x) =>
            {
                called = true;
            };
            using (var l = new Lua())
            {
                l["model"] = _model;
                l.DoString(@" model.DrawMe (0) ");
            }

            Assert.True(called);
        }

        [Test]
        public void TestCallDelegateWithParameters()
        {
            string sval = "";
            int nval = 0;
            using (var l = new Lua())
            {
                Action<string, int> c = (s, n) => { sval = s; nval = n; };
                l["d"] = c;
                l.DoString(" d ('string', 10) ");
            }

            Assert.AreEqual("string", sval, "#1");
            Assert.AreEqual(10, nval, "#2");
        }

        [Test]
        public void TestCallSimpleDelegate()
        {
            bool called = false;
            using (var l = new Lua())
            {
                Action c = () => { called = true; };
                l["d"] = c;
                l.DoString(" d () ");
            }

            Assert.True(called);
        }

        [Test]
        public void TestCallDelegateWithWrongParametersShouldFail()
        {
            bool fail = false;
            using (var l = new Lua())
            {
                Action c = () => { fail = false; };
                l["d"] = c;
                try
                {
                    l.DoString(" d (10) ");
                }
                catch (LuaScriptException)
                {
                    fail = true;
                }
            }

            Assert.True(fail);
        }

        [Test]
        public void TestOverloadedMethodCallOnBase()
        {
            using (var l = new Lua())
            {
                l.LoadCLRPackage();
                l.DoString(" import ('NLuaTest', 'NLuaTest.TestTypes') ");
                l.DoString(@"
                    p=Parameter()
                    r1 = testClass.read(p)     -- is not working. it is also not working if the method in base class has two parameters instead of one
                    r2 = testClass.read(1)     -- is working				
                ");
                string r1 = (string)l["r1"];
                string r2 = (string)l["r2"];
                Assert.AreEqual("parameter-field1", r1, "#1");
                Assert.AreEqual("int-test", r2, "#2");
            }
        }

        [Test]
        public void TestCallMethodWithParams2()
        {
            using (var l = new Lua())
            {
                l.LoadCLRPackage();
                l.DoString(" import ('NLuaTest','NLuaTest.TestTypes') ");
                l.DoString(@"					
                    r = TestClass.MethodWithParams(2)			
                ");
                int r = (int)l.GetNumber("r");
                Assert.AreEqual(0, r, "#1");
            }
        }

        [Test]
        public void TestCallMethodWithParamsOptional()
        {
            using (var l = new Lua())
            {
                l.LoadCLRPackage();
                l.DoString(" import ('NLuaTest','NLuaTest.TestTypes') ");
                l.DoString(@"					
                    r = TestClass.MethodWithParams(2, 7, 4)			
                ");
                int r = (int)l.GetNumber("r");
                Assert.AreEqual(2, r, "#1");
            }
        }

        [Test]
        public void TestCallMethodWithObjectParams()
        {
            using (var l = new Lua())
            {
                l.LoadCLRPackage();
                l.DoString(" import ('NLuaTest','NLuaTest.TestTypes') ");
                l.DoString(@"					
                    r = TestClass.MethodWithObjectParams(2, nil, 4, 'abc')			
                ");
                int r = (int)l.GetNumber("r");
                Assert.AreEqual(4, r, "#1");
            }
        }

        [Test]
        public void TestCallMethodWithObjectParamsAndNilAsFirstArgument()
        {
            using (var l = new Lua())
            {
                l.LoadCLRPackage();
                l.DoString(" import ('NLuaTest','NLuaTest.TestTypes') ");
                l.DoString(@"					
                    r = TestClass.MethodWithObjectParams(nil, 4, 'abc')			
                ");
                int r = (int)l.GetNumber("r");
                Assert.AreEqual(3, r, "#1");
            }
        }

        [Test]
        public void TestConstructorOverload()
        {
            using (var l = new Lua())
            {
                l.LoadCLRPackage();
                l.DoString(" import ('NLuaTest','NLuaTest.TestTypes') ");
                l.DoString(@"					
                    e1 = Entity()
                    e2 = Entity ('str_param')
                    e3 = Entity (10)
                    p1 = e1.Property
                    p2 = e2.Property
                    p3 = e3.Property
                ");
                string p1 = l.GetString("p1");
                string p2 = l.GetString("p2");
                string p3 = l.GetString("p3");
                Assert.AreEqual("Default", p1, "#1");
                Assert.AreEqual("String", p2, "#1");
                Assert.AreEqual("Int", p3, "#1");
            }
        }

        [Test]
        public void TestDefaultParameter()
        {
            using (var l = new Lua())
            {
                var obj = new TestClassWithMethodDefaultParameter();
                l["obj"] = obj;

                // Use both functions to avoid linker to remove.
                obj.Func("param1");
                obj.Func2("param1");

                obj.x = 0;

                l.DoString("obj:Func('param1')");
                Assert.AreEqual(1, obj.x, "#1");

                l.DoString("obj:Func('param1', 0, 0,'foo')");
                Assert.AreEqual(3, obj.x, "#2");

                l.DoString("obj:Func('param1', 0, 0,'')");
                Assert.AreEqual(7, obj.x, "#3");

                obj.x = 0;

                l.DoString("obj:Func('param1', 0, 0,nil)");
                Assert.AreEqual(1, obj.x, "#4");

                obj.x = 0;

                l.DoString("obj:Func2('param1')");
                Assert.AreEqual(4, obj.x, "#2.1");

                l.DoString("obj:Func2('param1', 0, 0,'foo')");
                Assert.AreEqual(6, obj.x, "#2.2");

                l.DoString("obj:Func2('param1', 0, 0,'')");
                Assert.AreEqual(14, obj.x, "#2.3");

                l.DoString("obj:Func2('param1', 0, 0,nil)");
                Assert.AreEqual(15, obj.x, "#2.4");
            }
        }

        [Test]
        public void TestUseLuaObjectAfterDisposeShouldNotCrash()
        {
            LuaTable table;
            LuaFunction function;

            using (var lua = new Lua())
            {
                lua.DoString("function F(a) return 2*a end");
                function = lua.GetFunction("F");
                table = lua.DoString("return { foo =\"Um dois tres\"}") [0] as LuaTable;
            }
            Assert.IsNotNull(function);
            Assert.IsNotNull(table);

            Assert.IsNull(table["foo"]);

            var result = function.Call(2);
            Assert.IsNull(result);

        }

        void ImplicitlyCreateATable(Lua lua)
        {
            var x = lua.DoString("return { foo = 1, bar = { la = 1 , la2 = 3, la4 = \"aidsjiasjdiasjdiajsdi\"} }")[0] as LuaTable;
            var foo = x["foo"];
            x = null;

        }

        void PleaseRunFinalizers()
        {
            for (int i = 0; i < 40; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                Thread.Yield();
                Thread.Sleep(1);
            }
        }

        ///*
        // * Tests if Lua objects are being released on finalizer
        // */
        [Test]
        public void TestTableFinalizerDispose()
        {
            using (Lua lua = new Lua())
            {
                int before = lua.State.GarbageCollector(LuaGC.Count, 0);

                for (int i = 0; i < 1000; i++)
                    ImplicitlyCreateATable(lua);

                int after1 = lua.State.GarbageCollector(LuaGC.Count, 0);

                PleaseRunFinalizers();

                ImplicitlyCreateATable(lua);

                lua.State.GarbageCollector(LuaGC.Collect, 0);
                lua.State.GarbageCollector(LuaGC.Collect, 0);

                int after2 = lua.State.GarbageCollector(LuaGC.Count, 0);

                int ratio = after2 / before;
                int ratio2 = after1 / after2;

                // The ratio two is very uncertain, lets use 5x, just to have some certain that 
                // the gc collect the tables
                Assert.True( ratio2 >= 5 , "#1:" + ratio2);
                Assert.True( ratio <= 1,  "#2:" + ratio);
            }
        }

        [Test]
        public void PassIntegerToLua()
        {
            long x = 0x7FFFC0DEC0DEC0DE;

            using (var lua = new Lua())
            {
                lua["x"] = x;

                long l = lua.GetLong("x");

                Assert.AreEqual(x, l, "#1");

                lua.DoString("y = x");

                l = lua.GetLong("y");

                Assert.AreEqual(x, l, "#1.1");

                var testObj = new TestClass();

                lua["test"] = testObj;

                lua.DoString("test:MethodWithLong(x)");

                Assert.AreEqual(x, testObj.LongValue, "#2");

                lua.DoString("test:MethodWithLong(0x7FFFC0DEC0DECAFF)");

                Assert.AreEqual(0x7FFFC0DEC0DECAFF, testObj.LongValue, "#2.2");

                lua.DoString("y = test:MethodWithLong(0x7FFFC0DECADECAFF)");

                Assert.AreEqual(0x7FFFC0DECADECAFF, lua.GetLong("y"), "#2.3");

            }
        }

        [Test]
        public void CallMethodWithGeneric()
        {
            using (var lua = new Lua())
            {
                var u = new TestClassWithGenericMethod();
                lua["u"] = u;
                lua["foo"] = new DateTime(1234);

                lua.DoString("u:GenericMethodWithCommonArgs(10, 11, foo)");

                var foo = (DateTime)u.PassedValue;
                int x = u.x;
                int y = u.y;

                Assert.AreEqual(foo, new DateTime(1234), "#1");
                Assert.AreEqual(x, 10, "#2");
                Assert.AreEqual(y, 11, "#3");
            }
        }

        [Test]
        public void CallStaticMethod()
        {
            using (var lua = new Lua())
            {
                lua.DoString("FakeType = {}");
                lua["FakeType.bar"] = (Func<object[],int>) TestClass.MethodWithObjectParams;

                lua.DoString("i = FakeType.bar('one', 1)");

                Assert.AreEqual(2, lua["i"], "#1");
            }
        }

        [Test]
        public void CallDictionary()
        {
            using (var lua = new Lua())
            {
                var obj = new Dictionary<string, string>()
                {
                    { "key1" ,"value1" },
                    { "key2" ,"value2" }
                };

                lua["obj"] = obj;

                lua.DoString("i = obj.key1");
                lua.DoString("j = obj['key2']");

                Assert.AreEqual("value1", lua["i"], "#1");
                Assert.AreEqual("value2", lua["j"], "#2");

                IDictionary<string,object> obj2 = new Dictionary<string, object>()
                {
                    { "key1" ,"value3" },
                    { "key2" ,"value4" }
                };

                lua["obj2"] = obj2;

                lua.DoString("l = obj2.key1");
                lua.DoString("m = obj2['key2']");

                Assert.AreEqual("value3", lua["l"], "#3");
                Assert.AreEqual("value4", lua["m"], "#4");

                IDictionary<string, object> obj3 = new System.Dynamic.ExpandoObject();

                obj3["key1"] = "value5";
                obj3["key2"] = "value6";

                lua["obj3"] = obj3;

                lua.DoString("n = obj3.key1");
                lua.DoString("o = obj3['key2']");

                Assert.AreEqual("value5", lua["n"], "#5");
                Assert.AreEqual("value6", lua["o"], "#6");
            }
        }

        [Test]
        public void ByteArrayParameter()
        {
            using (var lua = new Lua())
            {
                lua["WriteBinary"] = (Action<byte[]>)WriteBinary;
                lua.DoString(@"
                        local value = string.char(1, 2, 3, 0x3f, 0x40, 0xff, 0xf3, 0x9f)
                        WriteBinary (value);
                ");
            }
        }

        private void WriteBinary(byte [] buffer)
        {
            byte[] expected = { 1, 2, 3, 0x3f, 0x40, 0xff, 0xf3, 0x9f };
            Assert.True(Enumerable.SequenceEqual(expected, buffer));
        }


        static Lua m_lua;
    }
}
