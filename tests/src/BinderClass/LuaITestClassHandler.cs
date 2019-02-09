using System;
using NLua;

namespace NLuaTest.TestTypes
{
        class LuaITestClassHandler : ILuaGeneratedType, ITest
        {
            public LuaTable __luaInterface_luaTable;
            public Type[][] __luaInterface_returnTypes;

            public LuaITestClassHandler(LuaTable luaTable, Type[][] returnTypes)
            {
                __luaInterface_luaTable = luaTable;
                __luaInterface_returnTypes = returnTypes;
            }

            public LuaTable LuaInterfaceGetLuaTable()
            {
                return __luaInterface_luaTable;
            }

            public int intProp
            {
                get
                {
                    object[] args =  { __luaInterface_luaTable };
                    object[] inArgs = { __luaInterface_luaTable };
                    int[] outArgs =  { };
                    Type[] returnTypes = __luaInterface_returnTypes[0];
                    LuaFunction function = NLua.Method.LuaClassHelper.GetTableFunction(__luaInterface_luaTable, "get_intProp");
                    object ret = NLua.Method.LuaClassHelper.CallFunction(function, args, returnTypes, inArgs, outArgs);
                    return (int)ret;
                }
                set
                {
                    int i = value;
                    object[] args =  {
                        __luaInterface_luaTable ,
                        i
                    };
                    object[] inArgs =  {
                        __luaInterface_luaTable,
                        i
                    };
                    int[] outArgs = { };
                    Type[] returnTypes = __luaInterface_returnTypes[1];
                    LuaFunction function = NLua.Method.LuaClassHelper.GetTableFunction(__luaInterface_luaTable, "set_intProp");
                    NLua.Method.LuaClassHelper.CallFunction(function, args, returnTypes, inArgs, outArgs);
                }
            }

            public TestClass refProp
            {
                get
                {
                    object[] args =  { __luaInterface_luaTable };
                    object[] inArgs =  { __luaInterface_luaTable };
                    int[] outArgs =  { };
                    Type[] returnTypes = __luaInterface_returnTypes[2];
                    LuaFunction function = NLua.Method.LuaClassHelper.GetTableFunction(__luaInterface_luaTable, "get_refProp");
                    object ret = NLua.Method.LuaClassHelper.CallFunction(function, args, returnTypes, inArgs, outArgs);
                    return (TestClass)ret;
                }
                set
                {
                    TestClass test = value;
                    object[] args =  {
                        __luaInterface_luaTable ,
                        test
                    };
                    object[] inArgs =  {
                        __luaInterface_luaTable,
                        test
                    };
                    int[] outArgs =  { };
                    Type[] returnTypes = __luaInterface_returnTypes[3];
                    LuaFunction function = NLua.Method.LuaClassHelper.GetTableFunction(__luaInterface_luaTable, "set_refProp");
                    NLua.Method.LuaClassHelper.CallFunction(function, args, returnTypes, inArgs, outArgs);
                }
            }

            public int test1(int a, int b)
            {
                object[] args = {
                                __luaInterface_luaTable,
                                a,
                                b
                        };
                object[] inArgs =  {
                                __luaInterface_luaTable,
                                a,
                                b
                        };
                int[] outArgs =  { };
                Type[] returnTypes = __luaInterface_returnTypes[4];
                LuaFunction function = NLua.Method.LuaClassHelper.GetTableFunction(__luaInterface_luaTable, "test1");
                object ret = NLua.Method.LuaClassHelper.CallFunction(function, args, returnTypes, inArgs, outArgs);
                return (int)ret;
            }

            public int test2(int a, out int b)
            {
                object[] args = {
                                        __luaInterface_luaTable,
                                        a,
                                        0
                                };
                object[] inArgs =  {
                                        __luaInterface_luaTable,
                                        a
                                };
                int[] outArgs =  { 1 };
                Type[] returnTypes = __luaInterface_returnTypes[5];
                LuaFunction function = NLua.Method.LuaClassHelper.GetTableFunction(__luaInterface_luaTable, "test2");
                object ret = NLua.Method.LuaClassHelper.CallFunction(function, args, returnTypes, inArgs, outArgs);
                b = (int)args[1];
                return (int)ret;
            }

            public void test3(int a, ref int b)
            {
                object[] args =  {
                                        __luaInterface_luaTable,
                                        a,
                                        b
                                };
                object[] inArgs =  {
                                        __luaInterface_luaTable,
                                        a,
                                        b
                                };
                int[] outArgs =  { 1 };
                Type[] returnTypes = __luaInterface_returnTypes[6];
                LuaFunction function = NLua.Method.LuaClassHelper.GetTableFunction(__luaInterface_luaTable, "test3");
                NLua.Method.LuaClassHelper.CallFunction(function, args, returnTypes, inArgs, outArgs);
                b = (int)args[1];
            }

            public TestClass test4(int a, int b)
            {
                object[] args =  {
                                        __luaInterface_luaTable,
                                        a,
                                        b
                                };
                object[] inArgs = {
                                        __luaInterface_luaTable,
                                        a,
                                        b
                                };
                int[] outArgs =  { };
                Type[] returnTypes = __luaInterface_returnTypes[7];
                LuaFunction function = NLua.Method.LuaClassHelper.GetTableFunction(__luaInterface_luaTable, "test4");
                object ret = NLua.Method.LuaClassHelper.CallFunction(function, args, returnTypes, inArgs, outArgs);
                return (TestClass)ret;
            }

            public int test5(TestClass a, TestClass b)
            {
                object[] args = {
                                        __luaInterface_luaTable,
                                        a,
                                        b
                                };
                object[] inArgs =  {
                                        __luaInterface_luaTable,
                                        a,
                                        b
                                };
                int[] outArgs =  { };
                Type[] returnTypes = __luaInterface_returnTypes[8];
                LuaFunction function = NLua.Method.LuaClassHelper.GetTableFunction(__luaInterface_luaTable, "test5");
                object ret = NLua.Method.LuaClassHelper.CallFunction(function, args, returnTypes, inArgs, outArgs);
                return (int)ret;
            }

            public int test6(int a, out TestClass b)
            {
                object[] args =  {
                                        __luaInterface_luaTable,
                                        a,
                                        null
                                };
                object[] inArgs = {
                                        __luaInterface_luaTable,
                                        a,
                                };
                int[] outArgs =  { 1 };
                Type[] returnTypes = __luaInterface_returnTypes[9];
                LuaFunction function = NLua.Method.LuaClassHelper.GetTableFunction(__luaInterface_luaTable, "test6");
                object ret = NLua.Method.LuaClassHelper.CallFunction(function, args, returnTypes, inArgs, outArgs);
                b = (TestClass)args[1];

                return (int)ret;
            }

            public void test7(int a, ref TestClass b)
            {
                object[] args =  {
                                        __luaInterface_luaTable,
                                        a,
                                        b
                                };
                object[] inArgs =  {
                                        __luaInterface_luaTable,
                                        a,
                                        b
                                };
                int[] outArgs =  { 1 };
                Type[] returnTypes = __luaInterface_returnTypes[10];
                LuaFunction function = NLua.Method.LuaClassHelper.GetTableFunction(__luaInterface_luaTable, "test7");
                NLua.Method.LuaClassHelper.CallFunction(function, args, returnTypes, inArgs, outArgs);
                b = (TestClass)args[1];
            }
        }
}
