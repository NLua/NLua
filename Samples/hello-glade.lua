require 'CLRPackage'
import ('System')
import ('gtk-sharp','Gtk')
import ('glade-sharp','Glade')

Application.Init()

local gxml = XML("gui.glade","window1",nil)    --(nil,"gui.glade","window1",nil)
--gxml:AutoConnect (nil)
local win = gxml:GetWidget "window1"
win.DeleteEvent:Add(function()
    Application.Quit()
end)

local btn = gxml:GetWidget "button1"
btn.Clicked:Add(function(e,a)
  --  Console.WriteLine("I was clicked")
    -- note how we have to pass an empty Object[] as the last argument!
    local args = luanet.make_array(Object,{})
    local md = MessageDialog(win,
        DialogFlags.DestroyWithParent,
        MessageType.Question,
        ButtonsType.YesNo, "Are you sure you wanted to click that button?",
        args
    )
    local res = md:Run()
    res = luanet.enum(ResponseType,res)
    if res == ResponseType.Yes then   
        Console.WriteLine("ok!")
    end
   md:Destroy()
end)


Application.Run()


