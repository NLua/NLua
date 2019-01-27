using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace NLua.Extensions
{
    static class TypeExtensions
    {
        public static bool HasMethod(this Type t, string name)
        {
            var op = t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
            return op.Any(m => m.Name == name);
        }

        public static bool HasAdditionOperator(this Type t)
        {
            if (t.IsPrimitive)
                return true;

            return t.HasMethod("op_Addition");
        }

        public static bool HasSubtractionOperator(this Type t)
        {
            if (t.IsPrimitive)
                return true;

            return t.HasMethod("op_Subtraction");
        }

        public static bool HasMultiplyOperator(this Type t)
        {
            if (t.IsPrimitive)
                return true;

            return t.HasMethod("op_Multiply");
        }

        public static bool HasDivisionOperator(this Type t)
        {
            if (t.IsPrimitive)
                return true;

            return t.HasMethod("op_Division");
        }

        public static bool HasModulusOperator(this Type t)
        {
            if (t.IsPrimitive)
                return true;

            return t.HasMethod("op_Modulus");
        }

        public static bool HasUnaryNegationOperator(this Type t)
        {
            if (t.IsPrimitive)
                return true;
            // Unary - will always have only one version.
            var op = t.GetMethod("op_UnaryNegation", BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
            return op != null;
        }

        public static bool HasEqualityOperator(this Type t)
        {
            if (t.IsPrimitive)
                return true;
            return t.HasMethod("op_Equality");
        }

        public static bool HasLessThanOperator(this Type t)
        {
            if (t.IsPrimitive)
                return true;

            return t.HasMethod("op_LessThan");
        }

        public static bool HasLessThanOrEqualOperator(this Type t)
        {
            if (t.IsPrimitive)
                return true;
            return t.HasMethod("op_LessThanOrEqual");
        }

        public static MethodInfo[] GetMethods(this Type t, string name, BindingFlags flags)
        {
            return t.GetMethods(flags).Where(m => m.Name == name).ToArray();
        }

        public static MethodInfo[] GetExtensionMethods(this Type type, IEnumerable<Assembly> assemblies = null)
        {
            List<Type> types = new List<Type>();

            types.AddRange(type.Assembly.GetTypes().Where(t => t.IsPublic));

            if (assemblies != null)
            {
                foreach (Assembly item in assemblies)
                {
                    if (item == type.Assembly)
                        continue;
                    types.AddRange(item.GetTypes().Where(t => t.IsPublic));
                }
            }

            var query = from extensionType in types
                        where extensionType.IsSealed && !extensionType.IsGenericType && !extensionType.IsNested
                        from method in extensionType.GetMethods(BindingFlags.Static | BindingFlags.Public)
                        where method.IsDefined(typeof(ExtensionAttribute), false)
                        where (method.GetParameters()[0].ParameterType == type
                            || type.IsSubclassOf(method.GetParameters()[0].ParameterType)
                            || type.GetInterfaces().Contains(method.GetParameters()[0].ParameterType))
                        select method;
            return query.ToArray<MethodInfo>();
        }

        /// <summary>
        /// Extends the System.Type-type to search for a given extended MethodeName.
        /// </summary>
        /// <param name="MethodeName">Name of the Methode</param>
        /// <returns>the found Method or null</returns>
        public static MethodInfo GetExtensionMethod(this Type t, string name, IEnumerable<Assembly> assemblies = null)
        {
            var mi = from methode in t.GetExtensionMethods(assemblies)
                     where methode.Name == name
                     select methode;
            if (!mi.Any<MethodInfo>())
                return null;
            else
                return mi.First<MethodInfo>();
        }
    }

    static class StringExtensions
    {
        public static IEnumerable<string> SplitWithEscape(this string input, char separator, char escapeCharacter)
        {
            int start = 0;
            int index = 0;
            while (index < input.Length)
            {
                index = input.IndexOf(separator, index);
                if (index == -1)
                    break;

                if (input[index - 1] == escapeCharacter)
                {
                    input = input.Remove(index - 1, 1);
                    continue;
                }


                yield return input.Substring(start, index - start);
                index++;
                start = index;
            }
            yield return input.Substring(start);
        }
    }
}