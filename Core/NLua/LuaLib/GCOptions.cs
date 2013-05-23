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
	public enum GCOptions : int
	{
		/// <summary>
		/// Stops the garbage collector.
		/// </summary>
		Stop            = 0,
 
		/// <summary>
		/// Restarts the garbage collector.
		/// </summary>
		Restart         = 1,
 
		/// <summary>
		/// Performs a full garbage-collection cycle. 
		/// </summary>
		Collect         = 2,
 
		/// <summary>
		/// Returns the current amount of memory (in Kbytes) in use by KopiLua.Lua. 
		/// </summary>
		Count           = 3,
 
		/// <summary>
		/// Returns the remainder of dividing the current amount of bytes of memory in use by Lua by 1024. 
		/// </summary>
		CountB          = 4,
 
		/// <summary>
		/// Performs an incremental step of garbage collection. The step "size" is controlled by data (larger values mean more steps) in a non-specified way. ifyou want to control the step size you must experimentally tune the value of data. The function returns 1 ifthe step finished a garbage-collection cycle. 
		/// </summary>
		Step            = 5,
 
		/// <summary>
		/// Sets data as the new value for the pause (Controls how long the collector waits before starting a new cycle) of the collector (see §2.10). The function returns the previous value of the pause.
		/// </summary>
		SetPause        = 6,
 
		/// <summary>
		/// Sets data as the new value for the step multiplier of the collector (Controls the relative speed of the collector relative to memory allocation.). The function returns the previous value of the step multiplier. 
		/// </summary>
		SetStepMul      = 7
	}
}