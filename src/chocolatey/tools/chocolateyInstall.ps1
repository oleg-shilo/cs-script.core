$packageName = 'cs-script.core'
$url = 'https://github.com/oleg-shilo/cs-script.core/releases/download/v1.0.0.0/cs-script.core.7z'

try {
  $installDir = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"

  $cheksum = '5057CD1E9162C50D4C0BFF38961AE35C8917C4DDFD6597ECDAB2E50F9308112B'
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

  $css_full_dir = [System.Environment]::GetEnvironmentVariable('CSSCRIPT_DIR')
  [System.Environment]::SetEnvironmentVariable('CSSCRIPT_FULL_DIR', $css_full_dir, [System.EnvironmentVariableTarget]::User)

  [System.Environment]::SetEnvironmentVariable('CSSCRIPT_DIR', $installDir, [System.EnvironmentVariableTarget]::User)

} catch {
  throw $_.Exception
}
