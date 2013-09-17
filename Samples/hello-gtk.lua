require 'CLRPackage'
import ('gtk-sharp','Gtk')

Application.Init()

local win = Window("Hello from GTK#")
win.DeleteEvent:Add(function()
    Application.Quit()
end)

win:Resize(300,300)

local label = Label()
label.Text = "Hello World!"
win:Add(label)

win:ShowAll()

Application.Run()


