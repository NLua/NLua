
if ([System.IntPtr]::Size -eq 8) {
    $arch = "x64\*.*"
} elseif (([System.IntPtr]::Size -eq 4) -and (Test-Path Env:\PROCESSOR_ARCHITEW6432)) {
    $arch = "x64\*.*"
} elseif ([System.IntPtr]::Size -eq 4) {
    $arch = "x86\*.*"
}

$nativePath = $(Join-Path $installPath "lib\native")
$nativePath = $(Join-Path $nativePath $arch)


$LibLuaPostBuildCmd =  "
xcopy /s /y `"$nativePath`" `"`$(TargetDir)`""
