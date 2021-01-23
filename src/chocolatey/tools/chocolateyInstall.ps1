$packageName = 'cs-script.core'
$url = 'https://github.com/oleg-shilo/cs-script.core/releases/download/v1.4.5.0-NET5-RC5/cs-script.win.v1.4.5.0.7z'

try {
  $installDir = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"

  $cheksum = 'D9A4F3D389AD89C260BFB3E3EE7008A2D3C46BCBA177503B80B32EE30918C1C5'
  $checksumType = "sha256"

  function stop-server
  {
     param(
       $server,
       $port,
       $command
     )

    try {

        $client  = New-Object Net.Sockets.TcpClient($server, $port)
        $socketStream  = $client.GetStream()

        [Byte[]]$Buffer = [Text.Encoding]::ASCII.GetBytes($data)


        $socketStream.Write($Buffer, 0, $Buffer.Length)
        $socketStream.Flush()
    }
    catch{
    }
  }


  stop-server "localhost" "17001" "-exit" # prev release Roslyn compiling server requires "-exit"
  stop-server "localhost" "17001" "-stop" # starting from .NET 5 release CodeDom build server requires "-stop"

  # Download and unpack a zip file
  Install-ChocolateyZipPackage "$packageName" "$url" "$installDir" -checksum $checksum -checksumType $checksumType

  $css_full_dir = Get-EnvironmentVariable 'CSSCRIPT_FULL_DIR' User

  if($css_full_dir) {
    # already captured
  }
  else {
    $css_full_dir = Get-EnvironmentVariable 'CSSCRIPT_DIR' User
    Install-ChocolateyEnvironmentVariable 'CSSCRIPT_FULL_DIR' $css_full_dir User
  }

  Install-ChocolateyEnvironmentVariable 'CSSCRIPT_DIR' $installDir User

  Install-BinFile "css1" "$installDir\cscs.exe"
  
} catch {
  throw $_.Exception
}
