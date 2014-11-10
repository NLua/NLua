dnl Warning: This is an automatically generated file, do not edit!
dnl Process this file with autoconf to produce a configure script.
AC_PREREQ([2.54])
AC_INIT([NLua.Net40], [2.x])
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


AC_ARG_ENABLE(debug,
	AC_HELP_STRING([--enable-debug],
		[Use 'DEBUG' Configuration [default=NO]]),
		enable_debug=yes, enable_debug=no)
AM_CONDITIONAL(ENABLE_DEBUG, test x$enable_debug = xyes)
if test "x$enable_debug" = "xyes" ; then
	CONFIG_REQUESTED="yes"
fi
AC_ARG_ENABLE(debugkopilua,
	AC_HELP_STRING([--enable-debugkopilua],
		[Use 'DEBUGKOPILUA' Configuration [default=NO]]),
		enable_debugkopilua=yes, enable_debugkopilua=no)
AM_CONDITIONAL(ENABLE_DEBUGKOPILUA, test x$enable_debugkopilua = xyes)
if test "x$enable_debugkopilua" = "xyes" ; then
	CONFIG_REQUESTED="yes"
fi
AC_ARG_ENABLE(release,
	AC_HELP_STRING([--enable-release],
		[Use 'RELEASE' Configuration [default=YES]]),
		enable_release=yes, enable_release=no)
AM_CONDITIONAL(ENABLE_RELEASE, test x$enable_release = xyes)
if test "x$enable_release" = "xyes" ; then
	CONFIG_REQUESTED="yes"
fi
AC_ARG_ENABLE(releasekopilua,
	AC_HELP_STRING([--enable-releasekopilua],
		[Use 'RELEASEKOPILUA' Configuration [default=NO]]),
		enable_releasekopilua=yes, enable_releasekopilua=no)
AM_CONDITIONAL(ENABLE_RELEASEKOPILUA, test x$enable_releasekopilua = xyes)
if test "x$enable_releasekopilua" = "xyes" ; then
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
AC_ARG_ENABLE(debugkopilua_x64,
	AC_HELP_STRING([--enable-debugkopilua_x64],
		[Use 'DEBUGKOPILUA_X64' Configuration [default=NO]]),
		enable_debugkopilua_x64=yes, enable_debugkopilua_x64=no)
AM_CONDITIONAL(ENABLE_DEBUGKOPILUA_X64, test x$enable_debugkopilua_x64 = xyes)
if test "x$enable_debugkopilua_x64" = "xyes" ; then
	CONFIG_REQUESTED="yes"
fi
AC_ARG_ENABLE(releasekopilua_x64,
	AC_HELP_STRING([--enable-releasekopilua_x64],
		[Use 'RELEASEKOPILUA_X64' Configuration [default=NO]]),
		enable_releasekopilua_x64=yes, enable_releasekopilua_x64=no)
AM_CONDITIONAL(ENABLE_RELEASEKOPILUA_X64, test x$enable_releasekopilua_x64 = xyes)
if test "x$enable_releasekopilua_x64" = "xyes" ; then
	CONFIG_REQUESTED="yes"
fi
if test -z "$CONFIG_REQUESTED" ; then
	AM_CONDITIONAL(ENABLE_RELEASE, true)
	enable_release=yes
fi


dnl package checks, common for all configs

dnl package checks, per config


AC_CONFIG_FILES([
Core/KopiLua/KopiLua/kopilua.net40.pc
Core/KopiLua/KopiLua/Makefile
Core/KeraLua/src/keralua.net40.pc
Core/KeraLua/src/Makefile
Core/NLua/nlua.net40.pc
Core/NLua/Makefile
Core/Makefile
NLuaTest/nluatest.40.pc
NLuaTest/Makefile
ConsoleTest/consoletest.40
ConsoleTest/Makefile
Applications/LuaRunner/nlua.40
Applications/LuaRunner/Makefile
Makefile

])

AC_OUTPUT
