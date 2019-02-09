using System;
using System.Collections.Generic;
using System.Text;

namespace NLuaTest.TestTypes
{
    class LuaEventArgsHandler : NLua.Method.LuaDelegate
    {
        void CallFunction(object sender, EventArgs eventArgs)
        {
            object[] args = { sender, eventArgs };
            object[] inArgs = { sender, eventArgs };
            int[] outArgs = { };
            CallFunction(args, inArgs, outArgs);
        }
    }
}
