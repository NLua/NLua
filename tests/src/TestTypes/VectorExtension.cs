namespace NLuaTest.TestTypes
{
    public static class VectorExtension
    {
        public static double Length(this Vector v)
        {
            return v.x * v.x + v.y * v.y;
        }
    }
}