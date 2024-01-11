$ToolName = "ExtLauncher"
dotnet pack --configuration Release
dotnet tool uninstall $ToolName --global
dotnet tool install $ToolName --add-source .\$ToolName\nupkg\ --global
