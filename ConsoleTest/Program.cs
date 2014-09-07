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
		public static void func()
		{
			KeraLua.LuaDebug info = new KeraLua.LuaDebug ();
			int level = 0;
			StringBuilder sb = new StringBuilder ();
			while (m_lua.GetStack (level,ref info) != 0) {
				m_lua.GetInfo ("nSl", ref info);
				string name = "<unknow>";
				if (info.name != null && !string.IsNullOrEmpty(info.name.ToString()))
					name = info.name.ToString ();

				sb.AppendFormat ("[{0}] {1}:{2} -- {3} [{4}]\n",
					level, info.shortsrc, info.currentline,
					name, info.namewhat);
				++level;
			}
			string expected = "[0] [C]:-1 -- func [field]\n[1] [string \"chunk\"]:12 -- f3 [global]\n[2] [string \"chunk\"]:8 -- f2 [global]\n[3] [string \"chunk\"]:4 -- f1 [global]\n[4] [string \"chunk\"]:15 -- <unknow> []\n";
			string x = sb.ToString ();
			if (x == expected)
				Console.WriteLine ("OK");
			Console.Write (x);
		}
		static Lua m_lua;


		static void Main (string [] args)
		{

			using (Lua lua = new Lua ()) {
				lua.LoadCLRPackage ();
				m_lua = lua;
				lua.DoString (@" 
								import ('ConsoleTest')
								function f1 ()
									 f2 ()
								 end
								 
								function f2()
									f3()
								end

								function f3()
									Program.func()
								end
								
								f1 ()
								");				
			}

		}
	}
}
