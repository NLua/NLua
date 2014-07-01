NLua
========



[![Logo](https://secure.gravatar.com/avatar/77ecf0fb9d8419be7715c6e822e66562?s=150)]()

NLua is a fork of project LuaInterface (from Fábio Mascarenhas/Craig Presti).

[![Cmd](https://raw.github.com/NLua/NLua/master/NLuaCommand.gif)]()

Example: using NLua from command line.

NLua allow use Lua from C#, using Windows, Linux, Mac, iOS , Android, Windows Phone 7 and Windows Phone 8.

Linux: [![Build Status](https://travis-ci.org/NLua/NLua.png?branch=master)](https://travis-ci.org/NLua/NLua)

OSX: [![Build Status](http://codefoco.zapto.org:8085/buildStatus/icon?job=NLua)](http://codefoco.zapto.org:8085/job/NLua/) 
**Download** [![dwn_osx][2]][1]

  [1]: https://www.dropbox.com/s/w99igtc12uocq4k/NLua.OSX.zip
  [2]: http://nvlabs.github.com/cub/download-icon.png (Download for OSX)

iOS :  [![Build Status](http://codefoco.zapto.org:8085/buildStatus/icon?job=NLua_iOS)](http://codefoco.zapto.org:8085/job/NLua_iOS/)
**Download** [![dwn_ios][4]][3]

  [3]: https://www.dropbox.com/s/s3xte19719446lx/NLua.iOS.zip
  [4]: http://nvlabs.github.com/cub/download-icon.png (Download for iOS)

Android: [![Build Status](http://codefoco.zapto.org:8085/buildStatus/icon?job=NLua_Android)](http://codefoco.zapto.org:8085/job/NLua_Android/)**Download** [![dwn_android][6]][5]
  [5]: https://www.dropbox.com/s/mjet2sh67e7y6xo/NLua.Android.zip
  [6]: http://nvlabs.github.com/cub/download-icon.png (Download for Android)
  
Win32: **Download** [![dwn_w32][8]][7]

  [7]: https://www.dropbox.com/s/jkr1pnwvqw6w0r8/NLua.Win32.zip
  [8]: http://nvlabs.github.com/cub/download-icon.png (Download for Win32)
  
Win64: **Download** [![dwn_w64][10]][9]

  [9]: https://www.dropbox.com/s/xraxkgi2kuwbu4a/NLua.Win64.zip
  [10]: http://nvlabs.github.com/cub/download-icon.png (Download for Win64)
  
Windows Phone 7: **Download** [![dwn_wp7][12]][11]

  [11]: https://www.dropbox.com/s/c08wphdmk5o7tdx/NLua.WP7.zip
  [12]: http://nvlabs.github.com/cub/download-icon.png (Download for Windows Phone 7)
  
Windows Phone 8: **Download** [![dwn_wp8][14]][13]

  [13]: https://www.dropbox.com/s/47qqimfnux104a7/NLua.WP8.zip?
  [14]: http://nvlabs.github.com/cub/download-icon.png (Download for Windows Phone 8 (ARM+x86))

Unity3D support on branch [unity3d](https://github.com/NLua/NLua/tree/unity3d) by [miaodadao](https://github.com/miaodadao).

Windows: We don't have a CI Server for Windows. 
	 You can build NLua , you will need (msysgit, CMake, NUnit) http://screencast.com/t/rYuDtCdFG7
```csharp

			string script = @"
				
			local s = Scriptable (""My String Parameter"")
			s:DoSomething ()
			
			print (s.Param1)
			
			local ret = s:SumOfLengths (""Name"", 10);
			
			print (tostring(ret))
			
			Scriptable.Print(""Hello NLua"")
			
			s.Param3 = 0.5;
			
			local p2 = tostring(s.Param3)
			
			print (p2)
			";

			using (Lua lua = new Lua ()) {

				lua.LoadCLRPackage ();

				lua.DoString (@" import ('NLuaSample') ");
				
				lua ["gValue"] = "This is a global value"; // You can set a global value.

				var returns = lua.DoString (script);

				Console.WriteLine (returns);
			}
```

Copyright (c) 2014 Vinicius Jarina (viniciusjarina@gmail.com)

NLua 1.3.1
----------

Soon

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
> If you are using NLua consider to help with some easy todo items.
>
>### TODO: ###
> * Windows CI server.
> * Port to other platforms (using a csproj/sln for each platform like RestSharp/MonoGame/Cocos2d-XNA)
>	 	* Unity Pro (using p/invoke)
>		* Port NLua to use LuaJIT
> * Create a NuGet package


> * Fix warnings/Gendarme/FxCop issues.
>* Contributing
>  --------------
> * NLua is using the Mono Code-Style http://www.mono-project.com/Coding_Guidelines .
> * Please, do not change the line-end or re-indent the code.
> * Run the tests before push.
> * Avoid to push unneeded style changes (unless is really needed) renaming, move code.



Old History
-----------
LuaInterface  
--------------

Copyright (c) 2003-2006 Fabio Mascarenhas de Queiroz

Maintainer: Craig Presti, craig@vastpark.com

lua51.dll and lua51.exe are Copyright (c) 2005 Tecgraf, PUC-Rio


Getting started with NLua:
-------------------------

* Look at src/TestNLua/TestLua to see example usage from C# 
(optionally run this from inside of the NLua solution in 
the debugger).  Also provides a good example of how to override .net 
methods from Lua and use NLua from within your .NET application.
* Look at samples/testluaform.lua to see examples of how to use 
.NET from inside Lua
* More instructions for installing and using in the doc/guide.pdf file.

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
library loading for Lua has changed, and we haven't yet broken appart 
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
