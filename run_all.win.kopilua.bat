erase tests\*.dll
msbuild NLua.sln /p:Configuration=ReleaseKopiLua
cd tests/
nunit-console NLuaTest.dll /xml=$1
