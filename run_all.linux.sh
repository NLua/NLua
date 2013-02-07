#!/bin/sh
xbuild LuaInterface.sln /p:Configuration=Release
cd tests/
nunit-console LuaInterface.Test.dll
