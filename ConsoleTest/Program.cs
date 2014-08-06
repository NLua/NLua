using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLua;
using NLuaTest.Mock;

namespace ConsoleTest
{
	public class Vector
	{
		public double x;
		public double y;
		public static Vector operator *(float k, Vector v)
		{
			var r = new Vector();
			r.x = v.x * k;
			r.y = v.y * k;
			return r;
		}

		public static Vector operator * (Vector v, float k)
		{
			var r = new Vector ();
			r.x = v.x * k;
			r.y = v.y * k;
			return r;
		}
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
			 lua.LoadCLRPackage ();

			 lua.DoString (@" import ('ConsoleTest')
							  v = Vector()
							  v.x = 10
							  v.y = 3
							  v = v*2
							  v = 3 * v
			");

			 var v = lua ["v"];
			

			// lua.LoadCLRPackage ();
			
		

			
 		}


		}
	}
}
