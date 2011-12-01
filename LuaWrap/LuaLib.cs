// 
// LuaDLL.cs
//  
// Author:
//       Joshua Simmons <simmons.44@gmail.com>
// 
// Copyright (c) 2009 Joshua Simmons
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
 
using System;
using System.Runtime.InteropServices;
 
namespace LuaWrap
{
	/// <summary>
	/// Enumeration of basic lua globals.
	/// </summary>
	public enum LuaEnum : int
	{
		/// <summary>
		/// Option for multiple returns in `lua_pcall' and `lua_call'
		/// </summary>
		MultiRet		=	-1,
 
		/// <summary>
		/// Everything is OK.
		/// </summary>
		Ok				=	0,
 
		/// <summary>
		/// Thread status, Ok or Yield
		/// </summary>
		Yield			=	1,
 
		/// <summary>
		/// A Runtime error.
		/// </summary>
		ErrorRun		=	2,
 
		/// <summary>
		/// A syntax error.
		/// </summary>
		ErrorSyntax		=	3,
 
		/// <summary>
		/// A memory allocation error. For such errors, Lua does not call the error handler function. 
		/// </summary>
		ErrorMemory		=	4,
 
		/// <summary>
		/// An error in the error handling function.
		/// </summary>
		ErrorError		=	5,
 
		/// <summary>
		/// An extra error for file load errors when using luaL_loadfile.
		/// </summary>
		ErrorFile		=	6,
	}
	
	public enum References : int
	{
		RefNil 	= -1,
		NoRef 	= -2
	}
 
	public enum LuaType : int
	{
		None 			= -1,
		Nil 			= 0,
		Boolean 		= 1,
		LightUserdata 	= 2,
		Number 			= 3,
		String 			= 4,
		Table 			= 5,
		Function 		= 6,
		UserData 		= 7,
		Thread 			= 8
	}
 
	public enum GCOption : int
	{
		/// <summary>
		/// Stops the garbage collector.
		/// </summary>
		Stop 			= 0,
 
		/// <summary>
		/// Restarts the garbage collector.
		/// </summary>
		Restart 		= 1,
 
		/// <summary>
		/// Performs a full garbage-collection cycle. 
		/// </summary>
		Collect 		= 2,
 
		/// <summary>
		/// Returns the current amount of memory (in Kbytes) in use by Lua. 
		/// </summary>
		Count 			= 3,
 
		/// <summary>
		/// Returns the remainder of dividing the current amount of bytes of memory in use by Lua by 1024. 
		/// </summary>
		CountB 			= 4,
 
		/// <summary>
		/// Performs an incremental step of garbage collection. The step "size" is controlled by data (larger values mean more steps) in a non-specified way. If you want to control the step size you must experimentally tune the value of data. The function returns 1 if the step finished a garbage-collection cycle. 
		/// </summary>
		Step 			= 5,
 
		/// <summary>
		/// Sets data as the new value for the pause (Controls how long the collector waits before starting a new cycle) of the collector (see §2.10). The function returns the previous value of the pause.
		/// </summary>
		SetPause 		= 6,
 
		/// <summary>
		/// Sets data as the new value for the step multiplier of the collector (Controls the relative speed of the collector relative to memory allocation.). The function returns the previous value of the step multiplier. 
		/// </summary>
		SetStepMul 		= 7
	}
 
	public enum PseudoIndex : int
	{
		Registry 		= -10000,
		Environment 	= -10001,
		Globals 		= -10002
	}
 
	/// <summary>
	/// A delegate for C# function callbacks passed to Lua.
	/// </summary>
	public delegate int CallbackFunction( IntPtr state );
 
	public static class LuaLib
	{
		private const string Lib = "lua51.dll";
		private static int tag = 0;
 
		#region Core Library
 
		[DllImport( Lib, CallingConvention = CallingConvention.Cdecl )]
		/// <summary>
		/// If an error happens outside any protected environment, Lua calls a panic function and then calls exit(EXIT_FAILURE), thus exiting the host application. Your panic function can avoid this exit by never returning (e.g., doing a long jump). The panic function can access the error message at the top of the stack.
		/// </summary>
		/// <param name="state">
		/// A Lua State. <see cref="IntPtr"/>
		/// </param>
		/// <param name="cb">
		/// A new panic function. <see cref="CallbackFunction"/>
		/// </param>
		public static extern void lua_atpanic( IntPtr state, CallbackFunction cb );
 
		[DllImport( Lib, CallingConvention = CallingConvention.Cdecl )]
		/// <summary>
		/// To call a function you must use the following protocol: first, the function to be called is pushed onto the stack; then, the arguments to the function are pushed in direct order; that is, the first argument is pushed first. Finally you call lua_call.
		/// </summary>
		/// <param name="state">
		/// A Lua State <see cref="IntPtr"/>
		/// </param>
		/// <param name="nargs">
		/// Number of arguments to pass to the function. A <see cref="System.Int32"/>
		/// </param>
		/// <param name="nresults">
		/// Number of results expected, or Globals.MultiRet for all results. A <see cref="System.Int32"/>
		/// </param>
		public static extern void lua_call( IntPtr state, int nargs, int nresults );
 
		[DllImport( Lib, CallingConvention = CallingConvention.Cdecl )]
		/// <summary>
		/// Ensures that there are at least extra free stack slots in the stack. It returns false if it cannot grow the stack to that size. This function never shrinks the stack; if the stack is already larger than the new size, it is left unchanged. 
		/// </summary>
		/// <param name="state">
		/// A Lua State. <see cref="IntPtr"/>
		/// </param>
		/// <param name="extra">
		/// Extra slots to request. A <see cref="System.Int32"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.Boolean"/>
		/// </returns>
		public static extern bool lua_checkstack( IntPtr state, int extra );
 
		[DllImport( Lib, CallingConvention = CallingConvention.Cdecl )]
		/// <summary>
		/// Destroys all objects in the given Lua state (calling the corresponding garbage-collection metamethods, if any) and frees all dynamic memory used by this state.
		/// </summary>
		/// <param name="state">
		/// A Lua State to be destroyed. <see cref="IntPtr"/>
		/// </param>
		public static extern void lua_close( IntPtr state );
		
		[DllImport( Lib, CallingConvention = CallingConvention.Cdecl )]
		/// <summary>
		/// Concatenates the n values at the top of the stack, pops them, and leaves the result at the top. If n is 1, the result is the single value on the stack (that is, the function does nothing); if n is 0, the result is the empty string. Concatenation is performed following the usual semantics of Lua.
		/// </summary>
		/// <param name="state">
		/// A <see cref="IntPtr"/>
		/// </param>
		/// <param name="n">
		/// A <see cref="System.Int32"/>
		/// </param>
		public static extern void lua_concat( IntPtr state, int n );
 
		[DllImport( Lib, CallingConvention = CallingConvention.Cdecl )]
		/// <summary>
		/// Creates a new empty table and pushes it onto the stack. The new table has space pre-allocated for narr array elements and nrec non-array elements. This pre-allocation is useful when you know exactly how many elements the table will have. Otherwise you can use the function lua_newtable. 
		/// </summary>
		/// <param name="state">
		/// A <see cref="IntPtr"/>
		/// </param>
		/// <param name="narr">
		/// Number of pre-allocated array elements. A <see cref="System.Int32"/>
		/// </param>
		/// <param name="nrec">
		/// Number of pre-allocated non-array elements. A <see cref="System.Int32"/>
		/// </param>
		public static extern void lua_createtable( IntPtr state, int narr, int nrec );
 
		[DllImport( Lib, CallingConvention = CallingConvention.Cdecl )]
		/// <summary>
		/// Returns 1 if the two values in acceptable indices index1 and index2 are equal, following the semantics of the Lua == operator (that is, may call metamethods). Otherwise returns 0. Also returns 0 if any of the indices is non valid. 
		/// </summary>
		/// <param name="state">
		/// A Lua State <see cref="IntPtr"/>
		/// </param>
		/// <param name="index1">
		/// First index to compare. A <see cref="System.Int32"/>
		/// </param>
		/// <param name="index2">
		/// Second index to compare. A <see cref="System.Int32"/>
		/// </param>
		/// <returns>
		/// A result code integer <see cref="System.Int32"/>
		/// </returns>
		public static extern int lua_equal( IntPtr state, int index1, int index2 );
 
		[DllImport( Lib, CallingConvention = CallingConvention.Cdecl )]
		/// <summary>
		/// Generates a Lua error. The error message (which can actually be a Lua value of any type) must be on the stack top. This function does a long jump, and therefore never returns. (see luaL_error). 
		/// </summary>
		/// <param name="state">
		/// A Lua State<see cref="IntPtr"/>
		/// </param>
		public static extern void lua_error( IntPtr state );
 
		[DllImport( Lib, CallingConvention = CallingConvention.Cdecl )]
		/// <summary>
		/// Controls the garbage collector. 
		/// This function performs several tasks, according to the value of the parameter what:
		/// </summary>
		/// <param name="state">
		/// A Lua State. <see cref="IntPtr"/>
		/// </param>
		/// <param name="what">
		/// Collector option. <see cref="GCOption"/>
		/// </param>
		/// <param name="data">
		/// Value to change. A <see cref="System.Int32"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.Int32"/>
		/// </returns>
		public static extern int lua_gc( IntPtr state, GCOption what, int data );
 
		[DllImport( Lib, CallingConvention = CallingConvention.Cdecl )]
		/// <summary>
		/// Pushes onto the stack the environment table of the value at the given index. 
		/// </summary>
		/// <param name="state">
		/// A <see cref="IntPtr"/>
		/// </param>
		/// <param name="index">
		/// A <see cref="System.Int32"/>
		/// </param>
		public static extern void lua_getfenv( IntPtr state, int index );
 
		[DllImport( Lib, CallingConvention = CallingConvention.Cdecl )]
		/// <summary>
		/// Pushes onto the stack the value t[k], where t is the value at the given valid index. As in Lua, this function may trigger a metamethod for the "index" event
		/// </summary>
		/// <param name="state">
		/// A Lua State <see cref="IntPtr"/>
		/// </param>
		/// <param name="index">
		/// The stack index for the table to query. A <see cref="System.Int32"/>
		/// </param>
		/// <param name="key">
		/// The key to aquire. A <see cref="System.String"/>
		/// </param>
		public static extern void lua_getfield( IntPtr state, int index, string key );
 
		/// <summary>
		/// Pushes onto the stack the value of the global name.
		/// </summary>
		/// <param name="state">
		/// A <see cref="IntPtr"/>
		/// </param>
		/// <param name="name">
		/// A <see cref="System.String"/>
		/// </param>
		public static void lua_getglobal( IntPtr state, string name )
		{
			lua_getfield( state, (int)PseudoIndex.Globals, name );
		}
		
		[DllImport( Lib, CallingConvention = CallingConvention.Cdecl )]
		/// <summary>
		/// Pushes onto the stack the metatable of the value at the given acceptable index. If the index is not valid, or if the value does not have a metatable, the function returns 0 and pushes nothing on the stack. 
		/// </summary>
		/// <param name="state">
		/// A Lua State. <see cref="IntPtr"/>
		/// </param>
		/// <param name="index">
		/// A valid stack index. <see cref="System.Int32"/>
		/// </param>
		public static extern int lua_getmetatable( IntPtr state, int index );
 
		[DllImport( Lib, CallingConvention = CallingConvention.Cdecl )]
		/// <summary>
		/// Pushes onto the stack the value t[k], where t is the value at the given valid index and k is the value at the top of the stack. 
		/// This function pops the key from the stack (putting the resulting value in its place). 
		/// As in Lua, this function may trigger a metamethod for the "index" event
		/// </summary>
		/// <param name="state">
		/// A Lua State. <see cref="IntPtr"/>
		/// </param>
		/// <param name="index">
		/// A valid stack index. <see cref="System.Int32"/>
		/// </param>
		public static extern void lua_gettable( IntPtr state, int index );
 
		[DllImport( Lib, CallingConvention = CallingConvention.Cdecl )]
		/// <summary>
		/// Returns the index of the top element in the stack. Because indices start at 1, this result is equal to the number of elements in the stack (and so 0 means an empty stack). 
		/// </summary>
		/// <param name="state">
		/// A Lua State. <see cref="IntPtr"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.Int32"/>
		/// </returns>
		public static extern int lua_gettop( IntPtr state );
 
		[DllImport( Lib, CallingConvention = CallingConvention.Cdecl )]
		/// <summary>
		/// Moves the top element into the given valid index, shifting up the elements above this index to open space. Cannot be called with a pseudo-index, because a pseudo-index is not an actual stack position.
		/// </summary>
		/// <param name="state">
		/// A Lua State. <see cref="IntPtr"/>
		/// </param>
		/// <param name="index">
		/// A valid stack index. <see cref="System.Int32"/>
		/// </param>
		public static extern void lua_insert( IntPtr state, int index );
 
		public static bool lua_isboolean( IntPtr state, int index )
		{
			return lua_type( state, index ) == LuaType.Boolean;
		}
		
		[DllImport( Lib, CallingConvention = CallingConvention.Cdecl )]
		public static extern bool lua_iscfunction( IntPtr state, int index );
 
		public static bool lua_isfunction( IntPtr state, int index )
		{
			return lua_type( state, index ) == LuaType.Function;
		}
 
		public static bool lua_islightuserdata( IntPtr state, int index )
		{
			return lua_type( state, index ) == LuaType.LightUserdata;
		}
 
		public static bool lua_isnil( IntPtr state, int index )
		{
			return lua_type( state, index ) == LuaType.Nil;
		}
 
		public static bool lua_isnone( IntPtr state, int index )
		{
			return lua_type( state, index ) == LuaType.None;
		}
 
		public static bool lua_isnoneornil( IntPtr state, int index )
		{
			LuaType t = lua_type( state, index );
			return t == LuaType.None || t == LuaType.Nil;
		}
 
		public static bool lua_isnumber( IntPtr state, int index )
		{
			return lua_type( state, index ) == LuaType.Number;
		}
 
		public static bool lua_isstring( IntPtr state, int index )
		{
			return lua_type( state, index ) == LuaType.String;
		}
 
		public static bool lua_istable( IntPtr state, int index )
		{
			return lua_type( state, index ) == LuaType.Table;
		}
 
		public static bool lua_isthread( IntPtr state, int index )
		{
			return lua_type( state, index ) == LuaType.Thread;
		}
 
		public static bool lua_isuserdata( IntPtr state, int index )
		{
			return lua_type( state, index ) == LuaType.UserData;			
		}
		
		[DllImport( Lib, CallingConvention = CallingConvention.Cdecl )]
		/// <summary>
		/// Returns true if the value at acceptable index index1 is smaller than the value at acceptable index index2, following the semantics of the Lua < operator (that is, may call metamethods). Otherwise returns false. Also returns false if any of the indices is non valid. 
		/// </summary>
		/// <param name="state">
		/// A <see cref="IntPtr"/>
		/// </param>
		/// <param name="index1">
		/// A <see cref="System.Int32"/>
		/// </param>
		/// <param name="index2">
		/// A <see cref="System.Int32"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.Boolean"/>
		/// </returns>
		public static extern bool lua_lessthan( IntPtr state, int index1, int index2 );
 
		/// <summary>
		/// Creates a new empty table and pushes it onto the stack. It is equivalent to lua_createtable(L, 0, 0).
		/// </summary>
		/// <param name="state">
		/// A <see cref="IntPtr"/>
		/// </param>
		public static void lua_newtable( IntPtr state )
		{
			lua_createtable( state, 0, 0 );	
		}
		
		[DllImport( Lib, CallingConvention = CallingConvention.Cdecl )]
		/// <summary>
		/// This function allocates a new block of memory with the given size, pushes onto the stack a new full userdata with the block address, and returns this address. 
		/// </summary>
		/// <param name="state">
		/// A <see cref="IntPtr"/>
		/// </param>
		/// <param name="size">
		/// A <see cref="System.Int32"/>
		/// </param>
		/// <returns>
		/// A <see cref="IntPtr"/>
		/// </returns>
		public static extern IntPtr lua_newuserdata( IntPtr state, int size );
 
		[DllImport( Lib, CallingConvention = CallingConvention.Cdecl )]
		/// <summary>
		/// Pops a key from the stack, and pushes a key-value pair from the table at the given index (the "next" pair after the given key). If there are no more elements in the table, then lua_next returns false (and pushes nothing). 
		/// </summary>
		/// <param name="state">
		/// A <see cref="IntPtr"/>
		/// </param>
		/// <param name="index">
		/// A <see cref="System.Int32"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.Int32"/>
		/// </returns>
		public static extern bool lua_next( IntPtr state, int index );
 
		[DllImport( Lib, CallingConvention = CallingConvention.Cdecl )]
		/// <summary>
		/// Returns the "length" of the value at the given acceptable index: for strings, this is the string length; for tables, this is the result of the length operator ('#'); for userdata, this is the size of the block of memory allocated for the userdata; for other values, it is 0. 
		/// </summary>
		/// <param name="state">
		/// A <see cref="IntPtr"/>
		/// </param>
		/// <param name="index">
		/// A <see cref="System.Int32"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.Int32"/>
		/// </returns>
		public static extern int lua_objlen( IntPtr state, int index );
		
		[DllImport( Lib, CallingConvention = CallingConvention.Cdecl )]
		/// <summary>
		/// Calls a function in protected mode.
		/// Both nargs and nresults have the same meaning as in lua_call. If there are no errors during the call, lua_pcall behaves exactly like lua_call. However, if there is any error, lua_pcall catches it, pushes a single value on the stack (the error message), and returns an error code. Like lua_call, lua_pcall always removes the function and its arguments from the stack.
		/// If errfunc is 0, then the error message returned on the stack is exactly the original error message. Otherwise, errfunc is the stack index of an error handler function. (In the current implementation, this index cannot be a pseudo-index.) In case of runtime errors, this function will be called with the error message and its return value will be the message returned on the stack by lua_pcall.
		/// Typically, the error handler function is used to add more debug information to the error message, such as a stack traceback. Such information cannot be gathered after the return of lua_pcall, since by then the stack has unwound. 
		/// </summary>
		/// <param name="state">
		/// A <see cref="IntPtr"/>
		/// </param>
		/// <param name="nargs">
		/// A <see cref="System.Int32"/>
		/// </param>
		/// <param name="nresults">
		/// A <see cref="System.Int32"/>
		/// </param>
		/// <param name="errfunc">
		/// A <see cref="System.Int32"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.Int32"/>
		/// </returns>
		public static extern LuaEnum lua_pcall( IntPtr state, int nargs, int nresults, int errfunc );
 
		/// <summary>
		/// Pops n elements from the stack.
		/// </summary>
		/// <param name="state">
		/// A <see cref="IntPtr"/>
		/// </param>
		/// <param name="n">
		/// A <see cref="System.Int32"/>
		/// </param>
		public static void lua_pop( IntPtr state, int n )
		{
			lua_settop( state, -n - 1 );
		}
 
		[DllImport( Lib, CallingConvention = CallingConvention.Cdecl )]
		/// <summary>
		/// Pushes a boolean value with value b onto the stack. 
		/// </summary>
		/// <param name="state">
		/// A <see cref="IntPtr"/>
		/// </param>
		/// <param name="str">
		/// A <see cref="System.String"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.Int32"/>
		/// </returns>
		public static extern void lua_pushboolean( IntPtr state, bool b );
 
		[DllImport( Lib, CallingConvention = CallingConvention.Cdecl )]
		/// <summary>
		/// Pushes a new C closure onto the stack.
		/// When a C function is created, it is possible to associate some values with it, thus creating a C closure; these values are then accessible to the function whenever it is called. To associate values with a C function, first these values should be pushed onto the stack (when there are multiple values, the first value is pushed first). Then lua_pushcclosure is called to create and push the C function onto the stack, with the argument n telling how many values should be associated with the function. lua_pushcclosure also pops these values from the stack.
		/// </summary>
		/// <param name="state">
		/// A <see cref="IntPtr"/>
		/// </param>
		/// <param name="fn">
		/// A <see cref="CallbackFunction"/>
		/// </param>
		/// <param name="n">
		/// A <see cref="System.Int32"/>
		/// </param>
		public static extern void lua_pushcclosure( IntPtr state, CallbackFunction fn, int n );
 
		/// <summary>
		/// Pushes a C function onto the stack. This function receives a pointer to a C function and pushes onto the stack a Lua value of type function that, when called, invokes the corresponding C function. 
		/// </summary>
		/// <param name="state">
		/// A <see cref="IntPtr"/>
		/// </param>
		/// <param name="fn">
		/// A <see cref="CallbackFunction"/>
		/// </param>
		public static void lua_pushcfunction( IntPtr state, CallbackFunction fn )
		{
			lua_pushcclosure( state, fn, 0 );
		}
		
		[DllImport( Lib, CallingConvention = CallingConvention.Cdecl )]
		/// <summary>
		/// Pushes a number with value n onto the stack.
		/// </summary>
		/// <param name="state">
		/// A <see cref="IntPtr"/>
		/// </param>
		/// <param name="str">
		/// A <see cref="System.String"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.Int32"/>
		/// </returns>
		public static extern void lua_pushinteger( IntPtr state, int i );
 
		[DllImport( Lib, CallingConvention = CallingConvention.Cdecl )]
		/// <summary>
		/// Pushes a light userdata onto the stack. 
		/// Userdata represent C values in Lua. A light userdata represents a pointer. It is a value (like a number): you do not create it, it has no individual metatable, and it is not collected (as it was never created). A light userdata is equal to "any" light userdata with the same C address.
		/// </summary>
		/// <param name="state">
		/// A <see cref="IntPtr"/>
		/// </param>
		/// <param name="p">
		/// A <see cref="IntPtr"/>
		/// </param>
		public static extern void lua_pushlightuserdata( IntPtr state, IntPtr p );
 
		[DllImport( Lib, CallingConvention = CallingConvention.Cdecl )]
		/// <summary>
		/// Pushes the string s with size len onto the stack. Lua makes (or reuses) an internal copy of the given string, so the memory at s can be freed or reused immediately after the function returns. The string can contain embedded zeros.
		/// </summary>
		/// <param name="state">
		/// A <see cref="IntPtr"/>
		/// </param>
		/// <param name="s">
		/// A <see cref="System.String"/>
		/// </param>
		/// <param name="len">
		/// A <see cref="System.Int32"/>
		/// </param>
		public static extern void lua_pushlstring( IntPtr state, string s, int len );
 
		[DllImport( Lib, CallingConvention = CallingConvention.Cdecl )]
		/// <summary>
		/// Pushes nil onto the stack.
		/// </summary>
		/// <param name="state">
		/// A <see cref="IntPtr"/>
		/// </param>
		public static extern void lua_pushnil( IntPtr state );
 
		[DllImport( Lib, CallingConvention = CallingConvention.Cdecl )]
		/// <summary>
		/// Push a lua number onto the stack.
		/// </summary>
		/// <param name="state">
		/// A <see cref="IntPtr"/>
		/// </param>
		/// <param name="number">
		/// A <see cref="System.Double"/>
		/// </param>
		public static extern void lua_pushnumber( IntPtr state, double number );
 
		[DllImport( Lib, CallingConvention = CallingConvention.Cdecl )]
		/// <summary>
		/// Pushes a string onto the stack.
		/// </summary>
		/// <param name="state">
		/// A <see cref="IntPtr"/>
		/// </param>
		/// <param name="s">
		/// A <see cref="System.String"/>
		/// </param>
		public static extern void lua_pushstring( IntPtr state, string s );
 
		[DllImport( Lib, CallingConvention = CallingConvention.Cdecl )]
		/// <summary>
		/// Pushes a copy of the object at index onto the stack.
		/// </summary>
		/// <param name="state">
		/// A <see cref="IntPtr"/>
		/// </param>
		/// <param name="str">
		/// A <see cref="System.String"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.Int32"/>
		/// </returns>
		public static extern void lua_pushvalue( IntPtr state, int index );
 
		[DllImport( Lib, CallingConvention = CallingConvention.Cdecl )]
		/// <summary>
		/// Returns true if the two values in acceptable indices index1 and index2 are primitively equal (that is, without calling metamethods). Otherwise returns false. Also returns false if any of the indices are non valid. 
		/// </summary>
		/// <param name="state">
		/// A <see cref="IntPtr"/>
		/// </param>
		/// <param name="index1">
		/// A <see cref="System.Int32"/>
		/// </param>
		/// <param name="index2">
		/// A <see cref="System.Int32"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.Boolean"/>
		/// </returns>
		public static extern bool lua_rawequal( IntPtr state, int index1, int index2 );
 
		[DllImport( Lib, CallingConvention = CallingConvention.Cdecl )]
		/// <summary>
		/// Similar to lua_gettable, but does a raw access (i.e., without metamethods).
		/// </summary>
		/// <param name="state">
		/// A <see cref="IntPtr"/>
		/// </param>
		/// <param name="index">
		/// A <see cref="System.Int32"/>
		/// </param>
		public static extern void lua_rawget( IntPtr state, int index );
 
		[DllImport( Lib, CallingConvention = CallingConvention.Cdecl )]
		/// <summary>
		/// Pushes onto the stack the value t[n], where t is the value at the given valid index. The access is raw; that is, it does not invoke metamethods.
		/// </summary>
		/// <param name="state">
		/// A <see cref="IntPtr"/>
		/// </param>
		/// <param name="index">
		/// A <see cref="System.Int32"/>
		/// </param>
		/// <param name="n">
		/// A <see cref="System.Int32"/>
		/// </param>
		public static extern void lua_rawgeti( IntPtr state, int index, int n );
 
		[DllImport( Lib, CallingConvention = CallingConvention.Cdecl )]
		/// <summary>
		/// Similar to lua_settable, but does a raw assignment (i.e., without metamethods).
		/// </summary>
		/// <param name="state">
		/// A <see cref="IntPtr"/>
		/// </param>
		/// <param name="index">
		/// A <see cref="System.Int32"/>
		/// </param>
		public static extern void lua_rawset( IntPtr state, int index );
 
		[DllImport( Lib, CallingConvention = CallingConvention.Cdecl )]
		/// <summary>
		/// Does the equivalent of t[n] = v, where t is the value at the given valid index and v is the value at the top of the stack.
		/// This function pops the value from the stack. The assignment is raw; that is, it does not invoke metamethods.
		/// </summary>
		/// <param name="state">
		/// A <see cref="IntPtr"/>
		/// </param>
		/// <param name="str">
		/// A <see cref="System.String"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.Int32"/>
		/// </returns>
		public static extern void lua_rawseti( IntPtr state, int index, int n );
 
		/// <summary>
		/// Registers a C function as the global name.
		/// </summary>
		/// <param name="state">
		/// A <see cref="IntPtr"/>
		/// </param>
		/// <param name="name">
		/// A <see cref="System.String"/>
		/// </param>
		/// <param name="fn">
		/// A <see cref="CallbackFunction"/>
		/// </param>
		public static void lua_register( IntPtr state, string name, CallbackFunction fn )
		{
			lua_pushcfunction( state, fn );
			lua_setglobal( state, name );
		}
		
		[DllImport( Lib, CallingConvention = CallingConvention.Cdecl )]
		/// <summary>
		/// Removes the element at the given valid index, shifting down the elements above this index to fill the gap. Cannot be called with a pseudo-index, because a pseudo-index is not an actual stack position. 
		/// </summary>
		/// <param name="state">
		/// A <see cref="IntPtr"/>
		/// </param>
		/// <param name="index">
		/// A <see cref="System.Int32"/>
		/// </param>
		public static extern void lua_remove( IntPtr state, int index );
 
		[DllImport( Lib, CallingConvention = CallingConvention.Cdecl )]
		/// <summary>
		/// Moves the top element into the given position (and pops it), without shifting any element (therefore replacing the value at the given position). 
		/// </summary>
		/// <param name="state">
		/// A <see cref="IntPtr"/>
		/// </param>
		/// <param name="index">
		/// A <see cref="System.Int32"/>
		/// </param>
		public static extern void lua_replace( IntPtr state, int index );
 
		[DllImport( Lib, CallingConvention = CallingConvention.Cdecl )]
		/// <summary>
		/// Pops a table from the stack and sets it as the new environment for the value at the given index. If the value at the given index is neither a function nor a thread nor a userdata, lua_setfenv returns false. Otherwise it returns true. 
		/// </summary>
		/// <param name="state">
		/// A <see cref="IntPtr"/>
		/// </param>
		/// <param name="index">
		/// A <see cref="System.Int32"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.Int32"/>
		/// </returns>
		public static extern bool lua_setfenv( IntPtr state, int index );
		
		[DllImport( Lib, CallingConvention = CallingConvention.Cdecl )]
		/// <summary>
		/// Does the equivalent to t[k] = v, where t is the value at the given valid index and v is the value at the top of the stack.
		/// This function pops the value from the stack. As in Lua, this function may trigger a metamethod for the "newindex" event
		/// </summary>
		/// <param name="state">
		/// A <see cref="IntPtr"/>
		/// </param>
		/// <param name="index">
		/// A <see cref="System.Int32"/>
		/// </param>
		/// <param name="name">
		/// A <see cref="System.String"/>
		/// </param>
		public static extern void lua_setfield( IntPtr state, int index, string name );
 
		/// <summary>
		/// Pops a value from the stack and sets it as the new value of global name.
		/// </summary>
		/// <param name="state">
		/// A <see cref="IntPtr"/>
		/// </param>
		/// <param name="name">
		/// A <see cref="System.String"/>
		/// </param>
		public static void lua_setglobal( IntPtr state, string name )
		{
			lua_setfield( state, (int)PseudoIndex.Globals, name );
		}
 
		[DllImport( Lib, CallingConvention = CallingConvention.Cdecl )]
		/// <summary>
		/// Pops a table from the stack and sets it as the new metatable for the value at the given acceptable index. 
		/// </summary>
		/// <param name="state">
		/// A <see cref="IntPtr"/>
		/// </param>
		/// <param name="index">
		/// A <see cref="System.Int32"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.Boolean"/>
		/// </returns>
		public static extern void lua_setmetatable( IntPtr state, int index );
 
		[DllImport( Lib, CallingConvention = CallingConvention.Cdecl )]
		/// <summary>
		/// Does the equivalent to t[k] = v, where t is the value at the given valid index, v is the value at the top of the stack, and k is the value just below the top.
		/// This function pops both the key and the value from the stack. As in Lua, this function may trigger a metamethod for the "newindex" event.
		/// </summary>
		/// <param name="state">
		/// A <see cref="IntPtr"/>
		/// </param>
		/// <param name="index">
		/// A <see cref="System.Int32"/>
		/// </param>
		public static extern void lua_settable( IntPtr state, int index );
 
		[DllImport( Lib, CallingConvention = CallingConvention.Cdecl )]
		/// <summary>
		/// Accepts any acceptable index, or 0, and sets the stack top to this index. If the new top is larger than the old one, then the new elements are filled with nil. If index is 0, then all stack elements are removed. 
		/// </summary>
		/// <param name="state">
		/// A <see cref="IntPtr"/>
		/// </param>
		/// <param name="index">
		/// A <see cref="System.Int32"/>
		/// </param>
		public static extern void lua_settop( IntPtr state, int index );
 
		[DllImport( Lib, CallingConvention = CallingConvention.Cdecl )]
		/// <summary>
		/// Converts the Lua value at the given acceptable index to a boolean value. Like all tests in Lua, lua_toboolean returns true for any Lua value different from false and nil; otherwise it returns false. It also returns false when called with a non-valid index. (If you want to accept only actual boolean values, use lua_isboolean to test the value's type.)
		/// </summary>
		/// <param name="state">
		/// A <see cref="IntPtr"/>
		/// </param>
		/// <param name="index">
		/// A <see cref="System.Int32"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.Boolean"/>
		/// </returns>
		public static extern bool lua_toboolean( IntPtr state, int index );
 
		[DllImport( Lib, CallingConvention = CallingConvention.Cdecl )]
		/// <summary>
		/// Converts a value at the given acceptable index to a C function. That value must be a C function; otherwise, returns NULL.
		/// </summary>
		/// <param name="state">
		/// A <see cref="IntPtr"/>
		/// </param>
		/// <param name="index">
		/// A <see cref="System.Int32"/>
		/// </param>
		/// <returns>
		/// A <see cref="CallbackFunction"/>
		/// </returns>
		public static extern CallbackFunction lua_tocfunction( IntPtr state, int index );
 
		[DllImport( Lib, CallingConvention = CallingConvention.Cdecl )]
		/// <summary>
		/// Converts the Lua value at the given acceptable index to the signed integral type lua_Integer. The Lua value must be a number or a string convertible to a number (see §2.2.1); otherwise, lua_tointeger returns 0. 
		/// </summary>
		/// <param name="state">
		/// A <see cref="IntPtr"/>
		/// </param>
		/// <param name="index">
		/// A <see cref="System.Int32"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.Int32"/>
		/// </returns>
		public static extern int lua_tointeger( IntPtr state, int index );
 
		[DllImport( Lib, CallingConvention = CallingConvention.Cdecl )]
		/// <summary>
		/// Converts the Lua value at the given acceptable index to a C string. If len is not NULL, it also sets *len with the string length. The Lua value must be a string or a number; otherwise, the function returns NULL. If the value is a number, then lua_tolstring also changes the actual value in the stack to a string. (This change confuses lua_next when lua_tolstring is applied to keys during a table traversal.)
		/// lua_tolstring returns a fully aligned pointer to a string inside the Lua state. This string always has a zero ('\0') after its last character (as in C), but can contain other zeros in its body. Because Lua has garbage collection, there is no guarantee that the pointer returned by lua_tolstring will be valid after the corresponding value is removed from the stack. 
		/// </summary>
		/// <param name="state">
		/// A <see cref="IntPtr"/>
		/// </param>
		/// <param name="str">
		/// A <see cref="System.String"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.Int32"/>
		/// </returns>
		public static extern IntPtr lua_tolstring( IntPtr state, int index, out int length );
 
		[DllImport( Lib, CallingConvention = CallingConvention.Cdecl )]
		/// <summary>
		/// Converts the Lua value at the given acceptable index to a number. The Lua value must be a number or a string convertible to a number, otherwise returns 0.
		/// </summary>
		/// <param name="state">
		/// A <see cref="IntPtr"/>
		/// </param>
		/// <param name="index">
		/// A <see cref="System.Int32"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.Double"/>
		/// </returns>
		public static extern double lua_tonumber( IntPtr state, int index );
 
		[DllImport( Lib, CallingConvention = CallingConvention.Cdecl )]
		/// <summary>
		/// Converts the value at the given acceptable index to a generic pointer. The value can be a userdata, a table, a thread, or a function; otherwise, lua_topointer returns NULL. Different objects will give different pointers. There is no way to convert the pointer back to its original value. 
		/// </summary>
		/// <param name="state">
		/// A <see cref="IntPtr"/>
		/// </param>
		/// <param name="index">
		/// A <see cref="System.Int32"/>
		/// </param>
		/// <returns>
		/// A <see cref="IntPtr"/>
		/// </returns>
		public static extern IntPtr lua_topointer( IntPtr state, int index );
 
		/// <summary>
		/// Equivalent to lua_tolstring with len equal to NULL.
		/// </summary>
		/// <param name="state">
		/// A <see cref="IntPtr"/>
		/// </param>
		/// <param name="index">
		/// A <see cref="System.Int32"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.String"/>
		/// </returns>
		public static string lua_tostring( IntPtr state, int index )
		{
            int length;
            IntPtr str = lua_tolstring( state, index, out length );
            if( str != IntPtr.Zero )
                return Marshal.PtrToStringAnsi( str, length );
 
            return null;
		}
		
		[DllImport( Lib, CallingConvention = CallingConvention.Cdecl )]
		/// <summary>
		/// If the value at the given acceptable index is a full userdata, returns its block address. If the value is a light userdata, returns its pointer. Otherwise, returns NULL.
		/// </summary>
		/// <param name="state">
		/// A <see cref="IntPtr"/>
		/// </param>
		/// <param name="index">
		/// A <see cref="System.Int32"/>
		/// </param>
		/// <returns>
		/// A <see cref="IntPtr"/>
		/// </returns>
		public static extern IntPtr lua_touserdata( IntPtr state, int index );
 
		[DllImport( Lib, CallingConvention = CallingConvention.Cdecl )]
		/// <summary>
		/// Returns the type of the value in the given acceptable index, or NONE for a non-valid index (that is, an index to an "empty" stack position).
		/// </summary>
		/// <param name="state">
		/// A <see cref="IntPtr"/>
		/// </param>
		/// <param name="str">
		/// A <see cref="System.String"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.Int32"/>
		/// </returns>
		public static extern LuaType lua_type( IntPtr state, int index );
 
		[DllImport( Lib, CallingConvention = CallingConvention.Cdecl )]
		/// <summary>
		/// Returns the name of the type encoded by the value tp.
		/// </summary>
		/// <param name="state">
		/// A <see cref="IntPtr"/>
		/// </param>
		/// <param name="tp">
		/// A <see cref="LuaType"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.String"/>
		/// </returns>
		public static extern string lua_typename( IntPtr state, LuaType tp );
 
		#endregion
 
		#region Auxiliary Library
 
		[DllImport( Lib, CallingConvention = CallingConvention.Cdecl )]
		/// <summary>
		/// If the object at index obj has a metatable and this metatable has a field e, this function calls this field and passes the object as its only argument. In this case this function returns 1 and pushes onto the stack the value returned by the call. If there is no metatable or no metamethod, this function returns 0 (without pushing any value on the stack).
		/// </summary>
		/// <param name="state">
		/// A <see cref="IntPtr"/>
		/// </param>
		/// <param name="index">
		/// A <see cref="System.Int32"/>
		/// </param>
		/// <param name="key">
		/// A <see cref="System.String"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.Int32"/>
		/// </returns>
		public static extern int luaL_callmeta( IntPtr state, int index, string key );
 
		/// <summary>
		/// Loads and runs the given file.
		/// </summary>
		/// <param name="state">
		/// A <see cref="IntPtr"/>
		/// </param>
		/// <param name="filename">
		/// A <see cref="System.String"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.Boolean"/>
		/// </returns>
		public static bool luaL_dofile( IntPtr state, string filename )
		{
			return ( luaL_loadfile( state, filename ) == LuaEnum.Ok ) && ( lua_pcall( state, 0, (int)LuaEnum.MultiRet, 0 ) == LuaEnum.Ok );
		}
 
		/// <summary>
		/// Loads and runs the given string.
		/// </summary>
		/// <param name="state">
		/// A <see cref="IntPtr"/>
		/// </param>
		/// <param name="chunk">
		/// A <see cref="System.String"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.Boolean"/>
		/// </returns>
		public static bool luaL_dostring( IntPtr state, string chunk )
		{
			return ( luaL_loadstring( state, chunk ) == LuaEnum.Ok ) && ( lua_pcall( state, 0, (int)LuaEnum.MultiRet, 0 ) == LuaEnum.Ok );
		}
 
		[DllImport( Lib, CallingConvention = CallingConvention.Cdecl )]
		/// <summary>
		/// Raises an error. The error message format is given by fmt plus any extra arguments, following the same rules of lua_pushfstring. It also adds at the beginning of the message the file name and the line number where the error occurred, if this information is available. 
		/// </summary>
		/// <param name="state">
		/// A <see cref="IntPtr"/>
		/// </param>
		/// <param name="format">
		/// A <see cref="System.String"/>
		/// </param>
		/// <param name="__arglist">
		/// A <see cref="__arglist"/>
		/// </param>
		public static extern void luaL_error( IntPtr state, string format, IntPtr zero);
 
		[DllImport( Lib, CallingConvention = CallingConvention.Cdecl )]
		/// <summary>
		/// Pushes onto the stack the field key from the metatable of the object at the given index. If the object does not have a metatable, or if the metatable does not have this field, returns false and pushes nothing.
		/// </summary>
		/// <param name="state">
		/// A <see cref="IntPtr"/>
		/// </param>
		/// <param name="index">
		/// A <see cref="System.Int32"/>
		/// </param>
		/// <param name="key">
		/// A <see cref="System.String"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.Boolean"/>
		/// </returns>
		public static extern bool luaL_getmetafield( IntPtr state, int index, string key );
 
		[DllImport( Lib, CallingConvention = CallingConvention.Cdecl )]
		/// <summary>
		/// Pushes onto the stack the metatable associated with name tname in the registry (see luaL_newmetatable).
		/// </summary>
		/// <param name="state">
		/// A <see cref="IntPtr"/>
		/// </param>
		/// <param name="key">
		/// A <see cref="System.String"/>
		/// </param>
		public static extern void luaL_getmetatable( IntPtr state, string key );
 
		[DllImport( Lib, CallingConvention = CallingConvention.Cdecl )]
		/// <summary>
		/// Loads a buffer as a Lua chunk. This function uses lua_load to load the chunk in the buffer pointed to by buff with size sz. name is the chunk name, used for debug and errors.
		/// </summary>
		/// <param name="state">
		/// A <see cref="IntPtr"/>
		/// </param>
		/// <param name="buffer">
		/// A <see cref="System.String"/>
		/// </param>
		/// <param name="size">
		/// A <see cref="System.Int32"/>
		/// </param>
		/// <param name="name">
		/// A <see cref="System.String"/>
		/// </param>
		/// <returns>
		/// A <see cref="LuaEnum"/>
		/// </returns>
		public static extern LuaEnum luaL_loadbuffer( IntPtr state, string buffer, int size, string name );

		public static LuaEnum luaL_loadbuffer(IntPtr luaState, string buff, string name)
		{
			//char *cs1 = (char *) Marshal.StringToHGlobalAnsi(buff).ToPointer();
			//char *cs2 = (char *) Marshal.StringToHGlobalAnsi(name).ToPointer();

			LuaEnum result = luaL_loadbuffer(luaState, buff, buff.Length, name);

			//Marshal.FreeHGlobal(new IntPtr(cs1));
			//Marshal.FreeHGlobal(new IntPtr(cs2));

			return result;
		}
		
		[DllImport( Lib, CallingConvention = CallingConvention.Cdecl )]
		/// <summary>
		/// Loads a file as a Lua chunk. This function uses lua_load to load the chunk in the file named filename. If filename is NULL, then it loads from the standard input. The first line in the file is ignored if it starts with a #. 
		/// </summary>
		/// <param name="state">
		/// A <see cref="IntPtr"/>
		/// </param>
		/// <param name="str">
		/// A <see cref="System.String"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.Int32"/>
		/// </returns>
		public static extern LuaEnum luaL_loadfile( IntPtr state, string filename );
		
		[DllImport( Lib, CallingConvention = CallingConvention.Cdecl )]
		/// <summary>
		/// Loads a string as a Lua chunk. This function uses lua_load to load the chunk within the zero-terminated string.
		/// </summary>
		/// <param name="state">
		/// A Lua State.<see cref="IntPtr"/>
		/// </param>
		/// <param name="str">
		/// A chunk to load.<see cref="System.String"/>
		/// </param>
		/// <returns>
		/// A result code. LuaEnum.Ok, LuaEnum.ErrorSyntax or LuaEnum.ErrorMemory<see cref="System.Int32"/>
		/// </returns>
		public static extern LuaEnum luaL_loadstring( IntPtr state, string str );
 
		[DllImport( Lib, CallingConvention = CallingConvention.Cdecl )]
		/// <summary>
		/// If the registry already has the given key, returns false. Otherwise, creates a new table to be used as a metatable for userdata, adds it to the registry with the given key, and returns true.
		/// In both cases pushes onto the stack the final value associated with the given key in the registry.
		/// </summary>
		/// <param name="state">
		/// A <see cref="IntPtr"/>
		/// </param>
		/// <param name="key">
		/// A <see cref="System.String"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.Boolean"/>
		/// </returns>
		public static extern bool luaL_newmetatable( IntPtr state, string key );
		
		[DllImport( Lib, CallingConvention = CallingConvention.Cdecl )]
		/// <summary>
		/// Creates a new Lua state. It calls lua_newstate with an allocator based on the standard C realloc function and then sets a panic function (see lua_atpanic) that prints an error message to the standard error output in case of fatal errors. 
		/// </summary>
		/// <returns>
		/// A Lua State.<see cref="IntPtr"/>
		/// </returns>
		public static extern IntPtr luaL_newstate();
		
		[DllImport( Lib, CallingConvention = CallingConvention.Cdecl )]
		/// <summary>
		/// Opens all standard Lua libraries into the given state.
		/// </summary>
		/// <param name="state">
		/// A Lua State.<see cref="IntPtr"/>
		/// </param>
		public static extern void luaL_openlibs( IntPtr state );
 
		[DllImport( Lib, CallingConvention = CallingConvention.Cdecl )]
		/// <summary>
		/// Creates and returns a reference, in the table at index t, for the object at the top of the stack (and pops the object).
		/// A reference is a unique integer key. As long as you do not manually add integer keys into table t, luaL_ref ensures the uniqueness of the key it returns. You can retrieve an object referred by reference r by calling lua_rawgeti(L, t, r). Function luaL_unref frees a reference and its associated object.
		/// If the object at the top of the stack is nil, luaL_ref returns the constant LUA_REFNIL. The constant LUA_NOREF is guaranteed to be different from any reference returned by luaL_ref. 
		/// </summary>
		/// <param name="state">
		/// A <see cref="IntPtr"/>
		/// </param>
		/// <param name="t">
		/// A <see cref="System.Int32"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.Int32"/>
		/// </returns>
		public static extern int luaL_ref( IntPtr state, int t );
 
		/// <summary>
		/// Pops the value referenced by reference by r in the table at index t onto the stack.
		/// </summary>
		/// <param name="state">
		/// A <see cref="IntPtr"/>
		/// </param>
		/// <param name="t">
		/// A stack index
		/// </param>
		/// <param name="r">
		/// A <see cref="System.Int32"/>
		/// </param>
		public static void luaL_getref( IntPtr state, int t, int r )
		{
			lua_rawgeti( state, t, r );	
		}
		
		[DllImport( Lib, CallingConvention = CallingConvention.Cdecl )]
		/// <summary>
		/// Releases reference ref from the table at index t (see luaL_ef). The entry is removed from the table, so that the referred object can be collected. The reference ref is also freed to be used again. 
		/// </summary>
		/// <param name="state">
		/// A <see cref="IntPtr"/>
		/// </param>
		/// <param name="t">
		/// A <see cref="System.Int32"/>
		/// </param>
		/// <param name="r">
		/// A <see cref="System.Int32"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.Int32"/>
		/// </returns>
		public static extern int luaL_unref( IntPtr state, int t, int r );
		
		[DllImport( Lib, CallingConvention = CallingConvention.Cdecl )]
		/// <summary>
		/// Returns the name of the type of the value at the given index. 
		/// </summary>
		/// <param name="state">
		/// A <see cref="IntPtr"/>
		/// </param>
		/// <param name="index">
		/// A <see cref="System.Int32"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.String"/>
		/// </returns>
		public static extern string luaL_typename( IntPtr state, int index );
 
		[DllImport( Lib, CallingConvention = CallingConvention.Cdecl )]
		/// <summary>
		/// Generates an error with a message like the following:
		///      location: bad argument narg to 'func' (*** expected, got rt)
		/// where location is produced by luaL_where, func is the name of the current function, and rt is the type name of the actual argument.
		/// <param name="state">
		/// A <see cref="IntPtr"/>
		/// </param>
		/// <param name="narg">
		/// A <see cref="System.Int32"/>
		/// </param>
		/// <param name="expected">
		/// A <see cref="System.String"/>
		/// </param>
		/// </summary>
		/// <param name="state">
		/// A <see cref="IntPtr"/>
		/// </param>
		/// <param name="narg">
		/// A <see cref="System.Int32"/>
		/// </param>
		/// <param name="expected">
		/// A <see cref="System.String"/>
		/// </param>
		public static extern void luaL_typeerror( IntPtr state, int narg, string expected );
 
		[DllImport( Lib, CallingConvention = CallingConvention.Cdecl )]
		/// <summary>
		/// Pushes onto the stack a string identifying the current position of the control at level lvl in the call stack. Typically this string has the following format:
		///      chunkname:currentline:
		/// Level 0 is the running function, level 1 is the function that called the running function, etc.
		/// </summary>
		/// <param name="state">
		/// A <see cref="IntPtr"/>
		/// </param>
		/// <param name="level">
		/// A <see cref="System.Int32"/>
		/// </param>
		public static extern void luaL_where( IntPtr state, int level );

		public static bool luaL_checkmetatable(IntPtr luaState,int index)
		{
			bool retVal=false;

			if(lua_getmetatable(luaState,index)!=0) 
			{
				lua_pushlightuserdata(luaState, (IntPtr)tag);
				lua_rawget(luaState, -2);
				retVal = !lua_isnil(luaState, -1);
				lua_settop(luaState, -3);
			}
			return retVal;
		}

		public static IntPtr luanet_gettag() 
		{
			return (IntPtr)tag;
		}

		public static void lua_getref(IntPtr luaState, int reference)
		{
			lua_rawgeti(luaState, (int) PseudoIndex.Registry,reference);
		}

		public static void lua_unref(IntPtr luaState, int reference) 
		{
			luaL_unref(luaState, (int) PseudoIndex.Registry,reference);
		}

		public static int luanet_rawnetobj(IntPtr luaState,int obj)
		{
			IntPtr udata= lua_touserdata(luaState, obj);
			if(udata!=(IntPtr)0) return udata.ToInt32();
			return -1;
		}

		public static void lua_pushstdcallcfunction(IntPtr luaState, CallbackFunction function)
		{
			//IntPtr p = Marshal.GetFunctionPointerForDelegate(function);
			lua_pushcfunction(luaState, function);
		}

		public static IntPtr checkudata_raw(IntPtr luaState, int ud, string tname)
		{
			IntPtr p = lua_touserdata(luaState, ud);

			  if (p != (IntPtr)0) 
			  {  /* value is a userdata? */
				  if (lua_getmetatable(luaState, ud)!=0) 
				  {
					/* does it have a metatable? */
					lua_getfield(luaState, (int) PseudoIndex.Registry, tname);  /* get correct metatable */

					bool isEqual = lua_rawequal(luaState, -1, -2);

					// NASTY - we need our own version of the lua_pop macro
					// lua_pop(L, 2);  /* remove both metatables */
					lua_settop(luaState, -(2) - 1);

					if (isEqual)   /* does it have the correct mt? */
						return p;
				  }
			  }
		  
		  return (IntPtr)0;
		}

		public static int luanet_checkudata(IntPtr luaState, int ud, string tname)
		{
			//char *cs = (char *) Marshal::StringToHGlobalAnsi(tname).ToPointer();

		    IntPtr udata= checkudata_raw(luaState, ud, tname);

			//Marshal::FreeHGlobal(IntPtr(cs));

		    if(udata!=(IntPtr)0) return udata.ToInt32();
		    return -1;
		}

		public static void luanet_newudata(IntPtr luaState,int val)
		{
			/*IntPtr pointer= *///lua_newuserdata(luaState, sizeof(int));
			lua_newuserdata(luaState, val);
			//pointer=(IntPtr)val;
		}

		public static int luanet_tonetobject(IntPtr luaState,int index)
		{
			IntPtr udata;

			if(lua_type(luaState,index)==LuaType.UserData) 
			{
				if(luaL_checkmetatable(luaState, index)) 
				{
				udata=(IntPtr) lua_touserdata(luaState,index);
				if(udata!=(IntPtr)0) 
					return udata.ToInt32(); 
				}

			udata=(IntPtr)checkudata_raw(luaState,index, "luaNet_class");
			if(udata!=(IntPtr)0) return udata.ToInt32();
			udata=(IntPtr)checkudata_raw(luaState,index, "luaNet_searchbase");
			if(udata!=(IntPtr)0) return udata.ToInt32();
			udata=(IntPtr)checkudata_raw(luaState,index, "luaNet_function");
			if(udata!=(IntPtr)0) return udata.ToInt32();
			}
			return -1;
		}

		public static int lua_ref(IntPtr luaState, int lockRef)
		{
			if(lockRef!=0) 
			{
				return luaL_ref(luaState, (int) PseudoIndex.Registry);
			} 
			else return 0;
		}
 
		#endregion
	}
}