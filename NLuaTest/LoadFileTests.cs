using System;
using NLua;

using NLuaTest.Mock;

#if WINDOWS_PHONE
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using SetUp = Microsoft.VisualStudio.TestPlatform.UnitTestFramework.TestInitializeAttribute;
using TearDown = Microsoft.VisualStudio.TestPlatform.UnitTestFramework.TestCleanupAttribute;
using TestFixture = Microsoft.VisualStudio.TestPlatform.UnitTestFramework.TestClassAttribute;
using Test = Microsoft.VisualStudio.TestPlatform.UnitTestFramework.TestMethodAttribute;
#else
using NUnit.Framework;
#endif

#if MONOTOUCH
using MonoTouch.Foundation;
using MonoTouch;
#endif

namespace LoadFileTests
{
	[TestFixture]
	#if MONOTOUCH
	[Preserve (AllMembers = true)]
	#endif
	public class LoadLuaFile
	{
		/*
        * Tests capturing an exception
        */
		[Test]
		public void TestLoadFile ()
		{
			using (Lua lua = new Lua ()) {
				lua.LoadCLRPackage ();

				lua.DoFile ("test.lua");

				int width = (int)(double)lua ["width"];
				int height = (int)(double)lua ["height"];
				string message = (string)lua ["message"];
				int color_g	= (int)(double)lua ["color.g"];
				LuaFunction func = (LuaFunction)lua ["func"];
				object[] res = func.Call (12, 34);
				int x = (int)(double)res [0];
				int y = (int)(double)res [1];
				//function func(x,y)
				//	return x,x+y
				//end

				Assert.AreEqual (100, width);
				Assert.AreEqual (200, height);
				Assert.AreEqual ("Hello World!", message);
				Assert.AreEqual (20, color_g);
				Assert.AreEqual (12, x);
				Assert.AreEqual (46, y);
			}
		}


		[Test]
		public void TestBinaryLoadFile ()
		{
			using (Lua lua = new Lua ()) {
				lua.LoadCLRPackage ();
				if (IntPtr.Size == 4)
					lua.DoFile ("test_32.luac");
				else
					lua.DoFile ("test_64.luac");

				int width = (int)(double)lua ["width"];
				int height = (int)(double)lua ["height"];
				string message = (string)lua ["message"];
				int color_g	= (int)(double)lua ["color.g"];
				LuaFunction func = (LuaFunction)lua ["func"];
				object[] res = func.Call (12, 34);
				int x = (int)(double)res [0];
				int y = (int)(double)res [1];
				//function func(x,y)
				//	return x,x+y
				//end

				Assert.AreEqual (100, width);
				Assert.AreEqual (200, height);
				Assert.AreEqual ("Hello World!", message);
				Assert.AreEqual (20, color_g);
				Assert.AreEqual (12, x);
				Assert.AreEqual (46, y);
			}
		}
	}
}

