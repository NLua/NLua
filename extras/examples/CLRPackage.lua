---
--- This lua module provides auto importing of .net classes into a named package.
--- Makes for super easy use of LuaInterface glue
---
--- example:
---   Threading = CLRPackage("System", "System.Threading")
---   Threading.Thread.Sleep(100)
---
--- Extensions:
--- import() is a version of CLRPackage() which puts the package into a list which is used by a global __index lookup,
--- and thus works rather like C#'s using statement. It also recognizes the case where one is importing a local
--- assembly, which must end with an explicit .dll extension.

--- Alternatively, luanet.namespace can be used for convenience without polluting the global namespace:
---   local sys,sysi = luanet.namespace {'System','System.IO'}
--    sys.Console.WriteLine("we are at {0}",sysi.Directory.GetCurrentDirectory())


-- LuaInterface hosted with stock Lua interpreter will need to explicitly require this...
if not luanet then require 'luanet' end

local import_type, load_assembly = luanet.import_type, luanet.load_assembly

local mt = {
	--- Lookup a previously unfound class and add it to our table
	__index = function(package, classname)
		local class = rawget(package, classname)
		if class == nil then
			class = import_type(package.packageName .. "." .. classname)
			package[classname] = class		-- keep what we found around, so it will be shared
		end
		return class
	end
}

function luanet.namespace(ns)
    if type(ns) == 'table' then
        local res = {}
        for i = 1,#ns do
            res[i] = luanet.namespace(ns[i])
        end
        return unpack(res)
    end
    -- FIXME - table.packageName could instead be a private index (see Lua 13.4.4)
    local t = { packageName = ns }
    setmetatable(t,mt)
    return t
end

local globalMT, packages

local function set_global_mt()
    packages = {}
    globalMT = {
        __index = function(T,classname)
                for i,package in ipairs(packages) do
                    local class = package[classname]
                    if class then
                        _G[classname] = class
                        return class
                    end
                end
        end
    }
    setmetatable(_G, globalMT)
end

--- Create a new Package class
function CLRPackage(assemblyName, packageName)
  -- a sensible default...
  packageName = packageName or assemblyName
  local ok = pcall(load_assembly,assemblyName)			-- Make sure our assembly is loaded
  return luanet.namespace(packageName)
end

function import (assemblyName, packageName)
    if not globalMT then
        set_global_mt()
    end
    if not packageName then
		local i = assemblyName:find('%.dll$')
		if i then packageName = assemblyName:sub(1,i-1)
		else packageName = assemblyName end
	end
    local t = CLRPackage(assemblyName,packageName)
	table.insert(packages,t)
	return t
end


function luanet.make_array (tp,tbl)
    local arr = tp[#tbl]
	for i,v in ipairs(tbl) do
	    arr:SetValue(v,i-1)
	end
	return arr
end

function luanet.each(o)
   local e = o:GetEnumerator()
   return function()
      if e:MoveNext() then
        return e.Current
     end
   end
end
