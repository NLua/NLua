using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLua;
using NLuaTest.Mock;
using NLuaTest;

namespace ConsoleTest
{

	public class Program
	{
		static public void Method(int a, params int[] others) {
			Console.WriteLine (a);
			foreach (int val in others)
				Console.WriteLine (val);
		}

		static void Main (string [] args)
		{
			using (var l = new Lua ()) {
				l.LoadCLRPackage ();
				l.DoString (" import ('ConsoleTest') ");
				l.DoString (@"
						Program.Method (1)
				");
			}
		}
	}
}
