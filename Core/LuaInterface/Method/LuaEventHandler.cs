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

namespace LuaInterface.Method
{
	/*
	 * Base wrapper class for Lua function event handlers.
	 * Subclasses that do actual event handling are created
	 * at runtime.
	 * 
	 * Author: Fabio Mascarenhas
	 * Version: 1.0
	 */
	public class LuaEventHandler
	{
		public LuaFunction handler = null;

		// CP: Fix provided by Ben Bryant for delegates with one param
		// link: http://luaforge.net/forum/message.php?msg_id=9318
		public void handleEvent(object[] args)
		{
			handler.Call(args);
		}
		//public void handleEvent(object sender,object data) 
		//{
		//	handler.call(new object[] { sender,data },new Type[0]);
		//}
	}
}