param ([string] $PackageId)

. .\devops\BuildFunctions.ps1

$hash = Get-Current-Commit-Hash
$releaseNotes = "Release: $($hash)"
$NuSpecFile = $PackageId + '.nuspec'
$SymbolNuSpecFile = $PackageId + '.Symbol.nuspec'
$longSha = Get-Git-Commit-Sha
$nextVersion = Get-Next-Version-String 

Update-NuSpec-Version  $NuSpecFile $nextVersion
Update-NuSpec-Commit-Hash $NuSpecFile $longSha
Update-NuSpec-Release-Notes $NuSpecFile $releaseNotes

& nuget pack $NuSpecFile -NoPackageAnalysis

$currentPkgName = './' + $PackageId + '.' + $nextVersion + '.nupkg'
$targetPkgName  = $PackageId + '.nupkg'

Rename-Item -Path $currentPkgName -NewName $targetPkgName

if (Test-Path -Path $SymbolNuSpecFile) {
    
    Update-NuSpec-Version  $SymbolNuSpecFile $nextVersion
    Update-NuSpec-Commit-Hash $SymbolNuSpecFile $longSha
    Update-NuSpec-Release-Notes $SymbolNuSpecFile $releaseNotes

    & nuget pack $SymbolNuSpecFile -Symbols -SymbolPackageFormat snupkg -NoPackageAnalysis

    $currentPkgName = './' + $PackageId + '.' + $nextVersion + '.snupkg'
    $targetPkgName  = $PackageId + '.snupkg'

    Rename-Item -Path $currentPkgName -NewName $targetPkgName
}
