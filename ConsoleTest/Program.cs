using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLua;

namespace ConsoleTest
{
	class Program
	{
		static void Main (string [] args)
		{

			using (Lua lua = new Lua ()) {
				lua.DoString ("luanet.load_assembly('mscorlib')");
				lua.DoString ("luanet.load_assembly('ConsoleTest')");
				lua.DoString ("TestClass=luanet.import_type('NLuaTest.Mock.TestClass')");
				lua.DoString ("test=TestClass()");

				try {
					lua.DoString ("test:exceptionMethod()");
					//failed
					//Assert.True (false);
				} catch (Exception) {
					//passed
					//Assert.True (true);
				}
			}

		}
	}
}
