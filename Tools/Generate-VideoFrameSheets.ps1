param(
    [string]$SourceDirectory = "Assets/1_Internal/Data/Video_Processed",
    [string]$OutputDirectory = "Assets/1_Internal/Resources/VideoFrames",
    [int]$FramesPerSecond = 10
)

$ErrorActionPreference = "Stop"
$columns = 8
$rows = 8
$frameWidth = 256
$frameHeight = 144
$framesPerSheet = $columns * $rows

function Find-FFmpegTool([string]$toolName) {
    $command = Get-Command $toolName -ErrorAction SilentlyContinue
    if ($command) {
        return $command.Source
    }

    $wingetRoot = Join-Path $env:LOCALAPPDATA "Microsoft/WinGet/Packages"
    if (Test-Path -LiteralPath $wingetRoot) {
        $tool = Get-ChildItem -LiteralPath $wingetRoot -Recurse -Filter "$toolName.exe" -ErrorAction SilentlyContinue |
            Select-Object -First 1
        if ($tool) {
            return $tool.FullName
        }
    }

    throw "$toolName was not found. Install FFmpeg before running this script."
}

$ffmpeg = Find-FFmpegTool "ffmpeg"
$ffprobe = Find-FFmpegTool "ffprobe"
$projectRoot = (Resolve-Path -LiteralPath ".").Path
$sourceRoot = (Resolve-Path -LiteralPath $SourceDirectory).Path

if (!(Test-Path -LiteralPath $OutputDirectory)) {
    New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
}
$outputRoot = (Resolve-Path -LiteralPath $OutputDirectory).Path
if (!$outputRoot.StartsWith($projectRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw "OutputDirectory must stay inside the current Unity project."
}
$manifestVideos = @()

Get-ChildItem -LiteralPath $sourceRoot -Filter "*.mp4" | Sort-Object Name | ForEach-Object {
    $stem = $_.BaseName
    $targetDirectory = Join-Path $outputRoot $stem
    New-Item -ItemType Directory -Path $targetDirectory -Force | Out-Null

    Get-ChildItem -LiteralPath $targetDirectory -Filter "*.jpg" -ErrorAction SilentlyContinue |
        Remove-Item -Force

    $durationText = & $ffprobe -v error -show_entries format=duration -of "default=noprint_wrappers=1:nokey=1" $_.FullName
    $duration = [double]::Parse($durationText.Trim(), [Globalization.CultureInfo]::InvariantCulture)
    $frameCount = [Math]::Ceiling($duration * $FramesPerSecond - 0.0001)
    $outputPattern = Join-Path $targetDirectory ($stem + "_%03d.jpg")
    $filter = "fps=$FramesPerSecond,scale=${frameWidth}:${frameHeight}:flags=lanczos,tile=${columns}x${rows}:nb_frames=$framesPerSheet"

    & $ffmpeg -hide_banner -loglevel error -i $_.FullName -vf $filter -fps_mode vfr -q:v 4 $outputPattern
    if ($LASTEXITCODE -ne 0) {
        throw "FFmpeg failed for $($_.FullName)"
    }

    $sheetCount = (Get-ChildItem -LiteralPath $targetDirectory -Filter "*.jpg").Count
    $manifestVideos += [ordered]@{ stem = $stem; frameCount = $frameCount }
    Write-Host "${stem}: $frameCount frames, $sheetCount sheets"
}

$manifest = [ordered]@{ videos = $manifestVideos } | ConvertTo-Json -Depth 4
$manifestPath = Join-Path $outputRoot "manifest.json"
[IO.File]::WriteAllText($manifestPath, $manifest, [Text.UTF8Encoding]::new($false))
Write-Host "Updated $manifestPath"
