/*
 * This file is part of NLua.
 * 
 * Copyright (c) 2014 Vinicius Jarina (viniciusjarina@gmail.com)
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
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace NLua.Extensions
{
	/// <summary>
	/// Some random extension stuff.
	/// </summary>
	static class CheckNull
	{
		/// <summary>
		/// Determines whether the specified obj is null.
		/// </summary>
		/// <param name="obj">The obj.</param>
		/// <returns>
		/// 	<c>true</c> if the specified obj is null; otherwise, <c>false</c>.
		/// </returns>
		/// 
		
#if USE_KOPILUA
		public static bool IsNull (object obj)
		{
			return (obj == null);
		}
#else

		public static bool IsNull (IntPtr ptr)
		{
			return (ptr.Equals (IntPtr.Zero));
		}
#endif
	}

	static class TypeExtensions
	{
		public static bool HasMethod (this Type t, string name)
		{
#if NETFX_CORE
			var op = t.GetPublicMethods (name);
			return op.Any ();
#else
			var op = t.GetMethods (BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
			return op.Any (m => m.Name == name);
#endif
		}

		public static bool HasAdditionOpertator (this Type t)
		{
			if (t.IsPrimitive ()) 
				return true;

			return t.HasMethod ("op_Addition");
		}

		public static bool HasSubtractionOpertator (this Type t)
		{
			if (t.IsPrimitive ())
				return true;

			return t.HasMethod ("op_Subtraction");
		}

		public static bool HasMultiplyOpertator (this Type t)
		{
			if (t.IsPrimitive ())
				return true;

			return t.HasMethod ("op_Multiply");
		}

		public static bool HasDivisionOpertator (this Type t)
		{
			if (t.IsPrimitive ())
				return true;

			return t.HasMethod ("op_Division");
		}

		public static bool HasModulusOpertator (this Type t)
		{
			if (t.IsPrimitive ())
				return true;

			return t.HasMethod ("op_Modulus");
		}

		public static bool HasUnaryNegationOpertator (this Type t)
		{
			if (t.IsPrimitive ())
				return true;
			// Unary - will always have only one version.
			var op = t.GetMethod ("op_UnaryNegation", BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
			return op != null;
		}

		public static bool HasEqualityOpertator (this Type t)
		{
			if (t.IsPrimitive ())
				return true;
			return t.HasMethod ("op_Equality");
		}

		public static bool HasLessThanOpertator (this Type t)
		{
			if (t.IsPrimitive ())
				return true;

			return t.HasMethod ("op_LessThan");
		}

		public static bool HasLessThanOrEqualOpertator (this Type t)
		{
			if (t.IsPrimitive ())
				return true;
			return t.HasMethod ("op_LessThanOrEqual");
		}

		public static IEnumerable<MethodInfo> GetMethods (this Type t, string name, BindingFlags flags)
		{
			return t.GetMethods (flags).Where (m => m.Name == name);
		}

		public static MethodInfo [] GetExtensionMethods (this Type type, IEnumerable<Assembly> assemblies = null)
		{
			List<Type> types = new List<Type> ();

			types.AddRange (type.GetAssembly().GetTypes ().Where (t => t.IsPublic ()));

			if (assemblies != null) {
				foreach (Assembly item in assemblies) {
					if (item == type.GetAssembly ())
						continue;
					types.AddRange (item.GetTypes ().Where (t => t.IsPublic ()));
				}
			}

			var query = from extensionType in types
						where extensionType.IsSealed() && !extensionType.IsGenericType() && !extensionType.IsNested
						from method in extensionType.GetMethods (BindingFlags.Static | BindingFlags.Public)
						where method.IsDefined (typeof (ExtensionAttribute), false)
						where method.GetParameters () [0].ParameterType == type
						select method;
			return query.ToArray<MethodInfo> ();
		}

		/// <summary>
		/// Extends the System.Type-type to search for a given extended MethodeName.
		/// </summary>
		/// <param name="MethodeName">Name of the Methode</param>
		/// <returns>the found Methode or null</returns>
		public static MethodInfo GetExtensionMethod (this Type t, string name, IEnumerable<Assembly> assemblies = null)
		{
			var mi = from methode in t.GetExtensionMethods (assemblies)
					 where methode.Name == name
					 select methode;
			if (!mi.Any<MethodInfo> ())
				return null;
			else
				return mi.First<MethodInfo> ();
		}

		public static bool IsPrimitive (this Type t)
		{
#if NETFX_CORE
			return t.GetTypeInfo ().IsPrimitive;
#else
			return t.IsPrimitive;
#endif
		}

		public static bool IsClass (this Type t)
		{
#if NETFX_CORE
			return t.GetTypeInfo ().IsClass;
#else
			return t.IsClass;
#endif
		}

		public static bool IsEnum (this Type t)
		{
#if NETFX_CORE
			return t.GetTypeInfo ().IsEnum;
#else
			return t.IsEnum;
#endif
		}

		public static bool IsPublic (this Type t)
		{
#if NETFX_CORE
			return t.GetTypeInfo ().IsPublic;
#else
			return t.IsPublic;
#endif
		}

		public static bool IsSealed (this Type t)
		{
#if NETFX_CORE
			return t.GetTypeInfo ().IsSealed;
#else
			return t.IsSealed;
#endif
		}

		public static bool IsGenericType (this Type t)
		{
#if NETFX_CORE
			return t.GetTypeInfo ().IsGenericType;
#else
			return t.IsGenericType;
#endif
		}


		public static bool IsInterface (this Type t)
		{
#if NETFX_CORE
			return t.GetTypeInfo ().IsInterface;
#else
			return t.IsInterface;
#endif
		}		

		public static Assembly GetAssembly (this Type t)
		{
#if NETFX_CORE
			return t.GetTypeInfo ().Assembly;
#else
			return t.Assembly;
#endif
		}

#if NETFX_CORE
		public static IEnumerable<MethodInfo> GetStaticPublicMethods (this Type t, string name)
		{
			return GetAllDeclaredMethodsRecursively (t, name).Where (m => m.IsStatic);
		}

		public static IEnumerable<MethodInfo> GetStaticPublicMethods (this Type t)
		{
			return GetAllDeclaredMethodsRecursively (t).Where (m => m.IsStatic);
		}

		public static IEnumerable<MethodInfo> GetInstancePublicMethods (this Type t, string name)
		{
			return GetAllDeclaredMethodsRecursively (t, name).Where (m => !m.IsStatic);

		}

		public static IEnumerable<MethodInfo> GetInstancePublicMethods (this Type t)
		{
			return GetAllDeclaredMethodsRecursively (t).Where (m => !m.IsStatic);
		}

		public static IEnumerable<MethodInfo> GetPublicMethods (this Type t)
		{
			return GetAllDeclaredMethodsRecursively (t);
		}

		public static IEnumerable<MethodInfo> GetPublicMethods (this Type t, string name)
		{
			return GetAllDeclaredMethodsRecursively (t, name);
		}

		public static MethodInfo GetPublicMethod (this Type t, string name)
		{
			return GetPublicMethods (t, name).First ();
		}

		static bool Match (IEnumerable<ParameterInfo> parameters, Type[] signature)
		{
			if (parameters.Count () != signature.Count ())
				return false;
			int i = 0;
			foreach (var parameter in parameters) {
				if (parameters.GetType () != signature [i])
					return false;
				i++;
			}
			return true;
		}

		public static MethodInfo GetPublicMethod (this Type t, string name, Type[] signature)
		{
			var methods =  t.GetPublicMethods (name);
			return methods.Where (m => Match (m.GetParameters (), signature)).FirstOrDefault ();
		}

		static IEnumerable<MethodInfo> GetAllDeclaredMethodsRecursively (Type t, string name)
		{
			var methods = t.GetTypeInfo ().GetDeclaredMethods (name);
			if (t == typeof (object))
				return methods;
			var baseType = t.GetTypeInfo ().BaseType;
			return methods.Concat (GetAllDeclaredMethodsRecursively (baseType, name));
		}

		static IEnumerable<MemberInfo> GetAllDeclaredMembersRecursively (Type t)
		{
			var members = t.GetTypeInfo ().DeclaredMembers;
			if (t == typeof (object))
				return members;
			var baseType = t.GetTypeInfo ().BaseType;
			return members.Concat (GetAllDeclaredMembersRecursively (baseType));
		}

		static IEnumerable<MethodInfo> GetAllDeclaredMethodsRecursively (Type t)
		{
			var methods  = t.GetTypeInfo ().DeclaredMethods;
			if (t == typeof (object))
				return methods;
			var baseType = t.GetTypeInfo ().BaseType;
			return methods.Concat (GetAllDeclaredMethodsRecursively (baseType));
		}

		static FieldInfo GetDeclaredFieldRecursively (Type t, string name)
		{
			var field = t.GetTypeInfo ().GetDeclaredField (name);
			if (field != null || t == typeof (object))
				return field;
			
			var baseType = t.GetTypeInfo ().BaseType;
			return GetDeclaredFieldRecursively (baseType, name);
		}

		static IEnumerable<FieldInfo> GetAllDeclaredFieldsRecursively (Type t)
		{
			var fields = t.GetTypeInfo ().DeclaredFields;
			if (t == typeof (object))
				return fields;
			var baseType = t.GetTypeInfo ().BaseType;
			return fields.Concat (GetAllDeclaredFieldsRecursively (baseType));
		}

		public static IEnumerable<FieldInfo> GetPublicFields (this Type t)
		{
			return GetAllDeclaredFieldsRecursively (t);
		}

		static IEnumerable<Type> GetTypes (this Assembly assembly)
		{
			return assembly.ExportedTypes;
		}

		public static bool IsAssignableFrom (this Type t, Type t2)
		{
			return t.GetTypeInfo ().IsAssignableFrom (t2.GetTypeInfo ());
		}

		public static MethodInfo[] GetMethods (this Type t)
		{
			return null;
		}

		public static MethodInfo [] GetMethods (this Type t, BindingFlags flags)
		{
			return null;
		}

		public static MethodInfo GetMethod (this Type t, string name, BindingFlags flags)
		{
			return null;
		}

		public static MethodInfo GetMethod (this Type t, string name)
		{
			return null;
		}

		public static MethodInfo GetMethod (this Type t, string name, BindingFlags bindingAttr, Type[] signature)
		{
			return null;
		}

		public static ConstructorInfo [] GetConstructors (this Type t)
		{
			return null;
		}

		public static ConstructorInfo GetConstructor (this Type t, Type [] signature)
		{
			return null;
		}

		public static FieldInfo GetField (this Type t, string name)
		{
			return null;
		}

		public static FieldInfo [] GetFields (this Type t, BindingFlags bindingAttr)
		{
			return null;
		}

		public static bool ImplementInterface (this Type t, string name)
		{
			return false;
		}

		public static PropertyInfo [] GetProperties (this Type t, BindingFlags bindingAttr)
		{
			return null;
		}
#endif
	}

	static class StringExtensions
	{
		public static IEnumerable<string> SplitWithEscape (this string input, char separator, char escapeCharacter)
		{
			int start = 0;
			int index = 0;
			while (index < input.Length) {
				index = input.IndexOf (separator, index);
				if (index == -1)
					break;

				if (input [index - 1] == escapeCharacter) {
					input = input.Remove (index - 1, 1);
					continue;
				}


				yield return input.Substring (start, index - start);
				index++;
				start = index;
			}
			yield return input.Substring (start);
		}
	}
}