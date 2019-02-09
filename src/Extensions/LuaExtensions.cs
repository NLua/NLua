﻿
using System;
using System.Runtime.InteropServices;
using KeraLua;
using LuaState=KeraLua.Lua;

namespace NLua.Extensions
{
    static class LuaExtensions
    {
        public static bool CheckMetaTable(this LuaState state, int index, IntPtr tag)
        {
            if (!state.GetMetaTable(index))
                return false;

            state.PushLightUserData(tag);
            state.RawGet(-2);
            bool isNotNil = !state.IsNil(-1);
            state.SetTop(-3);
            return isNotNil;
        }

        public static void PopGlobalTable(this LuaState luaState)
        {
            luaState.RawSetInteger(LuaRegistry.Index, (long) LuaRegistryIndex.Globals);
        }

        public static void GetRef (this LuaState luaState, int reference)
        {
            luaState.RawGetInteger(LuaRegistry.Index, reference);
        }

        // ReSharper disable once IdentifierTypo
        public static void Unref (this LuaState luaState, int reference)
        {
            luaState.Unref(LuaRegistry.Index, reference);
        }

        public static bool AreEqual(this LuaState luaState, int ref1, int ref2)
        {
            return luaState.Compare(ref1, ref2, LuaCompare.Equal);
        }

        public static IntPtr CheckUData(this LuaState state, int ud, string name)
        {
            IntPtr p = state.ToUserData(ud);
            if (p == IntPtr.Zero)
                return IntPtr.Zero;
            if (!state.GetMetaTable(ud))
                return IntPtr.Zero;

            state.GetField(LuaRegistry.Index, name);

            bool isEqual = state.RawEqual(-1, -2);

            state.Pop(2);

            if (isEqual)
                return p;

            return IntPtr.Zero;
        }       

        public static int ToNetObject(this LuaState state, int index, IntPtr tag)
        {
            if (state.Type(index) != LuaType.UserData)
                return -1;

            IntPtr userData;

            if (state.CheckMetaTable(index, tag))
            {
                userData = state.ToUserData(index);
                if (userData != IntPtr.Zero)
                    return Marshal.ReadInt32(userData);
            }

            userData = state.CheckUData(index, "luaNet_class");
            if (userData != IntPtr.Zero)
                return Marshal.ReadInt32(userData);

            userData = state.CheckUData(index, "luaNet_searchbase");
            if (userData != IntPtr.Zero)
                return Marshal.ReadInt32(userData);

            userData = state.CheckUData(index, "luaNet_function");
            if (userData != IntPtr.Zero)
                return Marshal.ReadInt32(userData);

            return -1;
        }

        public static void NewUData(this LuaState state, int val)
        {
            IntPtr pointer = state.NewUserData(Marshal.SizeOf(typeof(int)));
            Marshal.WriteInt32(pointer, val);
        }

        public static int RawNetObj(this LuaState state, int index)
        {
            IntPtr pointer = state.ToUserData(index);
            if (pointer == IntPtr.Zero)
                return -1;

            return Marshal.ReadInt32(pointer);
        }

        public static int CheckUObject(this LuaState state, int index, string name)
        {
            IntPtr udata = state.CheckUData(index, name);
            if (udata == IntPtr.Zero)
                return -1;

            return Marshal.ReadInt32(udata);
        }

        public static bool IsNumericType(this LuaState state, int index)
        {
            return state.Type(index) == LuaType.Number;
        }
    }
}
