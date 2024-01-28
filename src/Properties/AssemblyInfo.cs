using System.Reflection;

// Information about this assembly is defined by the following attributes. 
// Change them to the values specific to your project.

#if NETFRAMEWORK
[assembly: AssemblyTitle ("NLua (.NET Framework 4.6)")]
#elif WINDOWS_UWP
[assembly: AssemblyTitle ("NLua (UWP)")]
#elif __ANDROID__
[assembly: AssemblyTitle ("NLua (Android)")]
#elif NETCOREAPP
[assembly: AssemblyTitle ("NLua (.NET Core)")]
#elif NETSTANDARD
[assembly: AssemblyTitle ("NLua (.NET Standard)")]
#elif __TVOS__
[assembly: AssemblyTitle ("NLua (tvOS)")]
#elif __WATCHOS__
[assembly: AssemblyTitle ("NLua (watchOS)")]
#elif __MACCATALYST__
[assembly: AssemblyTitle ("NLua (Mac Catalyst)")]
#elif __IOS__
[assembly: AssemblyTitle ("NLua (iOS)")]
#elif __MACOS__
[assembly: AssemblyTitle ("NLua (Mac)")]
#else
[assembly: AssemblyTitle("NLua (.NET)")]
#endif

[assembly: AssemblyDescription("NLua library")]
[assembly: AssemblyCompany("NLua.org")]
[assembly: AssemblyProduct("NLua")]
[assembly: AssemblyCopyright("Copyright © Vinicius Jarina 2024")]
[assembly: AssemblyCulture("")]


[assembly: AssemblyVersion("1.4.1.0")]
[assembly: AssemblyInformationalVersion("1.0.7+Branch.main.Sha.80a328a64f12ed9032a0f14a75e6ecad967514d0")]
[assembly: AssemblyFileVersion("1.4.1.0")]


