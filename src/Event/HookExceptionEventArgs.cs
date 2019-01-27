using System;

namespace NLua.Event
{
    public class HookExceptionEventArgs : EventArgs
    {
        private readonly Exception m_Exception;

        public Exception Exception {
            get { return m_Exception; }
        }

        public HookExceptionEventArgs(Exception ex)
        {
            m_Exception = ex;
        }
    }
}