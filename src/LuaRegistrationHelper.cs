using System;
using System.Reflection;
using System.Diagnostics.CodeAnalysis;

namespace NLua
{
    public static class LuaRegistrationHelper
    {
        #region Tagged instance methods
        /// <summary>
        /// Registers all public instance methods in an object tagged with <see cref="LuaMemberAttribute"/> as Lua global functions
        /// </summary>
        /// <param name="lua">The Lua VM to add the methods to</param>
        /// <param name="o">The object to get the methods from</param>
        public static void TaggedInstanceMethods(Lua lua, object o)
        {
            #region Sanity checks
            if (lua == null)
                throw new ArgumentNullException(nameof(lua));

            if (o == null)
                throw new ArgumentNullException(nameof(o));
            #endregion

            foreach (var method in o.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public))
            {
                foreach (LuaMemberAttribute attribute in method.GetCustomAttributes(typeof(LuaMemberAttribute), true))
                {
                    if (string.IsNullOrEmpty(attribute.Name))
                        lua.RegisterFunction(method.Name, o, method); // CLR name
                    else
                        lua.RegisterFunction(attribute.Name, o, method); // Custom name
                }
            }
        }
        #endregion

        #region Tagged static methods
        /// <summary>
        /// Registers all public static methods in a class tagged with <see cref="LuaMemberAttribute"/> as Lua global functions
        /// </summary>
        /// <param name="lua">The Lua VM to add the methods to</param>
        /// <param name="type">The class type to get the methods from</param>
        public static void TaggedStaticMethods(Lua lua, Type type)
        {
            #region Sanity checks
            if (lua == null)
                throw new ArgumentNullException(nameof(lua));

            if (type == null)
                throw new ArgumentNullException(nameof(type));

            if (!type.IsClass)
                throw new ArgumentException("The type must be a class!", nameof(type));
            #endregion

            foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Public))
            {
                foreach (LuaMemberAttribute attribute in method.GetCustomAttributes(typeof(LuaMemberAttribute), false))
                {
                    if (string.IsNullOrEmpty(attribute.Name))
                        lua.RegisterFunction(method.Name, null, method); // CLR name
                    else
                        lua.RegisterFunction(attribute.Name, null, method); // Custom name
                }
            }
        }
        #endregion

        /// <summary>
        /// Registers an enumeration's values for usage as a Lua variable table
        /// </summary>
        /// <typeparam name="T">The enum type to register</typeparam>
        /// <param name="lua">The Lua VM to add the enum to</param>
        [SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Justification = "The type parameter is used to select an enum type")]
        public static void Enumeration<T>(Lua lua)
        {
            if (lua == null)
                throw new ArgumentNullException(nameof(lua));

            Type type = typeof(T);

            if (!type.IsEnum)
                throw new ArgumentException("The type must be an enumeration!");


            string[] names = Enum.GetNames(type);
            var values = (T[])Enum.GetValues(type);
            lua.NewTable(type.Name);

            for (int i = 0; i < names.Length; i++)
            {
                string path = type.Name + "." + names[i];
                lua.SetObjectToPath(path, values[i]);
            }
        }
    }
}
