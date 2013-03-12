#!/bin/sh
# If you have mono x86 installed on a amd64 linux.

#export CFLAGS=-m32
#export CXXFLAGS=-m32
#export LDFLAGS=-m32
#export LD_LIBRARY_PATH=$PWD/external/lua/linux/lib64

cd Core/KeraLua/
make -f Makefile.Linux
xbuild KeraLua.sln /p:Configuration=Release
cd ../../
xbuild NLua.sln /p:Configuration=Release
export LD_LIBRARY_PATH=$PWD/Core/KeraLua/external/lua/linux/lib64
cd tests/
nunit-console NLuaTest.dll
