echo off
echo *******************
echo *  must be admin  *
echo *******************
rem choco apikey --key ???????? --source https://push.chocolatey.org/
choco push cs-script.core.1.4.4.0-NET5-RC4.nupkg --source https://push.chocolatey.org/
