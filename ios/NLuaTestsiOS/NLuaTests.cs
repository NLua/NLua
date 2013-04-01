using System;
using NUnit.Framework;
using NLua;

using NLuaTest.Mock;

#if MONOTOUCH
using MonoTouch.Foundation;
using MonoTouch;
#endif

namespace NLuaTest
{
	[TestFixture]
	#if MONOTOUCH
	[Preserve (AllMembers = true)]
	#endif
	public class ANLuaTests
	{
		/*
        * Tests capturing an exception
        */
		[Test]
		public void TestCLRPackage ()
		{
			using (Lua lua = new Lua ()) {
				lua.LoadCLRPackage ();

				lua.DoString ("import ('NLuaTest', 'NLuaTest.Mock') ");
				lua.DoString ("test = TestClass()");
				lua.DoString ("test:setVal(3)");
				object[] res = lua.DoString ("return test");
				TestClass test = (TestClass)res [0];
				Assert.AreEqual (3, test.testval);
			}
		}

#if MONOTOUCH
		[Test]
		public void TestUseNSUrl ()
		{
			using (Lua lua = new Lua ()) {
				lua.LoadCLRPackage ();

				lua.DoString ("import ('monotouch', 'MonoTouch.Foundation') ");
				lua.DoString ("testURL = NSUrl('http://nlua.org/?query=param')");
				lua.DoString ("host = testURL.Host");

				object res = lua["host"];
				string host = (string)res;
				Assert.AreEqual ("nlua.org", host);
			}
		}
#endif
	}
}

