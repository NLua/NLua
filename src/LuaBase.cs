using System;

namespace NLua
{
    /// <summary>
    /// Base class to provide consistent disposal flow across lua objects. Uses code provided by Yves Duhoux and suggestions by Hans Schmeidenbacher and Qingrui Li 
    /// </summary>
    public abstract class LuaBase : IDisposable
    {
        private bool _disposed;
        protected readonly int _Reference;
        protected Lua _Interpreter;

        protected LuaBase(int reference)
        {
            _Reference = reference;
        }

        ~LuaBase()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public virtual void Dispose(bool disposeManagedResources)
        {
            if (_disposed)
                return;

            if (_Reference != 0 && disposeManagedResources)
                _Interpreter.DisposeInternal(_Reference);

            _Interpreter = null;
            _disposed = true;
        }

        public override bool Equals(object o)
        {
            var reference = o as LuaBase;
            if (reference == null)
                return false;

            return _Interpreter.CompareRef(reference._Reference, _Reference);
        }

        public override int GetHashCode()
        {
            return _Reference;
        }
    }
}