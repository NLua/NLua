using System;
using System.Collections.Generic;
using NLua;
using NLua.Exceptions;
using NUnit.Framework;

namespace NLuaTest
{
    [TestFixture]
    public class InnerExceptionPreservationTests
    {
        private class CustomIndexerException : InvalidOperationException
        {
            public CustomIndexerException(string message) : base(message) { }
        }

        private class ThrowingIndexer
        {
            public object this[string key] => throw new CustomIndexerException("custom: " + key);
        }

        [Test]
        public void GetItem_AbsentKey_KeyNotFoundException_IsPreservedAsInner()
        {
            using (Lua lua = new Lua())
            {
                var dict = new Dictionary<string, object>();
                dict["present"] = 1;
                lua["d"] = dict;

                var ex = Assert.Throws<LuaScriptException>(() =>
                    lua.DoString("return d['absent']"));

                StringAssert.Contains("absent", ex.Message);
                StringAssert.Contains("not found", ex.Message);
                Assert.IsTrue(ex.IsNetException);
                Assert.IsInstanceOf<KeyNotFoundException>(ex.InnerException);
            }
        }

        [Test]
        public void GetItem_AbsentKey_MemberSyntax_KeyNotFoundIsPreserved()
        {
            using (Lua lua = new Lua())
            {
                var dict = new Dictionary<string, object>();
                dict["present"] = 1;
                lua["d"] = dict;

                var ex = Assert.Throws<LuaScriptException>(() =>
                    lua.DoString("return d.absent"));

                Assert.IsInstanceOf<KeyNotFoundException>(ex.InnerException);
            }
        }

        [Test]
        public void GetItem_CustomIndexerException_IsPreservedAsInner()
        {
            using (Lua lua = new Lua())
            {
                lua["obj"] = new ThrowingIndexer();

                var ex = Assert.Throws<LuaScriptException>(() =>
                    lua.DoString("return obj['x']"));

                StringAssert.Contains("exception indexing", ex.Message);
                StringAssert.Contains("'x'", ex.Message);
                Assert.IsTrue(ex.IsNetException);
                Assert.IsInstanceOf<CustomIndexerException>(ex.InnerException);
                StringAssert.Contains("custom:", ex.InnerException.Message);
            }
        }

        [Test]
        public void LuaScriptException_NewCtor_SetsMessageSourceAndInner()
        {
            var inner = new InvalidOperationException("some CLR failure");
            var ex = new LuaScriptException("readable message", "source-location", inner);

            Assert.AreEqual("readable message", ex.Message);
            Assert.AreEqual("source-location", ex.Source);
            Assert.AreSame(inner, ex.InnerException);
            Assert.IsTrue(ex.IsNetException);
        }
    }
}
