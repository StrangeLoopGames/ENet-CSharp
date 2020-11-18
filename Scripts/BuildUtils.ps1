function BuildUitls-DownloadAppVeyorRuntimes([string]$Token = "", [string]$AccountName = "theSLGjenkins", [string]$BuildId = "") {
    $apiUrl = 'https://ci.appveyor.com/api'

    if ($Token -eq "")
    {
        $tokenFilePath = "$HOME/.appveyor/token"
        if ([System.IO.File]::Exists($tokenFilePath))
        {
            $Token = Get-Content -Path $tokenFilePath
        }
        else
        {
            Write-Host "AppVeyor token not found";
            Exit 1;
        }
    }

    $token = $Token
    $headers = @{
        "Authorization" = "Bearer $token"
        "Content-type" = "application/json"
    }

    $accountName = $AccountName
    $projectSlug = 'enet-csharp'

    $runtimesPath = '$PSScriptRoot\..\runtimes'

    if ($BuildId -eq "")
    {
        # get project with last build details
        $project = Invoke-RestMethod -Method Get -Uri "$apiUrl/projects/$accountName/$projectSlug" -Headers $headers
    }
    else
    {
        # get project with specific build details
        $project = Invoke-RestMethod -Method Get -Uri "$apiUrl/projects/$accountName/$projectSlug/build/$BuildId" -Headers $headers
    }

    # download artifacts for all jobs in the build
    foreach ($job in $project.build.jobs)
    {
        $jobId = $job.jobId

        # get job artifacts (just to see what we've got)
        $artifacts = Invoke-RestMethod -Method Get -Uri "$apiUrl/buildjobs/$jobId/artifacts" -Headers $headers

        foreach ($artifact in $artifacts)
        {
            $artifactFileName = $artifact.fileName
            $libName = Split-Path -Path $artifactFileName -Leaf
            $runtime = switch ($job.name)
            {
                "Image: Visual Studio 2019; Platform: x86" { "win-x86" }
                "Image: Visual Studio 2019; Platform: x64" { "win-x64" }
                "Image: Ubuntu; Platform: x64" { "linux-x64" }
                "Image: macOS; Platform: x64" { "osx-x64" }
            }
            $artifactUri = "$apiUrl/buildjobs/$jobId/artifacts/$artifactFileName"
            $localArtifactDir = "$runtimesPath/$runtime/native"
            [void](New-Item -ItemType Directory -Force -Path $localArtifactDir)
            $localArtifactPath = "$localArtifactDir/$libName"
            Write-Host "$artifactUri -> $localArtifactPath"
            Invoke-RestMethod -Method Get -Uri "$apiUrl/buildjobs/$jobId/artifacts/$artifactFileName" -OutFile $localArtifactPath -Headers $headers
        }
    }
}

function BuildUtils-BuildPackage([string]$AppVeyorToken = "", [string]$AccountName = "theSLGjenkins", [string]$BuildId = "") {
    [Void](MSBuild.exe ENet-CSharp.sln -p:Configuration=Release)
    [Void](BuildUitls-DownloadAppVeyorRuntimes -Token $AppVeyorToken -AccountName $AccountName -BuildId $BuildId)

    $nuspec = "ENet-CSharp.nuspec"
    [Void](nuget pack $nuspec)
    [xml]$xml = Get-Content -Path $nuspec
    $meta = $xml.package.metadata
    "$($meta.id).$($meta.version).nupkg"
}
