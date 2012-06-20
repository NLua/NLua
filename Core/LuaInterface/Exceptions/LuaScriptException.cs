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

namespace LuaInterface.Exceptions
{
	/// <summary>
	/// Exceptions thrown by the Lua runtime because of errors in the script
	/// </summary>
	public class LuaScriptException : LuaException
	{
		/// <summary>
		/// Returns true if the exception has occured as the result of a .NET exception in user code
		/// </summary>
		public bool IsNetException { get; private set; }
		private readonly string source;

		/// <summary>
		/// The position in the script where the exception was triggered.
		/// </summary>
		public override string Source { get { return source; } }

		/// <summary>
		/// Creates a new Lua-only exception.
		/// </summary>
		/// <param name="message">The message that describes the error.</param>
		/// <param name="source">The position in the script where the exception was triggered.</param>
		public LuaScriptException(string message, string source) : base(message)
		{
			this.source = source;
		}

		/// <summary>
		/// Creates a new .NET wrapping exception.
		/// </summary>
		/// <param name="innerException">The .NET exception triggered by user-code.</param>
		/// <param name="source">The position in the script where the exception was triggered.</param>
		public LuaScriptException(Exception innerException, string source)
			: base("A .NET exception occured in user-code", innerException)
		{
			this.source = source;
			this.IsNetException = true;
		}

		public override string ToString()
		{
			// Prepend the error source
			return GetType().FullName + ": " + source + Message;
		}
	}
}