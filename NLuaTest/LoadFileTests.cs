using System;
using NUnit.Framework;
using NLua;

using NLuaTest.Mock;

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
// 			using (Lua lua = new Lua ()) {
// 				lua.LoadCLRPackage ();
// 
// 				lua.LoadFile ("test.luac");
// 
// 				int width = (int)(double)lua ["width"];
// 				int height = (int)(double)lua ["height"];
// 				string message = (string)lua ["message"];
// 				int color_g	= (int)(double)lua ["color.g"];
// 				LuaFunction func = (LuaFunction)lua ["func"];
// 				object[] res = func.Call (12, 34);
// 				int x = (int)(double)res [0];
// 				int y = (int)(double)res [1];
// 				//function func(x,y)
// 				//	return x,x+y
// 				//end
// 
// 				Assert.AreEqual (100, width);
// 				Assert.AreEqual (200, height);
// 				Assert.AreEqual ("Hello World!", message);
// 				Assert.AreEqual (20, color_g);
// 				Assert.AreEqual (12, x);
// 				Assert.AreEqual (46, y);
// 			}
		}

//		[Test]
//		public void TestBinaryLoadFile ()
//		{
//			using (Lua lua = new Lua ()) {
//				lua.LoadCLRPackage ();
//
//				lua.LoadFile ("test.luac");
//
//				int width = (int)(double)lua ["width"];
//				int height = (int)(double)lua ["height"];
//				string message = (string)lua ["message"];
//				int color_g	= (int)(double)lua ["color.g"];
//				LuaFunction func = (LuaFunction)lua ["func"];
//				object[] res = func.Call (12, 34);
//				int x = (int)(double)res [0];
//				int y = (int)(double)res [1];
//				//function func(x,y)
//				//	return x,x+y
//				//end
//
//				Assert.AreEqual (100, width);
//				Assert.AreEqual (200, height);
//				Assert.AreEqual ("Hello World!", message);
//				Assert.AreEqual (20, color_g);
//				Assert.AreEqual (12, x);
//				Assert.AreEqual (46, y);
//			}
//		}
	}
}

