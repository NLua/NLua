using System;
using LuaState = KeraLua.Lua;

namespace NLua
{
    class DelegateGenerator
    {
        private ObjectTranslator translator;
        private Type delegateType;


        public DelegateGenerator(ObjectTranslator objectTranslator, Type type)
        {
            translator = objectTranslator;
            delegateType = type;
        }

        public object ExtractGenerated(LuaState luaState, int stackPos)
        {
            return CodeGeneration.Instance.GetDelegate(delegateType, translator.GetFunction(luaState, stackPos));
        }
    }
}