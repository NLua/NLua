/*
** $Id: lstrlib.c,v 1.132.1.4 2008/07/11 17:27:21 roberto Exp $
** Standard library for string operations and pattern-matching
** See Copyright Notice in lua.h
*/

using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace KopiLua
{
	using ptrdiff_t = System.Int32;
	using lua_Integer = System.Int32;
	using LUA_INTFRM_T = System.Int64;
	using UNSIGNED_LUA_INTFRM_T = System.UInt64;

	public partial class Lua
	{
		private static int str_len (lua_State L) {
		  uint l;
		  luaL_checklstring(L, 1, out l);
		  lua_pushinteger(L, (int)l);
		  return 1;
		}


		private static ptrdiff_t posrelat (ptrdiff_t pos, uint len) {
		  /* relative string position: negative means back from end */
		  if (pos < 0) pos += (ptrdiff_t)len + 1;
		  return (pos >= 0) ? pos : 0;
		}


		private static int str_sub (lua_State L) {
		  uint l;
		  CharPtr s = luaL_checklstring(L, 1, out l);
		  ptrdiff_t start = posrelat(luaL_checkinteger(L, 2), l);
		  ptrdiff_t end = posrelat(luaL_optinteger(L, 3, -1), l);
		  if (start < 1) start = 1;
		  if (end > (ptrdiff_t)l) end = (ptrdiff_t)l;
		  if (start <= end)
			lua_pushlstring(L, s+start-1, (uint)(end-start+1));
		  else lua_pushliteral(L, "");
		  return 1;
		}


		private static int str_reverse (lua_State L) {
		  uint l;
		  luaL_Buffer b = new luaL_Buffer();
		  CharPtr s = luaL_checklstring(L, 1, out l);
		  luaL_buffinit(L, b);
		  while ((l--) != 0) luaL_addchar(b, s[l]);
		  luaL_pushresult(b);
		  return 1;
		}


		private static int str_lower (lua_State L) {
		  uint l;
		  uint i;
		  luaL_Buffer b = new luaL_Buffer();
		  CharPtr s = luaL_checklstring(L, 1, out l);
		  luaL_buffinit(L, b);
		  for (i=0; i<l; i++)
			  luaL_addchar(b, tolower(s[i]));
		  luaL_pushresult(b);
		  return 1;
		}


		private static int str_upper (lua_State L) {
		  uint l;
		  uint i;
		  luaL_Buffer b = new luaL_Buffer();
		  CharPtr s = luaL_checklstring(L, 1, out l);
		  luaL_buffinit(L, b);
		  for (i=0; i<l; i++)
			  luaL_addchar(b, toupper(s[i]));
		  luaL_pushresult(b);
		  return 1;
		}

		private static int str_rep (lua_State L) {
		  uint l;
		  luaL_Buffer b = new luaL_Buffer();
		  CharPtr s = luaL_checklstring(L, 1, out l);
		  int n = luaL_checkint(L, 2);
		  luaL_buffinit(L, b);
		  while (n-- > 0)
			luaL_addlstring(b, s, l);
		  luaL_pushresult(b);
		  return 1;
		}


		private static int str_byte (lua_State L) {
		  uint l;
		  CharPtr s = luaL_checklstring(L, 1, out l);
		  ptrdiff_t posi = posrelat(luaL_optinteger(L, 2, 1), l);
		  ptrdiff_t pose = posrelat(luaL_optinteger(L, 3, posi), l);
		  int n, i;
		  if (posi <= 0) posi = 1;
		  if ((uint)pose > l) pose = (int)l;
		  if (posi > pose) return 0;  /* empty interval; return no values */
		  n = (int)(pose -  posi + 1);
		  if (posi + n <= pose)  /* overflow? */
			luaL_error(L, "string slice too long");
		  luaL_checkstack(L, n, "string slice too long");
		  for (i=0; i<n; i++)
			  lua_pushinteger(L, (byte)(s[posi + i - 1]));
		  return n;
		}


		private static int str_char (lua_State L) {
		  int n = lua_gettop(L);  /* number of arguments */
		  int i;
		  luaL_Buffer b = new luaL_Buffer();
		  luaL_buffinit(L, b);
		  for (i=1; i<=n; i++) {
			int c = luaL_checkint(L, i);
			luaL_argcheck(L, (byte)(c) == c, i, "invalid value");
			luaL_addchar(b, (char)(byte)c);
		  }
		  luaL_pushresult(b);
		  return 1;
		}


		private static int writer (lua_State L, object b, uint size, object B)
		{
			if (b.GetType() != typeof(CharPtr))
			{
				using (MemoryStream stream = new MemoryStream())
				{
					// todo: figure out a way to do this
					/*
					BinaryFormatter formatter = new BinaryFormatter();
					formatter.Serialize(stream, b);
					stream.Flush();
					byte[] bytes = stream.GetBuffer();
					char[] chars = new char[bytes.Length];
					for (int i = 0; i < bytes.Length; i++)
						chars[i] = (char)bytes[i];
					b = new CharPtr(chars);
					 * */
				}
			}
			luaL_addlstring((luaL_Buffer)B, (CharPtr)b, size);
		  return 0;
		}


		private static int str_dump (lua_State L) {
		  luaL_Buffer b = new luaL_Buffer();
		  luaL_checktype(L, 1, LUA_TFUNCTION);
		  lua_settop(L, 1);
		  luaL_buffinit(L,b);
		  if (lua_dump(L, writer, b) != 0)
			luaL_error(L, "unable to dump given function");
		  luaL_pushresult(b);
		  return 1;
		}



		/*
		** {======================================================
		** PATTERN MATCHING
		** =======================================================
		*/


		public const int CAP_UNFINISHED	= (-1);
		public const int CAP_POSITION	= (-2);

		public class MatchState {

		  public MatchState()
		  {
			  for (int i = 0; i < LUA_MAXCAPTURES; i++)
				  capture[i] = new capture_();
		  }

		  public CharPtr src_init;  /* init of source string */
		  public CharPtr src_end;  /* end (`\0') of source string */
		  public lua_State L;
		  public int level;  /* total number of captures (finished or unfinished) */

		  public class capture_{
			public CharPtr init;
			public ptrdiff_t len;
		  };
		  public capture_[] capture = new capture_[LUA_MAXCAPTURES];
		};


		public const char L_ESC		= '%';
		public const string SPECIALS = "^$*+?.([%-";


		private static int check_capture (MatchState ms, int l) {
		  l -= '1';
		  if (l < 0 || l >= ms.level || ms.capture[l].len == CAP_UNFINISHED)
			return luaL_error(ms.L, "invalid capture index");
		  return l;
		}


		private static int capture_to_close (MatchState ms) {
		  int level = ms.level;
		  for (level--; level>=0; level--)
			if (ms.capture[level].len == CAP_UNFINISHED) return level;
		  return luaL_error(ms.L, "invalid pattern capture");
		}


		private static CharPtr classend (MatchState ms, CharPtr p) {
		  p = new CharPtr(p);
		  char c = p[0];
		  p = p.next();
		  switch (c) {
			case L_ESC: {
			  if (p[0] == '\0')
				luaL_error(ms.L, "malformed pattern (ends with " + LUA_QL("%%") + ")");
			  return p+1;
			}
			case '[': {
			  if (p[0] == '^') p = p.next();
			  do {  /* look for a `]' */
				if (p[0] == '\0')
				  luaL_error(ms.L, "malformed pattern (missing " + LUA_QL("]") + ")");
				c = p[0];
				p = p.next();
				if (c == L_ESC && p[0] != '\0')
				  p = p.next();  /* skip escapes (e.g. `%]') */
			  } while (p[0] != ']');
			  return p+1;
			}
			default: {
			  return p;
			}
		  }
		}


		private static int match_class (int c, int cl) {
		  bool res;
		  switch (tolower(cl)) {
			case 'a' : res = isalpha(c); break;
			case 'c' : res = iscntrl(c); break;
			case 'd' : res = isdigit(c); break;
			case 'l' : res = islower(c); break;
			case 'p' : res = ispunct(c); break;
			case 's' : res = isspace(c); break;
			case 'u' : res = isupper(c); break;
			case 'w' : res = isalnum(c); break;
			case 'x' : res = isxdigit((char)c); break;
			case 'z' : res = (c == 0); break;
			default: return (cl == c) ? 1 : 0;
		  }
		  return (islower(cl) ? (res ? 1 : 0) : ((!res) ? 1 : 0));
		}


		private static int matchbracketclass (int c, CharPtr p, CharPtr ec) {
		  int sig = 1;
		  if (p[1] == '^') {
			sig = 0;
			p = p.next();  /* skip the `^' */
		  }
		  while ((p=p.next()) < ec) {
			if (p == L_ESC) {
			  p = p.next();
			  if (match_class(c, (byte)(p[0])) != 0)
				return sig;
			}
			else if ((p[1] == '-') && (p + 2 < ec)) {
			  p+=2;
			  if ((byte)((p[-2])) <= c && (c <= (byte)p[0]))
				return sig;
			}
			else if ((byte)(p[0]) == c) return sig;
		  }
		  return (sig == 0) ? 1 : 0;
		}


		private static int singlematch (int c, CharPtr p, CharPtr ep) {
		  switch (p[0]) {
			case '.': return 1;  /* matches any char */
			case L_ESC: return match_class(c, (byte)(p[1]));
			case '[': return matchbracketclass(c, p, ep-1);
		    default: return ((byte)(p[0]) == c) ? 1 : 0;
		  }
		}


		private static CharPtr matchbalance (MatchState ms, CharPtr s,
										   CharPtr p) {
		  if ((p[0] == 0) || (p[1] == 0))
			luaL_error(ms.L, "unbalanced pattern");
		  if (s[0] != p[0]) return null;
		  else {
			int b = p[0];
			int e = p[1];
			int cont = 1;
			while ((s=s.next()) < ms.src_end) {
			  if (s[0] == e) {
				if (--cont == 0) return s+1;
			  }
			  else if (s[0] == b) cont++;
			}
		  }
		  return null;  /* string ends out of balance */
		}


		private static CharPtr max_expand (MatchState ms, CharPtr s,
										 CharPtr p, CharPtr ep) {
		  ptrdiff_t i = 0;  /* counts maximum expand for item */
		  while ( (s+i < ms.src_end) && (singlematch((byte)(s[i]), p, ep) != 0) )
			i++;
		  /* keeps trying to match with the maximum repetitions */
		  while (i>=0) {
			CharPtr res = match(ms, (s+i), ep+1);
			if (res != null) return res;
			i--;  /* else didn't match; reduce 1 repetition to try again */
		  }
		  return null;
		}


		private static CharPtr min_expand (MatchState ms, CharPtr s,
										 CharPtr p, CharPtr ep) {
		  for (;;) {
			CharPtr res = match(ms, s, ep+1);
			if (res != null)
			  return res;
		  else if ( (s < ms.src_end) && (singlematch((byte)(s[0]), p, ep) != 0) )
			  s = s.next();  /* try with one more repetition */
			else return null;
		  }
		}


		private static CharPtr start_capture (MatchState ms, CharPtr s,
											CharPtr p, int what) {
		  CharPtr res;
		  int level = ms.level;
		  if (level >= LUA_MAXCAPTURES) luaL_error(ms.L, "too many captures");
		  ms.capture[level].init = s;
		  ms.capture[level].len = what;
		  ms.level = level+1;
		  if ((res=match(ms, s, p)) == null)  /* match failed? */
			ms.level--;  /* undo capture */
		  return res;
		}


		private static CharPtr end_capture(MatchState ms, CharPtr s,
										  CharPtr p) {
		  int l = capture_to_close(ms);
		  CharPtr res;
		  ms.capture[l].len = s - ms.capture[l].init;  /* close capture */
		  if ((res = match(ms, s, p)) == null)  /* match failed? */
			ms.capture[l].len = CAP_UNFINISHED;  /* undo capture */
		  return res;
		}


		private static CharPtr match_capture(MatchState ms, CharPtr s, int l)
		{
		  uint len;
		  l = check_capture(ms, l);
		  len = (uint)ms.capture[l].len;
		  if ((uint)(ms.src_end-s) >= len &&
			  memcmp(ms.capture[l].init, s, len) == 0)
			return s+len;
		  else return null;
		}


		private static CharPtr match (MatchState ms, CharPtr s, CharPtr p) {
		  s = new CharPtr(s);
		  p = new CharPtr(p);
		  init: /* using goto's to optimize tail recursion */
		  switch (p[0]) {
			case '(': {  /* start capture */
			  if (p[1] == ')')  /* position capture? */
				return start_capture(ms, s, p+2, CAP_POSITION);
			  else
				return start_capture(ms, s, p+1, CAP_UNFINISHED);
			}
			case ')': {  /* end capture */
			  return end_capture(ms, s, p+1);
			}
			case L_ESC: {
			  switch (p[1]) {
				case 'b': {  /* balanced string? */
				  s = matchbalance(ms, s, p+2);
				  if (s == null) return null;
				  p+=4; goto init;  /* else return match(ms, s, p+4); */
				}
				case 'f': {  /* frontier? */
				  CharPtr ep; char previous;
				  p += 2;
				  if (p[0] != '[')
					luaL_error(ms.L, "missing " + LUA_QL("[") + " after " +
									   LUA_QL("%%f") + " in pattern");
				  ep = classend(ms, p);  /* points to what is next */
				  previous = (s == ms.src_init) ? '\0' : s[-1];
				  if ((matchbracketclass((byte)(previous), p, ep-1)!=0) ||
					 (matchbracketclass((byte)(s[0]), p, ep-1)==0)) return null;
				  p=ep; goto init;  /* else return match(ms, s, ep); */
				}
				default: {
				  if (isdigit((byte)(p[1]))) {  /* capture results (%0-%9)? */
					s = match_capture(ms, s, (byte)(p[1]));
					if (s == null) return null;
					p+=2; goto init;  /* else return match(ms, s, p+2) */
				  }
					//ismeretlen hiba miatt lett ide átmásolva
				{  /* it is a pattern item */
			  CharPtr ep = classend(ms, p);  /* points to what is next */
			  int m = (s<ms.src_end) && (singlematch((byte)(s[0]), p, ep)!=0) ? 1 : 0;
			  switch (ep[0]) {
				case '?': {  /* optional */
				  CharPtr res;
				  if ((m!=0) && ((res=match(ms, s+1, ep+1)) != null))
					return res;
				  p=ep+1; goto init;  /* else return match(ms, s, ep+1); */
				}
				case '*': {  /* 0 or more repetitions */
				  return max_expand(ms, s, p, ep);
				}
				case '+': {  /* 1 or more repetitions */
				  return ((m!=0) ? max_expand(ms, s+1, p, ep) : null);
				}
				case '-': {  /* 0 or more repetitions (minimum) */
				  return min_expand(ms, s, p, ep);
				}
				default: {
				  if (m==0) return null;
				  s = s.next(); p=ep; goto init;  /* else return match(ms, s+1, ep); */
				}
			  }
			}
				  //goto dflt;  /* case default */
				}
			  }
			}
			case '\0': {  /* end of pattern */
			  return s;  /* match succeeded */
			}
			case '$': {
			  if (p[1] == '\0')  /* is the `$' the last char in pattern? */
				return (s == ms.src_end) ? s : null;  /* check end of string */
			  else goto dflt;
			}
			default: dflt: {  /* it is a pattern item */
			  CharPtr ep = classend(ms, p);  /* points to what is next */
			  int m = (s<ms.src_end) && (singlematch((byte)(s[0]), p, ep)!=0) ? 1 : 0;
			  switch (ep[0]) {
				case '?': {  /* optional */
				  CharPtr res;
				  if ((m!=0) && ((res=match(ms, s+1, ep+1)) != null))
					return res;
				  p=ep+1; goto init;  /* else return match(ms, s, ep+1); */
				}
				case '*': {  /* 0 or more repetitions */
				  return max_expand(ms, s, p, ep);
				}
				case '+': {  /* 1 or more repetitions */
				  return ((m!=0) ? max_expand(ms, s+1, p, ep) : null);
				}
				case '-': {  /* 0 or more repetitions (minimum) */
				  return min_expand(ms, s, p, ep);
				}
				default: {
				  if (m==0) return null;
				  s = s.next(); p=ep; goto init;  /* else return match(ms, s+1, ep); */
				}
			  }
			}
		  }
		}



		private static CharPtr lmemfind (CharPtr s1, uint l1,
									   CharPtr s2, uint l2) {
		  if (l2 == 0) return s1;  /* empty strings are everywhere */
		  else if (l2 > l1) return null;  /* avoids a negative `l1' */
		  else {
			CharPtr init;  /* to search for a `*s2' inside `s1' */
			l2--;  /* 1st char will be checked by `memchr' */
			l1 = l1-l2;  /* `s2' cannot be found after that */
			while (l1 > 0 && (init = memchr(s1, s2[0], l1)) != null) {
			  init = init.next();   /* 1st char is already checked */
			  if (memcmp(init, s2+1, l2) == 0)
				return init-1;
			  else {  /* correct `l1' and `s1' to try again */
				l1 -= (uint)(init-s1);
				s1 = init;
			  }
			}
			return null;  /* not found */
		  }
		}


		private static void push_onecapture (MatchState ms, int i, CharPtr s,
															CharPtr e) {
		  if (i >= ms.level) {
			if (i == 0)  /* ms.level == 0, too */
			  lua_pushlstring(ms.L, s, (uint)(e - s));  /* add whole match */
			else
			  luaL_error(ms.L, "invalid capture index");
		  }
		  else {
			ptrdiff_t l = ms.capture[i].len;
			if (l == CAP_UNFINISHED) luaL_error(ms.L, "unfinished capture");
			if (l == CAP_POSITION)
			  lua_pushinteger(ms.L, ms.capture[i].init - ms.src_init + 1);
			else
			  lua_pushlstring(ms.L, ms.capture[i].init, (uint)l);
		  }
		}


		private static int push_captures (MatchState ms, CharPtr s, CharPtr e) {
		  int i;
		  int nlevels = ((ms.level == 0) && (s!=null)) ? 1 : ms.level;
		  luaL_checkstack(ms.L, nlevels, "too many captures");
		  for (i = 0; i < nlevels; i++)
			push_onecapture(ms, i, s, e);
		  return nlevels;  /* number of strings pushed */
		}


		private static int str_find_aux (lua_State L, int find) {
		  uint l1, l2;
		  CharPtr s = luaL_checklstring(L, 1, out l1);
		  CharPtr p = luaL_checklstring(L, 2, out l2);
		  ptrdiff_t init = posrelat(luaL_optinteger(L, 3, 1), l1) - 1;
		  if (init < 0) init = 0;
		  else if ((uint)(init) > l1) init = (ptrdiff_t)l1;
		  if ((find!=0) && ((lua_toboolean(L, 4)!=0) ||  /* explicit request? */
			  strpbrk(p, SPECIALS) == null)) {  /* or no special characters? */
			/* do a plain search */
			CharPtr s2 = lmemfind(s+init, (uint)(l1-init), p, (uint)(l2));
			if (s2 != null) {
			  lua_pushinteger(L, s2-s+1);
			  lua_pushinteger(L, (int)(s2-s+l2));
			  return 2;
			}
		  }
		  else {
			MatchState ms = new MatchState();
			int anchor = 0;
			if (p[0] == '^')
			{
				p = p.next();
				anchor = 1;
			}
			CharPtr s1=s+init;
			ms.L = L;
			ms.src_init = s;
			ms.src_end = s+l1;
			do {
			  CharPtr res;
			  ms.level = 0;
			  if ((res=match(ms, s1, p)) != null) {
				if (find != 0) {
				  lua_pushinteger(L, s1-s+1);  /* start */
				  lua_pushinteger(L, res-s);   /* end */
				  return push_captures(ms, null, null) + 2;
				}
				else
				  return push_captures(ms, s1, res);
			  }
			} while (((s1=s1.next()) <= ms.src_end) && (anchor==0));
		  }
		  lua_pushnil(L);  /* not found */
		  return 1;
		}


		private static int str_find (lua_State L) {
		  return str_find_aux(L, 1);
		}


		private static int str_match (lua_State L) {
		  return str_find_aux(L, 0);
		}


		private static int gmatch_aux (lua_State L) {
		  MatchState ms = new MatchState();
		  uint ls;
		  CharPtr s = lua_tolstring(L, lua_upvalueindex(1), out ls);
		  CharPtr p = lua_tostring(L, lua_upvalueindex(2));
		  CharPtr src;
		  ms.L = L;
		  ms.src_init = s;
		  ms.src_end = s+ls;
		  for (src = s + (uint)lua_tointeger(L, lua_upvalueindex(3));
			   src <= ms.src_end;
			   src = src.next()) {
			CharPtr e;
			ms.level = 0;
			if ((e = match(ms, src, p)) != null) {
			  lua_Integer newstart = e-s;
			  if (e == src) newstart++;  /* empty match? go at least one position */
			  lua_pushinteger(L, newstart);
			  lua_replace(L, lua_upvalueindex(3));
			  return push_captures(ms, src, e);
			}
		  }
		  return 0;  /* not found */
		}


		private static int gmatch (lua_State L) {
		  luaL_checkstring(L, 1);
		  luaL_checkstring(L, 2);
		  lua_settop(L, 2);
		  lua_pushinteger(L, 0);
		  lua_pushcclosure(L, gmatch_aux, 3);
		  return 1;
		}


		private static int gfind_nodef (lua_State L) {
		  return luaL_error(L, LUA_QL("string.gfind") + " was renamed to " +
							   LUA_QL("string.gmatch"));
		}


		private static void add_s (MatchState ms, luaL_Buffer b, CharPtr s,
														   CharPtr e) {
		  uint l, i;
		  CharPtr news = lua_tolstring(ms.L, 3, out l);
		  for (i = 0; i < l; i++) {
			if (news[i] != L_ESC)
			  luaL_addchar(b, news[i]);
			else {
			  i++;  /* skip ESC */
			  if (!isdigit((byte)(news[i])))
				luaL_addchar(b, news[i]);
			  else if (news[i] == '0')
				  luaL_addlstring(b, s, (uint)(e - s));
			  else {
				push_onecapture(ms, news[i] - '1', s, e);
				luaL_addvalue(b);  /* add capture to accumulated result */
			  }
			}
		  }
		}


		private static void add_value (MatchState ms, luaL_Buffer b, CharPtr s,
															   CharPtr e) {
		  lua_State L = ms.L;
		  switch (lua_type(L, 3)) {
			case LUA_TNUMBER:
			case LUA_TSTRING: {
			  add_s(ms, b, s, e);
			  return;
			}
			case LUA_TUSERDATA:
			case LUA_TFUNCTION: {
			  int n;
			  lua_pushvalue(L, 3);
			  n = push_captures(ms, s, e);
			  lua_call(L, n, 1);
			  break;
			}
			case LUA_TTABLE: {
			  push_onecapture(ms, 0, s, e);
			  lua_gettable(L, 3);
			  break;
			}
		  }
		  if (lua_toboolean(L, -1)==0) {  /* nil or false? */
			lua_pop(L, 1);
			lua_pushlstring(L, s, (uint)(e - s));  /* keep original text */
		  }
		  else if (lua_isstring(L, -1)==0)
			luaL_error(L, "invalid replacement value (a %s)", luaL_typename(L, -1)); 
		  luaL_addvalue(b);  /* add result to accumulator */
		}


		private static int str_gsub (lua_State L) {
		  uint srcl;
		  CharPtr src = luaL_checklstring(L, 1, out srcl);
		  CharPtr p = luaL_checkstring(L, 2);
		  int  tr = lua_type(L, 3);
		  int max_s = luaL_optint(L, 4, (int)(srcl+1));
		  int anchor = 0;
		  if (p[0] == '^')
		  {
			  p = p.next();
			  anchor = 1;
		  }
		  int n = 0;
		  MatchState ms = new MatchState();
		  luaL_Buffer b = new luaL_Buffer();
		  luaL_argcheck(L, tr == LUA_TNUMBER || tr == LUA_TSTRING ||
						   tr == LUA_TFUNCTION || tr == LUA_TTABLE ||
						   tr == LUA_TUSERDATA, 3,
							  "string/function/table expected");
		  luaL_buffinit(L, b);
		  ms.L = L;
		  ms.src_init = src;
		  ms.src_end = src+srcl;
		  while (n < max_s) {
			CharPtr e;
			ms.level = 0;
			e = match(ms, src, p);
			if (e != null) {
			  n++;
			  add_value(ms, b, src, e);
			}
			if ((e!=null) && e>src) /* non empty match? */
			  src = e;  /* skip it */
			else if (src < ms.src_end)
			{
				char c = src[0];
				src = src.next();
				luaL_addchar(b, c);
			}
			else break;
			if (anchor != 0) break;
		  }
		  luaL_addlstring(b, src, (uint)(ms.src_end-src));
		  luaL_pushresult(b);
		  lua_pushinteger(L, n);  /* number of substitutions */
		  return 2;
		}

		/* }====================================================== */


		/* maximum size of each formatted item (> len(format('%99.99f', -1e308))) */
		public const int MAX_ITEM	= 512;
		/* valid flags in a format specification */
		public const string FLAGS = "-+ #0";
		/*
		** maximum size of each format specification (such as '%-099.99d')
		** (+10 accounts for %99.99x plus margin of error)
		*/
		public static readonly int MAX_FORMAT = (FLAGS.Length+1) + (LUA_INTFRMLEN.Length+1) + 10;


		private static void addquoted (lua_State L, luaL_Buffer b, int arg) {
		  uint l;
		  CharPtr s = luaL_checklstring(L, arg, out l);
		  luaL_addchar(b, '"');
		  while ((l--) != 0) {
			switch (s[0]) {
			  case '"': case '\\': case '\n': {
				luaL_addchar(b, '\\');
				luaL_addchar(b, s[0]);
				break;
			  }
			  case '\r': {
				luaL_addlstring(b, "\\r", 2);
				break;
			  }
			  case '\0': {
				luaL_addlstring(b, "\\000", 4);
				break;
			  }
			  default: {
				luaL_addchar(b, s[0]);
				break;
			  }
			}
			s = s.next();
		  }
		  luaL_addchar(b, '"');
		}

		private static CharPtr scanformat (lua_State L, CharPtr strfrmt, CharPtr form) {
		  CharPtr p = strfrmt;
		  while (p[0] != '\0' && strchr(FLAGS, p[0]) != null) p = p.next();  /* skip flags */
		  if ((uint)(p - strfrmt) >= (FLAGS.Length+1))
			luaL_error(L, "invalid format (repeated flags)");
		  if (isdigit((byte)(p[0]))) p = p.next();  /* skip width */
		  if (isdigit((byte)(p[0]))) p = p.next();  /* (2 digits at most) */
		  if (p[0] == '.') {
			p = p.next();
			if (isdigit((byte)(p[0]))) p = p.next();  /* skip precision */
			if (isdigit((byte)(p[0]))) p = p.next();  /* (2 digits at most) */
		  }
		  if (isdigit((byte)(p[0])))
			luaL_error(L, "invalid format (width or precision too long)");
		  form[0] = '%';
		  form = form.next();
		  strncpy(form, strfrmt, p - strfrmt + 1);
		  form += p - strfrmt + 1;
		  form[0] = '\0';
		  return p;
		}


		private static void addintlen (CharPtr form) {
		  uint l = (uint)strlen(form);
		  char spec = form[l - 1];
		  strcpy(form + l - 1, LUA_INTFRMLEN);
		  form[l + (LUA_INTFRMLEN.Length + 1) - 2] = spec;
		  form[l + (LUA_INTFRMLEN.Length + 1) - 1] = '\0';
		}


		private static int str_format (lua_State L) {
		  int arg = 1;
		  uint sfl;
		  CharPtr strfrmt = luaL_checklstring(L, arg, out sfl);
		  CharPtr strfrmt_end = strfrmt+sfl;
		  luaL_Buffer b = new luaL_Buffer();
		  luaL_buffinit(L, b);
		  while (strfrmt < strfrmt_end) {
			  if (strfrmt[0] != L_ESC)
			  {
				  luaL_addchar(b, strfrmt[0]);
				  strfrmt = strfrmt.next();
			  }
			  else if (strfrmt[1] == L_ESC)
			  {
				  luaL_addchar(b, strfrmt[0]);  /* %% */
				  strfrmt = strfrmt + 2;
			  }
			  else
			  { /* format item */
				  strfrmt = strfrmt.next();
				  CharPtr form = new char[MAX_FORMAT];  /* to store the format (`%...') */
				  CharPtr buff = new char[MAX_ITEM];  /* to store the formatted item */
				  arg++;
				  strfrmt = scanformat(L, strfrmt, form);
				  char ch = strfrmt[0];
				  strfrmt = strfrmt.next();
				  switch (ch)
				  {
					  case 'c':
						  {
							  sprintf(buff, form, (int)luaL_checknumber(L, arg));
							  break;
						  }
					  case 'd':
					  case 'i':
						  {
							  addintlen(form);
							  sprintf(buff, form, (LUA_INTFRM_T)luaL_checknumber(L, arg));
							  break;
						  }
					  case 'o':
					  case 'u':
					  case 'x':
					  case 'X':
						  {
							  addintlen(form);
							  sprintf(buff, form, (UNSIGNED_LUA_INTFRM_T)luaL_checknumber(L, arg));
							  break;
						  }
					  case 'e':
					  case 'E':
					  case 'f':
					  case 'g':
					  case 'G':
						  {
							  sprintf(buff, form, (double)luaL_checknumber(L, arg));
							  break;
						  }
					  case 'q':
						  {
							  addquoted(L, b, arg);
							  continue;  /* skip the 'addsize' at the end */
						  }
					  case 's':
						  {
							  uint l;
							  CharPtr s = luaL_checklstring(L, arg, out l);
							  if ((strchr(form, '.') == null) && l >= 100)
							  {
								  /* no precision and string is too long to be formatted;
									 keep original string */
								  lua_pushvalue(L, arg);
								  luaL_addvalue(b);
								  continue;  /* skip the `addsize' at the end */
							  }
							  else
							  {
								  sprintf(buff, form, s);
								  break;
							  }
						  }
					  default:
						  {  /* also treat cases `pnLlh' */
							  return luaL_error(L, "invalid option " + LUA_QL("%%%c") + " to " +
												   LUA_QL("format"), strfrmt[-1]);
						  }
				  }
				  luaL_addlstring(b, buff, (uint)strlen(buff));
			  }
		  }
		  luaL_pushresult(b);
		  return 1;
		}


		private readonly static luaL_Reg[] strlib = {
		  new luaL_Reg("byte", str_byte),
		  new luaL_Reg("char", str_char),
		  new luaL_Reg("dump", str_dump),
		  new luaL_Reg("find", str_find),
		  new luaL_Reg("format", str_format),
		  new luaL_Reg("gfind", gfind_nodef),
		  new luaL_Reg("gmatch", gmatch),
		  new luaL_Reg("gsub", str_gsub),
		  new luaL_Reg("len", str_len),
		  new luaL_Reg("lower", str_lower),
		  new luaL_Reg("match", str_match),
		  new luaL_Reg("rep", str_rep),
		  new luaL_Reg("reverse", str_reverse),
		  new luaL_Reg("sub", str_sub),
		  new luaL_Reg("upper", str_upper),
		  new luaL_Reg(null, null)
		};


		private static void createmetatable (lua_State L) {
		  lua_createtable(L, 0, 1);  /* create metatable for strings */
		  lua_pushliteral(L, "");  /* dummy string */
		  lua_pushvalue(L, -2);
		  lua_setmetatable(L, -2);  /* set string metatable */
		  lua_pop(L, 1);  /* pop dummy string */
		  lua_pushvalue(L, -2);  /* string library... */
		  lua_setfield(L, -2, "__index");  /* ...is the __index metamethod */
		  lua_pop(L, 1);  /* pop metatable */
		}


		/*
		** Open string library
		*/
		public static int luaopen_string (lua_State L) {
		  luaL_register(L, LUA_STRLIBNAME, strlib);
		#if LUA_COMPAT_GFIND
		  lua_getfield(L, -1, "gmatch");
		  lua_setfield(L, -2, "gfind");
		#endif
		  createmetatable(L);
		  return 1;
		}

	}
}
