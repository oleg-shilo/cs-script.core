cd cscs.exe.core
md "..\out\.NET Core"
"..\out\.NET Core\css.exe" -server:stop
dotnet publish -c Release -f netcoreapp2.1 -o "..\out\.NET Core"
copy ..\css\bin\Release\css.exe "..\out\.NET Core\css.exe"
cd ..\out\.NET Core
del *.dbg
del *.pdb

echo >  -code.header //css_ac freestyle
echo >> -code.header using System;
echo >> -code.header using System.IO;
echo >> -code.header using System.Reflection;
echo >> -code.header using System.Diagnostics;

rem 7z.exe a -r "..\cs-script.core.7z" "*.*"
rem css -l:0 -code var version = System.Reflection.Assembly.LoadFrom(@``out\.NET Core\cscs.eng.dll``).GetName().Version.ToString();`nFile.Move(@``out\cs-script.core.7z``, $``out\\cs-script.core.v{version}.7z``);

cd ..\..\

pause