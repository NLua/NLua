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
			using (var l = new Lua ()) {
				l.LoadCLRPackage ();
				l.DoString (" import ('ConsoleTest') ");
				l.DoString (@"
					p=parameter()
					r1 = testClass2.read(p)     -- is not working. it is also not working if the method in base class has two parameters instead of one
					r2 = testClass2.read(1)     -- is working				
				");
			}
		}
	}
}
