/*
 * This file is part of LuaInterface.
 * 
 * Copyright (C) 2003-2005 Fabio Mascarenhas de Queiroz.
 * Copyright (C) 2012 Megax <http://megax.yeahunter.hu/>
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.  IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

using System;
using System.Reflection;
using System.Diagnostics.CodeAnalysis;
using LuaInterface.Extensions;

namespace LuaInterface
{
	public static class LuaRegistrationHelper
	{
		#region Tagged instance methods
		/// <summary>
		/// Registers all public instance methods in an object tagged with <see cref="LuaGlobalAttribute"/> as Lua global functions
		/// </summary>
		/// <param name="lua">The Lua VM to add the methods to</param>
		/// <param name="o">The object to get the methods from</param>
		public static void TaggedInstanceMethods(Lua lua, object o)
		{
			#region Sanity checks
			if(lua.IsNull())
				throw new ArgumentNullException("lua");

			if(o.IsNull())
				throw new ArgumentNullException("o");
			#endregion

			foreach(var method in o.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public))
			{
				foreach(LuaGlobalAttribute attribute in method.GetCustomAttributes(typeof(LuaGlobalAttribute), true))
				{
					if(string.IsNullOrEmpty(attribute.Name))
						lua.RegisterFunction(method.Name, o, method); // CLR name
					else
						lua.RegisterFunction(attribute.Name, o, method); // Custom name
				}
			}
		}
		#endregion

		#region Tagged static methods
		/// <summary>
		/// Registers all public static methods in a class tagged with <see cref="LuaGlobalAttribute"/> as Lua global functions
		/// </summary>
		/// <param name="lua">The Lua VM to add the methods to</param>
		/// <param name="type">The class type to get the methods from</param>
		public static void TaggedStaticMethods(Lua lua, Type type)
		{
			#region Sanity checks
			if(lua.IsNull())
				throw new ArgumentNullException("lua");

			if(type.IsNull())
				throw new ArgumentNullException("type");

			if(!type.IsClass)
				throw new ArgumentException("The type must be a class!", "type");
			#endregion

			foreach(var method in type.GetMethods(BindingFlags.Static | BindingFlags.Public))
			{
				foreach(LuaGlobalAttribute attribute in method.GetCustomAttributes(typeof(LuaGlobalAttribute), false))
				{
					if(string.IsNullOrEmpty(attribute.Name))
						lua.RegisterFunction(method.Name, null, method); // CLR name
					else
						lua.RegisterFunction(attribute.Name, null, method); // Custom name
				}
			}
		}
		#endregion

		#region Enumeration
		/// <summary>
		/// Registers an enumeration's values for usage as a Lua variable table
		/// </summary>
		/// <typeparam name="T">The enum type to register</typeparam>
		/// <param name="lua">The Lua VM to add the enum to</param>
		[SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Justification = "The type parameter is used to select an enum type")]
		public static void Enumeration<T>(Lua lua)
		{
			#region Sanity checks
			if(lua.IsNull())
				throw new ArgumentNullException("lua");
			#endregion

			var type = typeof(T);

			if(!type.IsEnum)
				throw new ArgumentException("The type must be an enumeration!");

			string[] names = Enum.GetNames(type);
			var values = (T[])Enum.GetValues(type);
			lua.NewTable(type.Name);

			for(int i = 0; i < names.Length; i++)
			{
				string path = type.Name + "." + names[i];
				lua[path] = values[i];
			}
		}
		#endregion
	}
}