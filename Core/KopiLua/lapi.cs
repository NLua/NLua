/*
** $Id: lapi.c,v 2.55.1.5 2008/07/04 18:41:18 roberto Exp $
** Lua API
** See Copyright Notice in lua.h
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace KopiLua
{
	using lu_mem = System.UInt32;
	using TValue = Lua.lua_TValue;
	using StkId = Lua.lua_TValue;
	using lua_Integer = System.Int32;
	using lua_Number = System.Double;
	using ptrdiff_t = System.Int32;
	using ZIO = Lua.Zio;

	public partial class Lua
	{
		public const string lua_ident =
		  "$Lua: " + LUA_RELEASE + " " + LUA_COPYRIGHT + " $\n" +
		  "$Authors: " + LUA_AUTHORS + " $\n" +
		  "$URL: www.lua.org $\n";

		public static void api_checknelems(lua_State L, int n)
		{
			api_check(L, n <= L.top - L.base_);
		}

		public static void api_checkvalidindex(lua_State L, StkId i)
		{
			api_check(L, i != luaO_nilobject);
		}

		public static void api_incr_top(lua_State L)
		{
			api_check(L, L.top < L.ci.top);
			StkId.inc(ref L.top);
		}



		static TValue index2adr (lua_State L, int idx) {
		  if (idx > 0) {
			TValue o = L.base_ + (idx - 1);
			api_check(L, idx <= L.ci.top - L.base_);
			if (o >= L.top) return luaO_nilobject;
			else return o;
		  }
		  else if (idx > LUA_REGISTRYINDEX) {
			api_check(L, idx != 0 && -idx <= L.top - L.base_);
			return L.top + idx;
		  }
		  else switch (idx) {  /* pseudo-indices */
			case LUA_REGISTRYINDEX: return registry(L);
			case LUA_ENVIRONINDEX: {
			  Closure func = curr_func(L);
			  sethvalue(L, L.env, func.c.env);
			  return L.env;
			}
			case LUA_GLOBALSINDEX: return gt(L);
			default: {
			  Closure func = curr_func(L);
			  idx = LUA_GLOBALSINDEX - idx;
			  return (idx <= func.c.nupvalues)
						? func.c.upvalue[idx-1]
						: (TValue)luaO_nilobject;
			}
		  }
		}


		private static Table getcurrenv (lua_State L) {
		  if (L.ci == L.base_ci[0])  /* no enclosing function? */
			return hvalue(gt(L));  /* use global table as environment */
		  else {
			Closure func = curr_func(L);
			return func.c.env;
		  }
		}


		public static void luaA_pushobject (lua_State L, TValue o) {
		  setobj2s(L, L.top, o);
		  api_incr_top(L);
		}


		public static int lua_checkstack (lua_State L, int size) {
		  int res = 1;
		  lua_lock(L);
		  if (size > LUAI_MAXCSTACK || (L.top - L.base_ + size) > LUAI_MAXCSTACK)
			res = 0;  /* stack overflow */
		  else if (size > 0) {
			luaD_checkstack(L, size);
			if (L.ci.top < L.top + size)
			  L.ci.top = L.top + size;
		  }
		  lua_unlock(L);
		  return res;
		}


		public static void lua_xmove (lua_State from, lua_State to, int n) {
		  int i;
		  if (from == to) return;
		  lua_lock(to);
		  api_checknelems(from, n);
		  api_check(from, G(from) == G(to));
		  api_check(from, to.ci.top - to.top >= n);
		  from.top -= n;
		  for (i = 0; i < n; i++) {
			setobj2s(to, StkId.inc(ref to.top), from.top + i);
		  }
		  lua_unlock(to);
		}


		public static void lua_setlevel (lua_State from, lua_State to) {
		  to.nCcalls = from.nCcalls;
		}


		public static lua_CFunction lua_atpanic (lua_State L, lua_CFunction panicf) {
		  lua_CFunction old;
		  lua_lock(L);
		  old = G(L).panic;
		  G(L).panic = panicf;
		  lua_unlock(L);
		  return old;
		}


		public static lua_State lua_newthread (lua_State L) {
		  lua_State L1;
		  lua_lock(L);
		  luaC_checkGC(L);
		  L1 = luaE_newthread(L);
		  setthvalue(L, L.top, L1);
		  api_incr_top(L);
		  lua_unlock(L);
		  luai_userstatethread(L, L1);
		  return L1;
		}



		/*
		** basic stack manipulation
		*/


		public static int lua_gettop (lua_State L) {
		  return cast_int(L.top - L.base_);
		}


		public static void lua_settop (lua_State L, int idx) {
		  lua_lock(L);
		  if (idx >= 0) {
			api_check(L, idx <= L.stack_last - L.base_);
			while (L.top < L.base_ + idx)
			  setnilvalue(StkId.inc(ref L.top));
			L.top = L.base_ + idx;
		  }
		  else {
			api_check(L, -(idx+1) <= (L.top - L.base_));
			L.top += idx+1;  /* `subtract' index (index is negative) */
		  }
		  lua_unlock(L);
		}


		public static void lua_remove (lua_State L, int idx) {
		  StkId p;
		  lua_lock(L);
		  p = index2adr(L, idx);
		  api_checkvalidindex(L, p);
		  while ((p=p[1]) < L.top) setobjs2s(L, p-1, p);
		  StkId.dec(ref L.top);
		  lua_unlock(L);
		}


		public static void lua_insert (lua_State L, int idx) {
		  StkId p;
		  StkId q;
		  lua_lock(L);
		  p = index2adr(L, idx);
		  api_checkvalidindex(L, p);
		  for (q = L.top; q>p; StkId.dec(ref q)) setobjs2s(L, q, q-1);
		  setobjs2s(L, p, L.top);
		  lua_unlock(L);
		}


		public static void lua_replace (lua_State L, int idx) {
		  StkId o;
		  lua_lock(L);
		  /* explicit test for incompatible code */
		  if (idx == LUA_ENVIRONINDEX && L.ci == L.base_ci[0])
			luaG_runerror(L, "no calling environment");
		  api_checknelems(L, 1);
		  o = index2adr(L, idx);
		  api_checkvalidindex(L, o);
		  if (idx == LUA_ENVIRONINDEX) {
			Closure func = curr_func(L);
			api_check(L, ttistable(L.top - 1)); 
			func.c.env = hvalue(L.top - 1);
			luaC_barrier(L, func, L.top - 1);
		  }
		  else {
			setobj(L, o, L.top - 1);
			if (idx < LUA_GLOBALSINDEX)  /* function upvalue? */
			  luaC_barrier(L, curr_func(L), L.top - 1);
		  }
		  StkId.dec(ref L.top);
		  lua_unlock(L);
		}


		public static void lua_pushvalue (lua_State L, int idx) {
		  lua_lock(L);
		  setobj2s(L, L.top, index2adr(L, idx));
		  api_incr_top(L);
		  lua_unlock(L);
		}



		/*
		** access functions (stack . C)
		*/


		public static int lua_type (lua_State L, int idx) {
		  StkId o = index2adr(L, idx);
		  return (o == luaO_nilobject) ? LUA_TNONE : ttype(o);
		}


		public static CharPtr lua_typename (lua_State L, int t) {
		  //UNUSED(L);
		  return (t == LUA_TNONE) ? "no value" : luaT_typenames[t];
		}


		public static bool lua_iscfunction (lua_State L, int idx) {
		  StkId o = index2adr(L, idx);
		  return iscfunction(o);
		}


		public static int lua_isnumber (lua_State L, int idx) {
		  TValue n = new TValue();
		  TValue o = index2adr(L, idx);
		  return tonumber(ref o, n);
		}


		public static int lua_isstring (lua_State L, int idx) {
		  int t = lua_type(L, idx);
		  return (t == LUA_TSTRING || t == LUA_TNUMBER) ? 1 : 0;
		}


		public static int lua_isuserdata (lua_State L, int idx) {
		  TValue o = index2adr(L, idx);
		  return (ttisuserdata(o) || ttislightuserdata(o)) ? 1 : 0;
		}


		public static int lua_rawequal (lua_State L, int index1, int index2) {
		  StkId o1 = index2adr(L, index1);
		  StkId o2 = index2adr(L, index2);
		  return (o1 == luaO_nilobject || o2 == luaO_nilobject) ? 0
				 : luaO_rawequalObj(o1, o2);
		}


		public static int lua_equal (lua_State L, int index1, int index2) {
		  StkId o1, o2;
		  int i;
		  lua_lock(L);  /* may call tag method */
		  o1 = index2adr(L, index1);
		  o2 = index2adr(L, index2);
		  i = (o1 == luaO_nilobject || o2 == luaO_nilobject) ? 0 : equalobj(L, o1, o2);
		  lua_unlock(L);
		  return i;
		}


		public static int lua_lessthan (lua_State L, int index1, int index2) {
		  StkId o1, o2;
		  int i;
		  lua_lock(L);  /* may call tag method */
		  o1 = index2adr(L, index1);
		  o2 = index2adr(L, index2);
		  i = (o1 == luaO_nilobject || o2 == luaO_nilobject) ? 0
			   : luaV_lessthan(L, o1, o2);
		  lua_unlock(L);
		  return i;
		}



		public static lua_Number lua_tonumber (lua_State L, int idx) {
		  TValue n = new TValue();
		  TValue o = index2adr(L, idx);
		  if (tonumber(ref o, n) != 0)
			return nvalue(o);
		  else
			return 0;
		}


		public static lua_Integer lua_tointeger (lua_State L, int idx) {
		  TValue n = new TValue();
		  TValue o = index2adr(L, idx);
		  if (tonumber(ref o, n) != 0) {
			lua_Integer res;
			lua_Number num = nvalue(o);
			lua_number2integer(out res, num);
			return res;
		  }
		  else
			return 0;
		}


		public static int lua_toboolean (lua_State L, int idx) {
		  TValue o = index2adr(L, idx);
		  return (l_isfalse(o) == 0) ? 1 : 0;
		}

		[CLSCompliantAttribute(false)]
		public static CharPtr lua_tolstring (lua_State L, int idx, out uint len) {
		  StkId o = index2adr(L, idx);
		  if (!ttisstring(o)) {
			lua_lock(L);  /* `luaV_tostring' may create a new string */
			if (luaV_tostring(L, o)==0) {  /* conversion failed? */
			  len = 0;
			  lua_unlock(L);
			  return null;
			}
			luaC_checkGC(L);
			o = index2adr(L, idx);  /* previous call may reallocate the stack */
			lua_unlock(L);
		  }
		  len = tsvalue(o).len;
		  return svalue(o);
		}

		[CLSCompliantAttribute(false)]
		public static uint lua_objlen (lua_State L, int idx) {
		  StkId o = index2adr(L, idx);
		  switch (ttype(o)) {
			case LUA_TSTRING: return tsvalue(o).len;
			case LUA_TUSERDATA: return uvalue(o).len;
			case LUA_TTABLE: return (uint)luaH_getn(hvalue(o));
			case LUA_TNUMBER: {
			  uint l;
			  lua_lock(L);  /* `luaV_tostring' may create a new string */
			  l = (luaV_tostring(L, o) != 0 ? tsvalue(o).len : 0);
			  lua_unlock(L);
			  return l;
			}
			default: return 0;
		  }
		}


		public static lua_CFunction lua_tocfunction (lua_State L, int idx) {
		  StkId o = index2adr(L, idx);
		  return (!iscfunction(o)) ? null : clvalue(o).c.f;
		}


		public static object lua_touserdata (lua_State L, int idx) {
		  StkId o = index2adr(L, idx);
		  switch (ttype(o)) {
			case LUA_TUSERDATA: return (rawuvalue(o).user_data);
			case LUA_TLIGHTUSERDATA: return pvalue(o);
			default: return null;
		  }
		}

		public static lua_State lua_tothread (lua_State L, int idx) {
		  StkId o = index2adr(L, idx);
		  return (!ttisthread(o)) ? null : thvalue(o);
		}


		public static object lua_topointer (lua_State L, int idx) {
		  StkId o = index2adr(L, idx);
		  switch (ttype(o)) {
			case LUA_TTABLE: return hvalue(o);
			case LUA_TFUNCTION: return clvalue(o);
			case LUA_TTHREAD: return thvalue(o);
			case LUA_TUSERDATA:
			case LUA_TLIGHTUSERDATA:
			  return lua_touserdata(L, idx);
			default: return null;
		  }
		}



		/*
		** push functions (C . stack)
		*/


		public static void lua_pushnil (lua_State L) {
		  lua_lock(L);
		  setnilvalue(L.top);
		  api_incr_top(L);
		  lua_unlock(L);
		}


		public static void lua_pushnumber (lua_State L, lua_Number n) {
		  lua_lock(L);
		  setnvalue(L.top, n);
		  api_incr_top(L);
		  lua_unlock(L);
		}


		public static void lua_pushinteger (lua_State L, lua_Integer n) {
		  lua_lock(L);
		  setnvalue(L.top, cast_num(n));
		  api_incr_top(L);
		  lua_unlock(L);
		}

		[CLSCompliantAttribute(false)]
		public static void lua_pushlstring (lua_State L, CharPtr s, uint len) {
		  lua_lock(L);
		  luaC_checkGC(L);
		  setsvalue2s(L, L.top, luaS_newlstr(L, s, len));
		  api_incr_top(L);
		  lua_unlock(L);
		}


		public static void lua_pushstring (lua_State L, CharPtr s) {
		  if (s == null)
			lua_pushnil(L);
		  else
			lua_pushlstring(L, s, (uint)strlen(s));
		}


		public static CharPtr lua_pushvfstring (lua_State L, CharPtr fmt,
											  object[] argp) {
		  CharPtr ret;
		  lua_lock(L);
		  luaC_checkGC(L);
		  ret = luaO_pushvfstring(L, fmt, argp);
		  lua_unlock(L);
		  return ret;
		}


		public static CharPtr lua_pushfstring (lua_State L, CharPtr fmt) {
			CharPtr ret;
			lua_lock(L);
			luaC_checkGC(L);
			ret = luaO_pushvfstring(L, fmt, null);
			lua_unlock(L);
			return ret;
		}

		public static CharPtr lua_pushfstring(lua_State L, CharPtr fmt, params object[] p)
		{
			  CharPtr ret;
			  lua_lock(L);
			  luaC_checkGC(L);
			  ret = luaO_pushvfstring(L, fmt, p);
			  lua_unlock(L);
			  return ret;
		}

		public static void lua_pushcclosure (lua_State L, lua_CFunction fn, int n) {
		  Closure cl;
		  lua_lock(L);
		  luaC_checkGC(L);
		  api_checknelems(L, n);
		  cl = luaF_newCclosure(L, n, getcurrenv(L));
		  cl.c.f = fn;
		  L.top -= n;
		  while (n-- != 0)
			setobj2n(L, cl.c.upvalue[n], L.top+n);
		  setclvalue(L, L.top, cl);
		  lua_assert(iswhite(obj2gco(cl)));
		  api_incr_top(L);
		  lua_unlock(L);
		}


		public static void lua_pushboolean (lua_State L, int b) {
		  lua_lock(L);
		  setbvalue(L.top, (b != 0) ? 1 : 0);  /* ensure that true is 1 */
		  api_incr_top(L);
		  lua_unlock(L);
		}


		public static void lua_pushlightuserdata (lua_State L, object p) {
		  lua_lock(L);
		  setpvalue(L.top, p);
		  api_incr_top(L);
		  lua_unlock(L);
		}


		public static int lua_pushthread (lua_State L) {
		  lua_lock(L);
		  setthvalue(L, L.top, L);
		  api_incr_top(L);
		  lua_unlock(L);
		  return (G(L).mainthread == L) ? 1 : 0;
		}



		/*
		** get functions (Lua . stack)
		*/


		public static void lua_gettable (lua_State L, int idx) {
		  StkId t;
		  lua_lock(L);
		  t = index2adr(L, idx);
		  api_checkvalidindex(L, t);
		  luaV_gettable(L, t, L.top - 1, L.top - 1);
		  lua_unlock(L);
		}

		public static void lua_getfield (lua_State L, int idx, CharPtr k) {
		  StkId t;
		  TValue key = new TValue();
		  lua_lock(L);
		  t = index2adr(L, idx);
		  api_checkvalidindex(L, t);
		  setsvalue(L, key, luaS_new(L, k));
		  luaV_gettable(L, t, key, L.top);
		  api_incr_top(L);
		  lua_unlock(L);
		}


		public static void lua_rawget (lua_State L, int idx) {
		  StkId t;
		  lua_lock(L);
		  t = index2adr(L, idx);
		  api_check(L, ttistable(t));
		  setobj2s(L, L.top - 1, luaH_get(hvalue(t), L.top - 1));
		  lua_unlock(L);
		}


		public static void lua_rawgeti (lua_State L, int idx, int n) {
		  StkId o;
		  lua_lock(L);
		  o = index2adr(L, idx);
		  api_check(L, ttistable(o));
		  setobj2s(L, L.top, luaH_getnum(hvalue(o), n));
		  api_incr_top(L);
		  lua_unlock(L);
		}


		public static void lua_createtable (lua_State L, int narray, int nrec) {
		  lua_lock(L);
		  luaC_checkGC(L);
		  sethvalue(L, L.top, luaH_new(L, narray, nrec));
		  api_incr_top(L);
		  lua_unlock(L);
		}


		public static int lua_getmetatable (lua_State L, int objindex) {
		  TValue obj;
		  Table mt = null;
		  int res;
		  lua_lock(L);
		  obj = index2adr(L, objindex);
		  switch (ttype(obj)) {
			case LUA_TTABLE:
			  mt = hvalue(obj).metatable;
			  break;
			case LUA_TUSERDATA:
			  mt = uvalue(obj).metatable;
			  break;
			default:
			  mt = G(L).mt[ttype(obj)];
			  break;
		  }
		  if (mt == null)
			res = 0;
		  else {
			sethvalue(L, L.top, mt);
			api_incr_top(L);
			res = 1;
		  }
		  lua_unlock(L);
		  return res;
		}


		public static void lua_getfenv (lua_State L, int idx) {
		  StkId o;
		  lua_lock(L);
		  o = index2adr(L, idx);
		  api_checkvalidindex(L, o);
		  switch (ttype(o)) {
			case LUA_TFUNCTION:
			  sethvalue(L, L.top, clvalue(o).c.env);
			  break;
			case LUA_TUSERDATA:
			  sethvalue(L, L.top, uvalue(o).env);
			  break;
			case LUA_TTHREAD:
			  setobj2s(L, L.top,  gt(thvalue(o)));
			  break;
			default:
			  setnilvalue(L.top);
			  break;
		  }
		  api_incr_top(L);
		  lua_unlock(L);
		}


		/*
		** set functions (stack . Lua)
		*/


		public static void lua_settable (lua_State L, int idx) {
		  StkId t;
		  lua_lock(L);
		  api_checknelems(L, 2);
		  t = index2adr(L, idx);
		  api_checkvalidindex(L, t);
		  luaV_settable(L, t, L.top - 2, L.top - 1);
		  L.top -= 2;  /* pop index and value */
		  lua_unlock(L);
		}


		public static void lua_setfield (lua_State L, int idx, CharPtr k) {
		  StkId t;
		  TValue key = new TValue();			
		  lua_lock(L);
		  api_checknelems(L, 1);
		  t = index2adr(L, idx);
		  api_checkvalidindex(L, t);
		  setsvalue(L, key, luaS_new(L, k));
		  luaV_settable(L, t, key, L.top - 1);
		  StkId.dec(ref L.top);  /* pop value */
		  lua_unlock(L);
		}


		public static void lua_rawset (lua_State L, int idx) {
		  StkId t;
		  lua_lock(L);
		  api_checknelems(L, 2);
		  t = index2adr(L, idx);
		  api_check(L, ttistable(t));
		  setobj2t(L, luaH_set(L, hvalue(t), L.top-2), L.top-1);
		  luaC_barriert(L, hvalue(t), L.top-1);
		  L.top -= 2;
		  lua_unlock(L);
		}


		public static void lua_rawseti (lua_State L, int idx, int n) {
		  StkId o;
		  lua_lock(L);
		  api_checknelems(L, 1);
		  o = index2adr(L, idx);
		  api_check(L, ttistable(o));
		  setobj2t(L, luaH_setnum(L, hvalue(o), n), L.top-1);
		  luaC_barriert(L, hvalue(o), L.top-1);
		  StkId.dec(ref L.top);
		  lua_unlock(L);
		}


		public static int lua_setmetatable (lua_State L, int objindex) {
		  TValue obj;
		  Table mt;
		  lua_lock(L);
		  api_checknelems(L, 1);
		  obj = index2adr(L, objindex);
		  api_checkvalidindex(L, obj);
		  if (ttisnil(L.top - 1))
			  mt = null;
		  else {
			api_check(L, ttistable(L.top - 1));
			mt = hvalue(L.top - 1);
		  }
		  switch (ttype(obj)) {
			case LUA_TTABLE: {
			  hvalue(obj).metatable = mt;
			  if (mt != null)
				luaC_objbarriert(L, hvalue(obj), mt);
			  break;
			}
			case LUA_TUSERDATA: {
			  uvalue(obj).metatable = mt;
			  if (mt != null)
				luaC_objbarrier(L, rawuvalue(obj), mt);
			  break;
			}
			default: {
			  G(L).mt[ttype(obj)] = mt;
			  break;
			}
		  }
		  StkId.dec(ref L.top);
		  lua_unlock(L);
		  return 1;
		}


		public static int lua_setfenv (lua_State L, int idx) {
		  StkId o;
		  int res = 1;
		  lua_lock(L);
		  api_checknelems(L, 1);
		  o = index2adr(L, idx);
		  api_checkvalidindex(L, o);
		  api_check(L, ttistable(L.top - 1));
		  switch (ttype(o)) {
			case LUA_TFUNCTION:
			  clvalue(o).c.env = hvalue(L.top - 1);
			  break;
			case LUA_TUSERDATA:
			  uvalue(o).env = hvalue(L.top - 1);
			  break;
			case LUA_TTHREAD:
			  sethvalue(L, gt(thvalue(o)), hvalue(L.top - 1));
			  break;
			default:
			  res = 0;
			  break;
		  }
		  if (res != 0) luaC_objbarrier(L, gcvalue(o), hvalue(L.top - 1));
		  StkId.dec(ref L.top);
		  lua_unlock(L);
		  return res;
		}


		/*
		** `load' and `call' functions (run Lua code)
		*/


		public static void adjustresults(lua_State L, int nres) {
			if (nres == LUA_MULTRET && L.top >= L.ci.top)
				L.ci.top = L.top;
		}


		public static void checkresults(lua_State L, int na, int nr) {
			api_check(L, (nr) == LUA_MULTRET || (L.ci.top - L.top >= (nr) - (na)));
		}
			

		public static void lua_call (lua_State L, int nargs, int nresults) {
		  StkId func;
		  lua_lock(L);
		  api_checknelems(L, nargs+1);
		  checkresults(L, nargs, nresults);
		  func = L.top - (nargs+1);
		  luaD_call(L, func, nresults);
		  adjustresults(L, nresults);
		  lua_unlock(L);
		}



		/*
		** Execute a protected call.
		*/
		public class CallS {  /* data to `f_call' */
		  public StkId func;
			public int nresults;
		};


		static void f_call (lua_State L, object ud) {
		  CallS c = ud as CallS;
		  luaD_call(L, c.func, c.nresults);
		}



		public static int lua_pcall (lua_State L, int nargs, int nresults, int errfunc) {
		  CallS c = new CallS();
		  int status;
		  ptrdiff_t func;
		  lua_lock(L);
		  api_checknelems(L, nargs+1);
		  checkresults(L, nargs, nresults);
		  if (errfunc == 0)
			func = 0;
		  else {
			StkId o = index2adr(L, errfunc);
			api_checkvalidindex(L, o);
			func = savestack(L, o);
		  }
		  c.func = L.top - (nargs+1);  /* function to be called */
		  c.nresults = nresults;
		  status = luaD_pcall(L, f_call, c, savestack(L, c.func), func);
		  adjustresults(L, nresults);
		  lua_unlock(L);
		  return status;
		}


		/*
		** Execute a protected C call.
		*/
		public class CCallS {  /* data to `f_Ccall' */
		  public lua_CFunction func;
		  public object ud;
		};


		static void f_Ccall (lua_State L, object ud) {
		  CCallS c = ud as CCallS;
		  Closure cl;
		  cl = luaF_newCclosure(L, 0, getcurrenv(L));
		  cl.c.f = c.func;
		  setclvalue(L, L.top, cl);  /* push function */
		  api_incr_top(L);
		  setpvalue(L.top, c.ud);  /* push only argument */
		  api_incr_top(L);
		  luaD_call(L, L.top - 2, 0);
		}


		public static int lua_cpcall (lua_State L, lua_CFunction func, object ud) {
		  CCallS c = new CCallS();
		  int status;
		  lua_lock(L);
		  c.func = func;
		  c.ud = ud;
		  status = luaD_pcall(L, f_Ccall, c, savestack(L, L.top), 0);
		  lua_unlock(L);
		  return status;
		}

		[CLSCompliantAttribute(false)]
		public static int lua_load (lua_State L, lua_Reader reader, object data,
							  CharPtr chunkname) {
		  ZIO z = new ZIO();
		  int status;
		  lua_lock(L);
		  if (chunkname == null) chunkname = "?";
		  luaZ_init(L, z, reader, data);
		  status = luaD_protectedparser(L, z, chunkname);
		  lua_unlock(L);
		  return status;
		}

		[CLSCompliantAttribute(false)]
		public static int lua_dump (lua_State L, lua_Writer writer, object data) {
		  int status;
		  TValue o;
		  lua_lock(L);
		  api_checknelems(L, 1);
		  o = L.top - 1;
		  if (isLfunction(o))
			status = luaU_dump(L, clvalue(o).l.p, writer, data, 0);
		  else
			status = 1;
		  lua_unlock(L);
		  return status;
		}


		public static int  lua_status (lua_State L) {
		  return L.status;
		}


		/*
		** Garbage-collection function
		*/

		public static int lua_gc (lua_State L, int what, int data) {
		  int res = 0;
		  global_State g;
		  lua_lock(L);
		  g = G(L);
		  switch (what) {
			case LUA_GCSTOP: {
			  g.GCthreshold = MAX_LUMEM;
			  break;
			}
			case LUA_GCRESTART: {
			  g.GCthreshold = g.totalbytes;
			  break;
			}
			case LUA_GCCOLLECT: {
			  luaC_fullgc(L);
			  break;
			}
			case LUA_GCCOUNT: {
			  /* GC values are expressed in Kbytes: #bytes/2^10 */
			  res = cast_int(g.totalbytes >> 10);
			  break;
			}
			case LUA_GCCOUNTB: {
			  res = cast_int(g.totalbytes & 0x3ff);
			  break;
			}
			case LUA_GCSTEP: {
			  lu_mem a = ((lu_mem)data << 10);
			  if (a <= g.totalbytes)
				g.GCthreshold = (uint)(g.totalbytes - a);
			  else
				g.GCthreshold = 0;
			  while (g.GCthreshold <= g.totalbytes) {
				luaC_step(L);
				if (g.gcstate == GCSpause) {  /* end of cycle? */
				  res = 1;  /* signal it */
				  break;
				}
			  }
			  break;
			}
			case LUA_GCSETPAUSE: {
			  res = g.gcpause;
			  g.gcpause = data;
			  break;
			}
			case LUA_GCSETSTEPMUL: {
			  res = g.gcstepmul;
			  g.gcstepmul = data;
			  break;
			}
			default:
				res = -1;  /* invalid option */
				break;
		  }
		  lua_unlock(L);
		  return res;
		}



		/*
		** miscellaneous functions
		*/


		public static int lua_error (lua_State L) {
		  lua_lock(L);
		  api_checknelems(L, 1);
		  luaG_errormsg(L);
		  lua_unlock(L);
		  return 0;  /* to avoid warnings */
		}


		public static int lua_next (lua_State L, int idx) {
		  StkId t;
		  int more;
		  lua_lock(L);
		  t = index2adr(L, idx);
		  api_check(L, ttistable(t));
		  more = luaH_next(L, hvalue(t), L.top - 1);
		  if (more != 0) {
			api_incr_top(L);
		  }
		  else  /* no more elements */
			StkId.dec(ref L.top);  /* remove key */
		  lua_unlock(L);
		  return more;
		}


		public static void lua_concat (lua_State L, int n) {
		  lua_lock(L);
		  api_checknelems(L, n);
		  if (n >= 2) {
			luaC_checkGC(L);
			luaV_concat(L, n, cast_int(L.top - L.base_) - 1);
			L.top -= (n-1);
		  }
		  else if (n == 0) {  /* push empty string */
			setsvalue2s(L, L.top, luaS_newlstr(L, "", 0));
			api_incr_top(L);
		  }
		  /* else n == 1; nothing to do */
		  lua_unlock(L);
		}


		public static lua_Alloc lua_getallocf (lua_State L, ref object ud) {
		  lua_Alloc f;
		  lua_lock(L);
		  if (ud != null) ud = G(L).ud;
		  f = G(L).frealloc;
		  lua_unlock(L);
		  return f;
		}


		public static void lua_setallocf (lua_State L, lua_Alloc f, object ud) {
		  lua_lock(L);
		  G(L).ud = ud;
		  G(L).frealloc = f;
		  lua_unlock(L);
		}

		[CLSCompliantAttribute(false)]
		public static object lua_newuserdata(lua_State L, uint size)
		{
			Udata u;
			lua_lock(L);
			luaC_checkGC(L);
			u = luaS_newudata(L, size, getcurrenv(L));
			setuvalue(L, L.top, u);
			api_incr_top(L);
			lua_unlock(L);
			return u.user_data;
		}

		// this one is used internally only
		internal static object lua_newuserdata(lua_State L, Type t)
		{
			Udata u;
			lua_lock(L);
			luaC_checkGC(L);
			u = luaS_newudata(L, t, getcurrenv(L));
			setuvalue(L, L.top, u);
			api_incr_top(L);
			lua_unlock(L);
			return u.user_data;
		}

		static CharPtr aux_upvalue (StkId fi, int n, ref TValue val) {
		  Closure f;
		  if (!ttisfunction(fi)) return null;
		  f = clvalue(fi);
		  if (f.c.isC != 0) {
			if (!(1 <= n && n <= f.c.nupvalues)) return null;
			val = f.c.upvalue[n-1];
			return "";
		  }
		  else {
			Proto p = f.l.p;
			if (!(1 <= n && n <= p.sizeupvalues)) return null;
			val = f.l.upvals[n-1].v;
			return getstr(p.upvalues[n-1]);
		  }
		}


		public static CharPtr lua_getupvalue (lua_State L, int funcindex, int n) {
		  CharPtr name;
		  TValue val = new TValue();
		  lua_lock(L);
		  name = aux_upvalue(index2adr(L, funcindex), n, ref val);
		  if (name != null) {
			setobj2s(L, L.top, val);
			api_incr_top(L);
		  }
		  lua_unlock(L);
		  return name;
		}


		public static CharPtr lua_setupvalue (lua_State L, int funcindex, int n) {
		  CharPtr name;
		  TValue val = new TValue();
		  StkId fi;
		  lua_lock(L);
		  fi = index2adr(L, funcindex);
		  api_checknelems(L, 1);
		  name = aux_upvalue(fi, n, ref val);
		  if (name != null) {
			StkId.dec(ref L.top);
			setobj(L, val, L.top);
			luaC_barrier(L, clvalue(fi), L.top);
		  }
		  lua_unlock(L);
		  return name;
		}

	}
}
