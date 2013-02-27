using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleTest
{
	class Program
	{
		static void Main (string [] args)
		{
			using (var lua = new LuaInterface.Lua ()) {

				lua.RegisterFunction ("p", null, typeof (System.Console).GetMethod ("WriteLine", new Type [] { typeof (String) }));
				/// Lua command that works (prints to console)
				lua.DoString ("p('Foo')");
				/// Yet this works...
				lua.DoString ("string.gsub('some string', '(%w+)', function(s) p(s) end)");
				/// This fails if you don't fix Lua5.1 lstrlib.c/add_value to treat LUA_TUSERDATA the same as LUA_FUNCTION
				lua.DoString ("string.gsub('some string', '(%w+)', p)");
			}
		}
	}
}
