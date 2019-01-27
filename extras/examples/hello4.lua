#!/home/azisa/bin/luai
--- another variant of hello3: look, Ma, no globals!
require 'CLRPackage'
local sys,sysi = luanet.namespace {'System','System.IO'}
sys.Console.WriteLine("we are at {0}",sysi.Directory.GetCurrentDirectory())
