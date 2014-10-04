﻿/*
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
	#else
	using LuaCore  = KeraLua.Lua;
	using LuaState = KeraLua.LuaState;
	#endif

	public class LuaUserData : LuaBase
	{
		public LuaUserData (int reference, Lua interpreter)
		{
			_Reference = reference;
			_Interpreter = interpreter;
		}

		/*
		 * Indexer for string fields of the userdata
		 */
		public object this [string field] {
			get {
				return _Interpreter.GetObject (_Reference, field);
			}
			set {
				_Interpreter.SetObject (_Reference, field, value);
			}
		}

		/*
		 * Indexer for numeric fields of the userdata
		 */
		public object this [object field] {
			get {
				return _Interpreter.GetObject (_Reference, field);
			}
			set {
				_Interpreter.SetObject (_Reference, field, value);
			}
		}

		/*
		 * Calls the userdata and returns its return values inside
		 * an array
		 */
		public object[] Call (params object[] args)
		{
			return _Interpreter.CallFunction (this, args);
		}


		public override string ToString ()
		{
			return "userdata";
		}
	}
}