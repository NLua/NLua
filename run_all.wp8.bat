erase tests\*.dll
cd Core\KeraLua
call Makefile.Win64.bat
msbuild KeraLua.WP8.sln /p:Configuration=Release /p:DefineConstants=WSTRING /p:Platform="Any CPU"
cd ..\..
xcopy Core\KeraLua\external\lua\win64\bin64\*.dll tests\*.dll
msbuild NLua.Net45.sln /p:Configuration=Release /p:DefineConstants="WSTRING;LUA_CORE;CATCH_EXCEPTIONS" /p:Platform="Any CPU"
cd tests/
nunit-console NLuaTest.dll /xml=$1
cd ..
