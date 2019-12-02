using System;
using System.Reflection;

namespace NLua.Method
{
    class RegisterEventHandler
    {
        private readonly EventHandlerContainer _pendingEvents;
        private readonly EventInfo _eventInfo;
        private readonly object _target;

        public RegisterEventHandler(EventHandlerContainer pendingEvents, object target, EventInfo eventInfo)
        {
            _target = target;
            _eventInfo = eventInfo;
            _pendingEvents = pendingEvents;
        }

        /*
         * Adds a new event handler
         */
        public Delegate Add(LuaFunction function)
        {
            Delegate handlerDelegate = CodeGeneration.Instance.GetDelegate(_eventInfo.EventHandlerType, function);
            return Add(handlerDelegate);
        }

        public Delegate Add(Delegate handlerDelegate)
        {
            _eventInfo.AddEventHandler(_target, handlerDelegate);
            _pendingEvents.Add(handlerDelegate, this);

            return handlerDelegate;
        }

        /*
         * Removes an existing event handler
         */
        public void Remove(Delegate handlerDelegate)
        {
            RemovePending(handlerDelegate);
            _pendingEvents.Remove(handlerDelegate);
        }

        /*
         * Removes an existing event handler (without updating the pending handlers list)
         */
        internal void RemovePending(Delegate handlerDelegate)
        {
            _eventInfo.RemoveEventHandler(_target, handlerDelegate);
        }
    }
}