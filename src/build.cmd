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

cd CSScriptLib\src\CSScriptLib

rem nuget fails with the endless loop :) so need to do it manually
rem nuget pack CS-Script.Core.Samples.nuspec
echo .
echo .
echo .
echo !!!! DON'T forgert to build HELP  manually !!!!
echo .
echo !!!! DON'T forgert to package nuget manually !!!!
echo .


pause