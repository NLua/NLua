using System;
using System.Collections.Generic;
using System.Linq;
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
        
        private static readonly Dictionary<Tuple<Type, string, IEnumerable<Assembly>>, MethodInfo[]> ExtensionCache = new Dictionary<Tuple<Type, string, IEnumerable<Assembly>>, MethodInfo[]>();
        internal static void ClearExtensionCache(IEnumerable<Assembly> assemblies)
        {
            // ReSharper disable once PossibleUnintendedReferenceComparison
            IEnumerable<KeyValuePair<Tuple<Type, string, IEnumerable<Assembly>>, MethodInfo[]>> results = ExtensionCache.Where(((pair, _) => pair.Key.Item3 == assemblies));
            foreach (var keyValuePair in results)
            {
                ExtensionCache.Remove(keyValuePair.Key);
            }
        }

        public static MethodInfo[] GetExtensionMethods(this Type type, string name, IEnumerable<Assembly> assemblies = null)
        {
            var cacheKey = new Tuple<Type, string, IEnumerable<Assembly>>(type, name, assemblies ?? Array.Empty<Assembly>());

            // Check if the result is already in the cache
            if (ExtensionCache.TryGetValue(cacheKey, out var cachedMethods))
            {
                return cachedMethods;
            }
            
            var types = new List<Type>();

            types.AddRange(type.Assembly.GetTypes().Where(t => t.IsPublic));

            if (assemblies != null)
            {
                foreach (Assembly item in assemblies)
                {
                    if (item == type.Assembly)
                        continue;
                    types.AddRange(item.GetTypes().Where(t => t.IsPublic && t.IsClass && t.IsSealed && t.IsAbstract && !t.IsNested));
                }
            }

            var query = types
                .SelectMany(extensionType => extensionType.GetMethods(name, BindingFlags.Static | BindingFlags.Public),
                    (extensionType, method) => new {extensionType, method})
                .Where(t => t.method.IsDefined(typeof(ExtensionAttribute), false))
                .Where(t =>
                    t.method.GetParameters()[0].ParameterType == type ||
                     t.method.GetParameters()[0].ParameterType.IsAssignableFrom(type) ||
                     type.GetInterfaces().Contains(t.method.GetParameters()[0].ParameterType))
                .Select(t => t.method);

            var methods = query.ToArray();
            ExtensionCache[cacheKey] = methods;
            return methods;
        }

        /// <summary>
        /// Extends the System.Type-type to search for a given extended MethodeName.
        /// </summary>
        /// <param name="t"></param>
        /// <param name="name"></param>
        /// <param name="assemblies"></param>
        /// <returns></returns>
        public static MethodInfo GetExtensionMethod(this Type t, string name, IEnumerable<Assembly> assemblies = null)
        {
            var mi = t.GetExtensionMethods(name, assemblies).ToArray();
            if (mi.Length == 0)
                return null;
            return mi[0];
        }
    }
}
