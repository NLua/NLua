#!/bin/sh
xbuild NLua.Net45.sln /p:Configuration=ReleaseKopiLua
cd tests/
export MONO_PATH="/Library/Frameworks/Mono.framework/Libraries/mono/4.5/"
nunit-console NLuaTest.dll -xml=$1
