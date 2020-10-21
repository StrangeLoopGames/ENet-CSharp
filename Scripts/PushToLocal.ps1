param([string]$AppVeyorToken = "", [string]$AccountName = "mirasrael", [string]$BuildId = "", [switch]$Help = $false)

if ($Help)
{
    Write-Host "USAGE: powershell ./PublishToLocal.ps1 [-AppVeyorToken <Token>] [-AccountName <AccountName>] [-BuildId <BuildId>] [-Help]"
    Exit 0
}

$ErrorActionPreference = "Stop"

. $PSScriptRoot/BuildUtils.ps1

$packageName = BuildUtils-BuildPackage -AppVeyorToken $AppVeyorToken -AccountName $AccountName -BuilId $BuildId

$localRepo = "$HOME/.nuget-local"
$localPath = "$localRepo/$packageName"
[Void](New-Item -ItemType Directory $localRepo -Force)
Copy-Item $packageName $localPath
Write-Host "Package installed to $localPath"

[xml]$xml = Get-Content -Path "$PSScriptRoot/../ENet-CSharp.nuspec"
$meta = $xml.package.metadata
Remove-Item "$HOME/.nuget/packages/$($meta.id.ToLower())/$($meta.version)/" -Recurse -Force -ErrorAction Ignore
