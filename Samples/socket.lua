--require("compat-5.1")

luanet.load_assembly("System")

WebClient=luanet.import_type("System.Net.WebClient")
StreamReader=luanet.import_type("System.IO.StreamReader")
Math=luanet.import_type("System.Math")

print(Math:Pow(2,3))

myWebClient = WebClient()
myStream = myWebClient:OpenRead(arg[1])
sr = StreamReader(myStream)
line=sr:ReadLine()
repeat
  print(line)
  line=sr:ReadLine()
until not line
myStream:Close()
