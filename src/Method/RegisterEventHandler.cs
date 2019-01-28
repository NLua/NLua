using System;
using System.Reflection;

namespace NLua.Method
{
    class RegisterEventHandler
    {
        private EventHandlerContainer pendingEvents;
        private EventInfo eventInfo;
        private object target;

        public RegisterEventHandler(EventHandlerContainer pendingEvents, object target, EventInfo eventInfo)
        {
            this.target = target;
            this.eventInfo = eventInfo;
            this.pendingEvents = pendingEvents;
        }

        /*
         * Adds a new event handler
         */
        public Delegate Add(LuaFunction function)
        {
            //CP: Fix by Ben Bryant for event handling with one parameter
            //link: http://luaforge.net/forum/message.php?msg_id=9266
            Delegate handlerDelegate = CodeGeneration.Instance.GetDelegate(eventInfo.EventHandlerType, function);
            eventInfo.AddEventHandler(target, handlerDelegate);
            pendingEvents.Add(handlerDelegate, this);

            return handlerDelegate;
        }

        /*
         * Removes an existing event handler
         */
        public void Remove(Delegate handlerDelegate)
        {
            RemovePending(handlerDelegate);
            pendingEvents.Remove(handlerDelegate);
        }

        /*
         * Removes an existing event handler (without updating the pending handlers list)
         */
        internal void RemovePending(Delegate handlerDelegate)
        {
            eventInfo.RemoveEventHandler(target, handlerDelegate);
        }
    }
}