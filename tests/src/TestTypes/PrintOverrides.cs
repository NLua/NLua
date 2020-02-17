using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace NLuaTest.TestTypes
{
    public class PrintOverrides
    {
        public static char SYMBOL_PRINT_CONCAT = '\t';

        private static void Print(params object[] arguments)
        {
            string output = "";

            if (arguments != null)
            {
                foreach (object argument in arguments)
                {
                    if (argument == null)
                    {
                        output += SYMBOL_PRINT_CONCAT + "nil";
                        continue;
                    }

                    output += SYMBOL_PRINT_CONCAT + argument.ToString();
                }
            }
            else
            {
                output = "nil";
            }

            Console.WriteLine(output.TrimStart(SYMBOL_PRINT_CONCAT));
        }

        public static MethodInfo GetPrintDetourMethodInfo()
        {
            return typeof(PrintOverrides).GetMethod(
                "Print",
                BindingFlags.NonPublic | BindingFlags.Static,
                null,
                new Type[] {
                    typeof(object[])
                },
                null);
        }
    }
}
