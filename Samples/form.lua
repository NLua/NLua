-- kevinh - the following lines are part of our standard init
-- require("compat-5.1")

luanet.load_assembly("System.Windows.Forms")
luanet.load_assembly("System.Drawing")

Form=luanet.import_type("System.Windows.Forms.Form")
Button=luanet.import_type("System.Windows.Forms.Button")
Point=luanet.import_type("System.Drawing.Point")

form1=Form()
button1=Button()
button2=Button()

function handleClick(sender,data)
  if sender.Text=="OK" then
    sender.Text="Clicked"
  else
    sender.Text="OK"
  end
  button1.MouseUp:Remove(handler)
  print(sender:ToString())
end

button1.Text = "OK"
button1.Location=Point(10,10)
button2.Text = "Cancel"
button2.Location=Point(button1.Left, button1.Height + button1.Top + 10)
handler=button1.MouseUp:Add(handleClick)
form1.Text = "My Dialog Box"
form1.HelpButton = true
form1.MaximizeBox=false
form1.MinimizeBox=false
form1.AcceptButton = button1
form1.CancelButton = button2
form1.Controls:Add(button1)
form1.Controls:Add(button2)
form1:ShowDialog()
