
using System;
using NUnit.Framework;
using NLua;
using NLua.Exceptions;
using System.IO;

#if MONOTOUCH
using MonoTouch.Foundation;
#endif

namespace NLuaTest
{
	[TestFixture]
	#if MONOTOUCH
	[Preserve (AllMembers = true)]
	#endif
	public class Core
	{
		Lua lua = null;

		string GetTestPath(string name)
		{
			string filePath = Path.Combine (Path.Combine ("LuaTests", "core"), name + ".lua");
			return filePath;
		}
		
		void AssertFile (string path)
		{
				lua.DoFile (path);
		}
		
		void TestLuaFile (string name)
		{
			string path = GetTestPath (name);
			AssertFile (path);
		}

		[SetUp]
		public void Setup()
		{
			lua = new Lua ();
			lua.RegisterFunction ("print", typeof (Console).GetMethod ("WriteLine", new Type [] { typeof(String) }));
		}
		
		[TearDown]
		public void TearDown ()
		{
			lua.Dispose ();
			lua = null;
		}

		[Test]
		public void Bisect ()
		{
			TestLuaFile ("bisect");
			Assert.True (true);
		}

		[Test]
		public void CF ()
		{
			TestLuaFile ("cf");
		}
		
		[Test]
		public void Env ()
		{
			TestLuaFile ("env");
		}
		
		[Test]
		public void Factorial ()
		{
			TestLuaFile ("factorial");
		}
		
		[Test]
		public void FibFor ()
		{
			TestLuaFile ("fibfor");
		}
		
		[Test]
		public void Life ()
		{
			TestLuaFile ("life");
		}
		
		[Test]
		public void Printf ()
		{
			TestLuaFile ("printf");
		}
		
		[Test]
		public void ReadOnly ()
		{
			TestLuaFile ("readonly");
		}
		
		[Test]
		public void Sieve ()
		{
			TestLuaFile ("sieve");
		}
		
		[Test]
		public void Sort ()
		{
			TestLuaFile ("sort");
		}
		
		[Test]
		public void TraceGlobals ()
		{
			TestLuaFile ("trace-globals");
		}
	}
}
