using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLua;
using NLuaTest.Mock;

namespace ConsoleTest
{
	 struct Sub2 {

		 int someval; 
	}

	 struct Sub {

		 public Sub2 z; 
	}

	 struct Top { 
		
		 public Sub y; 
	}
	

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

		 using (Lua lua = new Lua())
 		{
			 lua.DebugHook += DebugHook;
			 lua.SetDebugHook (NLua.Event.EventMasks.LUA_MASKLINE, 0);
			 var a = new System.Numerics.Complex (10, 0);
			 var b = new System.Numerics.Complex (0, 3);
			 var x = a + b;

			// lua.LoadCLRPackage ();
			 lua ["a"] = a;
			 lua ["b"] = 1;
			 var res = lua.DoString (@"return a + b")[0];

			
 		}


		}
	}
}
