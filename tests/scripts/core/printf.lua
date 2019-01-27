-- an implementation of printf

function sprintf(...)
 return string.format(...)
end

function printf (...)
	print(sprintf(...))
end

x = sprintf("Hello %s from %s on %s", "there", "Lua Tests", "XYZ")
assert (x == "Hello there from Lua Tests on XYZ")
print(x)
