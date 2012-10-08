/*
** $Id: ldebug.c,v 2.29.1.6 2008/05/08 16:56:26 roberto Exp $
** Debug Interface
** See Copyright Notice in lua.h
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace KopiLua
{
	using TValue = Lua.lua_TValue;
	using StkId = Lua.lua_TValue;
	using Instruction = System.UInt32;

	public partial class Lua
	{

		public static int pcRel(InstructionPtr pc, Proto p)
		{
			Debug.Assert(pc.codes == p.code);
			return pc.pc - 1;
		}
		public static int getline(Proto f, int pc) { return (f.lineinfo != null) ? f.lineinfo[pc] : 0; }
		public static void resethookcount(lua_State L) { L.hookcount = L.basehookcount; }


		private static int currentpc (lua_State L, CallInfo ci) {
		  if (!isLua(ci)) return -1;  /* function is not a Lua function? */
		  if (ci == L.ci)
			ci.savedpc = InstructionPtr.Assign(L.savedpc);
		  return pcRel(ci.savedpc, ci_func(ci).l.p);
		}


		private static int currentline (lua_State L, CallInfo ci) {
		  int pc = currentpc(L, ci);
		  if (pc < 0)
			return -1;  /* only active lua functions have current-line information */
		  else
			return getline(ci_func(ci).l.p, pc);
		}


		/*
		** this function can be called asynchronous (e.g. during a signal)
		*/
		public static int lua_sethook (lua_State L, lua_Hook func, int mask, int count) {
		  if (func == null || mask == 0) {  /* turn off hooks? */
			mask = 0;
			func = null;
		  }
		  L.hook = func;
		  L.basehookcount = count;
		  resethookcount(L);
		  L.hookmask = cast_byte(mask);
		  return 1;
		}


		public static lua_Hook lua_gethook (lua_State L) {
		  return L.hook;
		}


		public static int lua_gethookmask (lua_State L) {
		  return L.hookmask;
		}


		public static int lua_gethookcount (lua_State L) {
		  return L.basehookcount;
		}


		public static int lua_getstack (lua_State L, int level, lua_Debug ar) {
		  int status;
		  CallInfo ci;
		  lua_lock(L);
		  for (ci = L.ci; level > 0 && ci > L.base_ci[0]; CallInfo.dec(ref ci)) {
			level--;
			if (f_isLua(ci))  /* Lua function? */
			  level -= ci.tailcalls;  /* skip lost tail calls */
		  }
		  if (level == 0 && ci > L.base_ci[0]) {  /* level found? */
			status = 1;
			ar.i_ci = ci - L.base_ci[0];
		  }
		  else if (level < 0) {  /* level is of a lost tail call? */
			status = 1;
			ar.i_ci = 0;
		  }
		  else status = 0;  /* no such level */
		  lua_unlock(L);
		  return status;
		}


		private static Proto getluaproto (CallInfo ci) {
		  return (isLua(ci) ? ci_func(ci).l.p : null);
		}


		private static CharPtr findlocal (lua_State L, CallInfo ci, int n) {
		  CharPtr name;
		  Proto fp = getluaproto(ci);
		  if ((fp!=null) && (name = luaF_getlocalname(fp, n, currentpc(L, ci))) != null)
			return name;  /* is a local variable in a Lua function */
		  else {
			StkId limit = (ci == L.ci) ? L.top : (ci+1).func;
			if (limit - ci.base_ >= n && n > 0)  /* is 'n' inside 'ci' stack? */
			  return "(*temporary)";
			else
			  return null;
		  }
		}


		public static CharPtr lua_getlocal (lua_State L, lua_Debug ar, int n) {
		  CallInfo ci = L.base_ci[ar.i_ci];
		  CharPtr name = findlocal(L, ci, n);
		  lua_lock(L);
		  if (name != null)
			  luaA_pushobject(L, ci.base_[n - 1]);
		  lua_unlock(L);
		  return name;
		}


		public static CharPtr lua_setlocal (lua_State L, lua_Debug ar, int n) {
		  CallInfo ci = L.base_ci[ar.i_ci];
		  CharPtr name = findlocal(L, ci, n);
		  lua_lock(L);
		  if (name != null)
			  setobjs2s(L, ci.base_[n - 1], L.top-1);
		  StkId.dec(ref L.top);  /* pop value */
		  lua_unlock(L);
		  return name;
		}


		private static void funcinfo (lua_Debug ar, Closure cl) {
		  if (cl.c.isC != 0) {
			ar.source = "=[C]";
			ar.linedefined = -1;
			ar.lastlinedefined = -1;
			ar.what = "C";
		  }
		  else {
			ar.source = getstr(cl.l.p.source);
			ar.linedefined = cl.l.p.linedefined;
			ar.lastlinedefined = cl.l.p.lastlinedefined;
			ar.what = (ar.linedefined == 0) ? "main" : "Lua";
		  }
		  luaO_chunkid(ar.short_src, ar.source, LUA_IDSIZE);
		}


		private static void info_tailcall (lua_Debug ar) {
		  ar.name = ar.namewhat = "";
		  ar.what = "tail";
		  ar.lastlinedefined = ar.linedefined = ar.currentline = -1;
		  ar.source = "=(tail call)";
		  luaO_chunkid(ar.short_src, ar.source, LUA_IDSIZE);
		  ar.nups = 0;
		}


		private static void collectvalidlines (lua_State L, Closure f) {
		  if (f == null || (f.c.isC!=0)) {
			setnilvalue(L.top);
		  }
		  else {
			Table t = luaH_new(L, 0, 0);
			int[] lineinfo = f.l.p.lineinfo;
			int i;
			for (i=0; i<f.l.p.sizelineinfo; i++)
			  setbvalue(luaH_setnum(L, t, lineinfo[i]), 1);
			sethvalue(L, L.top, t); 
		  }
		  incr_top(L);
		}

		private static int auxgetinfo (lua_State L, CharPtr what, lua_Debug ar,
							Closure f, CallInfo ci) {
		  int status = 1;
		  if (f == null) {
			info_tailcall(ar);
			return status;
		  }
		  for (; what[0] != 0; what = what.next()) {
			switch (what[0]) {
			  case 'S': {
				funcinfo(ar, f);
				break;
			  }
			  case 'l': {
				ar.currentline = (ci != null) ? currentline(L, ci) : -1;
				break;
			  }
			  case 'u': {
				ar.nups = f.c.nupvalues;
				break;
			  }
			  case 'n': {
				ar.namewhat = (ci!=null) ? getfuncname(L, ci, ref ar.name) : null;
				if (ar.namewhat == null) {
				  ar.namewhat = "";  /* not found */
				  ar.name = null;
				}
				break;
			  }
			  case 'L':
			  case 'f':  /* handled by lua_getinfo */
				break;
			  default: status = 0;  break;/* invalid option */
			}
		  }
		  return status;
		}


		public static int lua_getinfo (lua_State L, CharPtr what, lua_Debug ar) {
		  int status;
		  Closure f = null;
		  CallInfo ci = null;
		  lua_lock(L);
		  if (what == '>') {
			StkId func = L.top - 1;
			luai_apicheck(L, ttisfunction(func));
			what = what.next();  /* skip the '>' */
			f = clvalue(func);
			StkId.dec(ref L.top);  /* pop function */
		  }
		  else if (ar.i_ci != 0) {  /* no tail call? */
			ci = L.base_ci[ar.i_ci];
			lua_assert(ttisfunction(ci.func));
			f = clvalue(ci.func);
		  }
		  status = auxgetinfo(L, what, ar, f, ci);
		  if (strchr(what, 'f') != null) {
			if (f == null) setnilvalue(L.top);
			else setclvalue(L, L.top, f);
			incr_top(L);
		  }
		  if (strchr(what, 'L') != null)
			collectvalidlines(L, f);
		  lua_unlock(L);
		  return status;
		}


		/*
		** {======================================================
		** Symbolic Execution and code checker
		** =======================================================
		*/

		private static int checkjump(Proto pt, int pc) { if (!(0 <= pc && pc < pt.sizecode)) return 0; return 1; }

		private static int checkreg(Proto pt, int reg) { if (!((reg) < (pt).maxstacksize)) return 0; return 1; }



		private static int precheck (Proto pt) {
		  if (!(pt.maxstacksize <= MAXSTACK)) return 0;
		  if (!(pt.numparams+(pt.is_vararg & VARARG_HASARG) <= pt.maxstacksize)) return 0;
		  if (!(((pt.is_vararg & VARARG_NEEDSARG)==0) ||
					  ((pt.is_vararg & VARARG_HASARG)!=0))) return 0;
		  if (!(pt.sizeupvalues <= pt.nups)) return 0;
		  if (!(pt.sizelineinfo == pt.sizecode || pt.sizelineinfo == 0)) return 0;
		  if (!(pt.sizecode > 0 && GET_OPCODE(pt.code[pt.sizecode - 1]) == OpCode.OP_RETURN)) return 0;
		  return 1;
		}


		public static int checkopenop(Proto pt, int pc) { return luaG_checkopenop(pt.code[pc + 1]); }

		[CLSCompliantAttribute(false)]
		public static int luaG_checkopenop (Instruction i) {
		  switch (GET_OPCODE(i)) {
			case OpCode.OP_CALL:
			case OpCode.OP_TAILCALL:
			case OpCode.OP_RETURN:
			case OpCode.OP_SETLIST: {
			  if (!(GETARG_B(i) == 0)) return 0;
			  return 1;
			}
			default: return 0;  /* invalid instruction after an open call */
		  }
		}


		private static int checkArgMode (Proto pt, int r, OpArgMask mode) {
		  switch (mode) {
			case OpArgMask.OpArgN: if (r!=0) return 0; break;
			case OpArgMask.OpArgU: break;
			case OpArgMask.OpArgR: checkreg(pt, r); break;
			case OpArgMask.OpArgK:
			  if (!( (ISK(r) != 0) ? INDEXK(r) < pt.sizek : r < pt.maxstacksize)) return 0;
			  break;
		  }
		  return 1;
		}


		private static Instruction symbexec (Proto pt, int lastpc, int reg) {
		  int pc;
		  int last;  /* stores position of last instruction that changed `reg' */
		  int dest;
		  last = pt.sizecode-1;  /* points to final return (a `neutral' instruction) */
		  if (precheck(pt)==0) return 0;
		  for (pc = 0; pc < lastpc; pc++) {
			Instruction i = pt.code[pc];
			OpCode op = GET_OPCODE(i);
			int a = GETARG_A(i);
			int b = 0;
			int c = 0;
			if (!((int)op < NUM_OPCODES)) return 0;
			checkreg(pt, a);
			switch (getOpMode(op)) {
			  case OpMode.iABC: {
				b = GETARG_B(i);
				c = GETARG_C(i);
				if (checkArgMode(pt, b, getBMode(op))==0) return 0;
				if (checkArgMode(pt, c, getCMode(op))==0) return 0;
				break;
			  }
			  case OpMode.iABx: {
				b = GETARG_Bx(i);
				if (getBMode(op) == OpArgMask.OpArgK) if (!(b < pt.sizek)) return 0;
				break;
			  }
			  case OpMode.iAsBx: {
				b = GETARG_sBx(i);
				if (getBMode(op) == OpArgMask.OpArgR) {
				  dest = pc+1+b;
				  if (!((0 <= dest && dest < pt.sizecode))) return 0;
				  if (dest > 0) {
					int j;
					/* check that it does not jump to a setlist count; this
					   is tricky, because the count from a previous setlist may
					   have the same value of an invalid setlist; so, we must
					   go all the way back to the first of them (if any) */
					for (j = 0; j < dest; j++) {
					  Instruction d = pt.code[dest-1-j];
					  if (!(GET_OPCODE(d) == OpCode.OP_SETLIST && GETARG_C(d) == 0)) break;
					}
					/* if 'j' is even, previous value is not a setlist (even if
					   it looks like one) */
					  if ((j&1)!=0) return 0;
				  }
				}
				break;
			  }
			}
			if (testAMode(op) != 0) {
			  if (a == reg) last = pc;  /* change register `a' */
			}
			if (testTMode(op) != 0) {
			  if (!(pc+2 < pt.sizecode)) return 0;  /* check skip */
			  if (!(GET_OPCODE(pt.code[pc + 1]) == OpCode.OP_JMP)) return 0;
			}
			switch (op) {
			  case OpCode.OP_LOADBOOL: {
				if (c == 1) {  /* does it jump? */
				  if (!(pc+2 < pt.sizecode)) return 0;  /* check its jump */
				  if (!(GET_OPCODE(pt.code[pc + 1]) != OpCode.OP_SETLIST ||
						GETARG_C(pt.code[pc + 1]) != 0)) return 0;
				}
				break;
			  }
			  case OpCode.OP_LOADNIL: {
				if (a <= reg && reg <= b)
				  last = pc;  /* set registers from `a' to `b' */
				break;
			  }
			  case OpCode.OP_GETUPVAL:
			  case OpCode.OP_SETUPVAL: {
				if (!(b < pt.nups)) return 0;
				break;
			  }
			  case OpCode.OP_GETGLOBAL:
			  case OpCode.OP_SETGLOBAL: {
				if (!(ttisstring(pt.k[b]))) return 0;
				break;
			  }
			  case OpCode.OP_SELF: {
				checkreg(pt, a+1);
				if (reg == a+1) last = pc;
				break;
			  }
			  case OpCode.OP_CONCAT: {
				if (!(b < c)) return 0;  /* at least two operands */
				break;
			  }
			  case OpCode.OP_TFORLOOP: {
				if (!(c >= 1)) return 0;  /* at least one result (control variable) */
				checkreg(pt, a+2+c);  /* space for results */
				if (reg >= a+2) last = pc;  /* affect all regs above its base */
				break;
			  }
			  case OpCode.OP_FORLOOP:
			  case OpCode.OP_FORPREP:
				checkreg(pt, a+3);
				/* go through ...no, on second thoughts don't, because this is C# */
				dest = pc + 1 + b;
				/* not full check and jump is forward and do not skip `lastpc'? */
				if (reg != NO_REG && pc < dest && dest <= lastpc)
					pc += b;  /* do the jump */
				break;

			  case OpCode.OP_JMP: {
				dest = pc+1+b;
				/* not full check and jump is forward and do not skip `lastpc'? */
				if (reg != NO_REG && pc < dest && dest <= lastpc)
				  pc += b;  /* do the jump */
				break;
			  }
			  case OpCode.OP_CALL:
			  case OpCode.OP_TAILCALL: {
				if (b != 0) {
				  checkreg(pt, a+b-1);
				}
				c--;  /* c = num. returns */
				if (c == LUA_MULTRET) {
				  if (checkopenop(pt, pc)==0) return 0;
				}
				else if (c != 0)
				  checkreg(pt, a+c-1);
				if (reg >= a) last = pc;  /* affect all registers above base */
				break;
			  }
			  case OpCode.OP_RETURN: {
				b--;  /* b = num. returns */
				if (b > 0) checkreg(pt, a+b-1);
				break;
			  }
			  case OpCode.OP_SETLIST: {
				if (b > 0) checkreg(pt, a + b);
				if (c == 0) {
				  pc++;
				  if (!(pc < pt.sizecode - 1)) return 0;
				}
				break;
			  }
			  case OpCode.OP_CLOSURE: {
				int nup, j;
				if (!(b < pt.sizep)) return 0;
				nup = pt.p[b].nups;
				if (!(pc + nup < pt.sizecode)) return 0;
				for (j = 1; j <= nup; j++) {
				  OpCode op1 = GET_OPCODE(pt.code[pc + j]);
				  if (!(op1 == OpCode.OP_GETUPVAL || op1 == OpCode.OP_MOVE)) return 0;
				}
				if (reg != NO_REG)  /* tracing? */
				  pc += nup;  /* do not 'execute' these pseudo-instructions */
				break;
			  }
			  case OpCode.OP_VARARG: {
				if (!(	(pt.is_vararg & VARARG_ISVARARG)!=0 &&
						(pt.is_vararg & VARARG_NEEDSARG)==0		)) return 0;
				b--;
				if (b == LUA_MULTRET) if (checkopenop(pt, pc)==0) return 0;
				checkreg(pt, a+b-1);
				break;
			  }
			  default:
				  break;
			}
		  }
		  return pt.code[last];
		}

		//#undef check
		//#undef checkjump
		//#undef checkreg

		/* }====================================================== */


		public static int luaG_checkcode (Proto pt) {
		  return (symbexec(pt, pt.sizecode, NO_REG) != 0) ? 1 : 0;
		}


		private static CharPtr kname (Proto p, int c) {
		  if (ISK(c)!=0 && ttisstring(p.k[INDEXK(c)]))
			return svalue(p.k[INDEXK(c)]);
		  else
			return "?";
		}


		private static CharPtr getobjname (lua_State L, CallInfo ci, int stackpos,
									   ref CharPtr name) {
		  if (isLua(ci)) {  /* a Lua function? */
			Proto p = ci_func(ci).l.p;
			int pc = currentpc(L, ci);
			Instruction i;
			name = luaF_getlocalname(p, stackpos+1, pc);
			if (name!=null)  /* is a local? */
			  return "local";
			i = symbexec(p, pc, stackpos);  /* try symbolic execution */
			lua_assert(pc != -1);
			switch (GET_OPCODE(i)) {
			  case OpCode.OP_GETGLOBAL: {
				int g = GETARG_Bx(i);  /* global index */
				lua_assert(ttisstring(p.k[g]));
				name = svalue(p.k[g]);
				return "global";
			  }
			  case OpCode.OP_MOVE: {
				int a = GETARG_A(i);
				int b = GETARG_B(i);  /* move from `b' to `a' */
				if (b < a)
				  return getobjname(L, ci, b, ref name);  /* get name for `b' */
				break;
			  }
			  case OpCode.OP_GETTABLE: {
				int k = GETARG_C(i);  /* key index */
				name = kname(p, k);
				return "field";
			  }
			  case OpCode.OP_GETUPVAL: {
				int u = GETARG_B(i);  /* upvalue index */
				name = (p.upvalues!=null) ? getstr(p.upvalues[u]) : "?";
				return "upvalue";
			  }
			  case OpCode.OP_SELF: {
				int k = GETARG_C(i);  /* key index */
				name = kname(p, k);
				return "method";
			  }
			  default: break;
			}
		  }
		  return null;  /* no useful name found */
		}


		private static CharPtr getfuncname (lua_State L, CallInfo ci, ref CharPtr name) {
		  Instruction i;
		  if ((isLua(ci) && ci.tailcalls > 0) || !isLua(ci - 1))
			return null;  /* calling function is not Lua (or is unknown) */
		  CallInfo.dec(ref ci);  /* calling function */
		  i = ci_func(ci).l.p.code[currentpc(L, ci)];
		  if (GET_OPCODE(i) == OpCode.OP_CALL || GET_OPCODE(i) == OpCode.OP_TAILCALL ||
			  GET_OPCODE(i) == OpCode.OP_TFORLOOP)
			return getobjname(L, ci, GETARG_A(i), ref name);
		  else
			return null;  /* no useful name can be found */
		}


		/* only ANSI way to check whether a pointer points to an array */
		private static int isinstack (CallInfo ci, TValue o) {
		  StkId p;
		  for (p = ci.base_; p < ci.top; StkId.inc(ref p))
			if (o == p) return 1;
		  return 0;
		}


		public static void luaG_typeerror (lua_State L, TValue o, CharPtr op) {
		  CharPtr name = null;
		  CharPtr t = luaT_typenames[ttype(o)];
		  CharPtr kind = (isinstack(L.ci, o)) != 0 ?
								 getobjname(L, L.ci, cast_int(o - L.base_), ref name) :
								 null;
		  if (kind != null)
			luaG_runerror(L, "attempt to %s %s " + LUA_QS + " (a %s value)",
						op, kind, name, t);
		  else
			luaG_runerror(L, "attempt to %s a %s value", op, t);
		}


		public static void luaG_concaterror (lua_State L, StkId p1, StkId p2) {
		  if (ttisstring(p1) || ttisnumber(p1)) p1 = p2;
		  lua_assert(!ttisstring(p1) && !ttisnumber(p1));
		  luaG_typeerror(L, p1, "concatenate");
		}


		public static void luaG_aritherror (lua_State L, TValue p1, TValue p2) {
		  TValue temp = new TValue();
		  if (luaV_tonumber(p1, temp) == null)
			p2 = p1;  /* first operand is wrong */
		  luaG_typeerror(L, p2, "perform arithmetic on");
		}


		public static int luaG_ordererror (lua_State L, TValue p1, TValue p2) {
		  CharPtr t1 = luaT_typenames[ttype(p1)];
		  CharPtr t2 = luaT_typenames[ttype(p2)];
		  if (t1[2] == t2[2])
			luaG_runerror(L, "attempt to compare two %s values", t1);
		  else
			luaG_runerror(L, "attempt to compare %s with %s", t1, t2);
		  return 0;
		}


		private static void addinfo (lua_State L, CharPtr msg) {
		  CallInfo ci = L.ci;
		  if (isLua(ci)) {  /* is Lua code? */
			CharPtr buff = new CharPtr(new char[LUA_IDSIZE]);  /* add file:line information */
			int line = currentline(L, ci);
			luaO_chunkid(buff, getstr(getluaproto(ci).source), LUA_IDSIZE);
			luaO_pushfstring(L, "%s:%d: %s", buff, line, msg);
		  }
		}


		public static void luaG_errormsg (lua_State L) {
		  if (L.errfunc != 0) {  /* is there an error handling function? */
			StkId errfunc = restorestack(L, L.errfunc);
			if (!ttisfunction(errfunc)) luaD_throw(L, LUA_ERRERR);
			setobjs2s(L, L.top, L.top - 1);  /* move argument */
			setobjs2s(L, L.top - 1, errfunc);  /* push function */
			incr_top(L);
			luaD_call(L, L.top - 2, 1);  /* call it */
		  }
		  luaD_throw(L, LUA_ERRRUN);
		}

		public static void luaG_runerror(lua_State L, CharPtr fmt, params object[] argp)
		{
			addinfo(L, luaO_pushvfstring(L, fmt, argp));
			luaG_errormsg(L);
		}

	}
}
