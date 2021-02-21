echo off
echo *******************
echo *  must be admin  *
echo *******************
rem choco apikey --key ???????? --source https://push.chocolatey.org/
choco push cs-script.core.1.4.5.0-NET5-RC5.nupkg --source https://push.chocolatey.org/
