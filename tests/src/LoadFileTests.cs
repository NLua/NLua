using System;
using System.IO;
using NLua;


using NUnit.Framework;

#if __IOS__ || __TVOS__ || __WATCHOS__
using Foundation;
#endif

namespace LoadFileTests
{
    [TestFixture]
#if __IOS__ || __TVOS__ || __WATCHOS__
    [Preserve (AllMembers = true)]
#endif
    public class LoadLuaFile
    {
        public static  string GetScriptsPath(string name)
        {
            string path = new Uri(typeof(LoadLuaFile).Assembly.CodeBase).AbsolutePath;
            path = Path.GetDirectoryName(path);
            path = Path.Combine(path, "scripts");
            path = Path.Combine (path, name);
            return path;
        }
        /*
        * Tests capturing an exception
        */
        [Test]
        public void TestLoadFile()
        {
            using (Lua lua = new Lua())
            {
                lua.LoadCLRPackage();

                string file = GetScriptsPath("test.lua");

                lua.DoFile(file);

                int width = (int)(double)lua["width"];
                int height = (int)(double)lua["height"];
                string message = (string)lua["message"];
                int color_g = (int)(double)lua["color.g"];
                var func = (LuaFunction)lua["func"];
                object[] res = func.Call(12, 34);
                int x = (int)(long)res[0];
                int y = (int)(long)res[1];
                //function func(x,y)
                //	return x,x+y
                //end

                Assert.AreEqual(100, width);
                Assert.AreEqual(200, height);
                Assert.AreEqual("Hello World!", message);
                Assert.AreEqual(20, color_g);
                Assert.AreEqual(12, x);
                Assert.AreEqual(46, y);
            }
        }


        [Test]
        public void TestBinaryLoadFile()
        {
            using (Lua lua = new Lua())
            {
                string file;
                lua.LoadCLRPackage();

                if (IntPtr.Size == 4)
                    file = GetScriptsPath("test_32.luac");
                else
                    file = GetScriptsPath("test_64.luac");

                lua.DoFile(file);

                int width = (int)(double)lua["width"];
                int height = (int)(double)lua["height"];
                string message = (string)lua["message"];
                int color_g = (int)(double)lua["color.g"];
                var func = (LuaFunction)lua["func"];
                object[] res = func.Call(12, 34);
                int x = (int)(long)res[0];
                int y = (int)(long)res[1];

                Assert.AreEqual(100, width);
                Assert.AreEqual(200, height);
                Assert.AreEqual("Hello World!", message);
                Assert.AreEqual(20, color_g);
                Assert.AreEqual(12, x);
                Assert.AreEqual(46, y);
            }
        }
    }
}

