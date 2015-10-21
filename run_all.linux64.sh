#!/bin/sh
# If you have mono x86 installed on a amd64 linux.

export CFLAGS=-m64
export CXXFLAGS=-m64
export LDFLAGS=-m64
#export LD_LIBRARY_PATH=$PWD/external/lua/linux/lib64

cd Core/KeraLua/
make -f Makefile.Linux
xbuild KeraLua.Net45.sln /p:Configuration=Release
cd ../../
xbuild NLua.Net45.sln /p:Configuration=Release
export LD_LIBRARY_PATH=$PWD/Core/KeraLua/external/lua/linux/lib64
cd tests/
nunit-console NLuaTest.dll
