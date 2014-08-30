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
			Console.WriteLine ("Casa");
		}

		static void DebugHook (object sender, NLua.Event.DebugHookEventArgs args)
		{

		}

		static void Main (string [] args)
		{

			using (Lua lua = new Lua ()) {
				lua.LoadCLRPackage ();

				lua.DoString (@" import ('NLuaTest')
							  v = Vector()
							  v.x = 10
							  v.y = 3
							  v = v*2 ");

				var v = (Vector)lua ["v"];

				double len = v.Lenght ();
				lua.DoString (" v:Lenght() ");
				lua.DoString (@" len2 = v:Lenght()");
				double len2 = (double)lua ["len2"];
				
			}

		}
	}
}
