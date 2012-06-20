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
using LuaInterface.Extensions;

namespace LuaInterface.Method
{
	/*
	 * Cached method
	 */
	struct MethodCache
	{
		private MethodBase _cachedMethod;

		public MethodBase cachedMethod
		{
			get
			{
				return _cachedMethod;
			}
			set
			{
				_cachedMethod = value;
				var mi = value as MethodInfo;

				if(!mi.IsNull())
					IsReturnVoid = string.Compare(mi.ReturnType.Name, "System.Void", true) == 0;
			}
		}
		
		public bool IsReturnVoid;
		// List or arguments
		public object[] args;
		// Positions of out parameters
		public int[] outList;
		// Types of parameters
		public MethodArgs[] argTypes;
	}
}