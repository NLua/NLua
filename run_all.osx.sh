#!/bin/sh
cd Core/KeraLua
make -f Makefile.OSX
xbuild KeraLua.Net40.sln /p:Configuration=Release
cd ../../
cp Core/KeraLua/external/lua/osx/lib/liblua52.dylib tests/liblua52.dylib
xbuild NLua.Net40.sln /p:Configuration=Release
cd tests/
export MONO_PATH="/Library/Frameworks/Mono.framework/Libraries/mono/4.0/"
nunit-console NLuaTest.dll -xml=$1
