---
--- This lua module provides auto importing of .net classes into a named package.
--- Makes for super easy use of LuaInterface glue
---
--- example: 
---   Threading = CLRPackage("System", "System.Threading")
---   Threading.Thread.Sleep(100)

local mt = {
	--- Lookup a previously unfound class and add it to our table
	__index = function(package, classname)
		local class = rawget(package, classname)

		if class == nil then
			class = luanet.import_type(package.packageName .. "." .. classname) 
			package[classname] = class		-- keep what we found around, so it will be shared
		end
			
		return class
	end
	}

--- Create a new Package class
function CLRPackage(assemblyName, packageName)
  local table = {}
  
  luanet.load_assembly(assemblyName)			-- Make sure our assembly is loaded
  
  -- FIXME - table.packageName could instead be a private index (see Lua 13.4.4)
  table.packageName = packageName
  setmetatable(table, mt)
  
  return table
end

