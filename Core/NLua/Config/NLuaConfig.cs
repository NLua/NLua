/*
 * This file is part of NLua.
 * 
 * Copyright (c) 2013 Vinicius Jarina (viniciusjarina@gmail.com)
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

namespace NLua.Config
{
	public static class Consts
	{
		public const string NLuaDescription = "Bridge between the Lua runtime and the CLR";
#if DEBUG
		public const string NLuaConfiguration = "Debug";
#else
		public const string NLuaConfiguration = "Release";
#endif
		public const string NLuaCompany = "NLua.org";
		public const string NLuaProduct = "NLua";
		public const string NLuaCopyright = "Copyright 2003-2013 Vinicius Jarina , Fabio Mascarenhas, Kevin Hesterm and Megax";
		public const string NLuaTrademark = "MIT license";
		public const string NLuaVersion = "1.3.0";
		public const string NLuaFileVersion = "1.3.0";
	}
}