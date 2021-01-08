echo off

dotnet test ".\Tests.CSScriptLib\Tests.CSScriptLib.csproj"
dotnet test ".\Tests.cscs\Tests.cscs.csproj"
explorer .\out

pause