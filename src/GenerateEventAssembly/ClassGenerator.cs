using System;
using LuaState = KeraLua.Lua;

namespace NLua
{
    class ClassGenerator
    {
        private ObjectTranslator translator;
        private Type klass;

        public ClassGenerator(ObjectTranslator objTranslator, Type typeClass)
        {
            translator = objTranslator;
            klass = typeClass;
        }

        public object ExtractGenerated(LuaState luaState, int stackPos)
        {
            return CodeGeneration.Instance.GetClassInstance(klass, translator.GetTable(luaState, stackPos));
        }
    }
}