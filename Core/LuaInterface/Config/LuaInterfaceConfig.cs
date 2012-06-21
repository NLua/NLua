/*
 * This file is part of LuaInterface.
 * 
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

namespace LuaInterface.Config
{
	public static class Consts
	{
		public const string LuaInterfaceDescription = "Bridge between the Lua runtime and the CLR";
#if DEBUG
		public const string LuaInterfaceConfiguration = "Debug";
#else
		public const string LuaInterfaceConfiguration = "Release";
#endif
		public const string LuaInterfaceCompany = "LuaInterface Productions";
		public const string LuaInterfaceProduct = "LuaInterface";
		public const string LuaInterfaceCopyright = "Copyright 2003-2008 Fabio Mascarenhas, Kevin Hesterm and 2012 Megax";
		public const string LuaInterfaceTrademark = "MIT license";
		public const string LuaInterfaceVersion = "2.0.4";
		public const string LuaInterfaceFileVersion = "2.0.4.0";
	}
}