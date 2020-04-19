using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace NLuaTest.TestTypes
{
    public class PrintOverrides
    {
        public static char SYMBOL_PRINT_CONCAT = '\t';

        // A custom print method as a replacement for the default Lua print function
        public static string GetPrintOutput(params object[] arguments)
        {
            StringBuilder output = new StringBuilder();

            if (arguments != null)
            {
                for(int i = 0; i < arguments.Length; i++)
                {
                    object argument = arguments[i];
                    string concat = i > 0 ? SYMBOL_PRINT_CONCAT.ToString() : "";

                    if (argument == null)
                    {
                        output.Append(concat + "nil");
                        continue;
                    }

                    output.Append(concat + argument.ToString());
                }
            }
            else
            {
                output.Append("nil");
            }

            return output.ToString();
        }
    }
}
