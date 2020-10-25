echo off

set msbuild="C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe"
%msbuild% ".\css\css (win launcher).csproj" -p:Configuration=Release -t:rebuild
copy .\css\bin\Release\css.exe ".\out\.NET Core\css.exe"

cd cscs

set target=net5.0

md "..\out\.NET Core"
rem "..\out\.NET Core\css.exe" -server:stop

dotnet publish -c Release -f %target% -o "..\out\.NET Core\console"

cd ..\csws
dotnet publish -c Release -f %target%-windows -o "..\out\.NET Core\win"

cd ..\cscs

copy "..\out\.NET Core\win" "..\out\.NET Core" /Y
copy "..\out\.NET Core\console" "..\out\.NET Core" /Y

rd "..\out\.NET Core\win" /S /Q
rd "..\out\.NET Core\console" /S /Q


rem copy ..\cscs\bin\Debug\netcoreapp3.0\cscs.exe "..\out\.NET Core\cscs.exe"
cd ..\out\.NET Core
del *.dbg
del *.pdb

echo >  -code.header    using System;
echo >> -code.header    using System.IO;
echo >> -code.header    using System.Collections;
echo >> -code.header    using System.Collections.Generic;
echo >> -code.header    using System.Linq;
echo >> -code.header    using System.Reflection;
echo >> -code.header    using System.Diagnostics;
echo >> -code.header    using static dbg;
echo >> -code.header    using static System.Environment;

echo off

7z.exe a -r "..\cs-script.core.7z" "*.*"
.\css -code var version = Assembly.LoadFrom(#''cscs.dll#'').GetName().Version.ToString();#nFile.Copy(#''..\\cs-script.core.7z#'', $#''..\\cs-script.core.v{version}.7z#'', true);
.\css -code var version = Assembly.LoadFrom(#''cscs.dll#'').GetName().Version.ToString();#nFile.Copy(#''..\\cs-script.core.7z#'', $#''..\\cs-script.core.v{version}.7z#'', true);
del ..\cs-script.core.7z

echo Published: %cd%
cd ..\..\.

rem nuget fails with the endless loop :) so need to do it manually
rem nuget pack CS-Script.Core.Samples.nuspec

build-nuget

echo .
echo .
echo .
echo !!!! DON'T forgert to build HELP (build-nuget.cmd) manually!!!!
echo      (need to target netstandard2; neither netstandard21 nor netstandard3 are supported by shfbproj yet)
echo !!!! DON'T forgert to build HELP (CS-Script.Core.Doc.ln) manually!!!!
echo .


pause