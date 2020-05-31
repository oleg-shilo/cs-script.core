cd cscs

md "..\out\.NET Core"
"..\out\.NET Core\css.exe" -server:stop

dotnet publish cscs.csproj -c Release -f netcoreapp3.1 -o "..\out\.NET Core"
dotnet publish csws.csproj -c Release -f netcoreapp3.1 -o "..\out\.NET Core"


copy ..\css\bin\Release\css.exe "..\out\.NET Core\css.exe"
rem copy ..\cscs\bin\Debug\netcoreapp3.0\cscs.exe "..\out\.NET Core\cscs.exe"
cd ..\out\.NET Core
del *.dbg
del *.pdb

echo >  -code.header //css_ac freestyle
echo >> -code.header using System;
echo >> -code.header using System.IO;
echo >> -code.header using System.Reflection;
echo >> -code.header using System.Diagnostics;

echo off

7z.exe a -r "..\cs-script.core.7z" "*.*"
css -code var version = Assembly.LoadFrom(@``cscs.dll``).GetName().Version.ToString();`nFile.Copy(``..\\cs-script.core.7z``, $``..\\cs-script.core.v{version}.7z``, true);
css -code var version = Assembly.LoadFrom(@``cscs.dll``).GetName().Version.ToString();`nFile.Copy(``..\\cs-script.core.7z``, $``..\\cs-script.core.v{version}.7z``, true);
del ..\cs-script.core.7z

cd ..\..

rem nuget fails with the endless loop :) so need to do it manually
rem nuget pack CS-Script.Core.Samples.nuspec

build-nuget

explorer ".\out"
explorer ".\CSScriptLib\output"
echo .
echo .
echo .
echo !!!! DON'T forgert to build HELP (build-nuget.cmd) manually!!!!
echo      (need to target netstandard2; neither netstandard21 nor netstandard3 are supported by shfbproj yet)
echo !!!! DON'T forgert to build HELP (CS-Script.Core.Doc.ln) manually!!!!
echo .


pause