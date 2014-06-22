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
			 
			 lua.DoString (@"function testing_hooks() return 10 end
							val = testing_hooks() 
							val = val + 1
			");
			 double res = (double)lua ["val"];
			 Console.WriteLine ("{0}", res);
 		}


		}
	}
}
