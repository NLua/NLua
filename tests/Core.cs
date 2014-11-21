
using System;
using NLua;
using NLua.Exceptions;
using System.IO;

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
using Foundation;
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
			lua.RegisterFunction ("WriteLineString", typeof (Console).GetMethod ("WriteLine", new Type [] { typeof (String) }));
			
			lua.DoString (@"
			function print (param)
				WriteLineString (tostring(param))
			end
			");
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
		}

		[Test]
		public void CF ()
		{
			TestLuaFile ("cf");
		}
		
		[Test]
		[Ignore]
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
		[Ignore]
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
		[Ignore]
		public void TraceGlobals ()
		{
			TestLuaFile ("trace-globals");
		}
	}
}
