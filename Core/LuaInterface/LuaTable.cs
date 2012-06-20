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
using System.Collections;
using System.Collections.Generic;

namespace LuaInterface
{
	using LuaCore = KopiLua.Lua;

	/*
	 * Wrapper class for Lua tables
	 *
	 * Author: Fabio Mascarenhas
	 * Version: 1.0
	 */
	public class LuaTable : LuaBase
	{
		public LuaTable(int reference, Lua interpreter)
		{
			_Reference = reference;
			_Interpreter = interpreter;
		}

		/*
		 * Indexer for string fields of the table
		 */
		public object this[string field]
		{
			get
			{
				return _Interpreter.getObject(_Reference, field);
			}
			set
			{
				_Interpreter.setObject(_Reference, field, value);
			}
		}

		/*
		 * Indexer for numeric fields of the table
		 */
		public object this[object field]
		{
			get
			{
				return _Interpreter.getObject(_Reference, field);
			}
			set
			{
				_Interpreter.setObject(_Reference, field, value);
			}
		}

		public System.Collections.IDictionaryEnumerator GetEnumerator()
		{
			return _Interpreter.GetTableDict(this).GetEnumerator();
		}

		public ICollection Keys
		{
			get { return _Interpreter.GetTableDict(this).Keys; }
		}

		public ICollection Values
		{
			get { return _Interpreter.GetTableDict(this).Values; }
		}

		/*
		 * Gets an string fields of a table ignoring its metatable,
		 * if it exists
		 */
		internal object rawget(string field)
		{
			return _Interpreter.rawGetObject(_Reference, field);
		}

		internal object rawgetFunction(string field)
		{
			object obj = _Interpreter.rawGetObject(_Reference, field);

			if(obj is LuaCore.lua_CFunction)
				return new LuaFunction((LuaCore.lua_CFunction)obj, _Interpreter);
			else
				return obj;
		}

		/*
		 * Pushes this table into the Lua stack
		 */
		internal void push(LuaCore.lua_State luaState)
		{
			LuaLib.lua_getref(luaState, _Reference);
		}

		public override string ToString()
		{
			return "table";
		}
	}
}