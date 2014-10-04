/*
 * This file is part of NLua.
 * 
 * Copyright (c) 2014 Vinicius Jarina (viniciusjarina@gmail.com)
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
	#if USE_KOPILUA
	using LuaCore  = KopiLua.Lua;
	using LuaState = KopiLua.LuaState;
	using LuaNativeFunction = KopiLua.LuaNativeFunction;
	#else
	using LuaCore  = KeraLua.Lua;
	using LuaState = KeraLua.LuaState;
	using LuaNativeFunction = KeraLua.LuaNativeFunction;
	#endif

	public class LuaFunction : LuaBase
	{
		internal LuaNativeFunction function;

		public LuaFunction (int reference, Lua interpreter)
		{
			_Reference = reference;
			this.function = null;
			_Interpreter = interpreter;
		}

		public LuaFunction (LuaNativeFunction function, Lua interpreter)
		{
			_Reference = 0;
			this.function = function;
			_Interpreter = interpreter;
		}

		/*
		 * Calls the function casting return values to the types
		 * in returnTypes
		 */
		internal object[] Call (object[] args, Type[] returnTypes)
		{
			return _Interpreter.CallFunction (this, args, returnTypes);
		}

		/*
		 * Calls the function and returns its return values inside
		 * an array
		 */
		public object[] Call (params object[] args)
		{
			return _Interpreter.CallFunction (this, args);
		}

		/*
		 * Pushes the function into the Lua stack
		 */
		internal void Push (LuaState luaState)
		{
			if (_Reference != 0)
				LuaLib.LuaGetRef (luaState, _Reference);
			else
				_Interpreter.PushCSFunction (function);
		}

		public override string ToString ()
		{
			return "function";
		}

		public override bool Equals (object o)
		{
			if (o is LuaFunction) {
				var l = (LuaFunction)o;

				if (this._Reference != 0 && l._Reference != 0)
					return _Interpreter.CompareRef (l._Reference, this._Reference);
				else
					return this.function == l.function;
			} else
				return false;
		}

		public override int GetHashCode ()
		{
			return _Reference != 0 ? _Reference : function.GetHashCode ();
		}
	}
}