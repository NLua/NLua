/*
** $Id: lstring.c,v 2.8.1.1 2007/12/27 13:02:25 roberto Exp $
** String table (keeps all strings handled by Lua)
** See Copyright Notice in lua.h
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace KopiLua
{
	using lu_byte = System.Byte;

	public partial class Lua
	{
		public static int sizestring(TString s) {return ((int)s.len + 1) * GetUnmanagedSize(typeof(char)); }

		public static int sizeudata(Udata u) { return (int)u.len; }

		public static TString luaS_new(lua_State L, CharPtr s) { return luaS_newlstr(L, s, (uint)strlen(s)); }
		public static TString luaS_newliteral(lua_State L, CharPtr s) { return luaS_newlstr(L, s, (uint)strlen(s)); }

		public static void luaS_fix(TString s)
		{
			lu_byte marked = s.tsv.marked;	// can't pass properties in as ref
			l_setbit(ref marked, FIXEDBIT);
			s.tsv.marked = marked;
		}

		public static void luaS_resize (lua_State L, int newsize) {
		  GCObject[] newhash;
		  stringtable tb;
		  int i;
		  if (G(L).gcstate == GCSsweepstring)
			return;  /* cannot resize during GC traverse */		  
		  newhash = new GCObject[newsize];
		  AddTotalBytes(L, newsize * GetUnmanagedSize(typeof(GCObjectRef)));
		  tb = G(L).strt;
		  for (i=0; i<newsize; i++) newhash[i] = null;

		  /* rehash */
		  for (i=0; i<tb.size; i++) {
			GCObject p = tb.hash[i];
			while (p != null) {  /* for each node in the list */
			  GCObject next = p.gch.next;  /* save next */
			  uint h = gco2ts(p).hash;
			  int h1 = (int)lmod(h, newsize);  /* new position */
			  lua_assert((int)(h%newsize) == lmod(h, newsize));
			  p.gch.next = newhash[h1];  /* chain it */
			  newhash[h1] = p;
			  p = next;
			}
		  }
		  //luaM_freearray(L, tb.hash);
		  if (tb.hash != null)
			  SubtractTotalBytes(L, tb.hash.Length * GetUnmanagedSize(typeof(GCObjectRef)));
		  tb.size = newsize;
		  tb.hash = newhash;
		}

		[CLSCompliantAttribute(false)]
		public static TString newlstr (lua_State L, CharPtr str, uint l,
											   uint h) {
		  TString ts;
		  stringtable tb;
		  if (l+1 > MAX_SIZET /GetUnmanagedSize(typeof(char)))
		    luaM_toobig(L);
		  ts = new TString(new char[l+1]);
		  AddTotalBytes(L, (int)(l + 1) * GetUnmanagedSize(typeof(char)) + GetUnmanagedSize(typeof(TString)));
		  ts.tsv.len = l;
		  ts.tsv.hash = h;
		  ts.tsv.marked = luaC_white(G(L));
		  ts.tsv.tt = LUA_TSTRING;
		  ts.tsv.reserved = 0;
		  //memcpy(ts+1, str, l*GetUnmanagedSize(typeof(char)));
		  memcpy(ts.str.chars, str.chars, str.index, (int)l);
		  ts.str[l] = '\0';  /* ending 0 */
		  tb = G(L).strt;
		  h = (uint)lmod(h, tb.size);
		  ts.tsv.next = tb.hash[h];  /* chain new entry */
		  tb.hash[h] = obj2gco(ts);
		  tb.nuse++;
		  if ((tb.nuse > (int)tb.size) && (tb.size <= MAX_INT/2))
		    luaS_resize(L, tb.size*2);  /* too crowded */
		  return ts;
		}

		[CLSCompliantAttribute(false)]
		public static TString luaS_newlstr (lua_State L, CharPtr str, uint l) {
		  GCObject o;
		  uint h = (uint)l;  /* seed */
		  uint step = (l>>5)+1;  /* if string is too long, don't hash all its chars */
		  uint l1;
		  for (l1=l; l1>=step; l1-=step)  /* compute hash */
			h = h ^ ((h<<5)+(h>>2)+(byte)str[l1-1]);
		  for (o = G(L).strt.hash[lmod(h, G(L).strt.size)];
			   o != null;
			   o = o.gch.next) {
			TString ts = rawgco2ts(o);			
			if (ts.tsv.len == l && (memcmp(str, getstr(ts), l) == 0)) {
			  /* string may be dead */
			  if (isdead(G(L), o)) changewhite(o);
			  return ts;
			}
		  }
		  //return newlstr(L, str, l, h);  /* not found */
		  TString res = newlstr(L, str, l, h);
		  return res;
		}

		[CLSCompliantAttribute(false)]
		public static Udata luaS_newudata(lua_State L, uint s, Table e)
		{
			Udata u = new Udata();
			u.uv.marked = luaC_white(G(L));  /* is not finalized */
			u.uv.tt = LUA_TUSERDATA;
			u.uv.len = s;
			u.uv.metatable = null;
			u.uv.env = e;
			u.user_data = new byte[s];
            AddTotalBytes(L, GetUnmanagedSize(typeof(Udata)) + sizeudata(u));
			/* chain it on udata list (after main thread) */
			u.uv.next = G(L).mainthread.next;
			G(L).mainthread.next = obj2gco(u);
			return u;
		}

		internal static Udata luaS_newudata(lua_State L, Type t, Table e)
		{
			Udata u = new Udata();
			u.uv.marked = luaC_white(G(L));  /* is not finalized */
			u.uv.tt = LUA_TUSERDATA;
			u.uv.len = 0; /* gfoot: not sizeof(t)? */
			u.uv.metatable = null;
			u.uv.env = e;
			u.user_data = luaM_realloc_(L, t);
			AddTotalBytes(L, GetUnmanagedSize(typeof(Udata)));
			/* chain it on udata list (after main thread) */
			u.uv.next = G(L).mainthread.next;
			G(L).mainthread.next = obj2gco(u);
			return u;
		}

	}
}
