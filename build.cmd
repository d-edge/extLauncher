@ECHO OFF

dotnet tool restore
dotnet build -- %*
