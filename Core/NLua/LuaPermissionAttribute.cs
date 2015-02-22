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
using System.Reflection;

namespace NLua
{
	/// <summary>
	/// Marks a method, field or property to only be usable if the given callback returns true.
	/// Callback must return a bool.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Field | AttributeTargets.Property)]
	public sealed class LuaPermissionAttribute : Attribute
	{
		internal Type type;
		internal string funcName;
		internal object[] funcParams;

		/// <summary>
		/// Message to show in the exception if access is not allowed
		/// </summary>
		public string Message { get; set; }

		public LuaPermissionAttribute(Type type, string funcName)
		{
			this.type = type;
			this.funcName = funcName;
			this.funcParams = null;
			this.Message = "Access denied";
		}

		public LuaPermissionAttribute(Type type, string funcName, params object[] funcParams)
		{
			this.type = type;
			this.funcName = funcName;
			this.funcParams = funcParams;
			this.Message = "Access denied";
		}

		/// <summary>
		/// Check whether Lua is allowed to access this member
		/// </summary>
		/// <param name="obj">The object on which to apply the callback function</param>
		/// <returns>True if allowed, false if not</returns>
		public bool Allowed(object obj)
		{
			BindingFlags bf = (BindingFlags)65535; // everything, so that we can get internal and private callback methods
			MethodInfo mi = this.type.GetMethod (this.funcName, bf);
			if (mi == null)
				throw new Exception ("Lua permission callback not found: '" + this.funcName + "'");
			return (bool)mi.Invoke (obj, this.funcParams);
		}
	}
}
