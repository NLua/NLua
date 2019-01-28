-- example of for with generator functions


local fibs = {
	1,
	1,
	2,
	3,
	5,
	8,
	13,
	21,
	34,
	55,
	89,
	144,
	233,
	377,
	610,
	987,
}


function generatefib (n)
  return coroutine.wrap(function ()
	local a,b = 1, 1
	while a <= n do
	  coroutine.yield(a)
	  a, b = b, a+b
	end
  end)
end
local j = 1
for i in generatefib(1000) do 
	print(i.." ".. fibs [j])
	assert (i == fibs [j])
	j = j + 1
end
