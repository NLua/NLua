/*
 * This file is part of NLua.
 * 
 * Copyright (c) 2013 Vinicius Jarina (viniciusjarina@gmail.com)
 * Copyright (C) 2003-2005 Fabio Mascarenhas de Queiroz.
 * Copyright (C) 2009 Joshua Simmons <simmons.44@gmail.com>
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
 
namespace NLua
{
	/// <summary>
	/// Enumeration of basic lua globals.
	/// </summary>
	public enum LuaEnums : int
	{
		/// <summary>
		/// Option for multiple returns in `lua_pcall' and `lua_call'
		/// </summary>
		MultiRet        = -1,
 
		/// <summary>
		/// Everything is OK.
		/// </summary>
		Ok              = 0,
 
		/// <summary>
		/// Thread status, Ok or Yield
		/// </summary>
		Yield           = 1,
 
		/// <summary>
		/// A Runtime error.
		/// </summary>
		ErrorRun        = 2,
 
		/// <summary>
		/// A syntax error.
		/// </summary>
		ErrorSyntax     = 3,
 
		/// <summary>
		/// A memory allocation error. For such errors, Lua does not call the error handler function. 
		/// </summary>
		ErrorMemory     = 4,
 
		/// <summary>
		/// An error in the error handling function.
		/// </summary>
		ErrorError      = 5,
 
		/// <summary>
		/// An extra error for file load errors when using luaL_loadfile.
		/// </summary>
		ErrorFile       = 6
	}
}