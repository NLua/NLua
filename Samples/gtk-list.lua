require 'CLRPackage'
import 'System'
import ('gtk-sharp','Gtk')
import('glib-sharp','GLib')
local ctype = luanet.ctype

Application.Init()

local win = Window("Hello from GTK#")
win.DeleteEvent:Add(function()
    Application.Quit()
end)

win:Resize(300,300)

local store = ListStore({GType.String,GType.String})
--local store = ListStore({ctype(String),ctype(String)})
store:AppendValues {"Dachsie","Fritz"}
store:AppendValues {"Collie","Butch"}

local view = TreeView()
view.Model = store
view.HeadersVisible = true

-- the long way to make a column
function new_col(title,kind,idx)
    local col = TreeViewColumn()
    col.Title = title
    local r = CellRendererText()
    col:PackStart(r,true)
    col:AddAttribute(r,kind,idx)
    view:AppendColumn(col)
end

new_col("Dogs","text",0)
--new_col("Name","text",1)

-- and the short way
local col = TreeViewColumn("Name",CellRendererText(),{"text",1})
view:AppendColumn(col)

view.Selection.Changed:Add(function(o,args)
    local selected, model, iter = o:GetSelected();
    if selected then
        local val = model:GetValue(iter,0)
        print("selected",val)
    end
end)

win:Add(view)

win:ShowAll()

Application.Run()


