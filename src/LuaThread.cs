
using System;
using System.Collections;

using NLua.Extensions;

using LuaState = KeraLua.Lua;

namespace NLua
{
    public class LuaThread : LuaBase
    {
        private LuaState _luaState;
        private ObjectTranslator _translator;

        public LuaState State => _luaState;

        /// <summary>
        /// Get the main thread object
        /// </summary>
        public LuaThread MainThread
        {
            get
            {
                LuaState mainThread = _luaState.MainThread;
                int oldTop = mainThread.GetTop();
                mainThread.PushThread();
                object returnValue = _translator.GetObject(mainThread, -1);

                mainThread.SetTop(oldTop);
                return (LuaThread)returnValue;
            }
        }

        public LuaThread(int reference, Lua interpreter): base(reference, interpreter)
        {
            _luaState = interpreter.GetThreadState(reference);
            _translator = interpreter.Translator;
        }

        /*
         * Resets this thread, cleaning its call stack and closing all pending to-be-closed variables.
         */
        public int Reset()
        {
            int oldTop = _luaState.GetTop();

            int statusCode = _luaState.ResetThread();  /* close its tbc variables */

            _luaState.SetTop(oldTop);
            return statusCode;
        }

        public void XMove(LuaState to, object val, int index = 1)
        {
            int oldTop = _luaState.GetTop();

            _translator.Push(_luaState, val);
            _luaState.XMove(to, index);

            _luaState.SetTop(oldTop);
        }

        public void XMove(Lua to, object val, int index = 1)
        {
            int oldTop = _luaState.GetTop();

            _translator.Push(_luaState, val);
            _luaState.XMove(to.State, index);

            _luaState.SetTop(oldTop);
        }

        public void XMove(LuaThread thread, object val, int index = 1)
        {
            int oldTop = _luaState.GetTop();

            _translator.Push(_luaState, val);
            _luaState.XMove(thread.State, index);

            _luaState.SetTop(oldTop);
        }

        /*
         * Pushes this thread into the Lua stack
         */
        internal void Push(LuaState luaState)
        {
            luaState.GetRef(_Reference);
        }

        public override string ToString()
        {
            return "thread";
        }
    }
}
