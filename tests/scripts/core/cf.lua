-- temperature conversion table (celsius to farenheit)
local cf = {}
cf[ -4] = -20
cf[ -2] = -19
cf[  0] = -18
cf[  1] = -17
cf[  3] = -16
cf[  5] = -15
cf[  7] = -14
cf[  9] = -13
cf[ 10] = -12
cf[ 12] = -11
cf[ 14] = -10
cf[ 16] =  -9
cf[ 18] =  -8
cf[ 19] =  -7
cf[ 21] =  -6
cf[ 23] =  -5
cf[ 25] =  -4
cf[ 27] =  -3
cf[ 28] =  -2
cf[ 30] =  -1
cf[ 32] =   0
cf[ 34] =   1
cf[ 36] =   2
cf[ 37] =   3
cf[ 39] =   4
cf[ 41] =   5
cf[ 43] =   6
cf[ 45] =   7
cf[ 46] =   8
cf[ 48] =   9
cf[ 50] =  10
cf[ 52] =  11
cf[ 54] =  12
cf[ 55] =  13
cf[ 57] =  14
cf[ 59] =  15
cf[ 61] =  16
cf[ 63] =  17
cf[ 64] =  18
cf[ 66] =  19
cf[ 68] =  20
cf[ 70] =  21
cf[ 72] =  22
cf[ 73] =  23
cf[ 75] =  24
cf[ 77] =  25
cf[ 79] =  26
cf[ 81] =  27
cf[ 82] =  28
cf[ 84] =  29
cf[ 86] =  30
cf[ 88] =  31
cf[ 90] =  32
cf[ 91] =  33
cf[ 93] =  34
cf[ 95] =  35
cf[ 97] =  36
cf[ 99] =  37
cf[100] =  38
cf[102] =  39
cf[104] =  40
cf[106] =  41
cf[108] =  42
cf[109] =  43
cf[111] =  44
cf[113] =  45
cf[115] =  46
cf[117] =  47
cf[118] =  48
cf[120] =  49

function round(num) 
    if num >= 0 then 
    	return math.floor(num+.5) 
    else 
    	return math.ceil(num-.5) 
    end
end

for c0=-20,50-1,10 do
	io.write("C ")
	for c=c0,c0+10-1 do
		io.write(string.format("%3.0f ",c))
	end
	io.write("\n")
	
	io.write("F ")
	for c=c0,c0+10-1 do
		f=(9/5)*c+32
		x = round(f)
		celcius = cf [x]
		assert (celcius == c)
		io.write(string.format("%3.0f ",f))
	end
	
	io.write("\n\n")
end
