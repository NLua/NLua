
using System;
using System.Collections;

using NLua.Extensions;

using LuaState = KeraLua.Lua;

namespace NLua
{
    public class LuaThread : LuaBase, IEquatable<LuaThread>, IEquatable<LuaState>, IEquatable<Lua>
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

        public override bool Equals(object obj)
        {
            if (obj is LuaThread thread)
                return this.State == thread.State;
            else if (obj is Lua interpreter)
                return this.State == interpreter.State;
            else if (obj is LuaState state)
                return this.State == state;
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public bool Equals(LuaThread other) => this.State == other.State;
        public bool Equals(LuaState other) => this.State == other;
        public bool Equals(Lua other) => this.State == other.State;

        public static explicit operator LuaState(LuaThread thread) => thread.State;
        public static explicit operator LuaThread(Lua interpreter) => interpreter.Thread;

        public static bool operator ==(LuaThread threadA, LuaThread threadB) => threadA.State == threadB.State;
        public static bool operator !=(LuaThread threadA, LuaThread threadB) => threadA.State != threadB.State;

        public static bool operator ==(LuaThread thread, LuaState state) => thread.State == state;
        public static bool operator !=(LuaThread thread, LuaState state) => thread.State != state;
        public static bool operator ==(LuaState state, LuaThread thread) => state == thread.State;
        public static bool operator !=(LuaState state, LuaThread thread) => state != thread.State;

        public static bool operator ==(LuaThread thread, Lua interpreter) => thread.State == interpreter.State;
        public static bool operator !=(LuaThread thread, Lua interpreter) => thread.State != interpreter.State;
        public static bool operator ==(Lua interpreter, LuaThread thread) => interpreter.State == thread.State;
        public static bool operator !=(Lua interpreter, LuaThread thread) => interpreter.State != thread.State;
    }
}
