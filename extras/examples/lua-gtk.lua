---- A simple interactive Console for LuaInterface using Gtk#
require 'CLRPackage'
import ('gtk-sharp','Gtk')
luanet.load_assembly 'gdk-sharp'
luanet.load_assembly 'glib-sharp'
local Gdk = luanet.namespace 'Gdk'
local Glib = luanet.namespace 'GLib'

local ferr = io.stderr  -- for debugging

--~ Glib.ExceptionManager.UnhandledException:Add(function(ex,what)
--~     ferr:write(tostring(ex),'  ',tostring(what),'\n')
--~     ex.ExitApplication = false
--~ end)

local Up, Down, Return = Gdk.Key.Up, Gdk.Key.Down, Gdk.Key.Return

Application.Init()

local win = Window("Gtk# Lua")

win.DeleteEvent:Add(function()
    Application.Quit()
end)

win:Resize(500,500)

local buffer
local history = {idx=1}

function add_history(line)
    if line ~= history[#history] then
        table.insert(history,line)
        history.idx = #history + 1
    end
end

local function clamp(i,s,n)
    if i < s then return 1
    elseif i > n then return n
    else return i
    end
end

function line_range(lno)
    lno = lno or buffer.LineCount - 1
    local start = buffer:GetIterAtLine(lno-1)
    local endi = buffer:GetIterAtLine(lno)
    return start,endi
end

local function set_last_line(text)
    if not text then return end
    local start =  buffer:GetIterAtLine(buffer.LineCount-1)
    local endi = buffer.EndIter
    start = buffer:Delete(start,endi)
    buffer:Insert(start,'> '..text)
end

-- we need to subclass Gtk.TextView, since we want to trap
-- the up and down keys for accessing command history

local edit = {}

function edit:OnKeyPressEvent(event)
    local key = event.Key
    if key == Up or key == Down then
        local delta 
        if key == Down then
            delta = 1
        else
            delta = -1
        end
        history.idx = clamp(history.idx + delta,1,#history)
        set_last_line(history[history.idx])        
        return true
    else
        return self.base:OnKeyPressEvent(event)
    end
end

luanet.make_object(edit,'Gtk.TextView')
buffer = edit.Buffer


local function create_tag(colour)
    local tag = TextTag(colour)
    tag.Foreground = colour
    buffer.TagTable:Add(tag)
    return {tag}
end

local plain,err_style = create_tag "#00F"  , create_tag "#F00" 

function write(txt,tag)
    tag = tag or plain
    buffer:InsertWithTags(buffer.EndIter,txt,tag)
end

local prompt = '> '
write 'Lua 5.1.4  Copyright (C) 1994-2008 Lua.org, PUC-Rio\n'
write(prompt)

function print(...)
    local args,n = {...},select('#',...)
    for i = 1,n do
        write(tostring(args[i])..'\t')
    end
   write '\n'
end

local function collect(ok,...)
    local args = {...}
    args.n = select('#',...)
    return ok, args
end

function eval(s)
    if s == 'quit' then Application.Quit() end
    local expr = s:match('^%s*=%s+(.+)')
    if expr then s = 'return '..expr end
    local chunk,err = loadstring(s,'tmp')
    local ok,res
    if chunk then
        ok,res = collect(pcall(chunk))
        if not ok then err = res[1] end
    end
    if err then
        write(tostring(err)..'\n',err_style)
    elseif res.n > 0 then
        print(unpack(res,1,res.n))
        _G._ = res[1]  -- last expression put into underscore global
    end
end

edit.KeyReleaseEvent:Add(function(obj,e)
    local event = e.Event
    if event.Key == Return then
        local start,endi = line_range()
        local stmt = start:GetText(endi)
        local i1,i2 = stmt:find(prompt,1,true)
        if i1 == 1 then
            stmt = stmt:sub(i2+1)
        end
        stmt = stmt:gsub('\n$','')
        eval(stmt)
        add_history(stmt)
        buffer:InsertAtCursor(prompt)
    end
end)

local sbox = ScrolledWindow()
sbox.VscrollbarPolicy = PolicyType.Always
sbox:Add(edit)
win:Add(sbox)
win:ShowAll()
Application.Run()

