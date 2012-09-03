/*
** $Id: print.c,v 1.55a 2006/05/31 13:30:05 lhf Exp $
** print bytecodes
** See Copyright Notice in lua.h
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace KopiLua
{
	using TValue = Lua.lua_TValue;
	using Instruction = System.UInt32;

	public partial class Lua
	{

		public static void luaU_print(Proto f, int full) {PrintFunction(f, full);}

		//#define Sizeof(x)	((int)sizeof(x))
		//#define VOID(p)		((const void*)(p))

		public static void PrintString(TString ts)
		{
		 CharPtr s=getstr(ts);
		 uint i,n=ts.tsv.len;
		 putchar('"');
		 for (i=0; i<n; i++)
		 {
		  int c=s[i];
		  switch (c)
		  {
		   case '"': printf("\\\""); break;
		   case '\\': printf("\\\\"); break;
		   case '\a': printf("\\a"); break;
		   case '\b': printf("\\b"); break;
		   case '\f': printf("\\f"); break;
		   case '\n': printf("\\n"); break;
		   case '\r': printf("\\r"); break;
		   case '\t': printf("\\t"); break;
		   case '\v': printf("\\v"); break;
		   default:	if (isprint((byte)c))
   					putchar(c);
				else
					printf("\\%03u",(byte)c);
				break;
		  }
		 }
		 putchar('"');
		}

		private static void PrintConstant(Proto f, int i)
		{
		 /*const*/ TValue o=f.k[i];
		 switch (ttype(o))
		 {
		  case LUA_TNIL:
			printf("nil");
			break;
		  case LUA_TBOOLEAN:
			printf(bvalue(o) != 0 ? "true" : "false");
			break;
		  case LUA_TNUMBER:
			printf(LUA_NUMBER_FMT,nvalue(o));
			break;
		  case LUA_TSTRING:
			PrintString(rawtsvalue(o));
			break;
		  default:				/* cannot happen */
			printf("? type=%d",ttype(o));
			break;
		 }
		}

		private static void PrintCode( Proto f)
		{
		 Instruction[] code = f.code;
		 int pc,n=f.sizecode;
		 for (pc=0; pc<n; pc++)
		 {
		  Instruction i = f.code[pc];
		  OpCode o=GET_OPCODE(i);
		  int a=GETARG_A(i);
		  int b=GETARG_B(i);
		  int c=GETARG_C(i);
		  int bx=GETARG_Bx(i);
		  int sbx=GETARG_sBx(i);
		  int line=getline(f,pc);
		  printf("\t%d\t",pc+1);
		  if (line>0) printf("[%d]\t",line); else printf("[-]\t");
		  printf("%-9s\t",luaP_opnames[(int)o]);
		  switch (getOpMode(o))
		  {
		   case OpMode.iABC:
			printf("%d",a);
			if (getBMode(o) != OpArgMask.OpArgN) printf(" %d", (ISK(b) != 0) ? (-1 - INDEXK(b)) : b);
			if (getCMode(o) != OpArgMask.OpArgN) printf(" %d", (ISK(c) != 0) ? (-1 - INDEXK(c)) : c);
			break;
		   case OpMode.iABx:
			if (getBMode(o)==OpArgMask.OpArgK) printf("%d %d",a,-1-bx); else printf("%d %d",a,bx);
			break;
		   case OpMode.iAsBx:
			if (o==OpCode.OP_JMP) printf("%d",sbx); else printf("%d %d",a,sbx);
			break;
		  }
		  switch (o)
		  {
		   case OpCode.OP_LOADK:
			printf("\t; "); PrintConstant(f,bx);
			break;
		   case OpCode.OP_GETUPVAL:
		   case OpCode.OP_SETUPVAL:
			printf("\t; %s", (f.sizeupvalues>0) ? getstr(f.upvalues[b]) : "-");
			break;
		   case OpCode.OP_GETGLOBAL:
		   case OpCode.OP_SETGLOBAL:
			printf("\t; %s",svalue(f.k[bx]));
			break;
		   case OpCode.OP_GETTABLE:
		   case OpCode.OP_SELF:
			if (ISK(c) != 0) { printf("\t; "); PrintConstant(f,INDEXK(c)); }
			break;
		   case OpCode.OP_SETTABLE:
		   case OpCode.OP_ADD:
		   case OpCode.OP_SUB:
		   case OpCode.OP_MUL:
		   case OpCode.OP_DIV:
		   case OpCode.OP_POW:
		   case OpCode.OP_EQ:
		   case OpCode.OP_LT:
		   case OpCode.OP_LE:
			if (ISK(b)!=0 || ISK(c)!=0)
			{
			 printf("\t; ");
			 if (ISK(b) != 0) PrintConstant(f,INDEXK(b)); else printf("-");
			 printf(" ");
			 if (ISK(c) != 0) PrintConstant(f,INDEXK(c)); else printf("-");
			}
			break;
		   case OpCode.OP_JMP:
		   case OpCode.OP_FORLOOP:
		   case OpCode.OP_FORPREP:
			printf("\t; to %d",sbx+pc+2);
			break;
		   case OpCode.OP_CLOSURE:
			printf("\t; %p",VOID(f.p[bx]));
			break;
		   case OpCode.OP_SETLIST:
			if (c==0) printf("\t; %d",(int)code[++pc]);
			else printf("\t; %d",c);
			break;
		   default:
			break;
		  }
		  printf("\n");
		 }
		}

		public static string SS(int x) { return (x == 1) ? "" : "s"; }
		//#define S(x)	x,SS(x)

		private static void PrintHeader(Proto f)
		{
		 CharPtr s=getstr(f.source);
		 if (s[0]=='@' || s[0]=='=')
		  s  = s.next();
		 else if (s[0]==LUA_SIGNATURE[0])
		  s="(bstring)";
		 else
		  s="(string)";
		 printf("\n%s <%s:%d,%d> (%d Instruction%s, %d bytes at %p)\n",
 			(f.linedefined==0)?"main":"function",s,
			f.linedefined,f.lastlinedefined,
			f.sizecode, SS(f.sizecode), f.sizecode * GetUnmanagedSize(typeof(Instruction)), VOID(f));
		 printf("%d%s param%s, %d slot%s, %d upvalue%s, ",
			f.numparams,(f.is_vararg != 0) ? "+" : "", SS(f.numparams),
			f.maxstacksize, SS(f.maxstacksize), f.nups, SS(f.nups));
		 printf("%d local%s, %d constant%s, %d function%s\n",
			f.sizelocvars, SS(f.sizelocvars), f.sizek, SS(f.sizek), f.sizep, SS(f.sizep));
		}

		private static void PrintConstants(Proto f)
		{
		 int i,n=f.sizek;
		 printf("constants (%d) for %p:\n",n,VOID(f));
		 for (i=0; i<n; i++)
		 {
		  printf("\t%d\t",i+1);
		  PrintConstant(f,i);
		  printf("\n");
		 }
		}

		private static void PrintLocals(Proto f)
		{
		 int i,n=f.sizelocvars;
		 printf("locals (%d) for %p:\n",n,VOID(f));
		 for (i=0; i<n; i++)
		 {
		  printf("\t%d\t%s\t%d\t%d\n",
		  i,getstr(f.locvars[i].varname),f.locvars[i].startpc+1,f.locvars[i].endpc+1);
		 }
		}

		private static void PrintUpvalues(Proto f)
		{
		 int i,n=f.sizeupvalues;
		 printf("upvalues (%d) for %p:\n",n,VOID(f));
		 if (f.upvalues==null) return;
		 for (i=0; i<n; i++)
		 {
		  printf("\t%d\t%s\n",i,getstr(f.upvalues[i]));
		 }
		}

		public static void PrintFunction(Proto f, int full)
		{
		 int i,n=f.sizep;
		 PrintHeader(f);
		 PrintCode(f);
		 if (full != 0)
		 {
		  PrintConstants(f);
		  PrintLocals(f);
		  PrintUpvalues(f);
		 }
		 for (i=0; i<n; i++) PrintFunction(f.p[i],full);
		}
	}
}
