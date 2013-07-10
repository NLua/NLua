using System;
using System.Text;
using System.Collections.Generic;
using NUnit.Framework;
using NLuaTest.Mock;
using System.Reflection;
using System.Threading;
using NLua;
using NLua.Exceptions;


namespace NLuaTest
{
	[TestFixture]
	public class AAACodeGenTests
	{
		/*
        * Tests passing a Lua function to a delegate
        * with value-type arguments
        */
		[Test]
		public void LuaDelegateValueTypes ()
		{
			using (Lua lua = new Lua ()) {
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
        * Tests making an object from a Lua table and calling the base
        * class version of one of the methods the table overrides.
        */
		[Test]
		public void LuaTableBaseMethod ()
		{
			using (Lua lua = new Lua ()) {
				lua.DoString ("luanet.load_assembly('NLuaTest')");
				lua.DoString ("TestClass=luanet.import_type('NLuaTest.Mock.TestClass')");
				lua.DoString ("test={}");
				lua.DoString ("function test:overridableMethod(x,y) print(self[base]); return 6 end");
				lua.DoString ("luanet.make_object(test,'NLuaTest.Mock.TestClass')");
				lua.DoString ("a=TestClass.callOverridable(test,2,3)");
				int a = (int)lua.GetNumber ("a");
				lua.DoString ("luanet.free_object(test)");
				Assert.AreEqual (6, a);
			}
		}
	}
}

