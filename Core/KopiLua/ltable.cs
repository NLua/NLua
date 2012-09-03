/*
** $Id: ltable.c,v 2.32.1.2 2007/12/28 15:32:23 roberto Exp $
** Lua tables (hash)
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
	using lua_Number = System.Double;

	public partial class Lua
	{
		/*
		** Implementation of tables (aka arrays, objects, or hash tables).
		** Tables keep its elements in two parts: an array part and a hash part.
		** Non-negative integer keys are all candidates to be kept in the array
		** part. The actual size of the array is the largest `n' such that at
		** least half the slots between 0 and n are in use.
		** Hash uses a mix of chained scatter table with Brent's variation.
		** A main invariant of these tables is that, if an element is not
		** in its main position (i.e. the `original' position that its hash gives
		** to it), then the colliding element is in its own main position.
		** Hence even when the load factor reaches 100%, performance remains good.
		*/

		internal static Node gnode(Table t, int i) { return t.node[i]; }
		internal static TKey_nk gkey(Node n) { return n.i_key.nk; }
		internal static TValue gval(Node n) { return n.i_val; }
		internal static Node gnext(Node n) { return n.i_key.nk.next; }

		internal static void gnext_set(Node n, Node v) { n.i_key.nk.next = v; }

		internal static TValue key2tval(Node n) { return n.i_key.tvk; }


		/*
		** max size of array part is 2^MAXBITS
		*/
		//#if LUAI_BITSINT > 26
		public const int MAXBITS = 26;	/* in the dotnet port LUAI_BITSINT is 32 */
		//#else
		//public const int MAXBITS		= (LUAI_BITSINT-2);
		//#endif

		public const int MAXASIZE	= (1 << MAXBITS);


		//public static Node gnode(Table t, int i)	{return t.node[i];}
		internal static Node hashpow2(Table t, lua_Number n) { return gnode(t, (int)lmod(n, sizenode(t))); }
		  
		public static Node hashstr(Table t, TString str)  {return hashpow2(t, str.tsv.hash);}
		public static Node hashboolean(Table t, int p)        {return hashpow2(t, p);}


		/*
		** for some types, it is better to avoid modulus by power of 2, as
		** they tend to have many 2 factors.
		*/
		public static Node hashmod(Table t, int n) { return gnode(t, (int)((uint)n % ((sizenode(t) - 1) | 1))); }

		public static Node hashpointer(Table t, object p) { return hashmod(t, p.GetHashCode()); }


		/*
		** number of ints inside a lua_Number
		*/
		public const int numints = sizeof(lua_Number) / sizeof(int);


		//static const Node dummynode_ = {
		//{{null}, LUA_TNIL},  /* value */
		  //{{{null}, LUA_TNIL, null}}  /* key */
		//};
		public static Node dummynode_ = new Node(new TValue(new Value(), LUA_TNIL), new TKey(new Value(), LUA_TNIL, null));
		public static Node dummynode = dummynode_;

		/*
		** hash for lua_Numbers
		*/
		private static Node hashnum (Table t, lua_Number n) {
    	  byte[] a = BitConverter.GetBytes(n);
		  for (int i = 1; i < a.Length; i++) a[0] += a[i];
		  return hashmod(t, (int)a[0]);
		}



		/*
		** returns the `main' position of an element in a table (that is, the index
		** of its hash value)
		*/
		private static Node mainposition (Table t, TValue key) {
		  switch (ttype(key)) {
			case LUA_TNUMBER:
			  return hashnum(t, nvalue(key));
			case LUA_TSTRING:
			  return hashstr(t, rawtsvalue(key));
			case LUA_TBOOLEAN:
			  return hashboolean(t, bvalue(key));
			case LUA_TLIGHTUSERDATA:
			  return hashpointer(t, pvalue(key));
			default:
				return hashpointer(t, gcvalue(key));
		  }
		}


		/*
		** returns the index for `key' if `key' is an appropriate key to live in
		** the array part of the table, -1 otherwise.
		*/
		private static int arrayindex (TValue key) {
		  if (ttisnumber(key)) {
			lua_Number n = nvalue(key);
			int k;
			lua_number2int(out k, n);
			if (luai_numeq(cast_num(k), n))
			  return k;
		  }
		  return -1;  /* `key' did not match some condition */
		}


		/*
		** returns the index of a `key' for table traversals. First goes all
		** elements in the array part, then elements in the hash part. The
		** beginning of a traversal is signalled by -1.
		*/
		private static int findindex (lua_State L, Table t, StkId key) {
		  int i;
		  if (ttisnil(key)) return -1;  /* first iteration */
		  i = arrayindex(key);
		  if (0 < i && i <= t.sizearray)  /* is `key' inside array part? */
			return i-1;  /* yes; that's the index (corrected to C) */
		  else {
			Node n = mainposition(t, key);
			do {  /* check whether `key' is somewhere in the chain */
			  /* key may be dead already, but it is ok to use it in `next' */
			  if ((luaO_rawequalObj(key2tval(n), key) != 0) ||
					(ttype(gkey(n)) == LUA_TDEADKEY && iscollectable(key) &&
					 gcvalue(gkey(n)) == gcvalue(key))) {
				i = cast_int(n - gnode(t, 0));  /* key index in hash table */
				/* hash elements are numbered after array ones */
				return i + t.sizearray;
			  }
			  else n = gnext(n);
			} while (n != null);
			luaG_runerror(L, "invalid key to " + LUA_QL("next"));  /* key not found */
			return 0;  /* to avoid warnings */
		  }
		}


		public static int luaH_next (lua_State L, Table t, StkId key) {
		  int i = findindex(L, t, key);  /* find original element */
		  for (i++; i < t.sizearray; i++) {  /* try first array part */
			if (!ttisnil(t.array[i])) {  /* a non-nil value? */
			  setnvalue(key, cast_num(i+1));
			  setobj2s(L, key+1, t.array[i]);
			  return 1;
			}
		  }
		  for (i -= t.sizearray; i < sizenode(t); i++) {  /* then hash part */
			if (!ttisnil(gval(gnode(t, i)))) {  /* a non-nil value? */
			  setobj2s(L, key, key2tval(gnode(t, i)));
			  setobj2s(L, key+1, gval(gnode(t, i)));
			  return 1;
			}
		  }
		  return 0;  /* no more elements */
		}


		/*
		** {=============================================================
		** Rehash
		** ==============================================================
		*/


		private static int computesizes (int[] nums, ref int narray) {
		  int i;
		  int twotoi;  /* 2^i */
		  int a = 0;  /* number of elements smaller than 2^i */
		  int na = 0;  /* number of elements to go to array part */
		  int n = 0;  /* optimal size for array part */
		  for (i = 0, twotoi = 1; twotoi/2 < narray; i++, twotoi *= 2) {
			if (nums[i] > 0) {
			  a += nums[i];
			  if (a > twotoi/2) {  /* more than half elements present? */
				n = twotoi;  /* optimal size (till now) */
				na = a;  /* all elements smaller than n will go to array part */
			  }
			}
			if (a == narray) break;  /* all elements already counted */
		  }
		  narray = n;
		  lua_assert(narray/2 <= na && na <= narray);
		  return na;
		}


		private static int countint (TValue key, int[] nums) {
		  int k = arrayindex(key);
		  if (0 < k && k <= MAXASIZE) {  /* is `key' an appropriate array index? */
			nums[ceillog2(k)]++;  /* count as such */
			return 1;
		  }
		  else
			return 0;
		}


		private static int numusearray (Table t, int[] nums) {
		  int lg;
		  int ttlg;  /* 2^lg */
		  int ause = 0;  /* summation of `nums' */
		  int i = 1;  /* count to traverse all array keys */
		  for (lg=0, ttlg=1; lg<=MAXBITS; lg++, ttlg*=2) {  /* for each slice */
			int lc = 0;  /* counter */
			int lim = ttlg;
			if (lim > t.sizearray) {
			  lim = t.sizearray;  /* adjust upper limit */
			  if (i > lim)
				break;  /* no more elements to count */
			}
			/* count elements in range (2^(lg-1), 2^lg] */
			for (; i <= lim; i++) {
			  if (!ttisnil(t.array[i-1]))
				lc++;
			}
			nums[lg] += lc;
			ause += lc;
		  }
		  return ause;
		}


		private static int numusehash (Table t, int[] nums, ref int pnasize) {
		  int totaluse = 0;  /* total number of elements */
		  int ause = 0;  /* summation of `nums' */
		  int i = sizenode(t);
		  while ((i--) != 0) {
			Node n = t.node[i];
			if (!ttisnil(gval(n))) {
			  ause += countint(key2tval(n), nums);
			  totaluse++;
			}
		  }
		  pnasize += ause;
		  return totaluse;
		}


		private static void setarrayvector (lua_State L, Table t, int size) {
		  int i;
		  luaM_reallocvector<TValue>(L, ref t.array, t.sizearray, size/*, TValue*/);
		  for (i=t.sizearray; i<size; i++)
			 setnilvalue(t.array[i]);
		  t.sizearray = size;
		}


		private static void setnodevector (lua_State L, Table t, int size) {
		  int lsize;
		  if (size == 0) {  /* no elements to hash part? */
			  t.node = new Node[] { dummynode };  /* use common `dummynode' */
			lsize = 0;
		  }
		  else {
			int i;
			lsize = ceillog2(size);
			if (lsize > MAXBITS)
			  luaG_runerror(L, "table overflow");
			size = twoto(lsize);
			Node[] nodes = luaM_newvector<Node>(L, size);
			t.node = nodes;
			for (i=0; i<size; i++) {
			  Node n = gnode(t, i);
			  gnext_set(n, null);
			  setnilvalue(gkey(n));
			  setnilvalue(gval(n));
			}
		  }
		  t.lsizenode = cast_byte(lsize);
		  t.lastfree = size;  /* all positions are free */
		}


		private static void resize (lua_State L, Table t, int nasize, int nhsize) {
		  int i;
		  int oldasize = t.sizearray;
		  int oldhsize = t.lsizenode;
		  Node[] nold = t.node;  /* save old hash ... */
		  if (nasize > oldasize)  /* array part must grow? */
			setarrayvector(L, t, nasize);
		  /* create new hash part with appropriate size */
		  setnodevector(L, t, nhsize);  
		  if (nasize < oldasize) {  /* array part must shrink? */
			t.sizearray = nasize;
			/* re-insert elements from vanishing slice */
			for (i=nasize; i<oldasize; i++) {
			  if (!ttisnil(t.array[i]))
				setobjt2t(L, luaH_setnum(L, t, i+1), t.array[i]);
			}
			/* shrink array */
			luaM_reallocvector<TValue>(L, ref t.array, oldasize, nasize/*, TValue*/);
		  }
		  /* re-insert elements from hash part */
		  for (i = twoto(oldhsize) - 1; i >= 0; i--) {
			Node old = nold[i];
			if (!ttisnil(gval(old)))
			  setobjt2t(L, luaH_set(L, t, key2tval(old)), gval(old));
		  }
		  if (nold[0] != dummynode)
			luaM_freearray(L, nold);  /* free old array */
		}


		public static void luaH_resizearray (lua_State L, Table t, int nasize) {
		  int nsize = (t.node[0] == dummynode) ? 0 : sizenode(t);
		  resize(L, t, nasize, nsize);
		}


		private static void rehash (lua_State L, Table t, TValue ek) {
		  int nasize, na;
		  int[] nums = new int[MAXBITS+1];  /* nums[i] = number of keys between 2^(i-1) and 2^i */
		  int i;
		  int totaluse;
		  for (i=0; i<=MAXBITS; i++) nums[i] = 0;  /* reset counts */
		  nasize = numusearray(t, nums);  /* count keys in array part */
		  totaluse = nasize;  /* all those keys are integer keys */
		  totaluse += numusehash(t, nums, ref nasize);  /* count keys in hash part */
		  /* count extra key */
		  nasize += countint(ek, nums);
		  totaluse++;
		  /* compute new size for array part */
		  na = computesizes(nums, ref nasize);
		  /* resize the table to new computed sizes */
		  resize(L, t, nasize, totaluse - na);
		}



		/*
		** }=============================================================
		*/


		public static Table luaH_new (lua_State L, int narray, int nhash) {
		  Table t = luaM_new<Table>(L);
		  luaC_link(L, obj2gco(t), LUA_TTABLE);
		  t.metatable = null;
		  t.flags = cast_byte(~0);
		  /* temporary values (kept only if some malloc fails) */
		  t.array = null;
		  t.sizearray = 0;
		  t.lsizenode = 0;
		  t.node = new Node[] { dummynode };
		  setarrayvector(L, t, narray);
		  setnodevector(L, t, nhash);
		  return t;
		}


		public static void luaH_free (lua_State L, Table t) {
		  if (t.node[0] != dummynode)
			luaM_freearray(L, t.node);
		  luaM_freearray(L, t.array);
		  luaM_free(L, t);
		}


		private static Node getfreepos (Table t) {
		  while (t.lastfree-- > 0) {
			if (ttisnil(gkey(t.node[t.lastfree])))
			  return t.node[t.lastfree];
		  }
		  return null;  /* could not find a free place */
		}



		/*
		** inserts a new key into a hash table; first, check whether key's main 
		** position is free. If not, check whether colliding node is in its main 
		** position or not: if it is not, move colliding node to an empty place and 
		** put new key in its main position; otherwise (colliding node is in its main 
		** position), new key goes to an empty position. 
		*/
		private static TValue newkey (lua_State L, Table t, TValue key) {
		  Node mp = mainposition(t, key);
		  if (!ttisnil(gval(mp)) || mp == dummynode) {
			Node othern;
			Node n = getfreepos(t);  /* get a free place */
			if (n == null) {  /* cannot find a free place? */
			  rehash(L, t, key);  /* grow table */
			  return luaH_set(L, t, key);  /* re-insert key into grown table */
			}
			lua_assert(n != dummynode);
			othern = mainposition(t, key2tval(mp));
			if (othern != mp) {  /* is colliding node out of its main position? */
			  /* yes; move colliding node into free position */
			  while (gnext(othern) != mp) othern = gnext(othern);  /* find previous */
			  gnext_set(othern, n);  /* redo the chain with `n' in place of `mp' */
			  n.i_val = new TValue(mp.i_val);	/* copy colliding node into free pos. (mp.next also goes) */
			  n.i_key = new TKey(mp.i_key);
			  gnext_set(mp, null);  /* now `mp' is free */
			  setnilvalue(gval(mp));
			}
			else {  /* colliding node is in its own main position */
			  /* new node will go into free position */
			  gnext_set(n, gnext(mp));  /* chain new position */
			  gnext_set(mp, n);
			  mp = n;
			}
		  }
		  gkey(mp).value.Copy(key.value); gkey(mp).tt = key.tt;
		  luaC_barriert(L, t, key);
		  lua_assert(ttisnil(gval(mp)));
		  return gval(mp);
		}

		/*
		** search function for integers
		*/
		public static TValue luaH_getnum(Table t, int key)
		{
		  /* (1 <= key && key <= t.sizearray) */
		  if ((uint)(key-1) < (uint)t.sizearray)
			return t.array[key-1];
		  else {
			lua_Number nk = cast_num(key);
			Node n = hashnum(t, nk);
			do {  /* check whether `key' is somewhere in the chain */
			  if (ttisnumber(gkey(n)) && luai_numeq(nvalue(gkey(n)), nk))
				return gval(n);  /* that's it */
			  else n = gnext(n);
			} while (n != null);
			return luaO_nilobject;
		  }
		}


		/*
		** search function for strings
		*/
		public static TValue luaH_getstr (Table t, TString key) {
		  Node n = hashstr(t, key);
		  do {  /* check whether `key' is somewhere in the chain */
			if (ttisstring(gkey(n)) && rawtsvalue(gkey(n)) == key)
			  return gval(n);  /* that's it */
			else n = gnext(n);
		  } while (n != null);
		  return luaO_nilobject;
		}


		/*
		** main search function
		*/
		public static TValue luaH_get (Table t, TValue key) {
		  switch (ttype(key)) {
			case LUA_TNIL: return luaO_nilobject;
			case LUA_TSTRING: return luaH_getstr(t, rawtsvalue(key));
			case LUA_TNUMBER: {
			  int k;
			  lua_Number n = nvalue(key);
			  lua_number2int(out k, n);
			  if (luai_numeq(cast_num(k), nvalue(key))) /* index is int? */
				return luaH_getnum(t, k);  /* use specialized version */
			  /* else go through ... actually on second thoughts don't, because this is C#*/
				Node node = mainposition(t, key);
				do
				{  /* check whether `key' is somewhere in the chain */
					if (luaO_rawequalObj(key2tval(node), key) != 0)
						return gval(node);  /* that's it */
					else node = gnext(node);
				} while (node != null);
				return luaO_nilobject;
			}
			default: {
				Node node = mainposition(t, key);
			  do {  /* check whether `key' is somewhere in the chain */
				if (luaO_rawequalObj(key2tval(node), key) != 0)
				  return gval(node);  /* that's it */
				else node = gnext(node);
			  } while (node != null);
			  return luaO_nilobject;
			}
		  }
		}


		public static TValue luaH_set (lua_State L, Table t, TValue key) {
		  TValue p = luaH_get(t, key);
		  t.flags = 0;
		  if (p != luaO_nilobject)
			return (TValue)p;
		  else {
			if (ttisnil(key)) luaG_runerror(L, "table index is nil");
			else if (ttisnumber(key) && luai_numisnan(nvalue(key)))
			  luaG_runerror(L, "table index is NaN");
			return newkey(L, t, key);
		  }
		}


		public static TValue luaH_setnum (lua_State L, Table t, int key) {
		  TValue p = luaH_getnum(t, key);
		  if (p != luaO_nilobject)
			return (TValue)p;
		  else {
			TValue k = new TValue();
			setnvalue(k, cast_num(key));
			return newkey(L, t, k);
		  }
		}

		public static TValue luaH_setstr (lua_State L, Table t, TString key) {
		  TValue p = luaH_getstr(t, key);
		  if (p != luaO_nilobject)
			return (TValue)p;
		  else {
			TValue k = new TValue();
			setsvalue(L, k, key);
			return newkey(L, t, k);
		  }
		}

		[CLSCompliantAttribute(false)]
		public static int unbound_search (Table t, uint j) {
		  uint i = j;  /* i is zero or a present index */
		  j++;
		  /* find `i' and `j' such that i is present and j is not */
		  while (!ttisnil(luaH_getnum(t, (int)j))) {
			i = j;
			j *= 2;
			if (j > (uint)MAX_INT) {  /* overflow? */
			  /* table was built with bad purposes: resort to linear search */
			  i = 1;
			  while (!ttisnil(luaH_getnum(t, (int)i))) i++;
			  return (int)(i - 1);
			}
		  }
		  /* now do a binary search between them */
		  while (j - i > 1) {
			uint m = (i+j)/2;
			if (ttisnil(luaH_getnum(t, (int)m))) j = m;
			else i = m;
		  }
		  return (int)i;
		}


		/*
		** Try to find a boundary in table `t'. A `boundary' is an integer index
		** such that t[i] is non-nil and t[i+1] is nil (and 0 if t[1] is nil).
		*/
		public static int luaH_getn (Table t) {
		  uint j = (uint)t.sizearray;
		  if (j > 0 && ttisnil(t.array[j - 1])) {
			/* there is a boundary in the array part: (binary) search for it */
			uint i = 0;
			while (j - i > 1) {
			  uint m = (i+j)/2;
			  if (ttisnil(t.array[m - 1])) j = m;
			  else i = m;
			}
			return (int)i;
		  }
		  /* else must find a boundary in hash part */
		  else if (t.node[0] == dummynode)  /* hash part is empty? */
			return (int)j;  /* that is easy... */
		  else return unbound_search(t, j);
		}



		//#if defined(LUA_DEBUG)

		//Node *luaH_mainposition (const Table *t, const TValue *key) {
		//  return mainposition(t, key);
		//}

		//int luaH_isdummy (Node *n) { return n == dummynode; }

		//#endif

	}
}
