/*
** $Id: lopcodes.c,v 1.37.1.1 2007/12/27 13:02:25 roberto Exp $
** See Copyright Notice in lua.h
*/

using System;
using System.Collections.Generic;
using System.Text;

namespace KopiLua
{
	using lu_byte = System.Byte;
	using Instruction = System.UInt32;

	public partial class Lua
	{
		/*===========================================================================
		  We assume that instructions are unsigned numbers.
		  All instructions have an opcode in the first 6 bits.
		  Instructions can have the following fields:
			`A' : 8 bits
			`B' : 9 bits
			`C' : 9 bits
			`Bx' : 18 bits (`B' and `C' together)
			`sBx' : signed Bx

		  A signed argument is represented in excess K; that is, the number
		  value is the unsigned value minus K. K is exactly the maximum value
		  for that argument (so that -max is represented by 0, and +max is
		  represented by 2*max), which is half the maximum for the corresponding
		  unsigned argument.
		===========================================================================*/


		public enum OpMode {iABC, iABx, iAsBx};  /* basic instruction format */


		/*
		** size and position of opcode arguments.
		*/
		public const int SIZE_C		= 9;
		public const int SIZE_B		= 9;
		public const int SIZE_Bx	= (SIZE_C + SIZE_B);
		public const int SIZE_A		= 8;

		public const int SIZE_OP	= 6;

		public const int POS_OP		= 0;
		public const int POS_A		= (POS_OP + SIZE_OP);
		public const int POS_C		= (POS_A + SIZE_A);
		public const int POS_B		= (POS_C + SIZE_C);
		public const int POS_Bx		= POS_C;


		/*
		** limits for opcode arguments.
		** we use (signed) int to manipulate most arguments,
		** so they must fit in LUAI_BITSINT-1 bits (-1 for sign)
		*/
		//#if SIZE_Bx < LUAI_BITSINT-1
		public const int MAXARG_Bx         = ((1<<SIZE_Bx)-1);
		public const int MAXARG_sBx        = (MAXARG_Bx>>1);         /* `sBx' is signed */
		//#else
		//public const int MAXARG_Bx			= System.Int32.MaxValue;
		//public const int MAXARG_sBx			= System.Int32.MaxValue;
		//#endif

		[CLSCompliantAttribute(false)]
		public const uint MAXARG_A        = (uint)((1 << (int)SIZE_A) -1);
		[CLSCompliantAttribute(false)]
		public const uint MAXARG_B		  = (uint)((1 << (int)SIZE_B) -1);
		[CLSCompliantAttribute(false)]
		public const uint MAXARG_C        = (uint)((1 << (int)SIZE_C) -1);


		/* creates a mask with `n' 1 bits at position `p' */
		//public static int MASK1(int n, int p) { return ((~((~(Instruction)0) << n)) << p); }
		internal static uint MASK1(int n, int p) { return (uint)((~((~0) << n)) << p); }

		/* creates a mask with `n' 0 bits at position `p' */
		internal static uint MASK0(int n, int p) { return (uint)(~MASK1(n, p)); }

		/*
		** the following macros help to manipulate instructions
		*/

		internal static OpCode GET_OPCODE(Instruction i)
		{
			return (OpCode)((i >> POS_OP) & MASK1(SIZE_OP, 0));
		}
		internal static OpCode GET_OPCODE(InstructionPtr i) { return GET_OPCODE(i[0]); }

		internal static void SET_OPCODE(ref Instruction i, Instruction o)
		{
			i = (Instruction)(i & MASK0(SIZE_OP, POS_OP)) | ((o << POS_OP) & MASK1(SIZE_OP, POS_OP));
		}
		internal static void SET_OPCODE(ref Instruction i, OpCode opcode)
		{
			i = (Instruction)(i & MASK0(SIZE_OP, POS_OP)) | (((uint)opcode << POS_OP) & MASK1(SIZE_OP, POS_OP));
		}
		internal static void SET_OPCODE(InstructionPtr i, OpCode opcode) { SET_OPCODE(ref i.codes[i.pc], opcode); }

		internal static int GETARG_A(Instruction i)
		{
			return (int)((i >> POS_A) & MASK1(SIZE_A, 0));
		}
		internal static int GETARG_A(InstructionPtr i) { return GETARG_A(i[0]); }

		internal static void SETARG_A(InstructionPtr i, int u)
		{
			i[0] = (Instruction)((i[0] & MASK0(SIZE_A, POS_A)) | ((u << POS_A) & MASK1(SIZE_A, POS_A)));
		}

		internal static int GETARG_B(Instruction i)
		{
			return (int)((i>>POS_B) & MASK1(SIZE_B,0));
		}
		internal static int GETARG_B(InstructionPtr i) { return GETARG_B(i[0]); }

		internal static void SETARG_B(InstructionPtr i, int b)
		{
			i[0] = (Instruction)((i[0] & MASK0(SIZE_B, POS_B)) | ((b << POS_B) & MASK1(SIZE_B, POS_B)));
		}

		internal static int GETARG_C(Instruction i)
		{
			return (int)((i>>POS_C) & MASK1(SIZE_C,0));
		}
		internal static int GETARG_C(InstructionPtr i) { return GETARG_C(i[0]); }

		internal static void SETARG_C(InstructionPtr i, int b)
		{
			i[0] = (Instruction)((i[0] & MASK0(SIZE_C, POS_C)) | ((b << POS_C) & MASK1(SIZE_C, POS_C)));
		}

		internal static int GETARG_Bx(Instruction i)
		{
			return (int)((i>>POS_Bx) & MASK1(SIZE_Bx,0));
		}
		internal static int GETARG_Bx(InstructionPtr i) { return GETARG_Bx(i[0]); }

		internal static void SETARG_Bx(InstructionPtr i, int b)
		{
			i[0] = (Instruction)((i[0] & MASK0(SIZE_Bx, POS_Bx)) | ((b << POS_Bx) & MASK1(SIZE_Bx, POS_Bx)));
		}

		internal static int GETARG_sBx(Instruction i)
		{
			return (GETARG_Bx(i) - MAXARG_sBx);
		}
		internal static int GETARG_sBx(InstructionPtr i) { return GETARG_sBx(i[0]); }

		internal static void SETARG_sBx(InstructionPtr i, int b)
		{
			SETARG_Bx(i, b + MAXARG_sBx);
		}

		internal static int CREATE_ABC(OpCode o, int a, int b, int c)
		{
			return (int)(((int)o << POS_OP) | (a << POS_A) | (b << POS_B) | (c << POS_C));
		}

		internal static int CREATE_ABx(OpCode o, int a, int bc)
		{
			int result = (int)(((int)o << POS_OP) | (a << POS_A) | (bc << POS_Bx));
			return result;
		}


		/*
		** Macros to operate RK indices
		*/

		/* this bit 1 means constant (0 means register) */
		internal readonly static int BITRK = (1 << (SIZE_B - 1));

		/* test whether value is a constant */
		internal static int ISK(int x) { return x & BITRK; }

		/* gets the index of the constant */
		internal static int INDEXK(int r) { return r & (~BITRK); }

		internal static readonly int MAXINDEXRK = BITRK - 1;

		/* code a constant index as a RK value */
		internal static int RKASK(int x) { return x | BITRK; }


		/*
		** invalid register that fits in 8 bits
		*/
		internal static readonly int NO_REG		= (int)MAXARG_A;


		/*
		** R(x) - register
		** Kst(x) - constant (in constant table)
		** RK(x) == if ISK(x) then Kst(INDEXK(x)) else R(x)
		*/


		/*
		** grep "ORDER OP" if you change these enums
		*/

		public enum OpCode {
		/*----------------------------------------------------------------------
		name		args	description
		------------------------------------------------------------------------*/
		OP_MOVE,/*	A B	R(A) := R(B)					*/
		OP_LOADK,/*	A Bx	R(A) := Kst(Bx)					*/
		OP_LOADBOOL,/*	A B C	R(A) := (Bool)B; if (C) pc++			*/
		OP_LOADNIL,/*	A B	R(A) := ... := R(B) := nil			*/
		OP_GETUPVAL,/*	A B	R(A) := UpValue[B]				*/

		OP_GETGLOBAL,/*	A Bx	R(A) := Gbl[Kst(Bx)]				*/
		OP_GETTABLE,/*	A B C	R(A) := R(B)[RK(C)]				*/

		OP_SETGLOBAL,/*	A Bx	Gbl[Kst(Bx)] := R(A)				*/
		OP_SETUPVAL,/*	A B	UpValue[B] := R(A)				*/
		OP_SETTABLE,/*	A B C	R(A)[RK(B)] := RK(C)				*/

		OP_NEWTABLE,/*	A B C	R(A) := {} (size = B,C)				*/

		OP_SELF,/*	A B C	R(A+1) := R(B); R(A) := R(B)[RK(C)]		*/

		OP_ADD,/*	A B C	R(A) := RK(B) + RK(C)				*/
		OP_SUB,/*	A B C	R(A) := RK(B) - RK(C)				*/
		OP_MUL,/*	A B C	R(A) := RK(B) * RK(C)				*/
		OP_DIV,/*	A B C	R(A) := RK(B) / RK(C)				*/
		OP_MOD,/*	A B C	R(A) := RK(B) % RK(C)				*/
		OP_POW,/*	A B C	R(A) := RK(B) ^ RK(C)				*/
		OP_UNM,/*	A B	R(A) := -R(B)					*/
		OP_NOT,/*	A B	R(A) := not R(B)				*/
		OP_LEN,/*	A B	R(A) := length of R(B)				*/

		OP_CONCAT,/*	A B C	R(A) := R(B).. ... ..R(C)			*/

		OP_JMP,/*	sBx	pc+=sBx					*/

		OP_EQ,/*	A B C	if ((RK(B) == RK(C)) ~= A) then pc++		*/
		OP_LT,/*	A B C	if ((RK(B) <  RK(C)) ~= A) then pc++  		*/
		OP_LE,/*	A B C	if ((RK(B) <= RK(C)) ~= A) then pc++  		*/

		OP_TEST,/*	A C	if not (R(A) <=> C) then pc++			*/ 
		OP_TESTSET,/*	A B C	if (R(B) <=> C) then R(A) := R(B) else pc++	*/ 

		OP_CALL,/*	A B C	R(A), ... ,R(A+C-2) := R(A)(R(A+1), ... ,R(A+B-1)) */
		OP_TAILCALL,/*	A B C	return R(A)(R(A+1), ... ,R(A+B-1))		*/
		OP_RETURN,/*	A B	return R(A), ... ,R(A+B-2)	(see note)	*/

		OP_FORLOOP,/*	A sBx	R(A)+=R(A+2);
					if R(A) <?= R(A+1) then { pc+=sBx; R(A+3)=R(A) }*/
		OP_FORPREP,/*	A sBx	R(A)-=R(A+2); pc+=sBx				*/

		OP_TFORLOOP,/*	A C	R(A+3), ... ,R(A+2+C) := R(A)(R(A+1), R(A+2)); 
								if R(A+3) ~= nil then R(A+2)=R(A+3) else pc++	*/ 
		OP_SETLIST,/*	A B C	R(A)[(C-1)*FPF+i] := R(A+i), 1 <= i <= B	*/

		OP_CLOSE,/*	A 	close all variables in the stack up to (>=) R(A)*/
		OP_CLOSURE,/*	A Bx	R(A) := closure(KPROTO[Bx], R(A), ... ,R(A+n))	*/

		OP_VARARG/*	A B	R(A), R(A+1), ..., R(A+B-1) = vararg		*/
		};


		public const int NUM_OPCODES	= (int)OpCode.OP_VARARG;



		/*===========================================================================
		  Notes:
		  (*) In OP_CALL, if (B == 0) then B = top. C is the number of returns - 1,
			  and can be 0: OP_CALL then sets `top' to last_result+1, so
			  next open instruction (OP_CALL, OP_RETURN, OP_SETLIST) may use `top'.

		  (*) In OP_VARARG, if (B == 0) then use actual number of varargs and
			  set top (like in OP_CALL with C == 0).

		  (*) In OP_RETURN, if (B == 0) then return up to `top'

		  (*) In OP_SETLIST, if (B == 0) then B = `top';
			  if (C == 0) then next `instruction' is real C

		  (*) For comparisons, A specifies what condition the test should accept
			  (true or false).

		  (*) All `skips' (pc++) assume that next instruction is a jump
		===========================================================================*/


		/*
		** masks for instruction properties. The format is:
		** bits 0-1: op mode
		** bits 2-3: C arg mode
		** bits 4-5: B arg mode
		** bit 6: instruction set register A
		** bit 7: operator is a test
		*/  

		public enum OpArgMask {
		  OpArgN,  /* argument is not used */
		  OpArgU,  /* argument is used */
		  OpArgR,  /* argument is a register or a jump offset */
		  OpArgK   /* argument is a constant or register/constant */
		};

		public static OpMode getOpMode(OpCode m)	{return (OpMode)(luaP_opmodes[(int)m] & 3);}
		public static OpArgMask getBMode(OpCode m) { return (OpArgMask)((luaP_opmodes[(int)m] >> 4) & 3); }
		public static OpArgMask getCMode(OpCode m) { return (OpArgMask)((luaP_opmodes[(int)m] >> 2) & 3); }
		public static int testAMode(OpCode m) { return luaP_opmodes[(int)m] & (1 << 6); }
		public static int testTMode(OpCode m) { return luaP_opmodes[(int)m] & (1 << 7); }


		/* number of list items to accumulate before a SETLIST instruction */
		public const int LFIELDS_PER_FLUSH	= 50;



		/* ORDER OP */

		private readonly static CharPtr[] luaP_opnames = {
		  "MOVE",
		  "LOADK",
		  "LOADBOOL",
		  "LOADNIL",
		  "GETUPVAL",
		  "GETGLOBAL",
		  "GETTABLE",
		  "SETGLOBAL",
		  "SETUPVAL",
		  "SETTABLE",
		  "NEWTABLE",
		  "SELF",
		  "ADD",
		  "SUB",
		  "MUL",
		  "DIV",
		  "MOD",
		  "POW",
		  "UNM",
		  "NOT",
		  "LEN",
		  "CONCAT",
		  "JMP",
		  "EQ",
		  "LT",
		  "LE",
		  "TEST",
		  "TESTSET",
		  "CALL",
		  "TAILCALL",
		  "RETURN",
		  "FORLOOP",
		  "FORPREP",
		  "TFORLOOP",
		  "SETLIST",
		  "CLOSE",
		  "CLOSURE",
		  "VARARG",
		};


		private static lu_byte opmode(lu_byte t, lu_byte a, OpArgMask b, OpArgMask c, OpMode m)
		{
			return (lu_byte)(((t) << 7) | ((a) << 6) | (((lu_byte)b) << 4) | (((lu_byte)c) << 2) | ((lu_byte)m));
		}

		private readonly static lu_byte[] luaP_opmodes = {
		/*       T  A    B       C     mode		   opcode	*/
		  opmode(0, 1, OpArgMask.OpArgR, OpArgMask.OpArgN, OpMode.iABC) 		/* OP_MOVE */
		 ,opmode(0, 1, OpArgMask.OpArgK, OpArgMask.OpArgN, OpMode.iABx)		/* OP_LOADK */
		 ,opmode(0, 1, OpArgMask.OpArgU, OpArgMask.OpArgU, OpMode.iABC)		/* OP_LOADBOOL */
		 ,opmode(0, 1, OpArgMask.OpArgR, OpArgMask.OpArgN, OpMode.iABC)		/* OP_LOADNIL */
		 ,opmode(0, 1, OpArgMask.OpArgU, OpArgMask.OpArgN, OpMode.iABC)		/* OP_GETUPVAL */
		 ,opmode(0, 1, OpArgMask.OpArgK, OpArgMask.OpArgN, OpMode.iABx)		/* OP_GETGLOBAL */
		 ,opmode(0, 1, OpArgMask.OpArgR, OpArgMask.OpArgK, OpMode.iABC)		/* OP_GETTABLE */
		 ,opmode(0, 0, OpArgMask.OpArgK, OpArgMask.OpArgN, OpMode.iABx)		/* OP_SETGLOBAL */
		 ,opmode(0, 0, OpArgMask.OpArgU, OpArgMask.OpArgN, OpMode.iABC)		/* OP_SETUPVAL */
		 ,opmode(0, 0, OpArgMask.OpArgK, OpArgMask.OpArgK, OpMode.iABC)		/* OP_SETTABLE */
		 ,opmode(0, 1, OpArgMask.OpArgU, OpArgMask.OpArgU, OpMode.iABC)		/* OP_NEWTABLE */
		 ,opmode(0, 1, OpArgMask.OpArgR, OpArgMask.OpArgK, OpMode.iABC)		/* OP_SELF */
		 ,opmode(0, 1, OpArgMask.OpArgK, OpArgMask.OpArgK, OpMode.iABC)		/* OP_ADD */
		 ,opmode(0, 1, OpArgMask.OpArgK, OpArgMask.OpArgK, OpMode.iABC)		/* OP_SUB */
		 ,opmode(0, 1, OpArgMask.OpArgK, OpArgMask.OpArgK, OpMode.iABC)		/* OP_MUL */
		 ,opmode(0, 1, OpArgMask.OpArgK, OpArgMask.OpArgK, OpMode.iABC)		/* OP_DIV */
		 ,opmode(0, 1, OpArgMask.OpArgK, OpArgMask.OpArgK, OpMode.iABC)		/* OP_MOD */
		 ,opmode(0, 1, OpArgMask.OpArgK, OpArgMask.OpArgK, OpMode.iABC)		/* OP_POW */
		 ,opmode(0, 1, OpArgMask.OpArgR, OpArgMask.OpArgN, OpMode.iABC)		/* OP_UNM */
		 ,opmode(0, 1, OpArgMask.OpArgR, OpArgMask.OpArgN, OpMode.iABC)		/* OP_NOT */
		 ,opmode(0, 1, OpArgMask.OpArgR, OpArgMask.OpArgN, OpMode.iABC)		/* OP_LEN */
		 ,opmode(0, 1, OpArgMask.OpArgR, OpArgMask.OpArgR, OpMode.iABC)		/* OP_CONCAT */
		 ,opmode(0, 0, OpArgMask.OpArgR, OpArgMask.OpArgN, OpMode.iAsBx)		/* OP_JMP */
		 ,opmode(1, 0, OpArgMask.OpArgK, OpArgMask.OpArgK, OpMode.iABC)		/* OP_EQ */
		 ,opmode(1, 0, OpArgMask.OpArgK, OpArgMask.OpArgK, OpMode.iABC)		/* OP_LT */
		 ,opmode(1, 0, OpArgMask.OpArgK, OpArgMask.OpArgK, OpMode.iABC)		/* OP_LE */
		 ,opmode(1, 1, OpArgMask.OpArgR, OpArgMask.OpArgU, OpMode.iABC)		/* OP_TEST */
		 ,opmode(1, 1, OpArgMask.OpArgR, OpArgMask.OpArgU, OpMode.iABC)		/* OP_TESTSET */
		 ,opmode(0, 1, OpArgMask.OpArgU, OpArgMask.OpArgU, OpMode.iABC)		/* OP_CALL */
		 ,opmode(0, 1, OpArgMask.OpArgU, OpArgMask.OpArgU, OpMode.iABC)		/* OP_TAILCALL */
		 ,opmode(0, 0, OpArgMask.OpArgU, OpArgMask.OpArgN, OpMode.iABC)		/* OP_RETURN */
		 ,opmode(0, 1, OpArgMask.OpArgR, OpArgMask.OpArgN, OpMode.iAsBx)		/* OP_FORLOOP */
		 ,opmode(0, 1, OpArgMask.OpArgR, OpArgMask.OpArgN, OpMode.iAsBx)		/* OP_FORPREP */
		 ,opmode(1, 0, OpArgMask.OpArgN, OpArgMask.OpArgU, OpMode.iABC)		/* OP_TFORLOOP */
		 ,opmode(0, 0, OpArgMask.OpArgU, OpArgMask.OpArgU, OpMode.iABC)		/* OP_SETLIST */
		 ,opmode(0, 0, OpArgMask.OpArgN, OpArgMask.OpArgN, OpMode.iABC)		/* OP_CLOSE */
		 ,opmode(0, 1, OpArgMask.OpArgU, OpArgMask.OpArgN, OpMode.iABx)		/* OP_CLOSURE */
		 ,opmode(0, 1, OpArgMask.OpArgU, OpArgMask.OpArgN, OpMode.iABC)		/* OP_VARARG */
		};

	}
}
