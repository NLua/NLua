using System;
using System.Text;
using System.Collections.Generic;
using NLuaTest.Mock;
using System.Reflection;
using System.Threading;
using NLua;
using NLua.Exceptions;
#if MONOTOUCH
using Foundation;
#endif

#if WINDOWS_PHONE
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using SetUp = Microsoft.VisualStudio.TestPlatform.UnitTestFramework.TestInitializeAttribute;
using TestFixture = Microsoft.VisualStudio.TestPlatform.UnitTestFramework.TestClassAttribute;
using Test = Microsoft.VisualStudio.TestPlatform.UnitTestFramework.TestMethodAttribute;
#else
using NUnit.Framework;
#endif

namespace NLuaTest
{
	public class parameter
	{
		public string field1 = "parameter-field1";
	}

	#if MONOTOUCH
	[Preserve (AllMembers = true)]
	#endif
	public class master
	{
		public static string read()
		{
			return "test-master";
		}

		public static string read( parameter test )
		{
			return test.field1;
		}
	}

	#if MONOTOUCH
	[Preserve (AllMembers = true)]
	#endif
	public class testClass : master 
	{
		public String strData;
		public int intData;
		public static string read2()
		{
			return "test";
		}

		public static string read( int test )
		{
			return "int-test";
		}
	}

	#if MONOTOUCH
	[Preserve (AllMembers = true)]
	#endif
	public class DefaultElementModel
	{  
		public Action<double> DrawMe{ get; set; }  
	}  
   
	#if MONOTOUCH
	[Preserve (AllMembers = true)]
	#endif
	public class TestCaseName {
		public string name = "name";
		public string Name {
			get {
				return "**" + name + "**";
			}
		}
	}



#if MONOTOUCH
	[Preserve (AllMembers = true)]
#endif
	public class Vector
	{
		public double x;
		public double y;
		public static Vector operator * (float k, Vector v)
		{
			var r = new Vector ();
			r.x = v.x * k;
			r.y = v.y * k;
			return r;
		}

		public static Vector operator * (Vector v, float k)
		{
			var r = new Vector ();
			r.x = v.x * k;
			r.y = v.y * k;
			return r;
		}

		public void Func ()
		{
			Console.WriteLine ("Func");
		}
	}

	public static class VectorExtension
	{
		public static double Length (this Vector v)
		{
			return v.x * v.x + v.y * v.y;
		}
	}
	
#if MONOTOUCH
	[Preserve (AllMembers = true)]
#endif
	public class Person
	{
		public string firstName;
	}
	
#if MONOTOUCH
	[Preserve (AllMembers = true)]
#endif
	public class Employee : Person
	{
		public string occupation;
	}
	
	public static class PersonExentsions
	{
		public static string GetFirstName (this Person argPerson)
		{
			return argPerson.firstName;
		}
	}

	[TestFixture]
	#if MONOTOUCH
	[Preserve (AllMembers = true)]
	#endif
	public class LuaTests
	{
		public static readonly char UnicodeChar = '\uE007';
		public static string UnicodeString
		{
			get
			{
				return Convert.ToString (UnicodeChar);
			}
		}
		public static string UnicodeStringRussian
		{
			get
			{
				return "Файл";
			}
		}
		/*
        * Tests capturing an exception
        */
		[Test]
		public void ThrowException ()
		{
			using (Lua lua = new Lua ()) {
				lua.DoString ("luanet.load_assembly('mscorlib')");
				lua.DoString ("luanet.load_assembly('NLuaTest')");
				lua.DoString ("TestClass=luanet.import_type('NLuaTest.Mock.TestClass')");
				lua.DoString ("test=TestClass()");
				lua.DoString ("err,errMsg=pcall(test.exceptionMethod,test)");
				bool err = (bool)lua ["err"];
				Exception errMsg = (Exception)lua ["errMsg"];
				Assert.AreEqual (false , err);
				Assert.AreNotEqual (null, errMsg.InnerException);
				Assert.AreEqual ("exception test", errMsg.InnerException.Message);
			}
		}

		/*
		* Tests passing a LuaFunction
		*/
		[Test]
		public void CallLuaFunction()
		{
			using (Lua lua = new Lua ()) {
				lua.DoString ("function someFunc(v1,v2) return v1 + v2 end");
				lua ["funcObject"] = lua.GetFunction ("someFunc");

				lua.DoString ("luanet.load_assembly('mscorlib')");
				lua.DoString ("luanet.load_assembly('NLuaTest')");
				lua.DoString ("TestClass=luanet.import_type('NLuaTest.Mock.TestClass')");
				lua.DoString ("b = TestClass():TestLuaFunction(funcObject)[0]");
				Assert.AreEqual (3, lua ["b"]);
				lua.DoString ("a = TestClass():TestLuaFunction(nil)");
				Assert.AreEqual (null, lua ["a"]);
			}
		}

		/*
        * Tests capturing an exception
        */
		[Test]
		public void ThrowUncaughtException ()
		{
			using (Lua lua = new Lua ()) {
				lua.DoString ("luanet.load_assembly('mscorlib')");
				lua.DoString ("luanet.load_assembly('NLuaTest')");
				lua.DoString ("TestClass=luanet.import_type('NLuaTest.Mock.TestClass')");
				lua.DoString ("test=TestClass()");

				try {
					lua.DoString ("test:exceptionMethod()");
					//failed
                    Assert.AreEqual(false, true);
				} catch (Exception) {
					//passed
					Assert.AreEqual (true, true);
				}
			}
		}


		/*
        * Tests nullable fields
        */
		[Test]
		public void TestNullable ()
		{
			using (Lua lua = new Lua ()) {
				lua.DoString ("luanet.load_assembly('mscorlib')");
				lua.DoString ("luanet.load_assembly('NLuaTest')");
				lua.DoString ("TestClass=luanet.import_type('NLuaTest.Mock.TestClass')");
				lua.DoString ("test=TestClass()");
				lua.DoString ("val=test.NullableBool");
				Assert.AreEqual (null, (object)lua ["val"]);
				lua.DoString ("test.NullableBool = true");
				lua.DoString ("val=test.NullableBool");
				Assert.AreEqual (true, (bool)lua ["val"]);
			}
		}

		/*
        * Tests structure assignment
        */
		[Test]
		public void TestStructs ()
		{
			using (Lua lua = new Lua ()) {
				lua.DoString ("luanet.load_assembly('NLuaTest')");
				lua.DoString ("TestClass=luanet.import_type('NLuaTest.Mock.TestClass')");
				lua.DoString ("test=TestClass()");
				lua.DoString ("TestStruct=luanet.import_type('NLuaTest.Mock.TestStruct')");
				lua.DoString ("struct=TestStruct(2)");
				lua.DoString ("test.Struct = struct");
				lua.DoString ("val=test.Struct.val");
				Assert.AreEqual (2.0d, (double)lua ["val"]);
			}
		}

		/*
		* Tests structure creation via the default constructor
		*/
		[Test]
		public void TestStructDefaultConstructor ()
		{
			using (Lua lua = new Lua ())
			{
				lua.DoString ("luanet.load_assembly('NLuaTest')");
				lua.DoString ("TestStruct=luanet.import_type('NLuaTest.Mock.TestStruct')");
				lua.DoString ("struct=TestStruct()");
				Assert.AreEqual (new TestStruct(), (TestStruct)lua ["struct"]);
			}
		}

        [Test]
        public void TestStructHashesEqual()
        {
            using (Lua lua = new Lua())
            {
                lua.DoString("luanet.load_assembly('NLuaTest')");
                lua.DoString("TestStruct=luanet.import_type('NLuaTest.Mock.TestStruct')");
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
                lua.DoString("luanet.load_assembly('NLuaTest')");
                lua.DoString("TestEnum=luanet.import_type('NLuaTest.Mock.TestEnum')");
                lua.DoString("enum1=TestEnum.ValueA");
                lua.DoString("enum2=TestEnum.ValueB");
                Assert.AreEqual(true, (bool)lua.DoString("return enum1 ~= enum2")[0]);
                Assert.AreEqual(false, (bool)lua.DoString("return enum1 == enum2")[0]);
            }
        }

		[Test]
		public void TestMethodOverloads ()
		{
			using (Lua lua = new Lua ()) {
				lua.DoString ("luanet.load_assembly('mscorlib')");
				lua.DoString ("luanet.load_assembly('NLuaTest')");
				lua.DoString ("TestClass=luanet.import_type('NLuaTest.Mock.TestClass')");
				lua.DoString ("test=TestClass()");
				lua.DoString ("test:MethodOverload()");
				lua.DoString ("test:MethodOverload(test)");
				lua.DoString ("test:MethodOverload(1,1,1)");
				lua.DoString ("test:MethodOverload(2,2,i)\r\nprint(i)");
			}
		}

		[Test]
		public void TestDispose ()
		{
			System.GC.Collect ();
#if !WINDOWS_PHONE
			long startingMem = System.Diagnostics.Process.GetCurrentProcess ().WorkingSet64;

			for (int i = 0; i < 100; i++) {
				using (Lua lua = new Lua ()) {
					_Calc (lua, i);
				}
			}

			//TODO: make this test assert so that it is useful
			Console.WriteLine ("Was using " + startingMem / 1024 / 1024 + "MB, now using: " + System.Diagnostics.Process.GetCurrentProcess ().WorkingSet64 / 1024 / 1024 + "MB");
#endif
        }

		private void _Calc (Lua lua, int i)
		{
			lua.DoString (
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
			lua.DoString ("function calcVP(a,b) return a+b end");
			LuaFunction lf = lua.GetFunction ("calcVP");
			lf.Call (i, 20);
		}

		[Test]
		public void TestThreading ()
		{
			using (Lua lua = new Lua ()) {
				object lua_locker = new object ();
				DoWorkClass doWork = new DoWorkClass ();
				lua.RegisterFunction ("dowork", doWork, typeof(DoWorkClass).GetMethod ("DoWork"));
				bool failureDetected = false;
				int completed = 0;
				int iterations = 10;

				for (int i = 0; i < iterations; i++) {
					ThreadPool.QueueUserWorkItem (new WaitCallback (delegate (object o) {
						try {
							lock (lua_locker) {
								lua.DoString ("dowork()");
							}
						} catch (Exception e) {
							Console.Write (e);
							failureDetected = true;
						}

						completed++;
					}));
				}

				while (completed < iterations && !failureDetected)
					Thread.Sleep (50);

				Assert.AreEqual (false, failureDetected);
			}
		}

		[Test]
		public void TestPrivateMethod ()
		{
			using (Lua lua = new Lua ()) {
				lua.DoString ("luanet.load_assembly('mscorlib')");
				lua.DoString ("luanet.load_assembly('NLuaTest')");
				lua.DoString ("TestClass=luanet.import_type('NLuaTest.Mock.TestClass')");
				lua.DoString ("test=TestClass()");

				try {
					lua.DoString ("test:_PrivateMethod()");
				} catch {
					Assert.AreEqual (true, true);
					return;
				}

                Assert.AreEqual(true, false);
			}
		}

		/*
        * Tests functions
        */
		[Test]
		public void TestFunctions ()
		{
			using (Lua lua = new Lua ()) {
				lua.DoString ("luanet.load_assembly('mscorlib')");
				lua.DoString ("luanet.load_assembly('NLuaTest')");
				lua.RegisterFunction ("p", null, typeof(System.Console).GetMethod ("WriteLine", new Type [] { typeof(String) }));
				/// Lua command that works (prints to console)
				lua.DoString ("p('Foo')");
				/// Yet this works...
				lua.DoString ("string.gsub('some string', '(%w+)', function(s) p(s) end)");
				/// This fails if you don't fix Lua5.1 lstrlib.c/add_value to treat LUA_TUSERDATA the same as LUA_FUNCTION
				lua.DoString ("string.gsub('some string', '(%w+)', p)");
			}
		}


		/*
        * Tests making an object from a Lua table and calling one of
        * methods the table overrides.
        */
		[Test]
		public void LuaTableOverridedMethod ()
		{
			using (Lua lua = new Lua ()) {
				lua.DoString ("luanet.load_assembly('NLuaTest')");
				lua.DoString ("TestClass=luanet.import_type('NLuaTest.Mock.TestClass')");
				lua.DoString ("test={}");
				lua.DoString ("function test:overridableMethod(x,y) return x*y; end");
				lua.DoString ("luanet.make_object(test,'NLuaTest.Mock.TestClass')");
				lua.DoString ("a=TestClass.callOverridable(test,2,3)");
				int a = (int)lua.GetNumber ("a");
				lua.DoString ("luanet.free_object(test)");
				Assert.AreEqual (6, a);
			}
		}


		/*
        * Tests making an object from a Lua table and calling a method
        * the table does not override.
        */
		[Test]
		public void LuaTableInheritedMethod ()
		{
			using (Lua lua = new Lua ()) {
				lua.DoString ("luanet.load_assembly('NLuaTest')");
				lua.DoString ("TestClass=luanet.import_type('NLuaTest.Mock.TestClass')");
				lua.DoString ("test={}");
				lua.DoString ("function test:overridableMethod(x,y) return x*y; end");
				lua.DoString ("luanet.make_object(test,'NLuaTest.Mock.TestClass')");
				lua.DoString ("test:setVal(3)");
				lua.DoString ("a=test.testval");
				int a = (int)lua.GetNumber ("a");
				lua.DoString ("luanet.free_object(test)");
				Assert.AreEqual (3, a);
				//Console.WriteLine("interface returned: "+a);
			}
		}


		/// <summary>
		/// Basic multiply method which expects 2 floats
		/// </summary>
		/// <param name="val"></param>
		/// <param name="val2"></param>
		/// <returns></returns>
		private float _TestException (float val, float val2)
		{
			return val * val2;
		}

		class LuaEventArgsHandler : NLua.Method.LuaDelegate
		{
			void CallFunction (object sender, EventArgs eventArgs)
			{
				object [] args = new object [] {sender, eventArgs };
				object [] inArgs = new object [] { sender, eventArgs };
				int [] outArgs = new int [] { };
				base.CallFunction (args, inArgs, outArgs);
			}
		}

		[Test]
		public void TestEventException ()
		{
			using (Lua lua = new Lua ()) {
				//Register a C# function
				MethodInfo testException = this.GetType ().GetMethod ("_TestException", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Instance, null, new Type [] {
                                typeof(float),
                                typeof(float)
                        }, null);
				lua.RegisterFunction ("Multiply", this, testException);
				lua.RegisterLuaDelegateType (typeof(EventHandler<EventArgs>), typeof(LuaEventArgsHandler));
				//create the lua event handler code for the entity
				//includes the bad code!
				lua.DoString ("function OnClick(sender, eventArgs)\r\n" +
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
				lua.DoString ("function SubscribeEntity(e)\r\ne.Clicked:Add(OnClick)\r\nend");
				//Create the entity object
				Entity entity = new Entity ();
				//Register the entity object with the event handler inside lua
				LuaFunction lf = lua.GetFunction ("SubscribeEntity");
				lf.Call (new object [1] { entity });

				try {
					//Cause the event to be fired
					entity.Click ();
					//failed
                    Assert.AreEqual(true, false);
				} catch (LuaException) {
					//passed
					Assert.AreEqual (true, true);
				}
			}
		}

		[Test]
		public void TestExceptionWithChunkOverload ()
		{
			using (Lua lua = new Lua ()) {
				try {
					lua.DoString ("thiswillthrowanerror", "MyChunk");
				} catch (Exception e) {
					Assert.AreEqual (true, e.Message.StartsWith ("[string \"MyChunk\"]"));
				}
			}
		}

		[Test]
		public void TestGenerics ()
		{
			//Im not sure support for generic classes is possible to implement, see: http://msdn.microsoft.com/en-us/library/system.reflection.methodinfo.containsgenericparameters.aspx
			//specifically the line that says: "If the ContainsGenericParameters property returns true, the method cannot be invoked"
			//TestClassGeneric<string> genericClass = new TestClassGeneric<string>();
			//lua.RegisterFunction("genericMethod", genericClass, typeof(TestClassGeneric<>).GetMethod("GenericMethod"));
			//lua.RegisterFunction("regularMethod", genericClass, typeof(TestClassGeneric<>).GetMethod("RegularMethod"));
			using (Lua lua = new Lua ()) {
				TestClassWithGenericMethod classWithGenericMethod = new TestClassWithGenericMethod ();

				////////////////////////////////////////////////////////////////////////////
				/// ////////////////////////////////////////////////////////////////////////
				///  IMPORTANT: Use generic method with the type you will call or generic methods will fail with iOS
				/// ////////////////////////////////////////////////////////////////////////
				classWithGenericMethod.GenericMethod<double>(99.0);
				classWithGenericMethod.GenericMethod<TestClass>(new TestClass (99));
				////////////////////////////////////////////////////////////////////////////
				/// ////////////////////////////////////////////////////////////////////////

				lua.RegisterFunction ("genericMethod2", classWithGenericMethod, typeof(TestClassWithGenericMethod).GetMethod ("GenericMethod"));

				try {
					lua.DoString ("genericMethod2(100)");
				} catch {
				}

				Assert.AreEqual (true, classWithGenericMethod.GenericMethodSuccess);
				Assert.AreEqual (true, classWithGenericMethod.Validate<double> (100)); //note the gotcha: numbers are all being passed to generic methods as doubles

				try {
					lua.DoString ("luanet.load_assembly('NLuaTest')");
					lua.DoString ("TestClass=luanet.import_type('NLuaTest.Mock.TestClass')");
					lua.DoString ("test=TestClass(56)");
					lua.DoString ("genericMethod2(test)");
				} catch {
				}

				Assert.AreEqual (true, classWithGenericMethod.GenericMethodSuccess);
				Assert.AreEqual (56, (classWithGenericMethod.PassedValue as TestClass).val);
			}
		}

		[Test]
		public void RegisterFunctionStressTest ()
		{
			const int Count = 200;  // it seems to work with 41
			using (Lua lua = new Lua ()) {
				MyClass t = new MyClass ();

				for (int i = 1; i < Count - 1; ++i) {
					lua.RegisterFunction ("func" + i, t, typeof(MyClass).GetMethod ("Func1"));
				}

				lua.RegisterFunction ("func" + (Count - 1), t, typeof(MyClass).GetMethod ("Func1"));
				lua.DoString ("print(func1())");
			}
		}

		[Test]
		public void TestMultipleOutParameters ()
		{
			using (Lua lua = new Lua ()) {
				TestClass t1 = new TestClass ();
				lua ["netobj"] = t1;
				lua.DoString ("a,b,c=netobj:outValMutiple(2)");
				int a = (int)lua.GetNumber ("a");
				string b = (string)lua.GetString ("b");
				string c = (string)lua.GetString ("c");
				Assert.AreEqual (2, a);
				Assert.AreNotEqual (null, b);
				Assert.AreNotEqual (null, c);
			}
		}

		[Test]
		public void TestLoadStringLeak ()
		{
			//Test to prevent stack overflow
			//See: http://code.google.com/p/nlua/issues/detail?id=5
			//number of iterations to test
			int count = 1000;
			using (Lua lua = new Lua ()) {
				for (int i = 0; i < count; i++) {
					lua.LoadString ("abc = 'def'", string.Empty);
				}
			}
			//any thrown exceptions cause the test run to fail
		}

		[Test]
		public void TestLoadFileLeak ()
		{
			//Test to prevent stack overflow
			//See: http://code.google.com/p/nlua/issues/detail?id=5
			//number of iterations to test
			int count = 1000;
			using (Lua lua = new Lua ()) {
				for (int i = 0; i < count; i++) {
					lua.LoadFile (Environment.CurrentDirectory + System.IO.Path.DirectorySeparatorChar + "test.lua");
				}
			}
			//any thrown exceptions cause the test run to fail
		}

		[Test]
		public void TestRegisterFunction ()
		{
			using (Lua lua = new Lua ()) {
				lua.RegisterFunction ("func1", null, typeof(TestClass2).GetMethod ("func"));
				object[] vals1 = lua.GetFunction ("func1").Call (2, 3);
				Assert.AreEqual (5.0f, Convert.ToSingle (vals1 [0]));
				TestClass2 obj = new TestClass2 ();
				lua.RegisterFunction ("func2", obj, typeof(TestClass2).GetMethod ("funcInstance"));
				vals1 = lua.GetFunction ("func2").Call (2, 3);
				Assert.AreEqual (5.0f, Convert.ToSingle (vals1 [0]));
			}
		}
	
		/*
		 * Tests passing a null object as a parameter to a
		 * method that accepts a nullable.
		 */
		[Test]
		public void TestNullableParameter ()
		{
			using (Lua lua = new Lua ()) {
				lua.DoString ("luanet.load_assembly('NLuaTest')");
				lua.DoString ("TestClass=luanet.import_type('NLuaTest.Mock.TestClass')");
				lua.DoString ("test=TestClass()");
				lua.DoString ("a = test:NullableMethod(nil)");
				Assert.AreEqual (null, lua ["a"]);
				lua ["timeVal"] = TimeSpan.FromSeconds (5);
				lua.DoString ("b = test:NullableMethod(timeVal)");
				Assert.AreEqual (TimeSpan.FromSeconds (5), lua ["b"]);
				lua.DoString ("d = test:NullableMethod2(2)");
				Assert.AreEqual (2, lua ["d"]);
				lua.DoString ("c = test:NullableMethod2(nil)");
				Assert.AreEqual (null, lua ["c"]);
			}
		}

		/*
        * Tests if DoString is correctly returning values
        */
		[Test]
		public void DoString ()
		{
			using (Lua lua = new Lua ()) {
				object[] res = lua.DoString ("a=2\nreturn a,3");
				//Console.WriteLine("a="+res[0]+", b="+res[1]);
				Assert.AreEqual (res [0], 2d);
				Assert.AreEqual (res [1], 3d);
			}
		}
		/*
        * Tests getting of global numeric variables
        */
		[Test]
		public void GetGlobalNumber ()
		{
			using (Lua lua = new Lua ()) {
				lua.DoString ("a=2");
				double num = lua.GetNumber ("a");
				//Console.WriteLine("a="+num);
				Assert.AreEqual (num, 2d);
			}
		}
		/*
        * Tests setting of global numeric variables
        */
		[Test]
		public void SetGlobalNumber ()
		{
			using (Lua lua = new Lua ()) {
				lua.DoString ("a=2");
				lua ["a"] = 3;
				double num = lua.GetNumber ("a");
				//Console.WriteLine("a="+num);
				Assert.AreEqual (num, 3d);
			}
		}
		/*
        * Tests getting of numeric variables from tables
        * by specifying variable path
        */
		[Test]
		public void GetNumberInTable ()
		{
			using (Lua lua = new Lua ()) {
				lua.DoString ("a={b={c=2}}");
				double num = lua.GetNumber ("a.b.c");
				//Console.WriteLine("a.b.c="+num);
				Assert.AreEqual (num, 2d);
			}
		}
		/*
        * Tests setting of numeric variables from tables
        * by specifying variable path
        */
		[Test]
		public void SetNumberInTable ()
		{
			using (Lua lua = new Lua ()) {
				lua.DoString ("a={b={c=2}}");
				lua ["a.b.c"] = 3;
				double num = lua.GetNumber ("a.b.c");
				//Console.WriteLine("a.b.c="+num);
				Assert.AreEqual (num, 3d);
			}
		}
		/*
        * Tests getting of global string variables
        */
		[Test]
		public void GetGlobalString ()
		{
			using (Lua lua = new Lua ()) {
				lua.DoString ("a=\"test\"");
				string str = lua.GetString ("a");
				//Console.WriteLine("a="+str);
				Assert.AreEqual (str, "test");
			}
		}
		/*
        * Tests setting of global string variables
        */
		[Test]
		public void SetGlobalString ()
		{
			using (Lua lua = new Lua ()) {
				lua.DoString ("a=\"test\"");
				lua ["a"] = "new test";
				string str = lua.GetString ("a");
				//Console.WriteLine("a="+str);
				Assert.AreEqual (str, "new test");
			}
		}
		/*
        * Tests getting of string variables from tables
        * by specifying variable path
        */
		[Test]
		public void GetStringInTable ()
		{
			using (Lua lua = new Lua ()) {
				lua.DoString ("a={b={c=\"test\"}}");
				string str = lua.GetString ("a.b.c");
				//Console.WriteLine("a.b.c="+str);
				Assert.AreEqual (str, "test");
			}
		}
		/*
        * Tests setting of string variables from tables
        * by specifying variable path
        */
		[Test]
		public void SetStringInTable ()
		{
			using (Lua lua = new Lua ()) {
				lua.DoString ("a={b={c=\"test\"}}");
				lua ["a.b.c"] = "new test";
				string str = lua.GetString ("a.b.c");
				//Console.WriteLine("a.b.c="+str);
				Assert.AreEqual (str, "new test");
			}
		}
		/*
        * Tests getting and setting of global table variables
        */
		[Test]
		public void GetAndSetTable ()
		{
			using (Lua lua = new Lua ()) {
				lua.DoString ("a={b={c=2}}\nb={c=3}");
				LuaTable tab = lua.GetTable ("b");
				lua ["a.b"] = tab;
				double num = lua.GetNumber ("a.b.c");
				//Console.WriteLine("a.b.c="+num);
				Assert.AreEqual (num, 3d);
			}
		}
		/*
        * Tests getting of numeric field of a table
        */
		[Test]
		public void GetTableNumericField1 ()
		{
			using (Lua lua = new Lua ()) {
				lua.DoString ("a={b={c=2}}");
				LuaTable tab = lua.GetTable ("a.b");
				double num = (double)tab ["c"];
				//Console.WriteLine("a.b.c="+num);
				Assert.AreEqual (num, 2d);
			}
		}
		/*
        * Tests getting of numeric field of a table
        * (the field is inside a subtable)
        */
		[Test]
		public void GetTableNumericField2 ()
		{
			using (Lua lua = new Lua ()) {
				lua.DoString ("a={b={c=2}}");
				LuaTable tab = lua.GetTable ("a");
				double num = (double)tab ["b.c"];
				//Console.WriteLine("a.b.c="+num);
				Assert.AreEqual (num, 2d);
			}
		}
		/*
        * Tests setting of numeric field of a table
        */
		[Test]
		public void SetTableNumericField1 ()
		{
			using (Lua lua = new Lua ()) {
				lua.DoString ("a={b={c=2}}");
				LuaTable tab = lua.GetTable ("a.b");
				tab ["c"] = 3;
				double num = lua.GetNumber ("a.b.c");
				//Console.WriteLine("a.b.c="+num);
				Assert.AreEqual (num, 3d);
			}
		}
		/*
        * Tests setting of numeric field of a table
        * (the field is inside a subtable)
        */
		[Test]
		public void SetTableNumericField2 ()
		{
			using (Lua lua = new Lua ()) {
				lua.DoString ("a={b={c=2}}");
				LuaTable tab = lua.GetTable ("a");
				tab ["b.c"] = 3;
				double num = lua.GetNumber ("a.b.c");
				//Console.WriteLine("a.b.c="+num);
				Assert.AreEqual (num, 3d);
			}
		}
		/*
        * Tests getting of string field of a table
        */
		[Test]
		public void GetTableStringField1 ()
		{
			using (Lua lua = new Lua ()) {
				lua.DoString ("a={b={c=\"test\"}}");
				LuaTable tab = lua.GetTable ("a.b");
				string str = (string)tab ["c"];
				//Console.WriteLine("a.b.c="+str);
				Assert.AreEqual (str, "test");
			}
		}
		/*
        * Tests getting of string field of a table
        * (the field is inside a subtable)
        */
		[Test]
		public void GetTableStringField2 ()
		{
			using (Lua lua = new Lua ()) {
				lua.DoString ("a={b={c=\"test\"}}");
				LuaTable tab = lua.GetTable ("a");
				string str = (string)tab ["b.c"];
				//Console.WriteLine("a.b.c="+str);
				Assert.AreEqual (str, "test");
			}
		}
		/*
        * Tests setting of string field of a table
        */
		[Test]
		public void SetTableStringField1 ()
		{
			using (Lua lua = new Lua ()) {
				lua.DoString ("a={b={c=\"test\"}}");
				LuaTable tab = lua.GetTable ("a.b");
				tab ["c"] = "new test";
				string str = lua.GetString ("a.b.c");
				//Console.WriteLine("a.b.c="+str);
				Assert.AreEqual (str, "new test");
			}
		}
		/*
        * Tests setting of string field of a table
        * (the field is inside a subtable)
        */
		[Test]
		public void SetTableStringField2 ()
		{
			using (Lua lua = new Lua ()) {
				lua.DoString ("a={b={c=\"test\"}}");
				LuaTable tab = lua.GetTable ("a");
				tab ["b.c"] = "new test";
				string str = lua.GetString ("a.b.c");
				//Console.WriteLine("a.b.c="+str);
				Assert.AreEqual (str, "new test");
			}
		}
		/*
        * Tests calling of a global function with zero arguments
        */
		[Test]
		public void CallGlobalFunctionNoArgs ()
		{
			using (Lua lua = new Lua ()) {
				lua.DoString ("a=2\nfunction f()\na=3\nend");
				lua.GetFunction ("f").Call ();
				double num = lua.GetNumber ("a");
				//Console.WriteLine("a="+num);
				Assert.AreEqual (num, 3d);
			}
		}
		/*
        * Tests calling of a global function with one argument
        */
		[Test]
		public void CallGlobalFunctionOneArg ()
		{
			using (Lua lua = new Lua ()) {
				lua.DoString ("a=2\nfunction f(x)\na=a+x\nend");
				lua.GetFunction ("f").Call (1);
				double num = lua.GetNumber ("a");
				//Console.WriteLine("a="+num);
				Assert.AreEqual (num, 3d);
			}
		}
		/*
        * Tests calling of a global function with two arguments
        */
		[Test]
		public void CallGlobalFunctionTwoArgs ()
		{
			using (Lua lua = new Lua ()) {
				lua.DoString ("a=2\nfunction f(x,y)\na=x+y\nend");
				lua.GetFunction ("f").Call (1, 3);
				double num = lua.GetNumber ("a");
				//Console.WriteLine("a="+num);
				Assert.AreEqual (num, 4d);
			}
		}
		/*
        * Tests calling of a global function that returns one value
        */
		[Test]
		public void CallGlobalFunctionOneReturn ()
		{
			using (Lua lua = new Lua ()) {
				lua.DoString ("function f(x)\nreturn x+2\nend");
				object[] ret = lua.GetFunction ("f").Call (3);
				//Console.WriteLine("ret="+ret[0]);
				Assert.AreEqual (1, ret.Length);
				Assert.AreEqual (5, (double)ret [0]);
			}
		}
		/*
        * Tests calling of a global function that returns two values
        */
		[Test]
		public void CallGlobalFunctionTwoReturns ()
		{
			using (Lua lua = new Lua ()) {
				lua.DoString ("function f(x,y)\nreturn x,x+y\nend");
				object[] ret = lua.GetFunction ("f").Call (3, 2);
				//Console.WriteLine("ret="+ret[0]+","+ret[1]);
				Assert.AreEqual (2, ret.Length);
				Assert.AreEqual (3, (double)ret [0]);
				Assert.AreEqual (5, (double)ret [1]);
			}
		}
		/*
        * Tests calling of a function inside a table
        */
		[Test]
		public void CallTableFunctionTwoReturns ()
		{
			using (Lua lua = new Lua ()) {
				lua.DoString ("a={}\nfunction a.f(x,y)\nreturn x,x+y\nend");
				object[] ret = lua.GetFunction ("a.f").Call (3, 2);
				//Console.WriteLine("ret="+ret[0]+","+ret[1]);
				Assert.AreEqual (2, ret.Length);
				Assert.AreEqual (3, (double)ret [0]);
				Assert.AreEqual (5, (double)ret [1]);
			}
		}
		/*
        * Tests setting of a global variable to a CLR object value
        */
		[Test]
		public void SetGlobalObject ()
		{
			using (Lua lua = new Lua ()) {
				TestClass t1 = new TestClass ();
				t1.testval = 4;
				lua ["netobj"] = t1;
				object o = lua ["netobj"];
				Assert.AreEqual (true, o is TestClass);
				TestClass t2 = (TestClass)lua ["netobj"];
				Assert.AreEqual (t2.testval, 4);
				Assert.AreEqual (t1 , t2);
			}
		}
		///*
		// * Tests if CLR object is being correctly collected by Lua
		// */
		//[Test]
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
		[Test]
		public void SetTableObjectField1 ()
		{
			using (Lua lua = new Lua ()) {
				lua.DoString ("a={b={c=\"test\"}}");
				LuaTable tab = lua.GetTable ("a.b");
				TestClass t1 = new TestClass ();
				t1.testval = 4;
				tab ["c"] = t1;
				TestClass t2 = (TestClass)lua ["a.b.c"];
				//Console.WriteLine("a.b.c="+t2.testval);
				Assert.AreEqual (4, t2.testval);
				Assert.AreEqual (t1 , t2);
			}
		}
		/*
        * Tests reading and writing of an object's field
        */
		[Test]
		public void AccessObjectField ()
		{
			using (Lua lua = new Lua ()) {
				TestClass t1 = new TestClass ();
				t1.val = 4;
				lua ["netobj"] = t1;
				lua.DoString ("var=netobj.val");
				double var = (double)lua ["var"];
				//Console.WriteLine("value from Lua="+var);
				Assert.AreEqual (4, var);
				lua.DoString ("netobj.val=3");
				Assert.AreEqual (3, t1.val);
				//Console.WriteLine("new val (from Lua)="+t1.val);
			}
		}
		/*
        * Tests reading and writing of an object's non-indexed
        * property
        */
		[Test]
		public void AccessObjectProperty ()
		{
			using (Lua lua = new Lua ()) {
				TestClass t1 = new TestClass ();
				t1.testval = 4;
				lua ["netobj"] = t1;
				lua.DoString ("var=netobj.testval");
				double var = (double)lua ["var"];
				//Console.WriteLine("value from Lua="+var);
				Assert.AreEqual (4, var);
				lua.DoString ("netobj.testval=3");
				Assert.AreEqual (3, t1.testval);
				//Console.WriteLine("new val (from Lua)="+t1.testval);
			}
		}

		[Test]
		public void AccessObjectStringProperty ()
		{
			using (Lua lua = new Lua ()) {
				TestClass t1 = new TestClass ();
				t1.teststrval = "This is a string test";
				lua ["netobj"] = t1;
				lua.DoString ("var=netobj.teststrval");
				string var = (string)lua ["var"];

				Assert.AreEqual ("This is a string test", var);
				lua.DoString ("netobj.teststrval='Another String'");
				Assert.AreEqual ("Another String", t1.teststrval);
				//Console.WriteLine("new val (from Lua)="+t1.testval);
			}
		}
		/*
        * Tests calling of an object's method with no overloads
        */
		[Test]
		public void CallObjectMethod ()
		{
			using (Lua lua = new Lua ()) {
				TestClass t1 = new TestClass ();
				t1.testval = 4;
				lua ["netobj"] = t1;
				lua.DoString ("netobj:setVal(3)");
				Assert.AreEqual (3, t1.testval);
				//Console.WriteLine("new val(from C#)="+t1.testval);
				lua.DoString ("val=netobj:getVal()");
				int val = (int)lua.GetNumber ("val");
				Assert.AreEqual (3, val);
				//Console.WriteLine("new val(from Lua)="+val);
			}
		}
		/*
        * Tests calling of an object's method with overloading
        */
		[Test]
		public void CallObjectMethodByType ()
		{
			using (Lua lua = new Lua ()) {
				TestClass t1 = new TestClass ();
				lua ["netobj"] = t1;
				lua.DoString ("netobj:setVal('str')");
				Assert.AreEqual ("str", t1.getStrVal ());
				//Console.WriteLine("new val(from C#)="+t1.getStrVal());
			}
		}
		/*
        * Tests calling of an object's method with no overloading
        * and out parameters
        */
		[Test]
		public void CallObjectMethodOutParam ()
		{
			using (Lua lua = new Lua ()) {
				TestClass t1 = new TestClass ();
				lua ["netobj"] = t1;
				lua.DoString ("a,b=netobj:outVal()");
				int a = (int)lua.GetNumber ("a");
				int b = (int)lua.GetNumber ("b");
				Assert.AreEqual (3, a);
				Assert.AreEqual (5, b);
				//Console.WriteLine("function returned (from lua)="+a+","+b);
			}
		}
		/*
        * Tests calling of an object's method with overloading and
        * out params
        */
		[Test]
		public void CallObjectMethodOverloadedOutParam ()
		{
			using (Lua lua = new Lua ()) {
				TestClass t1 = new TestClass ();
				lua ["netobj"] = t1;
				lua.DoString ("a,b=netobj:outVal(2)");
				int a = (int)lua.GetNumber ("a");
				int b = (int)lua.GetNumber ("b");
				Assert.AreEqual (2, a);
				Assert.AreEqual (5, b);
				//Console.WriteLine("function returned (from lua)="+a+","+b);
			}
		}
		/*
        * Tests calling of an object's method with ref params
        */
		[Test]
		public void CallObjectMethodByRefParam ()
		{
			using (Lua lua = new Lua ()) {
				TestClass t1 = new TestClass ();
				lua ["netobj"] = t1;
				lua.DoString ("a,b=netobj:outVal(2,3)");
				int a = (int)lua.GetNumber ("a");
				int b = (int)lua.GetNumber ("b");
				Assert.AreEqual (2, a);
				Assert.AreEqual (5, b);
				//Console.WriteLine("function returned (from lua)="+a+","+b);
			}
		}
		/*
        * Tests calling of two versions of an object's method that have
        * the same name and signature but implement different interfaces
        */
		[Test]
		public void CallObjectMethodDistinctInterfaces ()
		{
			using (Lua lua = new Lua ()) {
				TestClass t1 = new TestClass ();
				lua ["netobj"] = t1;
				lua.DoString ("a=netobj:foo()");
				lua.DoString ("b=netobj['NLuaTest.Mock.IFoo1.foo']");
				int a = (int)lua.GetNumber ("a");
				int b = (int)lua.GetNumber ("b");
				Assert.AreEqual (5, a);
				Assert.AreEqual (1, b);
				//Console.WriteLine("function returned (from lua)="+a+","+b);
			}
		}
		/*
        * Tests instantiating an object with no-argument constructor
        */
		[Test]
		public void CreateNetObjectNoArgsCons ()
		{
			using (Lua lua = new Lua ()) {
				lua.DoString ("luanet.load_assembly(\"NLuaTest\")");
				lua.DoString ("TestClass=luanet.import_type(\"NLuaTest.Mock.TestClass\")");
				lua.DoString ("test=TestClass()");
				lua.DoString ("test:setVal(3)");
				object[] res = lua.DoString ("return test");
				TestClass test = (TestClass)res [0];
				//Console.WriteLine("returned: "+test.testval);
				Assert.AreEqual (3, test.testval);
			}
		}
		/*
        * Tests instantiating an object with one-argument constructor
        */
		[Test]
		public void CreateNetObjectOneArgCons ()
		{
			using (Lua lua = new Lua ()) {
				lua.DoString ("luanet.load_assembly(\"NLuaTest\")");
				lua.DoString ("TestClass=luanet.import_type(\"NLuaTest.Mock.TestClass\")");
				lua.DoString ("test=TestClass(3)");
				object[] res = lua.DoString ("return test");
				TestClass test = (TestClass)res [0];
				//Console.WriteLine("returned: "+test.testval);
				Assert.AreEqual (3, test.testval);
			}
		}
		/*
        * Tests instantiating an object with overloaded constructor
        */
		[Test]
		public void CreateNetObjectOverloadedCons ()
		{
			using (Lua lua = new Lua ()) {
				lua.DoString ("luanet.load_assembly(\"NLuaTest\")");
				lua.DoString ("TestClass=luanet.import_type(\"NLuaTest.Mock.TestClass\")");
				lua.DoString ("test=TestClass('str')");
				object[] res = lua.DoString ("return test");
				TestClass test = (TestClass)res [0];
				//Console.WriteLine("returned: "+test.getStrVal());
				Assert.AreEqual ("str", test.getStrVal ());
			}
		}
		/*
        * Tests getting item of a CLR array
        */
		[Test]
		public void ReadArrayField ()
		{
			using (Lua lua = new Lua ()) {
				string[] arr = new string [] { "str1", "str2", "str3" };
				lua ["netobj"] = arr;
				lua.DoString ("val=netobj[1]");
				string val = lua.GetString ("val");
				Assert.AreEqual ("str2", val);
				//Console.WriteLine("new val(from array to Lua)="+val);
			}
		}
		/*G
        * Tests setting item of a CLR array
        */
		[Test]
		public void WriteArrayField ()
		{
			using (Lua lua = new Lua ()) {
				string[] arr = new string [] { "str1", "str2", "str3" };
				lua ["netobj"] = arr;
				lua.DoString ("netobj[1]='test'");
				Assert.AreEqual ("test", arr [1]);
				//Console.WriteLine("new val(from Lua to array)="+arr[1]);
			}
		}
		/*
        * Tests creating a new CLR array
        */
		[Test]
		public void CreateArray ()
		{
			using (Lua lua = new Lua ()) {
				lua.DoString ("luanet.load_assembly(\"NLuaTest\")");
				lua.DoString ("TestClass=luanet.import_type(\"NLuaTest.Mock.TestClass\")");
				lua.DoString ("arr=TestClass[3]");
				lua.DoString ("for i=0,2 do arr[i]=TestClass(i+1) end");
				TestClass[] arr = (TestClass[])lua ["arr"];
				Assert.AreEqual (arr [1].testval, 2);
			}
		}
		/*
        * Tests passing a Lua function to a delegate
        * with value-type arguments
        */
		[Test]
		public void LuaDelegateValueTypes ()
		{
			using (Lua lua = new Lua ()) {
				lua.RegisterLuaDelegateType (typeof(TestDelegate1), typeof(LuaTestDelegate1Handler));
				lua.DoString ("luanet.load_assembly('NLuaTest')");
				lua.DoString ("TestClass=luanet.import_type('NLuaTest.Mock.TestClass')");
				lua.DoString ("test=TestClass()");
				lua.DoString ("function func(x,y) return x+y; end");
				lua.DoString ("test=TestClass()");
				lua.DoString ("a=test:callDelegate1(func)");
				int a = (int)lua.GetNumber ("a");
				Assert.AreEqual (5, a);
				//Console.WriteLine("delegate returned: "+a);
			}
		}
		/*
        * Tests passing a Lua function to a delegate
        * with value-type arguments and out params
        */
		[Test]
		public void LuaDelegateValueTypesOutParam ()
		{
			using (Lua lua = new Lua ()) {
				lua.RegisterLuaDelegateType (typeof(TestDelegate2), typeof(LuaTestDelegate2Handler));
				lua.DoString ("luanet.load_assembly('NLuaTest')");
				lua.DoString ("TestClass=luanet.import_type('NLuaTest.Mock.TestClass')");
				lua.DoString ("test=TestClass()");
				lua.DoString ("function func(x) return x,x*2; end");
				lua.DoString ("test=TestClass()");
				lua.DoString ("a=test:callDelegate2(func)");
				int a = (int)lua.GetNumber ("a");
				Assert.AreEqual (6, a);
				//Console.WriteLine("delegate returned: "+a);
			}
		}
		/*
        * Tests passing a Lua function to a delegate
        * with value-type arguments and ref params
        */
		[Test]
		public void LuaDelegateValueTypesByRefParam ()
		{
			using (Lua lua = new Lua ()) {
				lua.RegisterLuaDelegateType (typeof(TestDelegate3), typeof(LuaTestDelegate3Handler));
				lua.DoString ("luanet.load_assembly('NLuaTest')");
				lua.DoString ("TestClass=luanet.import_type('NLuaTest.Mock.TestClass')");
				lua.DoString ("test=TestClass()");
				lua.DoString ("function func(x,y) return x+y; end");
				lua.DoString ("test=TestClass()");
				lua.DoString ("a=test:callDelegate3(func)");
				int a = (int)lua.GetNumber ("a");
				Assert.AreEqual (5, a);
				//Console.WriteLine("delegate returned: "+a);
			}
		}
		/*
        * Tests passing a Lua function to a delegate
        * with value-type arguments that returns a reference type
        */
		[Test]
		public void LuaDelegateValueTypesReturnReferenceType ()
		{
			using (Lua lua = new Lua ()) {
				lua.RegisterLuaDelegateType (typeof(TestDelegate4), typeof(LuaTestDelegate4Handler));
				lua.DoString ("luanet.load_assembly('NLuaTest')");
				lua.DoString ("TestClass=luanet.import_type('NLuaTest.Mock.TestClass')");
				lua.DoString ("test=TestClass()");
				lua.DoString ("function func(x,y) return TestClass(x+y); end");
				lua.DoString ("test=TestClass()");
				lua.DoString ("a=test:callDelegate4(func)");
				int a = (int)lua.GetNumber ("a");
				Assert.AreEqual (5, a);
				//Console.WriteLine("delegate returned: "+a);
			}
		}
		/*
        * Tests passing a Lua function to a delegate
        * with reference type arguments
        */
		[Test]
		public void LuaDelegateReferenceTypes ()
		{
			using (Lua lua = new Lua ()) {
				lua.RegisterLuaDelegateType (typeof(TestDelegate5), typeof(LuaTestDelegate5Handler));
				lua.DoString ("luanet.load_assembly('NLuaTest')");
				lua.DoString ("TestClass=luanet.import_type('NLuaTest.Mock.TestClass')");
				lua.DoString ("test=TestClass()");
				lua.DoString ("function func(x,y) return x.testval+y.testval; end");
				lua.DoString ("a=test:callDelegate5(func)");
				int a = (int)lua.GetNumber ("a");
				Assert.AreEqual (5, a);
				//Console.WriteLine("delegate returned: "+a);
			}
		}
		/*
        * Tests passing a Lua function to a delegate
        * with reference type arguments and an out param
        */
		[Test]
		public void LuaDelegateReferenceTypesOutParam ()
		{
			using (Lua lua = new Lua ()) {
				lua.RegisterLuaDelegateType (typeof(TestDelegate6), typeof(LuaTestDelegate6Handler));
				lua.DoString ("luanet.load_assembly('NLuaTest')");
				lua.DoString ("TestClass=luanet.import_type('NLuaTest.Mock.TestClass')");
				lua.DoString ("test=TestClass()");
				lua.DoString ("function func(x) return x,TestClass(x*2); end");
				lua.DoString ("test=TestClass()");
				lua.DoString ("a=test:callDelegate6(func)");
				int a = (int)lua.GetNumber ("a");
				Assert.AreEqual (6, a);
				//Console.WriteLine("delegate returned: "+a);
			}
		}
		/*
        * Tests passing a Lua function to a delegate
        * with reference type arguments and a ref param
        */
		[Test]
		public void LuaDelegateReferenceTypesByRefParam ()
		{
			using (Lua lua = new Lua ()) {
				lua.RegisterLuaDelegateType (typeof(TestDelegate7), typeof(LuaTestDelegate7Handler));
				lua.DoString ("luanet.load_assembly('NLuaTest')");
				lua.DoString ("TestClass=luanet.import_type('NLuaTest.Mock.TestClass')");
				lua.DoString ("test=TestClass()");
				lua.DoString ("function func(x,y) return TestClass(x+y.testval); end");
				lua.DoString ("a=test:callDelegate7(func)");
				int a = (int)lua.GetNumber ("a");
				Assert.AreEqual (5, a);
				//Console.WriteLine("delegate returned: "+a);
			}
		}


		/*
        * Tests passing a Lua table as an interface and
        * calling one of its methods with value-type params
        */
		[Test]
		public void NLuaAAValueTypes ()
		{
			using (Lua lua = new Lua ()) {
				lua.RegisterLuaClassType (typeof(ITest), typeof(LuaITestClassHandler));
				lua.DoString ("luanet.load_assembly('NLuaTest')");
				lua.DoString ("TestClass=luanet.import_type('NLuaTest.Mock.TestClass')");
				lua.DoString ("test=TestClass()");
				lua.DoString ("itest={}");
				lua.DoString ("function itest:test1(x,y) return x+y; end");
				lua.DoString ("test=TestClass()");
				lua.DoString ("a=test:callInterface1(itest)");
				int a = (int)lua.GetNumber ("a");
				Assert.AreEqual (5, a);
				//Console.WriteLine("interface returned: "+a);
			}
		}
		/*
        * Tests passing a Lua table as an interface and
        * calling one of its methods with value-type params
        * and an out param
        */
		[Test]
		public void NLuaValueTypesOutParam ()
		{
			using (Lua lua = new Lua ()) {
				lua.DoString ("luanet.load_assembly('NLuaTest')");
				lua.DoString ("TestClass=luanet.import_type('NLuaTest.Mock.TestClass')");
				lua.DoString ("test=TestClass()");
				lua.DoString ("itest={}");
				lua.DoString ("function itest:test2(x) return x,x*2; end");
				lua.DoString ("test=TestClass()");
				lua.DoString ("a=test:callInterface2(itest)");
				int a = (int)lua.GetNumber ("a");
				Assert.AreEqual (6, a);
				//Console.WriteLine("interface returned: "+a);
			}
		}
		/*
        * Tests passing a Lua table as an interface and
        * calling one of its methods with value-type params
        * and a ref param
        */
		[Test]
		public void NLuaValueTypesByRefParam ()
		{
			using (Lua lua = new Lua ()) {
				lua.DoString ("luanet.load_assembly('NLuaTest')");
				lua.DoString ("TestClass=luanet.import_type('NLuaTest.Mock.TestClass')");
				lua.DoString ("test=TestClass()");
				lua.DoString ("itest={}");
				lua.DoString ("function itest:test3(x,y) return x+y; end");
				lua.DoString ("test=TestClass()");
				lua.DoString ("a=test:callInterface3(itest)");
				int a = (int)lua.GetNumber ("a");
				Assert.AreEqual (5, a);
				//Console.WriteLine("interface returned: "+a);
			}
		}
		/*
        * Tests passing a Lua table as an interface and
        * calling one of its methods with value-type params
        * returning a reference type param
        */
		[Test]
		public void NLuaValueTypesReturnReferenceType ()
		{
			using (Lua lua = new Lua ()) {
				lua.DoString ("luanet.load_assembly('NLuaTest')");
				lua.DoString ("TestClass=luanet.import_type('NLuaTest.Mock.TestClass')");
				lua.DoString ("test=TestClass()");
				lua.DoString ("itest={}");
				lua.DoString ("function itest:test4(x,y) return TestClass(x+y); end");
				lua.DoString ("test=TestClass()");
				lua.DoString ("a=test:callInterface4(itest)");
				int a = (int)lua.GetNumber ("a");
				Assert.AreEqual (5, a);
				//Console.WriteLine("interface returned: "+a);
			}
		}
		/*
        * Tests passing a Lua table as an interface and
        * calling one of its methods with reference type params
        */
		[Test]
		public void NLuaReferenceTypes ()
		{
			using (Lua lua = new Lua ()) {
				lua.DoString ("luanet.load_assembly('NLuaTest')");
				lua.DoString ("TestClass=luanet.import_type('NLuaTest.Mock.TestClass')");
				lua.DoString ("test=TestClass()");
				lua.DoString ("itest={}");
				lua.DoString ("function itest:test5(x,y) return x.testval+y.testval; end");
				lua.DoString ("test=TestClass()");
				lua.DoString ("a=test:callInterface5(itest)");
				int a = (int)lua.GetNumber ("a");
				Assert.AreEqual (5, a);
				//Console.WriteLine("interface returned: "+a);
			}
		}
		/*
        * Tests passing a Lua table as an interface and
        * calling one of its methods with reference type params
        * and an out param
        */
		[Test]
		public void NLuaReferenceTypesOutParam ()
		{
			using (Lua lua = new Lua ()) {
				lua.DoString ("luanet.load_assembly('NLuaTest')");
				lua.DoString ("TestClass=luanet.import_type('NLuaTest.Mock.TestClass')");
				lua.DoString ("test=TestClass()");
				lua.DoString ("itest={}");
				lua.DoString ("function itest:test6(x) return x,TestClass(x*2); end");
				lua.DoString ("test=TestClass()");
				lua.DoString ("a=test:callInterface6(itest)");
				int a = (int)lua.GetNumber ("a");
				Assert.AreEqual (6, a);
				//Console.WriteLine("interface returned: "+a);
			}
		}
		/*
        * Tests passing a Lua table as an interface and
        * calling one of its methods with reference type params
        * and a ref param
        */
		[Test]
		public void NLuaReferenceTypesByRefParam ()
		{
			using (Lua lua = new Lua ()) {
				lua.DoString ("luanet.load_assembly('NLuaTest')");
				lua.DoString ("TestClass=luanet.import_type('NLuaTest.Mock.TestClass')");
				lua.DoString ("test=TestClass()");
				lua.DoString ("itest={}");
				lua.DoString ("function itest:test7(x,y) return TestClass(x+y.testval); end");
				lua.DoString ("a=test:callInterface7(itest)");
				int a = (int)lua.GetNumber ("a");
				Assert.AreEqual (5, a);
				//Console.WriteLine("interface returned: "+a);
			}
		}
              

#region LUA_BOILERPLATE_CLASS
		/*** This class is used to bind the .NET world with the Lua world, this boilerplate code is pratically the same, get values call Lua function return value back,
        * this class is usually dynamic generated using System.Reflection.Emit, but this will not work on iOS. */

		class LuaTestClassHandler: TestClass, ILuaGeneratedType
		{
			public LuaTable __luaInterface_luaTable;
			public Type[][] __luaInterface_returnTypes;

			public LuaTestClassHandler (LuaTable luaTable, Type[][] returnTypes)
			{
				__luaInterface_luaTable = luaTable;
				__luaInterface_returnTypes = returnTypes;
			}
                        
			public LuaTable LuaInterfaceGetLuaTable ()
			{
				return __luaInterface_luaTable;
			}

			public override int overridableMethod (int x, int y)
			{
				object [] args = new object [] {
                                        __luaInterface_luaTable,
                                        x,
                                        y
                                };
				object [] inArgs = new object [] {
                                        __luaInterface_luaTable,
                                        x,
                                        y
                                };
				int [] outArgs = new int [] { };
				Type [] returnTypes = __luaInterface_returnTypes [0];
				LuaFunction function = NLua.Method.LuaClassHelper.GetTableFunction (__luaInterface_luaTable, "overridableMethod");
				object ret = NLua.Method.LuaClassHelper.CallFunction (function, args, returnTypes, inArgs, outArgs);
				return (int)ret;
			}
		}

		class LuaITestClassHandler : ILuaGeneratedType, ITest
		{
			public LuaTable __luaInterface_luaTable;
			public Type[][] __luaInterface_returnTypes;

			public LuaITestClassHandler (LuaTable luaTable, Type[][] returnTypes)
			{
				__luaInterface_luaTable = luaTable;
				__luaInterface_returnTypes = returnTypes;
			}

			public LuaTable LuaInterfaceGetLuaTable ()
			{
				return __luaInterface_luaTable;
			}

			public int intProp {
				get {
					object [] args = new object [] { __luaInterface_luaTable };
					object [] inArgs = new object [] { __luaInterface_luaTable };
					int [] outArgs = new int [] { };
					Type [] returnTypes = __luaInterface_returnTypes [0];
					LuaFunction function = NLua.Method.LuaClassHelper.GetTableFunction (__luaInterface_luaTable, "get_intProp");
					object ret = NLua.Method.LuaClassHelper.CallFunction (function, args, returnTypes, inArgs, outArgs);
					return (int)ret;
				}
				set {
					int i = value;
					object [] args = new object [] {
						__luaInterface_luaTable ,
						i
					};
					object [] inArgs = new object [] {
						__luaInterface_luaTable,
						i
					};
					int [] outArgs = new int [] { };
					Type [] returnTypes = __luaInterface_returnTypes [1];
					LuaFunction function = NLua.Method.LuaClassHelper.GetTableFunction (__luaInterface_luaTable, "set_intProp");
					NLua.Method.LuaClassHelper.CallFunction (function, args, returnTypes, inArgs, outArgs);
				}
			}

			public TestClass refProp {
				get {
					object [] args = new object [] { __luaInterface_luaTable };
					object [] inArgs = new object [] { __luaInterface_luaTable };
					int [] outArgs = new int [] { };
					Type [] returnTypes = __luaInterface_returnTypes [2];
					LuaFunction function = NLua.Method.LuaClassHelper.GetTableFunction (__luaInterface_luaTable, "get_refProp");
					object ret = NLua.Method.LuaClassHelper.CallFunction (function, args, returnTypes, inArgs, outArgs);
					return (TestClass)ret;
				}
				set {
					TestClass test = value;
					object [] args = new object [] {
						__luaInterface_luaTable ,
						test
					};
					object [] inArgs = new object [] {
						__luaInterface_luaTable,
						test
					};
					int [] outArgs = new int [] { };
					Type [] returnTypes = __luaInterface_returnTypes [3];
					LuaFunction function = NLua.Method.LuaClassHelper.GetTableFunction (__luaInterface_luaTable, "set_refProp");
					NLua.Method.LuaClassHelper.CallFunction (function, args, returnTypes, inArgs, outArgs);
				}
			}

			public int test1 (int a, int b)
			{
				object [] args = new object [] {
                                __luaInterface_luaTable,
                                a,
                                b
                        };
				object [] inArgs = new object [] {
                                __luaInterface_luaTable,
                                a,
                                b
                        };
				int [] outArgs = new int [] { };
				Type [] returnTypes = __luaInterface_returnTypes [4];
				LuaFunction function = NLua.Method.LuaClassHelper.GetTableFunction (__luaInterface_luaTable, "test1");
				object ret = NLua.Method.LuaClassHelper.CallFunction (function, args, returnTypes, inArgs, outArgs);
				return (int)ret;
			}

			public int test2 (int a, out int b)
			{
				object [] args = new object [] {
                                        __luaInterface_luaTable,
                                        a,
                                        0
                                };
				object [] inArgs = new object [] {
                                        __luaInterface_luaTable,
                                        a
                                };
				int [] outArgs = new int [] { 1 };
				Type [] returnTypes = __luaInterface_returnTypes [5];
				LuaFunction function = NLua.Method.LuaClassHelper.GetTableFunction (__luaInterface_luaTable, "test2");
				object ret = NLua.Method.LuaClassHelper.CallFunction (function, args, returnTypes, inArgs, outArgs);
				b = (int)args [1];
				return (int)ret;
			}

			public void test3 (int a, ref int b)
			{
				object [] args = new object [] {
                                        __luaInterface_luaTable,
                                        a,
                                        b
                                };
				object [] inArgs = new object [] {
                                        __luaInterface_luaTable,
                                        a,
                                        b
                                };
				int [] outArgs = new int [] { 1 };
				Type [] returnTypes = __luaInterface_returnTypes [6];
				LuaFunction function = NLua.Method.LuaClassHelper.GetTableFunction (__luaInterface_luaTable, "test3");
				NLua.Method.LuaClassHelper.CallFunction (function, args, returnTypes, inArgs, outArgs);
				b = (int)args [1];
			}

			public TestClass test4 (int a, int b)
			{
				object [] args = new object [] {
                                        __luaInterface_luaTable,
                                        a,
                                        b
                                };
				object [] inArgs = new object [] {
                                        __luaInterface_luaTable,
                                        a,
                                        b
                                };
				int [] outArgs = new int [] { };
				Type [] returnTypes = __luaInterface_returnTypes [7];
				LuaFunction function = NLua.Method.LuaClassHelper.GetTableFunction (__luaInterface_luaTable, "test4");
				object ret = NLua.Method.LuaClassHelper.CallFunction (function, args, returnTypes, inArgs, outArgs);
				return (TestClass)ret;
			}

			public int test5 (TestClass a, TestClass b)
			{
				object [] args = new object [] {
                                        __luaInterface_luaTable,
                                        a,
                                        b
                                };
				object [] inArgs = new object [] {
                                        __luaInterface_luaTable,
                                        a,
                                        b
                                };
				int [] outArgs = new int [] { };
				Type [] returnTypes = __luaInterface_returnTypes [8];
				LuaFunction function = NLua.Method.LuaClassHelper.GetTableFunction (__luaInterface_luaTable, "test5");
				object ret = NLua.Method.LuaClassHelper.CallFunction (function, args, returnTypes, inArgs, outArgs);
				return (int)ret;
			}

			public int test6 (int a, out TestClass b)
			{
				object [] args = new object [] {
                                        __luaInterface_luaTable,
                                        a,
                                        null
                                };
				object [] inArgs = new object [] {
                                        __luaInterface_luaTable,
                                        a,
                                };
				int [] outArgs = new int [] { 1};
				Type [] returnTypes = __luaInterface_returnTypes [9];
				LuaFunction function = NLua.Method.LuaClassHelper.GetTableFunction (__luaInterface_luaTable, "test6");
				object ret = NLua.Method.LuaClassHelper.CallFunction (function, args, returnTypes, inArgs, outArgs);
				b = (TestClass)args [1];

				return (int)ret;
			}

			public void test7 (int a, ref TestClass b)
			{
				object [] args = new object [] {
                                        __luaInterface_luaTable,
                                        a,
                                        b
                                };
				object [] inArgs = new object [] {
                                        __luaInterface_luaTable,
                                        a,
                                        b
                                };
				int [] outArgs = new int [] { 1 };
				Type [] returnTypes = __luaInterface_returnTypes [10];
				LuaFunction function = NLua.Method.LuaClassHelper.GetTableFunction (__luaInterface_luaTable, "test7");
				NLua.Method.LuaClassHelper.CallFunction (function, args, returnTypes, inArgs, outArgs);
				b = (TestClass)args [1];
			}
		}
#endregion

		/*
        * Tests passing a Lua table as an interface and
        * accessing one of its value-type properties
        */
		[Test]
		public void NLuaValueProperty ()
		{
			using (Lua lua = new Lua ()) {
				lua.DoString ("luanet.load_assembly('NLuaTest')");
				lua.DoString ("TestClass=luanet.import_type('NLuaTest.Mock.TestClass')");
				lua.DoString ("test=TestClass()");
				lua.DoString ("itest={}");
				lua.DoString ("function itest:get_intProp() return itest.int_prop; end");
				lua.DoString ("function itest:set_intProp(val) itest.int_prop=val; end");
				lua.DoString ("a=test:callInterface8(itest)");
				int a = (int)lua.GetNumber ("a");
				Assert.AreEqual (3, a);
				//Console.WriteLine("interface returned: "+a);
			}
		}
		/*
        * Tests passing a Lua table as an interface and
        * accessing one of its reference type properties
        */
		[Test]
		public void NLuaReferenceProperty ()
		{
			using (Lua lua = new Lua ()) {
				lua.DoString ("luanet.load_assembly('NLuaTest')");
				lua.DoString ("TestClass=luanet.import_type('NLuaTest.Mock.TestClass')");
				lua.DoString ("test=TestClass()");
				lua.DoString ("itest={}");
				lua.DoString ("function itest:get_refProp() return TestClass(itest.int_prop); end");
				lua.DoString ("function itest:set_refProp(val) itest.int_prop=val.testval; end");
				lua.DoString ("a=test:callInterface9(itest)");
				int a = (int)lua.GetNumber ("a");
				Assert.AreEqual (3, a);
				//Console.WriteLine("interface returned: "+a);
			}
		}


		/*
        * Tests making an object from a Lua table and calling the base
        * class version of one of the methods the table overrides.
        */
		[Test]
		public void LuaTableBaseMethod ()
		{
			using (Lua lua = new Lua ()) {
				lua.RegisterLuaClassType (typeof(TestClass), typeof(LuaTestClassHandler));
				lua.DoString ("luanet.load_assembly('NLuaTest')");
				lua.DoString ("TestClass=luanet.import_type('NLuaTest.Mock.TestClass')");
				lua.DoString ("test={}");
				lua.DoString ("function test:overridableMethod(x,y) print(self[base]); return 6 end");
				lua.DoString ("luanet.make_object(test,'NLuaTest.Mock.TestClass')");
				lua.DoString ("a=TestClass.callOverridable(test,2,3)");
				int a = (int)lua.GetNumber ("a");
				lua.DoString ("luanet.free_object(test)");
				Assert.AreEqual (6, a);
				//                 lua.DoString("luanet.load_assembly('NLuaTest')");
				//                 lua.DoString("TestClass=luanet.import_type('NLuaTest.Mock.TestClass')");
				//                 lua.DoString("test={}");
				//
				//                 lua.DoString("luanet.make_object(test,'NLuaTest.Mock.TestClass')");
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
		public void GetMethodBySignatureFromObj ()
		{
			using (Lua lua = new Lua ()) {
				lua.DoString ("luanet.load_assembly('mscorlib')");
				lua.DoString ("luanet.load_assembly('NLuaTest')");
				lua.DoString ("TestClass=luanet.import_type('NLuaTest.Mock.TestClass')");
				lua.DoString ("test=TestClass()");
				lua.DoString ("setMethod=luanet.get_method_bysig(test,'setVal','System.String')");
				lua.DoString ("setMethod('test')");
				TestClass test = (TestClass)lua ["test"];
				Assert.AreEqual ("test", test.getStrVal ());
				//Console.WriteLine("interface returned: "+test.getStrVal());
			}
		}
		/*
        * Tests getting an object's method by its signature
        * (from type)
        */
		[Test]
		public void GetMethodBySignatureFromType ()
		{
			using (Lua lua = new Lua ()) {
				lua.DoString ("luanet.load_assembly('mscorlib')");
				lua.DoString ("luanet.load_assembly('NLuaTest')");
				lua.DoString ("TestClass=luanet.import_type('NLuaTest.Mock.TestClass')");
				lua.DoString ("test=TestClass()");
				lua.DoString ("setMethod=luanet.get_method_bysig(TestClass,'setVal','System.String')");
				lua.DoString ("setMethod(test,'test')");
				TestClass test = (TestClass)lua ["test"];
				Assert.AreEqual ("test", test.getStrVal ());
				//Console.WriteLine("interface returned: "+test.getStrVal());
			}
		}
		/*
        * Tests getting a type's method by its signature
        */
		[Test]
		public void GetStaticMethodBySignature ()
		{
			using (Lua lua = new Lua ()) {
				lua.DoString ("luanet.load_assembly('mscorlib')");
				lua.DoString ("luanet.load_assembly('NLuaTest')");
				lua.DoString ("TestClass=luanet.import_type('NLuaTest.Mock.TestClass')");
				lua.DoString ("make_method=luanet.get_method_bysig(TestClass,'makeFromString','System.String')");
				lua.DoString ("test=make_method('test')");
				TestClass test = (TestClass)lua ["test"];
				Assert.AreEqual ("test", test.getStrVal ());
				//Console.WriteLine("interface returned: "+test.getStrVal());
			}
		}
		/*
        * Tests getting an object's constructor by its signature
        */
		[Test]
		public void GetConstructorBySignature ()
		{
			using (Lua lua = new Lua ()) {
				lua.DoString ("luanet.load_assembly('mscorlib')");
				lua.DoString ("luanet.load_assembly('NLuaTest')");
				lua.DoString ("TestClass=luanet.import_type('NLuaTest.Mock.TestClass')");
				lua.DoString ("test_cons=luanet.get_constructor_bysig(TestClass,'System.String')");
				lua.DoString ("test=test_cons('test')");
				TestClass test = (TestClass)lua ["test"];
				Assert.AreEqual ("test", test.getStrVal ());
				//Console.WriteLine("interface returned: "+test.getStrVal());
			}
		}

		[Test]
		public void TestVarargs()
		{
			using(Lua lua = new Lua()){
				lua.DoString ("luanet.load_assembly('mscorlib')");
				lua.DoString ("luanet.load_assembly('NLuaTest')");
				lua.DoString ("TestClass=luanet.import_type('NLuaTest.Mock.TestClass')");
				lua.DoString ("test=TestClass()");
				lua.DoString ("test:Print('this will pass')");
				lua.DoString ("test:Print('this will ','fail')");
			}
		}

		[Test]
		public void TestCtype ()
		{
			using (Lua lua = new Lua ()) {
				lua.LoadCLRPackage ();
				lua.DoString ("import'System'");
				var x  = lua.DoString ("return luanet.ctype(String)")[0];
				Assert.AreEqual (x, typeof(String), "#1 String ctype test");
			}
		}

		[Test]
		public void TestPrintChars ()
		{
			using (Lua lua = new Lua ()) {
				lua.DoString (@"print(""waüäq?=()[&]ß"")");
				Assert.IsTrue (true);
			}
		}

		[Test]
		public void TestUnicodeChars ()
		{
			using (Lua lua = new Lua ()) {
				lua.LoadCLRPackage ();
				lua.DoString ("import('NLuaTest')");
				lua.DoString ("res = LuaTests.UnicodeString");
				string res = (string)lua ["res"];

				Assert.AreEqual (LuaTests.UnicodeString, res);
			}
		}

		[Test]
		public void TestUnicodeCharsInDoString()
		{
			using (Lua lua = new Lua ()) {
				lua.DoString("res = 'Файл'");
				string res = (string)lua["res"];

				Assert.AreEqual(LuaTests.UnicodeStringRussian, res);
			}
		}

		[Test]
		public void TestCoroutine ()
		{
			using (Lua lua = new Lua ()) {
				lua.LoadCLRPackage ();
				lua.RegisterFunction ("func1", null, typeof (TestClass2).GetMethod ("func"));
				lua.DoString ("function yielder() " +
								"a=1;" + "coroutine.yield();" +
								"func1(3,2);" + "coroutine.yield();" + // This line triggers System.NullReferenceException
								"a=2;" + "coroutine.yield();" +
							 "end;" +
							 "co_routine = coroutine.create(yielder);" +
							 "while coroutine.resume(co_routine) do end;");

				double num = lua.GetNumber ("a");
				//Console.WriteLine("a="+num);
				Assert.AreEqual (num, 2d);
			}
		}

		[Test]
		public void TestDebugHook ()
		{
			int [] lines = { 1, 2, 1, 3 };
			int line = 0;

			using (Lua lua = new Lua ()) {
				lua.DebugHook += (sender,args) => {
					Assert.AreEqual (args.LuaDebug.currentline,lines [line]);
					line ++;
				};
				lua.SetDebugHook (NLua.Event.EventMasks.LUA_MASKLINE, 0);

				lua.DoString (@"function testing_hooks() return 10 end
							val = testing_hooks() 
							val = val + 1");
			}
		}

		[Test]
		public void TestKeyWithDots ()
		{
			using (Lua lua = new Lua ()) {
				lua.DoString (@"g_dot = {} 
							 g_dot['key.with.dot'] = 42");

				Assert.AreEqual (42, (int)(double)lua ["g_dot.key\\.with\\.dot"]);
			}
		}
#if !WINDOWS_PHONE && !NET_3_5
		[Test]
		public void TestOperatorAdd ()
		{
			using (Lua lua = new Lua ()) {
				var a = new System.Numerics.Complex (10, 0);
				var b = new System.Numerics.Complex (0, 3);
				var x = a + b;

				lua ["a"] = a;
				lua ["b"] = b;
				var res = lua.DoString (@"return a + b") [0];
				Assert.AreEqual (x, res);
			}
		}

		[Test]
		public void TestOperatorMinus ()
		{
			using (Lua lua = new Lua ()) {
				var a = new System.Numerics.Complex (10, 0);
				var b = new System.Numerics.Complex (0, 3);
				var x = a - b;

				lua ["a"] = a;
				lua ["b"] = b;
				var res = lua.DoString (@"return a - b") [0];
				Assert.AreEqual (x, res);
			}
		}

		[Test]
		public void TestOperatorMultiply ()
		{
			using (Lua lua = new Lua ()) {
				var a = new System.Numerics.Complex (10, 0);
				var b = new System.Numerics.Complex (0, 3);
				var x = a * b;

				lua ["a"] = a;
				lua ["b"] = b;
				var res = lua.DoString (@"return a * b") [0];
				Assert.AreEqual (x, res);
			}
		}

		[Test]
		public void TestOperatorEqual ()
		{
			using (Lua lua = new Lua ()) {
				var a = new System.Numerics.Complex (10, 0);
				var b = new System.Numerics.Complex (0, 3);
				var x = a == b;

				lua ["a"] = a;
				lua ["b"] = b;
				var res = lua.DoString (@"return a == b") [0];
				Assert.AreEqual (x, res);
			}
		}

		[Test]
		public void TestOperatorNotEqual ()
		{
			using (Lua lua = new Lua ()) {
				var a = new System.Numerics.Complex (10, 0);
				var b = new System.Numerics.Complex (0, 3);
				var x = a != b;

				lua ["a"] = a;
				lua ["b"] = b;
				var res = lua.DoString (@"return a ~= b") [0];
				Assert.AreEqual (x, res);
			}
		}

		[Test]
		public void TestUnaryMinus ()
		{
			using (Lua lua = new Lua ()) {

				lua.LoadCLRPackage ();
				lua.DoString (@" import ('System.Numerics')
							  c = Complex (10, 5) 
							  c = -c ");

				var expected = new System.Numerics.Complex (-10, -5);

				var res = lua ["c"];
				Assert.AreEqual (expected, res);
			}
		}
#endif
		[Test]
		public void TestCaseFields ()
		{
			using (Lua lua = new Lua ()) {
				lua.LoadCLRPackage ();

				lua.DoString (@" import ('NLuaTest')
							  x = TestCaseName()
							  name  = x.name;
							  name2 = x.Name;
							  Name = x.Name;
							  Name2 = x.name");

				Assert.AreEqual ("name", lua ["name"]);
				Assert.AreEqual ("**name**", lua ["name2"]);
				Assert.AreEqual ("**name**", lua ["Name"]);
				Assert.AreEqual ("name", lua ["Name2"]);
			}
		}

		[Test]
		public void TestStaticOperators ()
		{
			using (Lua lua = new Lua ()) {
				lua.LoadCLRPackage ();

				lua.DoString (@" import ('NLuaTest')
							  v = Vector()
							  v.x = 10
							  v.y = 3
							  v = v*2 ");

				var v = (Vector)lua ["v"];

				Assert.AreEqual (20, v.x, "#1");
				Assert.AreEqual (6, v.y, "#2");

				lua.DoString (@" x = 2 * v");
				var x = (Vector)lua ["x"];

				Assert.AreEqual (40, x.x, "#3");
				Assert.AreEqual (12, x.y, "#4");
			}
		}

		[Test]
		public void TestExtensionMethods ()
		{
			using (Lua lua = new Lua ()) {
				lua.LoadCLRPackage ();

				lua.DoString (@" import ('NLuaTest')
							  v = Vector()
							  v.x = 10
							  v.y = 3
							  v = v*2 ");

				var v = (Vector)lua ["v"];

				double len = v.Length ();
				lua.DoString (" v:Length() ");
				lua.DoString (@" len2 = v:Length()");
				double len2 = (double)lua ["len2"];
				Assert.AreEqual (len, len2, "#1");
			}
		}
		
		[Test]
		public void TestBaseClassExtensionMethods ()
		{
			using (Lua lua = new Lua ()) {
				lua.LoadCLRPackage ();

				lua.DoString (@" import ('NLuaTest')
							  p = Employee()
							  p.firstName = 'Paulo'
							  p.occupation = 'Programmer'");

				var p = (Person)lua ["p"];

				string name = p.GetFirstName();
				lua.DoString (" p:GetFirstName() ");
				lua.DoString (@" name2 = p:GetFirstName()");
				string name2 = (string)lua ["name2"];
				Assert.AreEqual (name, name2, "#1");
			}
		}

		[Test]
		public void TestOverloadedMethods ()
		{
			using (Lua lua = new Lua ()) {
				var obj = new TestClassWithOverloadedMethod ();
				lua ["obj"] = obj;
				lua.DoString (@" 
								obj:Func (10)
								obj:Func ('10')
								obj:Func (10)
								obj:Func ('10')
								obj:Func (10)
								");
				Assert.AreEqual (3, obj.CallsToIntFunc,"#integer");
				Assert.AreEqual (2, obj.CallsToStringFunc, "#string");
			}
		}

		[Test]
		public void TestGetStack ()
		{
			using (Lua lua = new Lua ()) {
				lua.LoadCLRPackage ();
				m_lua = lua;
				lua.DoString (@" 
								import ('NLuaTest')
								function f1 ()
									 f2 ()
								 end
								 
								function f2()
									f3()
								end

								function f3()
									LuaTests.func()
								end
								
								f1 ()
								");
			}
			m_lua = null;
		}

		public static void func()
		{
#if USE_KOPILUA
			string expected = "[0] [C]:-1 -- func [field]\n[1] [string \"chunk\"]:12 -- f3 [global]\n[2] [string \"chunk\"]:8 -- f2 [global]\n[3] [string \"chunk\"]:4 -- f1 [global]\n[4] [string \"chunk\"]:15 -- <unknow> []\n";
			KopiLua.LuaDebug info = new KopiLua.LuaDebug ();
#else
			//string expected = "[0] func:-1 -- <unknown> [func]\n[1] f3:12 -- <unknown> [f3]\n[2] f2:8 -- <unknown> [f2]\n[3] f1:4 -- <unknown> [f1]\n[4] :15 --  []\n";
			KeraLua.LuaDebug info = new KeraLua.LuaDebug ();
#endif

			int level = 0;
			StringBuilder sb = new StringBuilder ();
			while (m_lua.GetStack (level,ref info) != 0) {
				m_lua.GetInfo ("nSl", ref info);
				string name = "<unknow>";
				if (info.name != null && !string.IsNullOrEmpty(info.name.ToString()))
					name = info.name.ToString ();

				sb.AppendFormat ("[{0}] {1}:{2} -- {3} [{4}]\n",
					level, info.shortsrc, info.currentline,
					name, info.namewhat);
				++level;
			}
			string x = sb.ToString ();
			Assert.True (!string.IsNullOrEmpty(x));
		}

		[Test]
		public void TestCallImplicitBaseMethod ()
		{
			using (var l = new Lua ()) {
				l.LoadCLRPackage ();
				l.DoString ("import ('NLuaTest')");
				l.DoString ("res = testClass.read() ");
				string res = (string)l ["res"];
				Assert.AreEqual (testClass.read (), res);
			}
		}

		[Test]
		public void TestPushLuaFunctionWhenReadingDelegateProperty ()
		{
			bool called = false;
			var _model = new DefaultElementModel ();
			_model.DrawMe = (x) => {
				called = true;
			};
			using (var l = new Lua ()) {
				l ["model"] = _model;
				l.DoString (@" model.DrawMe (0) ");
			}

			Assert.True (called);
		}

		[Test]
		public void TestCallDelegateWithParameters ()
		{
			string sval = "";
			int nval = 0;
			using (var l = new Lua ()) {
				Action<string,int> c = (s, n) => { sval = s; nval = n; };
				l ["d"] = c;
				l.DoString (" d ('string', 10) ");
			}

			Assert.AreEqual ("string", sval, "#1");
			Assert.AreEqual (10 , nval, "#2");
		}

		[Test]
		public void TestCallSimpleDelegate ()
		{
			bool called = false;
			using (var l = new Lua ()) {
				Action c = () => { called = true; };
				l ["d"] = c;
				l.DoString (" d () ");
			}

			Assert.True (called);
		}

		[Test]
		public void TestCallDelegateWithWrongParametersShouldFail ()
		{
			bool fail = false;
			using (var l = new Lua ()) {
				Action c = () => { fail = false; };
				l ["d"] = c;
				try {
				l.DoString (" d (10) ");
				}
				catch (LuaScriptException ) {
					fail = true;
				}
			}

			Assert.True (fail);
		}

		[Test]
		public void TestOverloadedMethodCallOnBase ()
		{
			using (var l = new Lua ()) {
				l.LoadCLRPackage ();
				l.DoString (" import ('NLuaTest') ");
				l.DoString (@"
					p=parameter()
					r1 = testClass.read(p)     -- is not working. it is also not working if the method in base class has two parameters instead of one
					r2 = testClass.read(1)     -- is working				
				");
				string r1 = (string) l ["r1"];
				string r2 = (string) l ["r2"];
				Assert.AreEqual ("parameter-field1", r1, "#1");
				Assert.AreEqual ("int-test" , r2, "#2");
			}
		}

		[Test]
		public void TestCallMethodWithParams2 ()
		{
			using (var l = new Lua ()) {
				l.LoadCLRPackage ();
				l.DoString (" import ('NLuaTest','NLuaTest.Mock') ");
				l.DoString (@"					
					r = TestClass.MethodWithParams(2)			
				");
				int r =  (int)l.GetNumber ("r");
				Assert.AreEqual (0, r, "#1");
			}
		}

	    [Test]
	    public void TestCallMethodWithParamsOptional()
	    {
	        using (var l = new Lua())
	        {
	            l.LoadCLRPackage();
	            l.DoString(" import ('NLuaTest','NLuaTest.Mock') ");
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
	            l.DoString(" import ('NLuaTest','NLuaTest.Mock') ");
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
	            l.DoString(" import ('NLuaTest','NLuaTest.Mock') ");
	            l.DoString(@"					
					r = TestClass.MethodWithObjectParams(nil, 4, 'abc')			
				");
	            int r = (int)l.GetNumber("r");
	            Assert.AreEqual(3, r, "#1");
	        }
	    }

        [Test]
		public void TestConstructorOverload ()
		{
			using (var l = new Lua ()) {
				l.LoadCLRPackage ();
				l.DoString (" import ('NLuaTest','NLuaTest.Mock') ");
				l.DoString (@"					
					e1 = Entity()
					e2 = Entity ('str_param')
					e3 = Entity (10)
					p1 = e1.Property
					p2 = e2.Property
					p3 = e3.Property
				");
				string p1 = l.GetString ("p1");
				string p2 = l.GetString ("p2");
				string p3 = l.GetString ("p3");
				Assert.AreEqual ("Default", p1, "#1");
				Assert.AreEqual ("String", p2, "#1");
				Assert.AreEqual ("Int", p3, "#1");
			}
		}

		static Lua m_lua;
					
	}
}
