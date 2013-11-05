
if( [System.IntPtr]::Size -ne 8) {
    $arch = "x86\*.*"
} else {
    $arch = "x64\*.*"
}

$nativePath = $(Join-Path $installPath "lib\native")
$nativePath = $(Join-Path $nativePath $arch)


$LibLuaPostBuildCmd =  "
xcopy /s /y `"$nativePath`" `"`$(TargetDir)`""