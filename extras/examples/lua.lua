-- lua.lua - Lua 5.1 interpreter (lua.c) reimplemented in Lua.
--
-- WARNING: This is not completed but was quickly done just an experiment.
-- Fix omissions/bugs and test if you want to use this in production.
-- Particularly pay attention to error handling.
--
-- (c) David Manura, 2008-08
-- Licensed under the same terms as Lua itself.
-- Based on lua.c from Lua 5.1.3.
-- Improvements by Shmuel Zeigerman.

-- Variables analogous to those in luaconf.h
local LUA_INIT = "LUA_INIT"
local LUA_PROGNAME = "lua"
local LUA_PROMPT   = "> "
local LUA_PROMPT2  = ">> "
local function LUA_QL(x) return "'" .. x .. "'" end

local lua51 = _VERSION:match '5%.1$'
-- Variables analogous to those in lua.h
local LUA_RELEASE, LUA_COPYRIGHT, eof_ender
if lua51 then
	LUA_RELEASE   = "Lua 5.1.4"
	LUA_COPYRIGHT = "Copyright (C) 1994-2008 Lua.org, PUC-Rio"
	eof_ender = LUA_QL("<eof>")
else
	LUA_RELEASE   = "Lua 5.2.0"
	LUA_COPYRIGHT = "Copyright (C) 1994-2011 Lua.org, PUC-Rio"
	eof_ender = '<eof>'
end
local EXTRA_COPYRIGHT = "lua.lua (c) David Manura, 2008-08"

-- Note: don't allow user scripts to change implementation.
-- Check for globals with "cat lua.lua | luac -p -l - | grep ETGLOBAL"

local _G = _G
local assert = assert
local collectgarbage = collectgarbage
local loadfile = loadfile
local loadstring = loadstring or load
local pcall = pcall
local rawget = rawget
local select = select
local tostring = tostring
local type = type
local unpack = unpack or table.unpack
local xpcall = xpcall
local io_stderr = io.stderr
local io_stdout = io.stdout
local io_stdin = io.stdin
local string_format = string.format
local string_sub = string.sub
local os_getenv = os.getenv
local os_exit = os.exit


local progname = LUA_PROGNAME

-- Use external functions, if available
local lua_stdin_is_tty = function() return true end
local setsignal = function() end

local function print_usage()
  io_stderr:write(string_format(
  "usage: %s [options] [script [args]].\n" ..
  "Available options are:\n" ..
  "  -e stat  execute string " .. LUA_QL("stat") .. "\n" ..
  "  -l name  require library " .. LUA_QL("name") .. "\n" ..
  "  -i       enter interactive mode after executing " ..
              LUA_QL("script") .. "\n" ..
  "  -v       show version information\n" ..
  "  --       stop handling options\n" ..
  "  -        execute stdin and stop handling options\n"
  ,
  progname))
  io_stderr:flush()
end

local our_tostring = tostring

local tuple = table.pack or function(...)
  return {n=select('#', ...), ...}
end

local using_lsh,lsh

local function our_print (...)
    local args = tuple(...)
    for i = 1,args.n do
        io.write(our_tostring(args[i]),'\t')
    end
    _G._ = args[1]
    io.write '\n'
end

local function saveline(s)
  if using_lsh then
	lsh.saveline(s)
  end
end

local function getline(prmt)
  if using_lsh then
    return lsh.readline(prmt)
  else
    io_stdout:write(prmt)
    io_stdout:flush()
    return io_stdin:read'*l'
  end
end

local function l_message (pname, msg)
  if pname then io_stderr:write(string_format("%s: ", pname)) end
  io_stderr:write(string_format("%s\n", msg))
  io_stderr:flush()
end

local function report(status, msg)
  if not status and msg ~= nil then
    msg = tostring(msg)
--~     msg = (type(msg) == 'string' or type(msg) == 'number') and tostring(msg)
--~           or "(error object is not a string)"
    l_message(progname, msg);
  end
  return status
end

local function traceback (message)
  local tp = type(message)
  if tp ~= "string" and tp ~= "number" then return message end
  local debug = _G.debug
  if type(debug) ~= "table" then return message end
  local tb = debug.traceback
  if type(tb) ~= "function" then return message end
  return tb(message, 2)
end

local function docall(f, ...)
  local tp = {...}  -- no need in tuple (string arguments only)
  local F = function() return f(unpack(tp)) end
  setsignal(true)
  local result = tuple(xpcall(F, traceback))
  setsignal(false)
  -- force a complete garbage collection in case of errors
  if not result[1] then collectgarbage("collect") end
  return unpack(result, 1, result.n)
end

function dofile(name)
  local f, msg = loadfile(name)
  if f then f, msg = docall(f) end
  return report(f, msg)
end

local function dostring(s, name)
  local f, msg = loadstring(s, name)
  if f then f, msg = docall(f) end
  return report(f, msg)
end

local function dolibrary (name)
  return report(docall(_G.require, name))
end

local function print_version()
  l_message(nil, LUA_RELEASE .. "  " .. LUA_COPYRIGHT.."\n"..EXTRA_COPYRIGHT)
end

local function getargs (argv, n)
  local arg = {}
  for i=1,#argv do arg[i - n] = argv[i] end
  if _G.arg then
    local i = 0
    while _G.arg[i] do
      arg[i - n] = _G.arg[i]
      i = i - 1
    end
  end
  return arg
end

local function get_prompt (firstline)
  -- use rawget to play fine with require 'strict'
  local pmt = rawget(_G, firstline and "_PROMPT" or "_PROMPT2")
  local tp = type(pmt)
  if tp == "string" or tp == "number" then
    return tostring(pmt)
  end
  return firstline and LUA_PROMPT or LUA_PROMPT2
end

local function fetchline(firstline)
  return getline(get_prompt(firstline))
end

local function incomplete (msg)
  if msg then    
    if string_sub(msg, -#eof_ender) == eof_ender then
      return true
    end
  end
  return false
end


local function pushline (firstline)
  local fine,b = true
  repeat  
    b = fetchline(firstline)
    if not b then return end -- no input
    if using_lsh then
	fine = lsh.checkline(b)
    end
  until fine
  if firstline and string_sub(b, 1, 1) == '=' then
    return "return " .. string_sub(b, 2)  -- change '=' to `return'
  else
    return b
  end
end


local function loadline ()
  local b = pushline(true)
  if not b then return -1 end  -- no input
  local f, msg
  while true do  -- repeat until gets a complete line
    f, msg = loadstring(b, "=stdin")
    if not incomplete(msg) then break end  -- cannot try to add lines?
    local b2 = pushline(false)
    if not b2 then -- no more input?
      return -1
    end
    b = b .. "\n" .. b2 -- join them
  end

  saveline(b)

  return f, msg
end


local function dotty ()
  local oldprogname = progname
  progname = nil
  using_lsh,lsh = false -- pcall(require, 'luaish') blows with LI ??
  if using_lsh then
	our_tostring = lsh.tostring
  else
    --print('problem loading luaish:',lsh)
    our_tostring = tostring
  end
  while true do
    local result
    local status, msg = loadline()
    if status == -1 then break end
    if status then
      result = tuple(docall(status))
      status, msg = result[1], result[2]
    end
    report(status, msg)
    if status and result.n > 1 then  -- any result to print?
      status, msg = pcall(our_print, unpack(result, 2, result.n))
      if not status then
        l_message(progname, string_format(
            "error calling %s (%s)",
            LUA_QL("print"), msg))
      end
    end
  end
  io_stdout:write"\n"
  io_stdout:flush()
  progname = oldprogname
end


local function handle_script(argv, n)
  _G.arg = getargs(argv, n)  -- collect arguments
  local fname = argv[n]
  if fname == "-" and argv[n-1] ~= "--" then
    fname = nil  -- stdin
  end
  local status, msg = loadfile(fname)
  if status then
    status, msg = docall(status, unpack(_G.arg))
  end
  return report(status, msg)
end


local function collectargs (argv, p)
  local i = 1
  while i <= #argv do
    if string_sub(argv[i], 1, 1) ~= '-' then  -- not an option?
      return i
    end
    local prefix = string_sub(argv[i], 1, 2)
    if prefix == '--' then
      if #argv[i] > 2 then return -1 end
      return argv[i+1] and i+1 or 0
    elseif prefix == '-' then
      return i
    elseif prefix == '-i' then
      if #argv[i] > 2 then return -1 end
      p.i = true
      p.v = true
    elseif prefix == '-v' then
      if #argv[i] > 2 then return -1 end
      p.v = true
    elseif prefix == '-e' then
      p.e = true
      if #argv[i] == 2 then
        i = i + 1
        if argv[i] == nil then return -1 end
      end
    elseif prefix == '-l' then
      if #argv[i] == 2 then
        i = i + 1
        if argv[i] == nil then return -1 end
      end
    else
      return -1  -- invalid option
    end
    i = i + 1
  end
  return 0
end


local function runargs(argv, n)
  local i = 1
  while i <= n do if argv[i] then
    assert(string_sub(argv[i], 1, 1) == '-')
    local c = string_sub(argv[i], 2, 2) -- option
    if c == 'e' then
      local chunk = string_sub(argv[i], 3)
      if chunk == '' then i = i + 1; chunk = argv[i] end
      assert(chunk)
      if not dostring(chunk, "=(command line)") then return false end
    elseif c == 'l' then
      local filename = string_sub(argv[i], 3)
      if filename == '' then i = i + 1; filename = argv[i] end
      assert(filename)
      if not dolibrary(filename) then return false end
    end
    i = i + 1
  end end
  return true
end


local function handle_luainit()
  local init = os_getenv(LUA_INIT)
  if init == nil then
    return  -- status OK
  elseif string_sub(init, 1, 1) == '@' then
    dofile(string_sub(init, 2))
  else
    dostring(init, "=" .. LUA_INIT)
  end
end


local import_ = _G.import
if import_ then
  lua_stdin_is_tty = import_.lua_stdin_is_tty or lua_stdin_is_tty
  setsignal        = import_.setsignal or setsignal
  LUA_RELEASE      = import_.LUA_RELEASE or LUA_RELEASE
  LUA_COPYRIGHT    = import_.LUA_COPYRIGHT or LUA_COPYRIGHT
  _G.import = nil
end

if _G.arg and _G.arg[0] and #_G.arg[0] > 0 then progname = _G.arg[0] end
local argv = {...}
handle_luainit()
local has = {i=false, v=false, e=false}
local script = collectargs(argv, has)
if script < 0 then -- invalid args?
  print_usage()
  os_exit(1)
end
if has.v then print_version() end
local status = runargs(argv, (script > 0) and script-1 or #argv)
if not status then os_exit(1) end
if script ~= 0 then
  status = handle_script(argv, script)
  if not status then os_exit(1) end
else
  _G.arg = nil
end
if has.i then  
  dotty()
elseif script == 0 and not has.e and not has.v then
  if lua_stdin_is_tty() then
    print_version()
    require 'CLRPackage'
    import 'System'
    dotty()
  else dofile(nil)  -- executes stdin as a file
  end
end
