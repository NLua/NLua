#!/bin/sh
cd Core/KeraLua
make -f Makefile.iOS
cd ../../
make -f Makefile.iOS run

