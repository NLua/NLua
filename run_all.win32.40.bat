erase tests\*.dll
cd Core\KeraLua
call Makefile.Win32.bat
msbuild KeraLua.Net40.sln /p:Configuration=Release /p:DefineConstants="USE_DYNAMIC_DLL_REGISTER;WSTRING" /p:Platform="Any CPU"
cd ..\..
xcopy Core\KeraLua\external\lua\win32\bin\*.dll tests\*.dll
msbuild NLua.Net40.sln /p:Configuration=Release /p:DefineConstants="USE_DYNAMIC_DLL_REGISTER;WSTRING;LUA_CORE;CATCH_EXCEPTIONS" /p:Platform="Any CPU"
cd tests/
nunit-console-x86 NLuaTest.dll /xml=$1
cd ..

