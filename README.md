👋 Hello there! | 
------------ | 
> 🔭 Thank you for checking out this project.
>
> 🍻 We've made the project Open Source and **MIT** license so everyone can enjoy it. 
>
> 🛠 To deliver a project with quality we have to spent a lot of time working on it.
> 
> ⭐️ If you liked the project please star it.
>
> 💕 We also appreaciate any **Sponsor**  [ [Patreon](https://www.patreon.com/codefoco) | [PayPal](https://paypal.me/viniciusjarina) ] 

[![Logo](https://secure.gravatar.com/avatar/77ecf0fb9d8419be7715c6e822e66562?s=150)]()

NLua
=======

| NuGet |
| ------|
|[![nuget](https://badgen.net/nuget/v/NLua?icon=nuget)](https://www.nuget.org/packages/NLua)|

|  | Status | 
| :------ | :------: | 
| ![linux](https://badgen.net/badge/icon/Ubuntu%20Linux%20x64?icon=travis&label&color=orange)   | [![Linux](https://travis-ci.org/NLua/NLua.svg?branch=master)](https://travis-ci.org/NLua/NLua) |
| ![win](https://badgen.net/badge/icon/Windows?icon=windows&label&color=blue) | [![Build status](https://ci.appveyor.com/api/projects/status/jkqcy9m9k35jwolx?svg=true)](https://ci.appveyor.com/project/viniciusjarina/NLua)|
| ![mac](https://badgen.net/badge/icon/macOS,iOS,tvOS,watchOS?icon=apple&label&color=purple&list=1) | [![Build Status](https://dev.azure.com/codefoco/NuGets/_apis/build/status/NLua/NLua.Mac?branchName=master)](https://dev.azure.com/codefoco/NuGets/_build/latest?definitionId=13&branchName=master) |
|![win](https://badgen.net/badge/icon/Windows,.NET%20Core?icon=windows&label&list=1) | [![Build Status](https://dev.azure.com/codefoco/NuGets/_apis/build/status/NLua/NLua.Windows?branchName=master)](https://dev.azure.com/codefoco/NuGets/_build/latest?definitionId=14&branchName=master) |


Bridge between Lua world and the .NET (compatible with Xamarin.iOS/Mac/Android/.NET/.NET Core) 

Building
---------

	msbuild NLua.sln


***

NLua allows the usage of Lua from C#, on Windows, Linux, Mac, iOS , Android.

[![Cmd](https://raw.github.com/NLua/NLua/master/extras/screenshot/NLuaCommand.gif)]()


NLua is a fork project of LuaInterface (from Fábio Mascarenhas/Craig Presti).

Example:
You can use/instantiate any .NET class without any previous registration or annotation. 
```csharp
	public class SomeClass
	{
		public string MyProperty {get; private set;}
		
		public SomeClass (string param1 = "defaulValue")
		{
			MyProperty = param1;
		}
		
		public int Func1 ()
		{
			return 32;
		}
		
		public string AnotherFunc (int val1, string val2)
		{
			return "Some String";
		}
		
		public static string StaticMethod (int param)
		{
			return "Return of Static Method";
		}

```

* Using UTF-8 Encoding:

NLua runs on top of [KeraLua](https://github.com/NLua/KeraLua) binding, it encodes the string using the ASCII encoding by default.
If you want to use UTF-8 encoding, just set the `Lua.State.Encoding` property to `Encoding.UTF8`:

```csharp

using (Lua lua = new Lua())
{
	lua.State.Encoding = Encoding.UTF8;
	lua.DoString("res = 'Файл'");
	string res = (string)lua["res"];

	Assert.AreEqual("Файл", res);
}

```



Creating Lua state:

```csharp
	using NLua;
	
	Lua state = new Lua ()

```

Evaluating simple expressions:
```csharp
	var res = state.DoString ("return 10 + 3*(5 + 2)")[0] as double;
	// Lua can return multiple values, for this reason DoString return a array of objects
```

Passing raw values to the state:

```csharp
	double val = 12.0;
	state ["x"] = val; // Create a global value 'x' 
	var res = (double)state.DoString ("return 10 + x*(5 + 2)")[0];
```


Retrieving global values:

```csharp
	state.DoString ("y = 10 + x*(5 + 2)");
	var y = state ["y"] as double; // Retrieve the value of y
```

Retrieving Lua functions:

```csharp
	state.DoString (@"
	function ScriptFunc (val1, val2)
		if val1 > val2 then
			return val1 + 1
		else
			return val2 - 1
		end
	end
	");
	var scriptFunc = state ["ScriptFunc"] as LuaFunction;
	var res = (int)scriptFunc.Call (3, 5).First ();
	// LuaFunction.Call will also return a array of objects, since a Lua function
	// can return multiple values
```

##Using the .NET objects.##

Passing .NET objects to the state:

```csharp
	SomeClass obj = new SomeClass ("Param");
	state ["obj"] = obj; // Create a global value 'obj' of .NET type SomeClass 
	// This could be any .NET object, from BCL or from your assemblies
```

Using .NET assemblies inside Lua:

To access any .NET assembly to create objects, events etc inside Lua you need to ask NLua to use CLR as a Lua package.
To do this just use the method `LoadCLRPackage` and use the `import` function inside your Lua script to load the Assembly.

```csharp
	state.LoadCLRPackage ();
	state.DoString (@" import ('MyAssembly', 'MyNamespace') 
			   import ('System.Web') ");
	// import will load any .NET assembly and they will be available inside the Lua context.
```

Creating .NET objects:
To create object you only need to use the class name with the `()`.

```csharp
state.DoString (@"
	 obj2 = SomeClass() -- you can suppress default values.
	 client = WebClient()
	");
```

Calling instance methods:
To call instance methods you need to use the `:` notation, you can call methods from objects passed to Lua or to objects created inside the Lua context.

```csharp
	state.DoString (@"
	local res1 = obj:Func1()
	local res2 = obj2:AnotherFunc (10, 'hello')
	local res3 = client:DownloadString('http://nlua.org')
	");
```

Calling static methods:
You can call static methods using only the class name and the `.` notation from Lua.

```csharp
	state.DoString (@"
	local res4 = SomeClass.StaticMethod(4)
	");
```

Calling properties:
You can get (or set) any property using  `.` notation from Lua.

```csharp
	state.DoString (@"
	local res5 = obj.MyProperty
	");
```

All methods, events or property need to be public available, NLua will fail to call non-public members.

If you are using Xamarin.iOS you need to [`Preserve`](http://developer.xamarin.com/guides/ios/advanced_topics/linker/) the class you want to use inside NLua, otherwise the Linker will remove the class from final binary if the class is not in use.

##Sandboxing##

There is many ways to sandbox scripts inside your application. I strongly recommend you to use plain Lua to do your sandbox.
You can re-write the `import` function before load the user script and if the user try to import a .NET assembly nothing will happen.

```csharp
	state.DoString (@"
		import = function () end
	");
```
[Lua-Sandbox user-list](http://lua-users.org/wiki/SandBoxes)


Copyright (c) 2019 Vinicius Jarina (viniciusjarina@gmail.com)

NLua 1.4.x
----------

NLua huge cleanup and refactor after a few years.

* Moved to .NET C# style.
* Using KeraLua as nuget dependencie.
* Droped support for KopiLua/Silverlight/Windows Phone


NLua 1.3.2
----------

* Migration to unified Xamarin.iOS (iOS)
* Added __call method to call Actions/Funcs from Lua as Lua functions.
* Fixed [#116](https://github.com/NLua/NLua/issues/116) problem accessing base class method
* Fixed [#117](https://github.com/NLua/NLua/issues/117) problem with same method in class and base class
* Fixed [#125](https://github.com/NLua/NLua/issues/125) calling methods with params keyword.

NLua 1.3.1
----------
* Added support to WinRT (Windows Phone 8)
* Added support to Unity3D
* Update Lua 5.2.3 with latest patches
* Fixed support to Unicode strings (UTF-8)
* [Fixed x86/x64 issue](https://github.com/NLua/NLua/issues/67). 
* [Fixed overload issue](https://github.com/NLua/NLua/issues/103)
* [Fixed support to Debug and DebugHook APIs](https://github.com/NLua/NLua/issues/31)
* [Added support to operators call](https://github.com/NLua/NLua/issues/57)
* [Fixed access to keys with .](https://github.com/NLua/NLua/issues/68)
* [Fixed issue with ValueTypes](https://github.com/NLua/NLua/issues/73)

NLua 1.3.0
----------
* Update Lua to 5.2.3
* Update to Xamarin components store. (http://components.xamarin.com/view/NLua)

NLua 1.2.0
----------
* NuGet Package (https://www.nuget.org/packages/NLua/)
* Port to Android 15+ (armeabi, v7a, x86)
* Updated Lua 5.2.2 (patch 7)
* Lot of Bug fixes.


NLua 1.1.0
----------
* Port to WP7 (Thanks to Mangatome)
* NLua now using Lua 5.2.2
* Bug fixes.

NLua 1.0.0
----------
* Forked from LuaInterface 2.0.4
* Added iOS support using KeraLua (C# P/Invoke Lua)


>###Help NLua###
>* Contributing
>  --------------
> * NLua uses the Mono Code-Style http://www.mono-project.com/Coding_Guidelines .
> * Please, do not change the line-end or re-indent the code.
> * Run the tests before you push.
> * Avoid pushing style changes (unless they are really needed), renaming and move code.

Old History
-----------
LuaInterface  
--------------

Copyright (c) 2003-2006 Fabio Mascarenhas de Queiroz

Maintainer: Craig Presti, craig@vastpark.com

lua51.dll and lua51.exe are Copyright (c) 2005 Tecgraf, PUC-Rio


Getting started with NLua:
-------------------------

* Look at src/TestNLua/TestLua to see an example of usage from C# 
(optionally you can run this from inside the NLua solution using the debugger).  
Also provides a good example of how to override .NET methods of Lua and usage of NLua
from within your .NET application.

* Look at samples/testluaform.lua to see examples of how to use 
.NET inside Lua

* More installation and usage instructions in the doc/guide.pdf file.

What's new in LuaInterface 2.0.3
------------------------------
* Fix: Private methods accessible via LuaInterface
* Fix: Method overload lookup failures
* Fix: Lua DoFile memory leaks when file not found (submitted by Paul Moore)
* Fix: Lua Dispose not freeing memory (submitted by Paul Moore)
* Fix: Better support for accessing indexers
* Fix: Parsing error for MBCS characters (qingrui.li)
* Fix: Dispose errors originating from LuaTable, LuaFunction, LuaUserData
* Fix: LuaInterface no longer disposes the state when passed one via the overloaded constructor
* Added: LoadString and LoadFile (submitted by Paul Moore)
* Added: Overloaded DoString
* Added: Lua debugging support (rostermeier)


What's new in LuaInterface 2.0.1
------------------------------
* Apparently the 2.0 built binaries had an issue for some users, this is just a rebuild with the lua sources pulled into the LuaInterface.zip

What's new in LuaInterface 2.0
------------------------------
* The base lua5.1.2 library is now built as entirely manged code.  LuaInterface is now pure CIL
* Various adapters to connect the older x86 version of lua are no longer needed
* Performance fixes contributed by Toby Lawrence, Oliver Nemoz and Craig Presti

What's new in LuaInterface 1.5.3
----------
* Internal lua panics (due to API violations) now throw LuaExceptions into .net
* If .net code throws an exception into Lua and lua does not handle it, the
original exception is forwarded back out to .net land.
* Fix bug in the Lua 5.1.1 gmatch C code - it was improperly assuming gmatch
only works with tables.

What's new in LuaInterface 1.5.2
----------
* Overriding C# methods from Lua is fixed (broken with .net 2.0!)
* Registering static C# functions for Lua is fixed (broken with Lua-5.1.1)
* Rebuilt to fix linking problems with the binaries included in 1.5.1
* RegisterFunction has been leaking things onto the stack 

What's new in LuaInterface 1.5.1
----------
Fix a serious bug w.r.t. garbage collection - made especially apparent 
with the new lua5.1 switch: If you were *very* unlucky with timing 
sometimes Lua would loose track of pointers to CLR functions.

When I added support for static methods, I allowed the user to use either a 
colon or a dot to separate the method from the class name.  This was not 
correct - it broke disambiguation between overloaded static methods.  
Therefore, LuaInterface is now more strict: If you want to call a static 
method, you must use dot to separate the method name from the class name.  Of
course you can still use a colon if an _instance_ is being used.

Static method calls are now much faster (due to better caching).

What's new in LuaInterface 1.5
----------
LuaInterface is now updated to be based on Lua5.1.1.  You can either use 
your own build/binaries for Lua5.1.1 or use the version distributed here. 
(Lots of thanks to Steffen Itterheim for this work!)

LuaInterface.Lua no longer has OpenLibs etc... The base mechanism for 
library loading for Lua has changed, and we haven't yet broken apart 
the library loading for LuaInterface.  Instead, all standard Lua libraries
are automatically loaded at start up.

Fixed a bug where calls of some static methods would reference an 
invalid pointer.

Fixed a bug when strings with embedded null characters are passed in or 
out of Lua (Thanks to Daniel N�ri for the report & fix!)
 
The native components in LuaInterface (i.e. Lua51 and the loader) are 
both built as release builds - to prevent problems loading standard 
windows libraries.

Note: You do not need to download/build lua-5.1.1.zip unless you want to 
modify Lua internals (a built version of lua51.dll is included in the 
regular LuaInterface distribution)

What's New in LuaInterface 1.4
----------

Note: Fabio area of interest has moved off in other directions (hopefully only temporarily).
I've talked with Fabio and he's said he's okay with me doing a new release with various fixes
I've made over the last few months.  Changes since 1.3:

Visual Studio 2005/.Net 2.0 is supported.

Compat-5.1 is modified to expect backslash as the path seperator.

LuaInterface will now work correctly with Generic C# classes.

CLR inner types are now supported.

Fixed a problem where sometimes Lua proxy objects would be associated with the wrong CLR object.

If a CLR class has an array accessor, the elements can be accessed using the regular Lua indexing 
interface.

Add CLRPackage.lua to the samples directory.  This class makes it much easier to automatically 
load referenced assemblies.  In the next release this loading will be automatic.

To see an quick demonstration of LuaInterface, cd into nlua/samples and then 
type: ..\..\Built\debug\LuaRunner.exe testluaform.lua

Various other minor fixes that I've forgotten.  I'll keep better track next time.

Note: LuaInterface is still based on Lua 5.0.2.  If someone really wants us to upgrade to Lua 5.1
please send me a note.  In the mean time, I'm also distributing a version of
Lua 5.0.2 with an appropriate VS 2005 project file.  You do not need to
download this file unless you want to modify Lua internals (a built version
of lua50.dll is included in the regular LuaInterface distribution)

What's New in LuaInterface 1.3
----------

LuaInterface now works with LuaBinaries Release 2 (http://luabinaries.luaforge.net)
and Compat-5.1 Release 3 (http://luaforge.net/projects/compat). The loader DLL is now 
called luanet.dll, and does not need a nlua.lua file anymore
(just put LuaInterface.dll in the GAC, luanet.dll in your package.cpath, and
do require"luanet").

Fixed a bug in the treatment of the char type (thanks to Ron Scott).

LuaInterface.dll now has a strong name, and can be put in the GAC (thanks to Ivan Voras).

You can now use foreach with instances of LuaTable (thanks to Zachary Landau).

There is an alternate form of loading assemblies and importing types (based on an
anonymous contribution in the Lua wiki). Check the _alt files in the samples folder.


What's New in LuaInterface 1.2.1
--------------------------------

Now checks if two LuaInterface.Lua instances are trying to share the same Lua state,
and throws an exception if this is the case. Also included readonly clauses in public
members of the Lua and ObjectTranslator classes.

This version includes the source of LuaInterfaceLoader.dll, with VS.Net 2003 project
files.

What's New in LuaInterface 1.2
------------------------------

LuaInterface now can be loaded as a module, so you can use the lua standalone
interpreter to run scripts. Thanks to Paul Winwood for this idea and sample code
showing how to load the CLR from a C++ program. The module is "nlua". Make
sure Lua can find nlua.lua, and LuaInterfaceLoader.dll is either in the
current directory or the GAC. The samples now load LuaInterface as a module, in
its own namespace.

The get_method_bysig, get_constructor_bysig and make_object were changed: now you
pass the *names* of the types to them, instead of the types themselves. E.g:

  get_method_bysig(obj,"method","System.String")

instead of

  String = import_type("System.String")
  get_method_bysig(obj,"method",String)

Make sure the assemblies of the types you are passing have been loaded, or the call
will fail. The test cases in src/TestLuaInterface/TestLua.cs have examples of the new
functions.



