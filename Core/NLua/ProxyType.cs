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
using System.Globalization;
using System.Reflection;

#if NETFX_CORE
using  NLua.Extensions;
#endif

namespace NLua
{
	/// <summary>
	/// Summary description for ProxyType.
	/// </summary>
	public class ProxyType
	{
		private Type proxy;

		public ProxyType (Type proxy)
		{
			this.proxy = proxy;
		}

		/// <summary>
		/// Provide human readable short hand for this proxy object
		/// </summary>
		/// <returns></returns>
		public override string ToString ()
		{
			return "ProxyType(" + UnderlyingSystemType + ")";
		}

		public Type UnderlyingSystemType {
			get { return proxy; }
		}

		public override bool Equals (object obj)
		{
			if (obj is Type)
				return proxy.Equals ((Type)obj);
			if (obj is ProxyType)
				return proxy.Equals (((ProxyType)obj).UnderlyingSystemType);
			return proxy.Equals (obj);
		}

		public override int GetHashCode ()
		{
			return proxy.GetHashCode ();
		}

		public MemberInfo[] GetMember (string name, BindingFlags bindingAttr)
		{
			return proxy.GetMember (name, bindingAttr);
		}

		public MethodInfo GetMethod (string name, BindingFlags bindingAttr, Type[] signature)
		{
#if NETFX_CORE
			return proxy.GetMethod (name, bindingAttr, signature);
#else			
			return proxy.GetMethod (name, bindingAttr, null, signature, null);
#endif
		}
	}
}