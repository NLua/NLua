dnl Warning: This is an automatically generated file, do not edit!
dnl Process this file with autoconf to produce a configure script.
AC_PREREQ([2.54])
AC_INIT([LuaInterface], [2.x])
AM_INIT_AUTOMAKE([foreign])
AM_MAINTAINER_MODE

dnl pkg-config
AC_PATH_PROG(PKG_CONFIG, pkg-config, no)
if test "x$PKG_CONFIG" = "xno"; then
        AC_MSG_ERROR([You need to install pkg-config])
fi

SHAMROCK_EXPAND_LIBDIR
SHAMROCK_EXPAND_BINDIR
SHAMROCK_EXPAND_DATADIR

AC_PROG_INSTALL

AC_PATH_PROG(DMCS, dmcs, no)
if test "x$DMCS" = "xno"; then
        AC_MSG_ERROR([dmcs Not found])
fi


AC_ARG_ENABLE(debug_x86,
	AC_HELP_STRING([--enable-debug_x86],
		[Use 'DEBUG_X86' Configuration [default=NO]]),
		enable_debug_x86=yes, enable_debug_x86=no)
AM_CONDITIONAL(ENABLE_DEBUG_X86, test x$enable_debug_x86 = xyes)
if test "x$enable_debug_x86" = "xyes" ; then
	CONFIG_REQUESTED="yes"
fi
AC_ARG_ENABLE(release_x86,
	AC_HELP_STRING([--enable-release_x86],
		[Use 'RELEASE_X86' Configuration [default=YES]]),
		enable_release_x86=yes, enable_release_x86=no)
AM_CONDITIONAL(ENABLE_RELEASE_X86, test x$enable_release_x86 = xyes)
if test "x$enable_release_x86" = "xyes" ; then
	CONFIG_REQUESTED="yes"
fi
AC_ARG_ENABLE(debug_x64,
	AC_HELP_STRING([--enable-debug_x64],
		[Use 'DEBUG_X64' Configuration [default=NO]]),
		enable_debug_x64=yes, enable_debug_x64=no)
AM_CONDITIONAL(ENABLE_DEBUG_X64, test x$enable_debug_x64 = xyes)
if test "x$enable_debug_x64" = "xyes" ; then
	CONFIG_REQUESTED="yes"
fi
AC_ARG_ENABLE(release_x64,
	AC_HELP_STRING([--enable-release_x64],
		[Use 'RELEASE_X64' Configuration [default=NO]]),
		enable_release_x64=yes, enable_release_x64=no)
AM_CONDITIONAL(ENABLE_RELEASE_X64, test x$enable_release_x64 = xyes)
if test "x$enable_release_x64" = "xyes" ; then
	CONFIG_REQUESTED="yes"
fi
if test -z "$CONFIG_REQUESTED" ; then
	AM_CONDITIONAL(ENABLE_RELEASE_X86, true)
	enable_release_x86=yes
fi


dnl package checks, common for all configs

dnl package checks, per config


AC_CONFIG_FILES([
Core/KopiLua/kopilua.pc
Core/KopiLua/Makefile
Core/LuaInterface/luainterface.pc
Core/LuaInterface/Makefile
Core/Makefile
Test/TestLuaInterface/testluainterface
Test/TestLuaInterface/Makefile
Applications/LuaRunner/luarunner
Applications/LuaRunner/Makefile
Makefile

])

AC_OUTPUT
