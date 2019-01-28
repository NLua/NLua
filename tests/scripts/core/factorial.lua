-- function closures are powerful
local fact = {}
fact[0] = 1
fact[1] = 1
fact[2] = 2
fact[3] = 6
fact[4] = 24
fact[5] = 120
fact[6] = 720
fact[7] = 5040
fact[8] = 40320
fact[9] = 362880
fact[10] = 3628800
fact[11] = 39916800
fact[12] = 479001600
fact[13] = 6227020800
fact[14] = 87178291200
fact[15] = 1307674368000
fact[16] = 20922789888000

-- traditional fixed-point operator from functional programming
Y = function (g)
      local a = function (f) return f(f) end
      return a(function (f)
                 return g(function (x)
                             local c=f(f)
                             return c(x)
                           end)
               end)
end


-- factorial without recursion
F = function (f)
      return function (n)
               if n == 0 then return 1
               else return n*f(n-1) end
             end
    end

factorial = Y(F)   -- factorial is the fixed point of F

-- now test it
function test(x)
  local val = factorial(x)
	print(x.." ".."! = ".." ".. val.." ".."\n")
  return val
end

for n=0,16 do
	local val = test (n)
  assert (val == fact [n])
end
