erase tests\*.dll
cd Core\KeraLua
call Makefile.Win32.bat
msbuild KeraLua.sln /p:Configuration=Release
cd ..\..
xcopy Core\KeraLua\external\lua\win32\bin\*.dll tests\*.dll
msbuild NLua.sln /p:Configuration=Release
cd tests/
nunit-console NLuaTest.dll /xml=$1
cd ..
erase tests\*.dll
msbuild NLua.sln /p:Configuration=ReleaseKopiLua
cd tests/
nunit-console NLuaTest.dll /xml=$1

