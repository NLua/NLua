param ([string] $PackageId, [string] $NuSpecFile)

. .\BuildFunctions.ps1

$hash = Get-Current-Commit-Hash
$releaseNotes = "Release: $($hash)"

Update-NuSpec-Release-Notes $NuSpecFile $releaseNotes

& nuget pack $NuSpecFile