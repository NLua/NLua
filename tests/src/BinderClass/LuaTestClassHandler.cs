using System;
using NLua;

namespace NLuaTest.TestTypes
{
    class LuaTestClassHandler : TestClass, ILuaGeneratedType
    {
        public LuaTable __luaInterface_luaTable;
        public Type[][] __luaInterface_returnTypes;

        public LuaTestClassHandler(LuaTable luaTable, Type[][] returnTypes)
        {
            __luaInterface_luaTable = luaTable;
            __luaInterface_returnTypes = returnTypes;
        }

        public LuaTable LuaInterfaceGetLuaTable()
        {
            return __luaInterface_luaTable;
        }

        public override int overridableMethod(int x, int y)
        {
            object[] args = new object[] {
                __luaInterface_luaTable,
                x,
                y
            };
            object[] inArgs = new object[] {
                __luaInterface_luaTable,
                x,
                y
            };
            int[] outArgs = new int[] { };
            Type[] returnTypes = __luaInterface_returnTypes[0];
            LuaFunction function = NLua.Method.LuaClassHelper.GetTableFunction(__luaInterface_luaTable, "overridableMethod");
            object ret = NLua.Method.LuaClassHelper.CallFunction(function, args, returnTypes, inArgs, outArgs);
            return (int)ret;
        }
    }

}
