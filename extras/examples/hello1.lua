luanet.load_assembly "System"
Console = luanet.import_type "System.Console"
Math = luanet.import_type "System.Math"
Directory = luanet.import_type "System.IO.Directory"

Console.WriteLine("we are at {0}",Directory.GetCurrentDirectory())
Console.WriteLine("sqrt(2) is {0}",Math.Sqrt(2))
