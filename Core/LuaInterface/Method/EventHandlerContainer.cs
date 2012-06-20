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
using System.Diagnostics;
using System.Collections.Generic;

namespace LuaInterface.Method
{
	/// <summary>
	/// We keep track of what delegates we have auto attached to an event - to allow us to cleanly exit a LuaInterface session
	/// </summary>
	class EventHandlerContainer : IDisposable
	{
		private Dictionary<Delegate, RegisterEventHandler> dict = new Dictionary<Delegate, RegisterEventHandler>();

		public void Add(Delegate handler, RegisterEventHandler eventInfo)
		{
			dict.Add(handler, eventInfo);
		}

		public void Remove(Delegate handler)
		{
			bool found = dict.Remove(handler);
			Debug.Assert(found);
		}

		/// <summary>
		/// Remove any still registered handlers
		/// </summary>
		public void Dispose()
		{
			foreach(KeyValuePair<Delegate, RegisterEventHandler> pair in dict)
				pair.Value.RemovePending(pair.Key);

			dict.Clear();
		}
	}
}