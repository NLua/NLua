
using System;
using NLua;
using System.IO;
using LoadFileTests;
using NUnit.Framework;

#if __IOS__ || __TVOS__ || __WATCHOS__
using Foundation;
#endif

namespace NLuaTest
{
    [TestFixture]
#if __IOS__ || __TVOS__ || __WATCHOS__
    [Preserve (AllMembers = true)]
#endif
    public class Core
    {
        Lua _lua;

        string GetTestPath(string name)
        {
            string core = LoadLuaFile.GetScriptsPath("core");
            return Path.Combine(core, name + ".lua");
        }

        void AssertFile(string path)
        {
            _lua.DoFile(path);
        }

        void TestLuaFile(string name)
        {
            string path = GetTestPath(name);
            AssertFile(path);
        }

        [SetUp]
        public void Setup()
        {
            _lua = new Lua();
            _lua.RegisterFunction("WriteLineString", typeof(Console).GetMethod("WriteLine", new Type[] { typeof(String) }));

            _lua.DoString(@"
            function print (param)
                WriteLineString (tostring(param))
            end
            ");
        }

        [TearDown]
        public void TearDown()
        {
            _lua.Dispose();
            _lua = null;
        }

        [Test]
        public void Bisect()
        {
            TestLuaFile("bisect");
        }

        [Test]
        public void CF()
        {
            TestLuaFile("cf");
        }


        [Test]
        public void Factorial()
        {
            TestLuaFile("factorial");
        }

        [Test]
        public void FibFor()
        {
            TestLuaFile("fibfor");
        }

        [Test]
        public void Life()
        {
            TestLuaFile("life");
        }

        [Test]
        public void Printf()
        {
            TestLuaFile("printf");
        }


        [Test]
        public void Sieve()
        {
            TestLuaFile("sieve");
        }

        [Test]
        public void Sort()
        {
            TestLuaFile("sort");
        }
    }
}
