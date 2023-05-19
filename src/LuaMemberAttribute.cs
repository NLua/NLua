using System;
using System.Linq;
using System.Reflection;

namespace NLua
{
    /// <summary>
    /// Allows the user to specify the name of the member when accessed in Lua
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class LuaMemberAttribute : Attribute
    {
        /// <summary>
        /// The name of the member when used accessed in Lua
        /// </summary>
        public string Name { get; set; }

        public static MethodInfo[] GetMethodsForType(Type type, string methodName, BindingFlags bindingFlags, Type[] signature)
        {
            return type.GetMethods(bindingFlags).Where(m =>
            {
                if (m.GetCustomAttribute<LuaHideAttribute>() != null)
                    return false;

                if (m.GetCustomAttribute<LuaMemberAttribute>() != null)
                {
                    var attr = m.GetCustomAttribute<LuaMemberAttribute>();
                    return attr.Name == methodName && m.GetParameters().Select(p => p.ParameterType).SequenceEqual(signature);
                }

                return m.Name == methodName && m.GetParameters().Select(p => p.ParameterType).SequenceEqual(signature);
            }).ToArray();
        }

        public static MethodInfo[] GetMethodsForType(Type type, string methodName, BindingFlags bindingFlags)
        {
            return type.GetMethods(bindingFlags).Where(m =>
            {
                if (m.GetCustomAttribute<LuaHideAttribute>() != null)
                    return false;

                if (m.GetCustomAttribute<LuaMemberAttribute>() != null)
                {
                    var attr = m.GetCustomAttribute<LuaMemberAttribute>();
                    return attr.Name == methodName;
                }

                return m.Name == methodName;
            }).ToArray();
        }

        public static MemberInfo[] GetMembersForType(Type type, string memberName, BindingFlags bindingFlags)
        {
            return type.GetMembers(bindingFlags).Where(m =>
            {
                if (m.GetCustomAttribute<LuaHideAttribute>() != null)
                    return false;

                if (m.GetCustomAttribute<LuaMemberAttribute>() != null)
                {
                    var attr = m.GetCustomAttribute<LuaMemberAttribute>();
                    return attr.Name == memberName;
                }

                return m.Name == memberName;
            }).ToArray();
        }
    }
}