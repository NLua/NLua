param ([string] $PackageId)

. .\devops\BuildFunctions.ps1

if (-Not (Test-Should-Deploy)) {
	return
}

$nupkgFile  = $PackageId + '.nupkg'

& nuget push $nupkgFile -Source https://api.nuget.org/v3/index.json

