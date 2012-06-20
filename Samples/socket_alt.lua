--require("compat-5.1")

System=luanet.System

WebClient=System.Net.WebClient
StreamReader=System.IO.StreamReader
Math=System.Math

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
