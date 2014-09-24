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
		static void Main (string [] args)
		{
			Core c = new Core ();
			c.Setup ();
			c.Sieve ();

		}
	}
}
