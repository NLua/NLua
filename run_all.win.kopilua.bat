erase tests\*.dll
msbuild NLua.sln /p:Configuration=ReleaseKopiLua /p:Platform="Any CPU"
cd tests/
nunit-console NLuaTest.dll /xml=$1
cd ..