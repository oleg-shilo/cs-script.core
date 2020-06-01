$packageName = 'cs-script.core'
$url = 'https://github.com/oleg-shilo/cs-script.core/releases/download/v1.3.2.0/cs-script.core.v1.3.2.0.7z'

try {
  $installDir = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"

  $cheksum = 'C02F07CBD39F3D3DE7B1336CA9A9F2459A679108B499B3BD6684F6DF78DA2606'
  $checksumType = "sha256"

  $server = "localhost"
  $port = "17001"
  $data = "-exit"

  try {

      $client  = New-Object Net.Sockets.TcpClient($server, $port)
      $socketStream  = $client.GetStream()

      [Byte[]]$Buffer = [Text.Encoding]::ASCII.GetBytes($data)


      $socketStream.Write($Buffer, 0, $Buffer.Length)
      $socketStream.Flush()
  }
  catch{
  }

  # Download and unpack a zip file
  Install-ChocolateyZipPackage "$packageName" "$url" "$installDir" -checksum $checksum -checksumType $checksumType

  $css_full_dir = [System.Environment]::GetEnvironmentVariable('CSSCRIPT_FULL_DIR')

  if($css_full_dir) {
    # already captured
  }
  else {
    $css_full_dir = [System.Environment]::GetEnvironmentVariable('CSSCRIPT_DIR')
    [System.Environment]::SetEnvironmentVariable('CSSCRIPT_FULL_DIR', $css_full_dir, [System.EnvironmentVariableTarget]::User)
  }

  [System.Environment]::SetEnvironmentVariable('CSSCRIPT_DIR', $installDir, [System.EnvironmentVariableTarget]::User)

} catch {
  throw $_.Exception
}
