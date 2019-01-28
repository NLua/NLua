require 'CLRPackage'
import 'System.Reflection'
import 'LuaInterface'

local ctype, enum = luanet.ctype, luanet.enum

-- get all the static methods of LuaDLL and import them into global
local mm = ctype(LuaDLL):GetMethods(enum(BindingFlags,'Static,Public'))
for i = 0, mm.Length-1 do
    local name = mm[i].Name
    _G[name] = LuaDLL[name]
end

-- we can now do standard Lua API things in Lua...
local L = luaL_newstate()
luaL_openlibs(L)
lua_pushstring(L,"hello dolly")
print(lua_gettop(L))
print(lua_tostring(L,-1))

