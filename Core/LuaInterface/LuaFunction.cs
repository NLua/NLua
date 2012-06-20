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
using System.Text;
using System.Collections.Generic;

namespace LuaInterface
{
	using LuaCore = KopiLua.Lua;

	public class LuaFunction : LuaBase
	{
		internal LuaCore.lua_CFunction function;

		public LuaFunction(int reference, Lua interpreter)
		{
			_Reference = reference;
			this.function = null;
			_Interpreter = interpreter;
		}

		public LuaFunction(LuaCore.lua_CFunction function, Lua interpreter)
		{
			_Reference = 0;
			this.function = function;
			_Interpreter = interpreter;
		}

		/*
		 * Calls the function casting return values to the types
		 * in returnTypes
		 */
		internal object[] call(object[] args, Type[] returnTypes)
		{
			return _Interpreter.callFunction(this, args, returnTypes);
		}

		/*
		 * Calls the function and returns its return values inside
		 * an array
		 */
		public object[] Call(params object[] args)
		{
			return _Interpreter.callFunction(this, args);
		}

		/*
		 * Pushes the function into the Lua stack
		 */
		internal void push(LuaCore.lua_State luaState)
		{
			if(_Reference != 0)
				LuaLib.lua_getref(luaState, _Reference);
			else
				_Interpreter.pushCSFunction(function);
		}

		public override string ToString()
		{
			return "function";
		}

		public override bool Equals(object o)
		{
			if(o is LuaFunction)
			{
				var l = (LuaFunction)o;

				if(this._Reference != 0 && l._Reference != 0)
					return _Interpreter.compareRef(l._Reference, this._Reference);
				else
					return this.function == l.function;
			}
			else
				return false;
		}

		public override int GetHashCode()
		{
			return _Reference != 0 ? _Reference : function.GetHashCode();
		}
	}
}