/*
 * This file is part of LuaInterface.
 * 
 * Copyright (C) 2003-2005 Fabio Mascarenhas de Queiroz.
 * Copyright (C) 2012 Megax <http://megax.yeahunter.hu/>
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.  IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

using System;
using System.Reflection;

namespace LuaInterface.Method
{
	/*
	 * Wrapper class for events that does registration/deregistration
	 * of event handlers.
	 * 
	 * Author: Fabio Mascarenhas
	 * Version: 1.0
	 */
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
			//MethodInfo mi = eventInfo.EventHandlerType.GetMethod("Invoke");
			//ParameterInfo[] pi = mi.GetParameters();
			//LuaEventHandler handler=CodeGeneration.Instance.GetEvent(pi[1].ParameterType,function);
			//Delegate handlerDelegate=Delegate.CreateDelegate(eventInfo.EventHandlerType,handler,"HandleEvent");
			//eventInfo.AddEventHandler(target,handlerDelegate);
			//pendingEvents.Add(handlerDelegate, this);
			//return handlerDelegate;
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