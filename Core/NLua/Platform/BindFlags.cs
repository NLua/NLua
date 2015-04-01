using System;

#if !UNITY_3D
namespace System.Reflection
{
	public enum BindingFlags
	{
		Instance = 4,
		Static = 8,
		Public = 16,
	}
}
#endif