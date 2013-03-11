cd Core/KeraLua
Makefile.Win32.bat
msbuild KeraLua.sln /p:Configuration=Release
xcopy Core\KeraLua\external\lua\win32\bin\*.dll tests\*.dll
msbuild LuaInterface.sln /p:Configuration=Release
cd tests/
nunit-console LuaInterfaceTest.dll -xml=$1
