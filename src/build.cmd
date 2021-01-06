echo off

set vs_edition=Community
if exist "C:\Program Files (x86)\Microsoft Visual Studio\2019\%vs_edition%" (
    echo Visual Studio 2019 (Community)
) else (
    set vs_edition=Professional
    echo Visual Studio 2019 (PRO)
)

set msbuild="C:\Program Files (x86)\Microsoft Visual Studio\2019\%vs_edition%\MSBuild\Current\Bin\MSBuild.exe"
%msbuild% ".\css\css (win launcher).csproj" -p:Configuration=Release -t:rebuild
copy .\css\bin\Release\css.exe ".\out\.NET Core\css.exe"

set target=net5.0
md "out\.NET Core"

cd BuildServer
echo ----------------
echo Building build.dll from %cd%
echo ----------------
dotnet publish -c Release 

cd ..\cscs
echo ----------------
echo Building cscs.dll from %cd%
echo ----------------
dotnet publish -c Release -f %target% -o "..\out\.NET Core\console"

cd ..\csws
echo ----------------
echo Building csws.dll from %cd%
echo ----------------
dotnet publish -c Release -f %target%-windows -o "..\out\.NET Core\win"


cd ..\CSScriptLib\src\CSScriptLib
echo ----------------
echo Building CSScriptLib.dll from %cd%
echo ----------------
dotnet build -c Release

cd ..\..\..\cscs

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

.\css ..\..\CSScriptLib\src\CSScriptLib\output\aggregate.cs
copy ..\..\CSScriptLib\src\CSScriptLib\output\*.*nupkg ..\


7z.exe a -r "..\cs-script.core.7z" "*.*"
.\css -code var version = Assembly.LoadFrom(#''cscs.dll#'').GetName().Version.ToString();#nFile.Copy(#''..\\cs-script.core.7z#'', $#''..\\cs-script.core.v{version}.7z#'', true);
.\css -code var version = Assembly.LoadFrom(#''cscs.dll#'').GetName().Version.ToString();#nFile.Copy(#''..\\cs-script.core.7z#'', $#''..\\cs-script.core.v{version}.7z#'', true);
del ..\cs-script.core.7z

echo Published: %cd%
cd ..\..\.

echo .
echo .
echo .
echo      (need to target netstandard2; neither netstandard21 nor netstandard3 are supported by shfbproj yet)
echo !!!! DON'T forgert to build HELP (CS-Script.Core.Doc.ln) manually!!!!
echo .


pause