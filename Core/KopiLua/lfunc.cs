/*
** $Id: lfunc.c,v 2.12.1.2 2007/12/28 14:58:43 roberto Exp $
** Auxiliary functions to manipulate prototypes and closures
** See Copyright Notice in lua.h
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace KopiLua
{
	using TValue = Lua.lua_TValue;
	using StkId = Lua.lua_TValue;
	using Instruction = System.UInt32;

	public partial class Lua
	{

		public static int sizeCclosure(int n) {
			return GetUnmanagedSize(typeof(CClosure)) + GetUnmanagedSize(typeof(TValue)) * (n - 1);
		}

		public static int sizeLclosure(int n) {
			return GetUnmanagedSize(typeof(LClosure)) + GetUnmanagedSize(typeof(TValue)) * (n - 1);
		}

		public static Closure luaF_newCclosure (lua_State L, int nelems, Table e) {
		  //Closure c = (Closure)luaM_malloc(L, sizeCclosure(nelems));	
		  Closure c = luaM_new<Closure>(L);
		  AddTotalBytes(L, sizeCclosure(nelems));
		  luaC_link(L, obj2gco(c), LUA_TFUNCTION);
		  c.c.isC = 1;
		  c.c.env = e;
		  c.c.nupvalues = cast_byte(nelems);
		  c.c.upvalue = new TValue[nelems];
		  for (int i = 0; i < nelems; i++)
			  c.c.upvalue[i] = new lua_TValue();
		  return c;
		}


		public static Closure luaF_newLclosure (lua_State L, int nelems, Table e) {
		  //Closure c = (Closure)luaM_malloc(L, sizeLclosure(nelems));
		  Closure c = luaM_new<Closure>(L);
		  AddTotalBytes(L, sizeLclosure(nelems));
		  luaC_link(L, obj2gco(c), LUA_TFUNCTION);
		  c.l.isC = 0;
		  c.l.env = e;
		  c.l.nupvalues = cast_byte(nelems);
		  c.l.upvals = new UpVal[nelems];
		  for (int i = 0; i < nelems; i++)
			  c.l.upvals[i] = new UpVal();
		  while (nelems-- > 0) c.l.upvals[nelems] = null;
		  return c;
		}


		public static UpVal luaF_newupval (lua_State L) {
		  UpVal uv = luaM_new<UpVal>(L);
		  luaC_link(L, obj2gco(uv), LUA_TUPVAL);
		  uv.v = uv.u.value;
		  setnilvalue(uv.v);
		  return uv;
		}

		public static UpVal luaF_findupval (lua_State L, StkId level) {
		  global_State g = G(L);
		  GCObjectRef pp = new OpenValRef(L);
		  UpVal p;
		  UpVal uv;
		  while (pp.get() != null && (p = ngcotouv(pp.get())).v >= level) {
			lua_assert(p.v != p.u.value);
			if (p.v == level) {  /* found a corresponding upvalue? */
			  if (isdead(g, obj2gco(p)))  /* is it dead? */
				changewhite(obj2gco(p));  /* ressurect it */
			  return p;
			}
			pp = new NextRef(p);
		  }
		  uv = luaM_new<UpVal>(L);  /* not found: create a new one */
		  uv.tt = LUA_TUPVAL;
		  uv.marked = luaC_white(g);
		  uv.v = level;  /* current value lives in the stack */
		  uv.next = pp.get();  /* chain it in the proper position */
		  pp.set( obj2gco(uv) );
		  uv.u.l.prev = g.uvhead;  /* double link it in `uvhead' list */
		  uv.u.l.next = g.uvhead.u.l.next;
		  uv.u.l.next.u.l.prev = uv;
		  g.uvhead.u.l.next = uv;
		  lua_assert(uv.u.l.next.u.l.prev == uv && uv.u.l.prev.u.l.next == uv);
		  return uv;
		}


		private static void unlinkupval (UpVal uv) {
		  lua_assert(uv.u.l.next.u.l.prev == uv && uv.u.l.prev.u.l.next == uv);
		  uv.u.l.next.u.l.prev = uv.u.l.prev;  /* remove from `uvhead' list */
		  uv.u.l.prev.u.l.next = uv.u.l.next;
		}


		public static void luaF_freeupval (lua_State L, UpVal uv) {
		  if (uv.v != uv.u.value)  /* is it open? */
			unlinkupval(uv);  /* remove from open list */
		  luaM_free(L, uv);  /* free upvalue */
		}


		public static void luaF_close (lua_State L, StkId level) {
		  UpVal uv;
		  global_State g = G(L);
		  while (L.openupval != null && (uv = ngcotouv(L.openupval)).v >= level) {
			GCObject o = obj2gco(uv);
			lua_assert(!isblack(o) && uv.v != uv.u.value);
			L.openupval = uv.next;  /* remove from `open' list */
			if (isdead(g, o))
			  luaF_freeupval(L, uv);  /* free upvalue */
			else {
			  unlinkupval(uv);
			  setobj(L, uv.u.value, uv.v);
			  uv.v = uv.u.value;  /* now current value lives here */
			  luaC_linkupval(L, uv);  /* link upvalue into `gcroot' list */
			}
		  }
		}


		public static Proto luaF_newproto (lua_State L) {
		  Proto f = luaM_new<Proto>(L);
		  luaC_link(L, obj2gco(f), LUA_TPROTO);
		  f.k = null;
		  f.sizek = 0;
		  f.p = null;
		  f.sizep = 0;
		  f.code = null;
		  f.sizecode = 0;
		  f.sizelineinfo = 0;
		  f.sizeupvalues = 0;
		  f.nups = 0;
		  f.upvalues = null;
		  f.numparams = 0;
		  f.is_vararg = 0;
		  f.maxstacksize = 0;
		  f.lineinfo = null;
		  f.sizelocvars = 0;
		  f.locvars = null;
		  f.linedefined = 0;
		  f.lastlinedefined = 0;
		  f.source = null;
		  return f;
		}

		public static void luaF_freeproto (lua_State L, Proto f) {
		  luaM_freearray<Instruction>(L, f.code);
		  luaM_freearray<Proto>(L, f.p);
		  luaM_freearray<TValue>(L, f.k);
		  luaM_freearray<Int32>(L, f.lineinfo);
		  luaM_freearray<LocVar>(L, f.locvars);
		  luaM_freearray<TString>(L, f.upvalues);
		  luaM_free(L, f);
		}

		// we have a gc, so nothing to do
		public static void luaF_freeclosure (lua_State L, Closure c) {
		  int size = (c.c.isC != 0) ? sizeCclosure(c.c.nupvalues) :
								  sizeLclosure(c.l.nupvalues);
		  //luaM_freemem(L, c, size);
		  SubtractTotalBytes(L, size);
		}


		/*
		** Look for n-th local variable at line `line' in function `func'.
		** Returns null if not found.
		*/
		public static CharPtr luaF_getlocalname (Proto f, int local_number, int pc) {
		  int i;
		  for (i = 0; i<f.sizelocvars && f.locvars[i].startpc <= pc; i++) {
			if (pc < f.locvars[i].endpc) {  /* is variable active? */
			  local_number--;
			  if (local_number == 0)
				return getstr(f.locvars[i].varname);
			}
		  }
		  return null;  /* not found */
		}

	}
}
