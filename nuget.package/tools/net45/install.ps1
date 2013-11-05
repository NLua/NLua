param($installPath, $toolsPath, $package, $project)

. (Join-Path $toolsPath "GetLibLuaPostBuildCmd.ps1")

# Get the current Post Build Event cmd
$currentPostBuildCmd = $project.Properties.Item("PostBuildEvent").Value


# Append our post build command if it's not already there
if (!$currentPostBuildCmd.Contains($LibLuaPostBuildCmd)) {
    $project.Properties.Item("PostBuildEvent").Value += $LibLuaPostBuildCmd
}