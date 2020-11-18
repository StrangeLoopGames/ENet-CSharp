param([string]$Token = "", [string]$AccountName = "theSLGjenkins", [string]$BuildId = "", [switch]$Help = $false)

if ($Help)
{
    Write-Host "USAGE: powershell ./UpdateRuntimeFromAppVeyor.ps1 [-Token <Token>] [-AccountName <AccountName>] [-BuildId <BuildId>] [-Help]"
    Exit 0
}

$ErrorActionPreference = "Stop"

. $PSScriptRoot/BuildUtils.ps1

BuildUitls-DownloadAppVeyorRuntimes -Token Token -AccountName $AccountName -BuildId $BuildId