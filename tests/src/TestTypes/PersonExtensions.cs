namespace NLuaTest.TestTypes
{
    public static class PersonExtensions
    {
        public static string GetFirstName(this Person argPerson)
        {
            return argPerson.firstName;
        }
    }
}