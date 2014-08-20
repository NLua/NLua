#!/bin/sh
xbuild NLua.Net40.sln /p:Configuration=ReleaseKopiLua
cd tests/
export MONO_PATH="/Library/Frameworks/Mono.framework/Libraries/mono/4.0/"
nunit-console NLuaTest.dll -xml=$1
