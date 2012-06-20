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

namespace LuaInterface.Method
{
	/*
	 * Static helper methods for Lua tables acting as CLR objects.
	 * 
	 * Author: Fabio Mascarenhas
	 * Version: 1.0
	 */
	public class LuaClassHelper
	{
		/*
		 *  Gets the function called name from the provided table,
		 * returning null if it does not exist
		 */
		public static LuaFunction getTableFunction(LuaTable luaTable, string name)
		{
			object funcObj = luaTable.rawget(name);

			if(funcObj is LuaFunction)
				return (LuaFunction)funcObj;
			else
				return null;
		}

		/*
		 * Calls the provided function with the provided parameters
		 */
		public static object callFunction(LuaFunction function, object[] args, Type[] returnTypes, object[] inArgs, int[] outArgs)
		{
			// args is the return array of arguments, inArgs is the actual array
			// of arguments passed to the function (with in parameters only), outArgs
			// has the positions of out parameters
			object returnValue;
			int iRefArgs;
			object[] returnValues = function.call(inArgs, returnTypes);

			if(returnTypes[0] == typeof(void))
			{
				returnValue = null;
				iRefArgs = 0;
			}
			else
			{
				returnValue = returnValues[0];
				iRefArgs = 1;
			}

			for(int i = 0; i < outArgs.Length; i++)
			{
				args[outArgs[i]] = returnValues[iRefArgs];
				iRefArgs++;
			}

			return returnValue;
		}
	}
}