erase tests\*.dll
cd Core\KeraLua
call Makefile.Win64.bat
msbuild KeraLua.sln /p:Configuration=Release /p:Platform="Any CPU"
cd ..\..
xcopy Core\KeraLua\external\lua\win64\bin64\*.dll tests\*.dll
msbuild NLua.sln /p:Configuration=Release /p:Platform="Any CPU"
cd tests/
nunit-console NLuaTest.dll /xml=$1
cd ..
