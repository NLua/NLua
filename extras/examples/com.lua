import 'System'
import 'System.Reflection'
local get_flags = luanet.enum(BindingFlags,'GetProperty,IgnoreCase,Public')
local put_flags = luanet.enum(BindingFlags,'SetProperty,IgnoreCase,Public')
local call_flags = luanet.enum(BindingFlags,'InvokeMethod,IgnoreCase,Public')

local function A(a)
  return luanet.make_array(Object,a)
end

local empty = A{}
local com_wrapper
local T = luanet.ctype(__ComObject)


local function maybe_wrap(res)
    if type(res) == 'userdata' then
        if res:GetType() == T then return com_wrapper(res) end
    end
    return res
end


local function caller(obj,key)
  local T = obj:GetType()
  return setmetatable({},{
    __call = function(t,o,...)
        return maybe_wrap(T:InvokeMember(key,call_flags,nil,obj,A{...}))
   end
  })
end

function com_wrapper(obj)
  local T = obj:GetType()
  return setmetatable({},{
    __index = function(self,key)
      local ok,res = pcall(T.InvokeMember, T, key, get_flags, nil, obj, empty)
      if ok then
        return maybe_wrap(res)
      else 
        return caller(obj, key)
      end
    end;
    __newindex = function(self,key,value)
        T:InvokeMember(key,put_flags,nil,A{value})
    end
  })
end

com = {}

function com.CreateObject(progid)
    local ft = Type.GetTypeFromProgID(progid)
    local f = Activator.CreateInstance(ft)
    return com_wrapper(f)
end

com.wrap = maybe_wrap

return com