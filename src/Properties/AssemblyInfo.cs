using System.Reflection;

// Information about this assembly is defined by the following attributes. 
// Change them to the values specific to your project.

#if NETFRAMEWORK
[assembly: AssemblyTitle ("NLua (.NET Framework 4.5)")]
#elif __ANDROID__
[assembly: AssemblyTitle ("NLua (Xamarin.Android)")]
#elif NETCOREAPP
[assembly: AssemblyTitle ("NLua (.NET Core)")]
#elif NETSTANDARD
[assembly: AssemblyTitle ("NLua (.NET Standard)")]
#elif __TVOS__
[assembly: AssemblyTitle ("NLua (Xamarin.tvOS)")]
#elif __WATCHOS__
[assembly: AssemblyTitle ("NLua (Xamarin.watchOS)")]
#elif __IOS__
[assembly: AssemblyTitle ("NLua (Xamarin.iOS)")]
#elif __MACOS__
[assembly: AssemblyTitle ("NLua (Xamarin.Mac)")]
#else
[assembly: AssemblyTitle ("NLua (.NET Framework)")]
#endif

[assembly: AssemblyDescription ("Library to create simple Mazes")]
[assembly: AssemblyCompany ("NLua.org")]
[assembly: AssemblyProduct ("NLua")]
[assembly: AssemblyCopyright ("Copyright © Vinicius Jarina 2019")]
[assembly: AssemblyCulture ("")]


[assembly: AssemblyVersion("1.4.1.0")]
[assembly: AssemblyInformationalVersion("1.0.7+Branch.master.Sha.80a328a64f12ed9032a0f14a75e6ecad967514d0")]
[assembly: AssemblyFileVersion("1.4.1.0")]


