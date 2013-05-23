/*
 * This file is part of NLua.
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
using System.Threading;
using NLua;

/*
 * Application to run Lua scripts that can use NLua
 * from the console
 * 
 * Author: Fabio Mascarenhas
 * Version: 1.0
 */
namespace LuaRunner
{
	public class LuaNetRunner
	{
		/*
		 * Runs the Lua script passed as the first command-line argument.
		 * It passed all the command-line arguments to the script.
		 */
		[STAThread]		// steffenj: testluaform.lua "Load" button complained with an exception that STAThread was missing
		public static void Main(string[] args) 
		{
			if(args.Length > 0) 
			{
				// For attaching from the debugger
				// Thread.Sleep(20000);

				using(Lua lua = new Lua())
				{
					//lua.OpenLibs();			// steffenj: Lua 5.1.1 API change (all libs already opened in Lua constructor!)
					lua.NewTable("arg");
					LuaTable argc = (LuaTable)lua["arg"];
					argc[-1] = "LuaRunner";
					argc[0] = args[0];

					for(int i = 1; i < args.Length; i++)
						argc[i] = args[i];

					argc["n"] = args.Length - 1;

					try
					{
						//Console.WriteLine("DoFile(" + args[0] + ");");
						lua.DoFile(args[0]);
					}
					catch(Exception e)
					{
						// steffenj: BEGIN error message improved, output is now in decending order of importance (message, where, stacktrace)
						// limit size of strack traceback message to roughly 1 console screen height
						string trace = e.StackTrace;

						if(e.StackTrace.Length > 1300)
							trace = e.StackTrace.Substring(0, 1300) + " [...] (traceback cut short)";

						Console.WriteLine();
						Console.WriteLine(e.Message);
						Console.WriteLine(e.Source + " raised a " + e.GetType().ToString());
						Console.WriteLine(trace);

						// wait for keypress if there is an error
						Console.ReadKey();
						// steffenj: END error message improved
					}
				}
			} 
			else 
			{
				Console.WriteLine("LuaRunner -- runs Lua scripts with CLR access");
				Console.WriteLine("Usage: luarunner <script.lua> [{<arg>}]");
			}
		}
	}
}