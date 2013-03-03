#!/bin/sh
cd Core/KeraLua
make -f Makefile.iOS
xbuild KeraLua.sln /p:Configuration=Release
cd ../../
make -f Makefile.iOS run

