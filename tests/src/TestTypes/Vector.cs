using System;

namespace NLuaTest.TestTypes
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

        public static Vector operator *(Vector v, float k)
        {
            var r = new Vector();
            r.x = v.x * k;
            r.y = v.y * k;
            return r;
        }

        public void Func()
        {
            Console.WriteLine("Func");
        }
    }
}