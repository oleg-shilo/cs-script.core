echo off
@set msbuild="C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\MSBuild.exe"

%msbuild% CSScriptLib\CSScriptLib.nuget.sln /p:configuration=Release /p:platform="Any CPU"
