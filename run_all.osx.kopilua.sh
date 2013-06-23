#!/bin/sh
xbuild NLua.sln /p:Configuration=ReleaseKopiLua
cd tests/
nunit-console NLuaTest.dll -xml=$1
