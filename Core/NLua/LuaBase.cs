﻿/*
 * This file is part of NLua.
 * 
 * Copyright (c) 2013 Vinicius Jarina (viniciusjarina@gmail.com)
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
using System.Text;
using System.Collections.Generic;

namespace NLua
{
	/// <summary>
	/// Base class to provide consistent disposal flow across lua objects. Uses code provided by Yves Duhoux and suggestions by Hans Schmeidenbacher and Qingrui Li 
	/// </summary>
	public abstract class LuaBase : IDisposable
	{
		private bool _Disposed;
		[CLSCompliantAttribute(false)]
		protected int
			_Reference;
		[CLSCompliantAttribute(false)]
		protected Lua
			_Interpreter;

		~LuaBase ()
		{
			Dispose (false);
		}

		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}

		public virtual void Dispose (bool disposeManagedResources)
		{
			if (!_Disposed) {
				if (_Reference != 0)
					_Interpreter.dispose (_Reference);

				_Interpreter = null;
				_Disposed = true;
			}
		}

		public override bool Equals (object o)
		{
			if (o is LuaBase) {
				var l = (LuaBase)o;
				return _Interpreter.compareRef (l._Reference, _Reference);
			} else
				return false;
		}

		public override int GetHashCode ()
		{
			return _Reference;
		}
	}
}
