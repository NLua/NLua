/*
** $Id: lgc.c,v 2.38.1.1 2007/12/27 13:02:25 roberto Exp $
** Garbage Collector
** See Copyright Notice in lua.h
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace KopiLua
{
	using lu_int32 = System.UInt32;
	using l_mem = System.Int32;
	using lu_mem = System.UInt32;
	using TValue = Lua.lua_TValue;
	using StkId = Lua.lua_TValue;
	using lu_byte = System.Byte;
	using Instruction = System.UInt32;

	public partial class Lua
	{
		/*
		** Possible states of the Garbage Collector
		*/
		public const int GCSpause		= 0;
		public const int GCSpropagate	= 1;
		public const int GCSsweepstring	= 2;
		public const int GCSsweep		= 3;
		public const int GCSfinalize	= 4;


		/*
		** some userful bit tricks
		*/
		public static int resetbits(ref lu_byte x, int m) { x &= (lu_byte)~m; return x; }
		public static int setbits(ref lu_byte x, int m) { x |= (lu_byte)m; return x; }
		public static bool testbits(lu_byte x, int m) { return (x & (lu_byte)m) != 0; }
		public static int bitmask(int b)	{return 1<<b;}
		public static int bit2mask(int b1, int b2)	{return (bitmask(b1) | bitmask(b2));}
		public static int l_setbit(ref lu_byte x, int b) { return setbits(ref x, bitmask(b)); }
		public static int resetbit(ref lu_byte x, int b) { return resetbits(ref x, bitmask(b)); }
		public static bool testbit(lu_byte x, int b) { return testbits(x, bitmask(b)); }
		public static int set2bits(ref lu_byte x, int b1, int b2) { return setbits(ref x, (bit2mask(b1, b2))); }
		public static int reset2bits(ref lu_byte x, int b1, int b2) { return resetbits(ref x, (bit2mask(b1, b2))); }
		public static bool test2bits(lu_byte x, int b1, int b2) { return testbits(x, (bit2mask(b1, b2))); }



		/*
		** Layout for bit use in `marked' field:
		** bit 0 - object is white (type 0)
		** bit 1 - object is white (type 1)
		** bit 2 - object is black
		** bit 3 - for userdata: has been finalized
		** bit 3 - for tables: has weak keys
		** bit 4 - for tables: has weak values
		** bit 5 - object is fixed (should not be collected)
		** bit 6 - object is "super" fixed (only the main thread)
		*/


		public const int WHITE0BIT		= 0;
		public const int WHITE1BIT		= 1;
		public const int BLACKBIT		= 2;
		public const int FINALIZEDBIT	= 3;
		public const int KEYWEAKBIT		= 3;
		public const int VALUEWEAKBIT	= 4;
		public const int FIXEDBIT		= 5;
		public const int SFIXEDBIT		= 6;
		public readonly static int WHITEBITS		= bit2mask(WHITE0BIT, WHITE1BIT);


		public static bool iswhite(GCObject x) { return test2bits(x.gch.marked, WHITE0BIT, WHITE1BIT); }
		public static bool isblack(GCObject x) { return testbit(x.gch.marked, BLACKBIT); }
		public static bool isgray(GCObject x) { return (!isblack(x) && !iswhite(x)); }

		public static int otherwhite(global_State g) { return g.currentwhite ^ WHITEBITS; }
		public static bool isdead(global_State g, GCObject v) { return (v.gch.marked & otherwhite(g) & WHITEBITS) != 0; }

		public static void changewhite(GCObject x) { x.gch.marked ^= (byte)WHITEBITS; }
		public static void gray2black(GCObject x) { l_setbit(ref x.gch.marked, BLACKBIT); }

		public static bool valiswhite(TValue x) { return (iscollectable(x) && iswhite(gcvalue(x))); }

		public static byte luaC_white(global_State g) { return (byte)(g.currentwhite & WHITEBITS); }


		public static void luaC_checkGC(lua_State L)
		{
			//condhardstacktests(luaD_reallocstack(L, L.stacksize - EXTRA_STACK - 1));
			//luaD_reallocstack(L, L.stacksize - EXTRA_STACK - 1);
			if (G(L).totalbytes >= G(L).GCthreshold)
				luaC_step(L);
		}


		public static void luaC_barrier(lua_State L, object p, TValue v) { if (valiswhite(v) && isblack(obj2gco(p)))
			luaC_barrierf(L,obj2gco(p),gcvalue(v)); }

		public static void luaC_barriert(lua_State L, Table t, TValue v) { if (valiswhite(v) && isblack(obj2gco(t)))
		    luaC_barrierback(L,t); }

		public static void luaC_objbarrier(lua_State L, object p, object o)
			{ if (iswhite(obj2gco(o)) && isblack(obj2gco(p)))
				luaC_barrierf(L,obj2gco(p),obj2gco(o)); }

		public static void luaC_objbarriert(lua_State L, Table t, object o)
			{ if (iswhite(obj2gco(o)) && isblack(obj2gco(t))) luaC_barrierback(L,t); }

		[CLSCompliantAttribute(false)]
		public const uint GCSTEPSIZE	= 1024;
		public const int GCSWEEPMAX		= 40;
		public const int GCSWEEPCOST	= 10;
		public const int GCFINALIZECOST	= 100;


		public static byte maskmarks	= (byte)(~(bitmask(BLACKBIT)|WHITEBITS));

		public static void makewhite(global_State g, GCObject x)
		{
		   x.gch.marked = (byte)(x.gch.marked & maskmarks | luaC_white(g));
		}

		public static void white2gray(GCObject x) { reset2bits(ref x.gch.marked, WHITE0BIT, WHITE1BIT); }
		public static void black2gray(GCObject x) { resetbit(ref x.gch.marked, BLACKBIT); }

		public static void stringmark(TString s) {reset2bits(ref s.tsv.marked, WHITE0BIT, WHITE1BIT);}

		public static bool isfinalized(Udata_uv u) { return testbit(u.marked, FINALIZEDBIT); }
		public static void markfinalized(Udata_uv u)
		{
			lu_byte marked = u.marked;	// can't pass properties in as ref
			l_setbit(ref marked, FINALIZEDBIT);
			u.marked = marked;
		}


		public static int KEYWEAK		= bitmask(KEYWEAKBIT);
		public static int VALUEWEAK		= bitmask(VALUEWEAKBIT);

		public static void markvalue(global_State g, TValue o) 
		{
			checkconsistency(o);
			if (iscollectable(o) && iswhite(gcvalue(o)))
				reallymarkobject(g,gcvalue(o));
		}

		public static void markobject(global_State g, object t)
		{
			if (iswhite(obj2gco(t)))
				reallymarkobject(g, obj2gco(t));
		}

		public static void setthreshold(global_State g)
		{
			g.GCthreshold = (uint)((g.estimate / 100) * g.gcpause);
		}

		private static void removeentry (Node n) {
		  lua_assert(ttisnil(gval(n)));
		  if (iscollectable(gkey(n)))
			setttype(gkey(n), LUA_TDEADKEY);  /* dead key; remove it */
		}


		private static void reallymarkobject (global_State g, GCObject o) {
		  lua_assert(iswhite(o) && !isdead(g, o));
		  white2gray(o);
		  switch (o.gch.tt) {
			case LUA_TSTRING: {
			  return;
			}
			case LUA_TUSERDATA: {
			  Table mt = gco2u(o).metatable;
			  gray2black(o);  /* udata are never gray */
			  if (mt != null) markobject(g, mt);
			  markobject(g, gco2u(o).env);
			  return;
			}
			case LUA_TUPVAL: {
			  UpVal uv = gco2uv(o);
			  markvalue(g, uv.v);
			  if (uv.v == uv.u.value)  /* closed? */
				gray2black(o);  /* open upvalues are never black */
			  return;
			}
			case LUA_TFUNCTION: {
			  gco2cl(o).c.gclist = g.gray;
			  g.gray = o;
			  break;
			}
			case LUA_TTABLE: {
			  gco2h(o).gclist = g.gray;
			  g.gray = o;
			  break;
			}
			case LUA_TTHREAD: {
			  gco2th(o).gclist = g.gray;
			  g.gray = o;
			  break;
			}
			case LUA_TPROTO: {
			  gco2p(o).gclist = g.gray;
			  g.gray = o;
			  break;
			}
			default: lua_assert(0); break;
		  }
		}


		private static void marktmu (global_State g) {
		  GCObject u = g.tmudata;
		  if (u != null) {
			do {
			  u = u.gch.next;
			  makewhite(g, u);  /* may be marked, if left from previous GC */
			  reallymarkobject(g, u);
			} while (u != g.tmudata);
		  }
		}


		/* move `dead' udata that need finalization to list `tmudata' */
		[CLSCompliantAttribute(false)]
		public static uint luaC_separateudata (lua_State L, int all) {
		  global_State g = G(L);
		  uint deadmem = 0;
		  GCObjectRef p = new NextRef(g.mainthread);
		  GCObject curr;
		  while ((curr = p.get()) != null) {
			if (!(iswhite(curr) || (all!=0)) || isfinalized(gco2u(curr)))
			  p = new NextRef(curr.gch);  /* don't bother with them */
			else if (fasttm(L, gco2u(curr).metatable, TMS.TM_GC) == null) {
			  markfinalized(gco2u(curr));  /* don't need finalization */
			  p = new NextRef(curr.gch);
			}
			else {  /* must call its gc method */
			  deadmem += (uint)sizeudata(gco2u(curr));
			  markfinalized(gco2u(curr));
			  p.set( curr.gch.next );
			  /* link `curr' at the end of `tmudata' list */
			  if (g.tmudata == null)  /* list is empty? */
				g.tmudata = curr.gch.next = curr;  /* creates a circular list */
			  else {
				curr.gch.next = g.tmudata.gch.next;
				g.tmudata.gch.next = curr;
				g.tmudata = curr;
			  }
			}
		  }
		  return deadmem;
		}


		private static int traversetable (global_State g, Table h) {
		  int i;
		  int weakkey = 0;
		  int weakvalue = 0;
		  /*const*/ TValue mode;
		  if (h.metatable != null)
			markobject(g, h.metatable);
		  mode = gfasttm(g, h.metatable, TMS.TM_MODE);
		  if ((mode != null) && ttisstring(mode)) {  /* is there a weak mode? */
			  weakkey = (strchr(svalue(mode), 'k') != null) ? 1 : 0 ;
			  weakvalue = (strchr(svalue(mode), 'v') != null) ? 1 : 0;
			if ((weakkey!=0) || (weakvalue!=0)) {  /* is really weak? */
			  h.marked &= (byte)~(KEYWEAK | VALUEWEAK);  /* clear bits */
			  h.marked |= cast_byte((weakkey << KEYWEAKBIT) |
									 (weakvalue << VALUEWEAKBIT));
			  h.gclist = g.weak;  /* must be cleared after GC, ... */
			  g.weak = obj2gco(h);  /* ... so put in the appropriate list */
			}
		  }
		  if ((weakkey!=0) && (weakvalue!=0)) return 1;
		  if (weakvalue==0) {
			i = h.sizearray;
			while ((i--) != 0)
			  markvalue(g, h.array[i]);
		  }
		  i = sizenode(h);
		  while ((i--) != 0) {
			Node n = gnode(h, i);
			lua_assert(ttype(gkey(n)) != LUA_TDEADKEY || ttisnil(gval(n)));
			if (ttisnil(gval(n)))
			  removeentry(n);  /* remove empty entries */
			else {
			  lua_assert(!ttisnil(gkey(n)));
			  if (weakkey==0) markvalue(g, gkey(n));
			  if (weakvalue==0) markvalue(g, gval(n));
			}
		  }
		  return ((weakkey != 0) || (weakvalue != 0)) ? 1 : 0;
		}


		/*
		** All marks are conditional because a GC may happen while the
		** prototype is still being created
		*/
		private static void traverseproto (global_State g, Proto f) {
		  int i;
		  if (f.source != null) stringmark(f.source);
		  for (i=0; i<f.sizek; i++)  /* mark literals */
			markvalue(g, f.k[i]);
		  for (i=0; i<f.sizeupvalues; i++) {  /* mark upvalue names */
			if (f.upvalues[i] != null)
			  stringmark(f.upvalues[i]);
		  }
		  for (i=0; i<f.sizep; i++) {  /* mark nested protos */
			if (f.p[i] != null)
			  markobject(g, f.p[i]);
		  }
		  for (i=0; i<f.sizelocvars; i++) {  /* mark local-variable names */
			if (f.locvars[i].varname != null)
			  stringmark(f.locvars[i].varname);
		  }
		}



		private static void traverseclosure (global_State g, Closure cl) {
		  markobject(g, cl.c.env);
		  if (cl.c.isC != 0) {
			int i;
			for (i=0; i<cl.c.nupvalues; i++)  /* mark its upvalues */
			  markvalue(g, cl.c.upvalue[i]);
		  }
		  else {
			int i;
			lua_assert(cl.l.nupvalues == cl.l.p.nups);
			markobject(g, cl.l.p);
			for (i=0; i<cl.l.nupvalues; i++)  /* mark its upvalues */
			  markobject(g, cl.l.upvals[i]);
		  }
		}


		private static void checkstacksizes (lua_State L, StkId max) {
		  int ci_used = cast_int(L.ci - L.base_ci[0]);  /* number of `ci' in use */
		  int s_used = cast_int(max - L.stack);  /* part of stack in use */
		  if (L.size_ci > LUAI_MAXCALLS)  /* handling overflow? */
			return;  /* do not touch the stacks */
		  if (4*ci_used < L.size_ci && 2*BASIC_CI_SIZE < L.size_ci)
			luaD_reallocCI(L, L.size_ci/2);  /* still big enough... */
		  //condhardstacktests(luaD_reallocCI(L, ci_used + 1));
		  if (4*s_used < L.stacksize &&
			  2*(BASIC_STACK_SIZE+EXTRA_STACK) < L.stacksize)
			luaD_reallocstack(L, L.stacksize/2);  /* still big enough... */
		  //condhardstacktests(luaD_reallocstack(L, s_used));
		}


		private static void traversestack (global_State g, lua_State l) {
		  StkId o, lim;
		  CallInfo ci;
		  markvalue(g, gt(l));
		  lim = l.top;
		  for (ci = l.base_ci[0]; ci <= l.ci; CallInfo.inc(ref ci)) {
			lua_assert(ci.top <= l.stack_last);
			if (lim < ci.top) lim = ci.top;
		  }
		  for (o = l.stack[0]; o < l.top; StkId.inc(ref o))
			markvalue(g, o);
		  for (; o <= lim; StkId.inc(ref o))
			setnilvalue(o);
		  checkstacksizes(l, lim);
		}


		/*
		** traverse one gray object, turning it to black.
		** Returns `quantity' traversed.
		*/
		private static l_mem propagatemark (global_State g) {
		  GCObject o = g.gray;
		  lua_assert(isgray(o));
		  gray2black(o);
		  switch (o.gch.tt) {
			case LUA_TTABLE: {
			  Table h = gco2h(o);
			  g.gray = h.gclist;
			  if (traversetable(g, h) != 0)  /* table is weak? */
				black2gray(o);  /* keep it gray */
			    return	GetUnmanagedSize(typeof(Table)) +
						GetUnmanagedSize(typeof(TValue)) * h.sizearray +
						GetUnmanagedSize(typeof(Node)) * sizenode(h);
			}
			case LUA_TFUNCTION: {
			  Closure cl = gco2cl(o);
			  g.gray = cl.c.gclist;
			  traverseclosure(g, cl);
			  return (cl.c.isC != 0) ? sizeCclosure(cl.c.nupvalues) :
								   sizeLclosure(cl.l.nupvalues);
			}
			case LUA_TTHREAD: {
			  lua_State th = gco2th(o);
			  g.gray = th.gclist;
			  th.gclist = g.grayagain;
			  g.grayagain = o;
			  black2gray(o);
			  traversestack(g, th);
			  return	GetUnmanagedSize(typeof(lua_State)) +
						GetUnmanagedSize(typeof(TValue)) * th.stacksize +
						GetUnmanagedSize(typeof(CallInfo)) * th.size_ci;
			}
			case LUA_TPROTO: {
			  Proto p = gco2p(o);
			  g.gray = p.gclist;
			  traverseproto(g, p);
			  return	GetUnmanagedSize(typeof(Proto)) +
						GetUnmanagedSize(typeof(Instruction)) * p.sizecode +
						GetUnmanagedSize(typeof(Proto)) * p.sizep +
						GetUnmanagedSize(typeof(TValue)) * p.sizek + 
						GetUnmanagedSize(typeof(int)) * p.sizelineinfo +
						GetUnmanagedSize(typeof(LocVar)) * p.sizelocvars +
						GetUnmanagedSize(typeof(TString)) * p.sizeupvalues;
			}
			default: lua_assert(0); return 0;
		  }
		}


		private static uint propagateall (global_State g) {
		  uint m = 0;
		  while (g.gray != null) m += (uint)propagatemark(g);
		  return m;
		}


		/*
		** The next function tells whether a key or value can be cleared from
		** a weak table. Non-collectable objects are never removed from weak
		** tables. Strings behave as `values', so are never removed too. for
		** other objects: if really collected, cannot keep them; for userdata
		** being finalized, keep them in keys, but not in values
		*/
		private static bool iscleared (TValue o, bool iskey) {
		  if (!iscollectable(o)) return false;
		  if (ttisstring(o)) {
			stringmark(rawtsvalue(o));  /* strings are `values', so are never weak */
			return false;
		  }
		  return iswhite(gcvalue(o)) ||
			(ttisuserdata(o) && (!iskey && isfinalized(uvalue(o))));
		}


		/*
		** clear collected entries from weaktables
		*/
		private static void cleartable (GCObject l) {
		  while (l != null) {
			Table h = gco2h(l);
			int i = h.sizearray;
			lua_assert(testbit(h.marked, VALUEWEAKBIT) ||
					   testbit(h.marked, KEYWEAKBIT));
			if (testbit(h.marked, VALUEWEAKBIT)) {
			  while (i--!= 0) {
				TValue o = h.array[i];
				if (iscleared(o, false))  /* value was collected? */
				  setnilvalue(o);  /* remove value */
			  }
			}
			i = sizenode(h);
			while (i-- != 0) {
			  Node n = gnode(h, i);
			  if (!ttisnil(gval(n)) &&  /* non-empty entry? */
				  (iscleared(key2tval(n), true) || iscleared(gval(n), false))) {
				setnilvalue(gval(n));  /* remove value ... */
				removeentry(n);  /* remove entry from Table */
			  }
			}
			l = h.gclist;
		  }
		}


		private static void freeobj (lua_State L, GCObject o) {
		  switch (o.gch.tt) {
			case LUA_TPROTO: luaF_freeproto(L, gco2p(o)); break;
			case LUA_TFUNCTION: luaF_freeclosure(L, gco2cl(o)); break;
			case LUA_TUPVAL: luaF_freeupval(L, gco2uv(o)); break;
			case LUA_TTABLE: luaH_free(L, gco2h(o)); break;
			case LUA_TTHREAD: {
			  lua_assert(gco2th(o) != L && gco2th(o) != G(L).mainthread);
			  luaE_freethread(L, gco2th(o));
			  break;
			}
			case LUA_TSTRING: {
			  G(L).strt.nuse--;
			  SubtractTotalBytes(L, sizestring(gco2ts(o)));
			  luaM_freemem(L, gco2ts(o));
			  break;
			}
			case LUA_TUSERDATA: {
			  SubtractTotalBytes(L, sizeudata(gco2u(o)));
			  luaM_freemem(L, gco2u(o));
			  break;
			}
			default: lua_assert(0); break;
		  }
		}



		public static void sweepwholelist(lua_State L, GCObjectRef p) { sweeplist(L, p, MAX_LUMEM); }


		private static GCObjectRef sweeplist (lua_State L, GCObjectRef p, lu_mem count) {
		  GCObject curr;
		  global_State g = G(L);
		  int deadmask = otherwhite(g);
		  while ((curr = p.get()) != null && count-- > 0) {
			if (curr.gch.tt == LUA_TTHREAD)  /* sweep open upvalues of each thread */
			  sweepwholelist(L, new OpenValRef( gco2th(curr) ));
			if (((curr.gch.marked ^ WHITEBITS) & deadmask) != 0) {  /* not dead? */
			  lua_assert(!isdead(g, curr) || testbit(curr.gch.marked, FIXEDBIT));
			  makewhite(g, curr);  /* make it white (for next cycle) */
			  p = new NextRef(curr.gch);
			}
			else {  /* must erase `curr' */
			  lua_assert(isdead(g, curr) || deadmask == bitmask(SFIXEDBIT));
			  p.set( curr.gch.next );
			  if (curr == g.rootgc)  /* is the first element of the list? */
				g.rootgc = curr.gch.next;  /* adjust first */
			  freeobj(L, curr);
			}
		  }
		  return p;
		}


		private static void checkSizes (lua_State L) {
		  global_State g = G(L);
		  /* check size of string hash */
		  if (g.strt.nuse < (lu_int32)(g.strt.size/4) &&
			  g.strt.size > MINSTRTABSIZE*2)
			luaS_resize(L, g.strt.size/2);  /* table is too big */
		  /* check size of buffer */
		  if (luaZ_sizebuffer(g.buff) > LUA_MINBUFFER*2) {  /* buffer too big? */
			uint newsize = luaZ_sizebuffer(g.buff) / 2;
			luaZ_resizebuffer(L, g.buff, (int)newsize);
		  }
		}


		private static void GCTM (lua_State L) {
		  global_State g = G(L);
		  GCObject o = g.tmudata.gch.next;  /* get first element */
		  Udata udata = rawgco2u(o);
		  TValue tm;
		  /* remove udata from `tmudata' */
		  if (o == g.tmudata)  /* last element? */
			g.tmudata = null;
		  else
			g.tmudata.gch.next = udata.uv.next;
		  udata.uv.next = g.mainthread.next;  /* return it to `root' list */
		  g.mainthread.next = o;
		  makewhite(g, o);
		  tm = fasttm(L, udata.uv.metatable, TMS.TM_GC);
		  if (tm != null) {
			lu_byte oldah = L.allowhook;
			lu_mem oldt = (lu_mem)g.GCthreshold;
			L.allowhook = 0;  /* stop debug hooks during GC tag method */
			g.GCthreshold = 2*g.totalbytes;  /* avoid GC steps */
			setobj2s(L, L.top, tm);
			setuvalue(L, L.top+1, udata);
			L.top += 2;
			luaD_call(L, L.top - 2, 0);
			L.allowhook = oldah;  /* restore hooks */
			g.GCthreshold = (uint)oldt;  /* restore threshold */
		  }
		}


		/*
		** Call all GC tag methods
		*/
		public static void luaC_callGCTM (lua_State L) {
		  while (G(L).tmudata != null)
			GCTM(L);
		}


		public static void luaC_freeall (lua_State L) {
		  global_State g = G(L);
		  int i;
		  g.currentwhite = (byte)(WHITEBITS | bitmask(SFIXEDBIT));  /* mask to collect all elements */
		  sweepwholelist(L, new RootGCRef(g));
		  for (i = 0; i < g.strt.size; i++)  /* free all string lists */
			sweepwholelist(L, new ArrayRef(g.strt.hash, i));
		}


		private static void markmt (global_State g) {
		  int i;
		  for (i=0; i<NUM_TAGS; i++)
			if (g.mt[i] != null) markobject(g, g.mt[i]);
		}


		/* mark root set */
		private static void markroot (lua_State L) {
		  global_State g = G(L);
		  g.gray = null;
		  g.grayagain = null;
		  g.weak = null;
		  markobject(g, g.mainthread);
		  /* make global table be traversed before main stack */
		  markvalue(g, gt(g.mainthread));
		  markvalue(g, registry(L));
		  markmt(g);
		  g.gcstate = GCSpropagate;
		}


		private static void remarkupvals (global_State g) {
		  UpVal uv;
		  for (uv = g.uvhead.u.l.next; uv != g.uvhead; uv = uv.u.l.next) {
			lua_assert(uv.u.l.next.u.l.prev == uv && uv.u.l.prev.u.l.next == uv);
			if (isgray(obj2gco(uv)))
			  markvalue(g, uv.v);
		  }
		}


		private static void atomic (lua_State L) {
		  global_State g = G(L);
		  uint udsize;  /* total size of userdata to be finalized */
		  /* remark occasional upvalues of (maybe) dead threads */
		  remarkupvals(g);
		  /* traverse objects cautch by write barrier and by 'remarkupvals' */
		  propagateall(g);
		  /* remark weak tables */
		  g.gray = g.weak;
		  g.weak = null;
		  lua_assert(!iswhite(obj2gco(g.mainthread)));
		  markobject(g, L);  /* mark running thread */
		  markmt(g);  /* mark basic metatables (again) */
		  propagateall(g);
		  /* remark gray again */
		  g.gray = g.grayagain;
		  g.grayagain = null;
		  propagateall(g);
		  udsize = luaC_separateudata(L, 0);  /* separate userdata to be finalized */
		  marktmu(g);  /* mark `preserved' userdata */
		  udsize += propagateall(g);  /* remark, to propagate `preserveness' */
		  cleartable(g.weak);  /* remove collected objects from weak tables */
		  /* flip current white */
		  g.currentwhite = cast_byte(otherwhite(g));
		  g.sweepstrgc = 0;
		  g.sweepgc = new RootGCRef(g);
		  g.gcstate = GCSsweepstring;
		  g.estimate = g.totalbytes - udsize;  /* first estimate */
		}


		private static l_mem singlestep (lua_State L) {
		  global_State g = G(L);
		  /*lua_checkmemory(L);*/
		  switch (g.gcstate) {
			case GCSpause: {
			  markroot(L);  /* start a new collection */
			  return 0;
			}
			case GCSpropagate: {
			  if (g.gray != null)
				return propagatemark(g);
			  else {  /* no more `gray' objects */
				atomic(L);  /* finish mark phase */
				return 0;
			  }
			}
			case GCSsweepstring: {
			  lu_mem old = (lu_mem)g.totalbytes;
			  sweepwholelist(L, new ArrayRef(g.strt.hash, g.sweepstrgc++));
			  if (g.sweepstrgc >= g.strt.size)  /* nothing more to sweep? */
				g.gcstate = GCSsweep;  /* end sweep-string phase */
			  lua_assert(old >= g.totalbytes);
			  g.estimate -= (uint)(old - g.totalbytes);
			  return GCSWEEPCOST;
			}
			case GCSsweep: {
			  lu_mem old = (lu_mem)g.totalbytes;
			  g.sweepgc = sweeplist(L, g.sweepgc, GCSWEEPMAX);
			  if (g.sweepgc.get() == null) {  /* nothing more to sweep? */
				checkSizes(L);
				g.gcstate = GCSfinalize;  /* end sweep phase */
			  }
			  lua_assert(old >= g.totalbytes);
			  g.estimate -= (uint)(old - g.totalbytes);
			  return GCSWEEPMAX*GCSWEEPCOST;
			}
			case GCSfinalize: {
			  if (g.tmudata != null) {
				GCTM(L);
				if (g.estimate > GCFINALIZECOST)
				  g.estimate -= GCFINALIZECOST;
				return GCFINALIZECOST;
			  }
			  else {
				g.gcstate = GCSpause;  /* end collection */
				g.gcdept = 0;
				return 0;
			  }
			}
			default: lua_assert(0); return 0;
		  }
		}

		public static void luaC_step (lua_State L) {
		  global_State g = G(L);
		  l_mem lim = (l_mem)((GCSTEPSIZE / 100) * g.gcstepmul);
		  if (lim == 0)
			lim = (l_mem)((MAX_LUMEM-1)/2);  /* no limit */
		  g.gcdept += g.totalbytes - g.GCthreshold;
		  do {
			lim -= singlestep(L);
			if (g.gcstate == GCSpause)
			  break;
		  } while (lim > 0);
		  if (g.gcstate != GCSpause) {
			if (g.gcdept < GCSTEPSIZE)
			  g.GCthreshold = g.totalbytes + GCSTEPSIZE;  /* - lim/g.gcstepmul;*/
			else {
			  g.gcdept -= GCSTEPSIZE;
			  g.GCthreshold = g.totalbytes;
			}
		  }
		  else {
			lua_assert(g.totalbytes >= g.estimate);
			setthreshold(g);
		  }
		}


		public static void luaC_fullgc (lua_State L) {
		  global_State g = G(L);
		  if (g.gcstate <= GCSpropagate) {
			/* reset sweep marks to sweep all elements (returning them to white) */
			g.sweepstrgc = 0;
			g.sweepgc = new RootGCRef(g);
			/* reset other collector lists */
			g.gray = null;
			g.grayagain = null;
			g.weak = null;
			g.gcstate = GCSsweepstring;
		  }
		  lua_assert(g.gcstate != GCSpause && g.gcstate != GCSpropagate);
		  /* finish any pending sweep phase */
		  while (g.gcstate != GCSfinalize) {
			lua_assert(g.gcstate == GCSsweepstring || g.gcstate == GCSsweep);
			singlestep(L);
		  }
		  markroot(L);
		  while (g.gcstate != GCSpause) {
			singlestep(L);
		  }
		  setthreshold(g);
		}


		public static void luaC_barrierf (lua_State L, GCObject o, GCObject v) {
		  global_State g = G(L);
		  lua_assert(isblack(o) && iswhite(v) && !isdead(g, v) && !isdead(g, o));
		  lua_assert(g.gcstate != GCSfinalize && g.gcstate != GCSpause);
		  lua_assert(ttype(o.gch) != LUA_TTABLE);
		  /* must keep invariant? */
		  if (g.gcstate == GCSpropagate)
			reallymarkobject(g, v);  /* restore invariant */
		  else  /* don't mind */
			makewhite(g, o);  /* mark as white just to avoid other barriers */
		}


		public static void luaC_barrierback(lua_State L, Table t)
		{
		  global_State g = G(L);
		  GCObject o = obj2gco(t);
		  lua_assert(isblack(o) && !isdead(g, o));
		  lua_assert(g.gcstate != GCSfinalize && g.gcstate != GCSpause);
		  black2gray(o);  /* make table gray (again) */
		  t.gclist = g.grayagain;
		  g.grayagain = o;
		}


		public static void luaC_link (lua_State L, GCObject o, lu_byte tt) {
		  global_State g = G(L);
		  o.gch.next = g.rootgc;
		  g.rootgc = o;
		  o.gch.marked = luaC_white(g);
		  o.gch.tt = tt;
		}


		public static void luaC_linkupval (lua_State L, UpVal uv) {
		  global_State g = G(L);
		  GCObject o = obj2gco(uv);
		  o.gch.next = g.rootgc;  /* link upvalue into `rootgc' list */
		  g.rootgc = o;
		  if (isgray(o)) { 
			if (g.gcstate == GCSpropagate) {
			  gray2black(o);  /* closed upvalues need barrier */
			  luaC_barrier(L, uv, uv.v);
			}
			else {  /* sweep phase: sweep it (turning it into white) */
			  makewhite(g, o);
			  lua_assert(g.gcstate != GCSfinalize && g.gcstate != GCSpause);
			}
		  }
		}

	}
}
