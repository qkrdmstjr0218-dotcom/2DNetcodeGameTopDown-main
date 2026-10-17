param([string]$CheckpointName = 'CharacterMotion-2026-10-18')
$ErrorActionPreference = 'Stop'
if ($CheckpointName -notmatch '^[A-Za-z0-9_-]+$') { throw 'Use a simple checkpoint name.' }
$projectRoot = [IO.Path]::GetFullPath((Split-Path -Parent $PSScriptRoot))
$exportRoot = Join-Path $projectRoot 'Exports'
[IO.Directory]::CreateDirectory($exportRoot) | Out-Null
$zipPath = Join-Path $exportRoot ($CheckpointName + '.zip')
$bundlePath = Join-Path $exportRoot ($CheckpointName + '.bundle')
$manifestPath = Join-Path $exportRoot ($CheckpointName + '.manifest.json')
foreach ($destination in @($zipPath,$bundlePath,$manifestPath)) {
    if (Test-Path -LiteralPath $destination) { throw "Already exists: $destination" }
}
Push-Location $projectRoot
try {
    $commit = (& git rev-parse HEAD).Trim()
    if ($LASTEXITCODE -ne 0) { throw 'Cannot resolve Git checkpoint.' }
    & git bundle create $bundlePath --all
    if ($LASTEXITCODE -ne 0) { throw 'Git bundle creation failed.' }
    & git bundle verify $bundlePath
    if ($LASTEXITCODE -ne 0) { throw 'Git bundle validation failed.' }

    $selectedFiles = [Collections.Generic.List[IO.FileInfo]]::new()
    $sourceDirectories = @('Assets','Packages','ProjectSettings','Docs','Tools','LocalTools/Analysis','LocalTools/UnityMcpPackage')
    foreach ($relativeDirectory in $sourceDirectories) {
        $directory = Join-Path $projectRoot $relativeDirectory
        if (!(Test-Path -LiteralPath $directory -PathType Container)) { throw "Missing: $relativeDirectory" }
        foreach ($file in (Get-ChildItem -LiteralPath $directory -File -Recurse -Force)) {
            if ($file.FullName -match '[\\/](node_modules|__pycache__|\.git)[\\/]') { continue }
            $selectedFiles.Add($file)
        }
    }
    foreach ($relativeFile in (& git ls-files)) {
        if ($relativeFile.Contains('/')) { continue }
        $rootFile = Join-Path $projectRoot $relativeFile
        if (Test-Path -LiteralPath $rootFile -PathType Leaf) { $selectedFiles.Add((Get-Item -LiteralPath $rootFile)) }
    }
    $selectedFiles.Add((Get-Item -LiteralPath $bundlePath))
    $manifest = [ordered]@{
        checkpoint = $CheckpointName; date = '2026-10-18'; gitCommit = $commit
        unityVersion = '6000.3.18f1'; mainScene = 'Assets/RightWalkPrototype/SideScrollFrontPlayableTrial.unity'
        excluded = @('Library','Temp','Obj','Build','Builds','Logs','UserSettings','.git','Exports','node_modules','__pycache__')
        files = [Collections.Generic.List[object]]::new()
    }
    $stream = [IO.File]::Open($zipPath,[IO.FileMode]::CreateNew)
    $archive = [IO.Compression.ZipArchive]::new($stream,[IO.Compression.ZipArchiveMode]::Create,$false,[Text.Encoding]::UTF8)
    try {
        foreach ($file in $selectedFiles) {
            $relativePath = [IO.Path]::GetRelativePath($projectRoot,$file.FullName).Replace('\','/')
            if ($file.FullName -eq $bundlePath) { $relativePath = 'GitHistory.bundle' }
            $hash = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash
            $entry = $archive.CreateEntry($relativePath,[IO.Compression.CompressionLevel]::Fastest)
            $input = [IO.File]::OpenRead($file.FullName)
            $output = $entry.Open()
            try { $input.CopyTo($output) } finally { $output.Dispose(); $input.Dispose() }
            $manifest.files.Add([ordered]@{path=$relativePath;bytes=$file.Length;sha256=$hash})
        }
        $manifestText = $manifest | ConvertTo-Json -Depth 6
        [IO.File]::WriteAllText($manifestPath,$manifestText,[Text.UTF8Encoding]::new($false))
        $manifestEntry = $archive.CreateEntry('CHECKPOINT-MANIFEST.json')
        $writer = [IO.StreamWriter]::new($manifestEntry.Open(),[Text.UTF8Encoding]::new($false))
        try { $writer.Write($manifestText) } finally { $writer.Dispose() }
    } finally { $archive.Dispose(); $stream.Dispose() }

    $verified = 0
    $archive = [IO.Compression.ZipFile]::OpenRead($zipPath)
    try {
        $entries = @{}
        foreach ($entry in $archive.Entries) { $entries[$entry.FullName] = $entry }
        foreach ($file in $manifest.files) {
            $entry = $entries[$file.path]
            if ($null -eq $entry -or $entry.Length -ne $file.bytes) { throw "Missing or truncated: $($file.path)" }
            $input = $entry.Open(); $sha = [Security.Cryptography.SHA256]::Create()
            try { $actualHash = [Convert]::ToHexString($sha.ComputeHash($input)) } finally { $sha.Dispose(); $input.Dispose() }
            if ($actualHash -ne $file.sha256) { throw "Archive content mismatch: $($file.path)" }
            $verified++
        }
    } finally { $archive.Dispose() }
    [pscustomobject]@{Zip=$zipPath;Commit=$commit;VerifiedFiles=$verified;SizeMB=[math]::Round((Get-Item -LiteralPath $zipPath).Length/1MB,1)} | ConvertTo-Json -Compress
} finally { Pop-Location }
