require 'com'

-- http://ss64.com/vb/filesystemobject.html

fo = com.CreateObject("Scripting.FileSystemObject")
each = luanet.each
print(fo:FileExists 'com.lua')
print 'and'
f = fo:GetFile 'com.lua'
print (f.Name)

drives = fo.Drives
print(drives.Count)
print(drives)

-- this is weird: can access as property!
ee = drives.GetEnumerator
while ee:MoveNext() do
    -- have to wrap this COM object explicitly!
    local drive = com.wrap(ee.Current)
    print(drive.DriveLetter)
end

function com.each(obj)
    local e = obj.GetEnumerator
    return function()
      if e:MoveNext() then
        return com.wrap(e.Current)
     end
    end
end

for d in com.each(drives) do print(d.DriveType) end

--print(fo.Drives['C'])

print(fo:FolderExists 'lua')
print(fo:GetAbsolutePathName 'lua')

this = fo:GetFolder '..'
for f in com.each(this.SubFolders) do print(f.Name) end

--~ drive = fo:Drives 'C'
--~ print(drive.AvailableSpace)
