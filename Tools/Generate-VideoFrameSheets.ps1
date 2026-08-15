param(
    [string]$RawDirectory = "Assets/1_Internal/Data/Video_Raw",
    [string]$ProcessedDirectory = "Assets/1_Internal/Data/Video_Processed",
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

function Get-MediaDuration([string]$videoPath) {
    $durationText = & $ffprobe -v error -show_entries format=duration -of "default=noprint_wrappers=1:nokey=1" $videoPath
    if ($LASTEXITCODE -ne 0) {
        throw "FFprobe failed to read duration for $videoPath"
    }

    return [double]::Parse($durationText.Trim(), [Globalization.CultureInfo]::InvariantCulture)
}

function Get-ContentDuration([string]$videoPath) {
    $mediaDuration = Get-MediaDuration $videoPath
    $previousErrorAction = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    $detectOutput = & $ffmpeg -hide_banner -nostats -i $videoPath `
        -vf "blackdetect=d=0.5:pix_th=0.02" -an -f null - 2>&1
    $detectExitCode = $LASTEXITCODE
    $ErrorActionPreference = $previousErrorAction
    if ($detectExitCode -ne 0) {
        throw "FFmpeg failed while detecting trailing black frames in $videoPath"
    }

    $contentDuration = $mediaDuration
    foreach ($line in $detectOutput) {
        $match = [regex]::Match($line.ToString(), "black_start:(?<start>[0-9.]+) black_end:(?<end>[0-9.]+) black_duration:(?<duration>[0-9.]+)")
        if (!$match.Success) {
            continue
        }

        $blackStart = [double]::Parse($match.Groups["start"].Value, [Globalization.CultureInfo]::InvariantCulture)
        $blackEnd = [double]::Parse($match.Groups["end"].Value, [Globalization.CultureInfo]::InvariantCulture)
        if ($blackEnd -ge $mediaDuration - 0.25 -and $blackStart -gt 0.1) {
            $contentDuration = [Math]::Min($contentDuration, $blackStart)
        }
    }

    return $contentDuration
}

$ffmpeg = Find-FFmpegTool "ffmpeg"
$ffprobe = Find-FFmpegTool "ffprobe"
$projectRoot = (Resolve-Path -LiteralPath ".").Path
$rawRoot = (Resolve-Path -LiteralPath $RawDirectory).Path

if (!(Test-Path -LiteralPath $ProcessedDirectory)) {
    New-Item -ItemType Directory -Path $ProcessedDirectory -Force | Out-Null
}
$processedRoot = (Resolve-Path -LiteralPath $ProcessedDirectory).Path
if (!$processedRoot.StartsWith($projectRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw "ProcessedDirectory must stay inside the current Unity project."
}

if (!(Test-Path -LiteralPath $OutputDirectory)) {
    New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
}
$outputRoot = (Resolve-Path -LiteralPath $OutputDirectory).Path
if (!$outputRoot.StartsWith($projectRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw "OutputDirectory must stay inside the current Unity project."
}
$manifestVideos = @()
$processedVideos = @()

Get-ChildItem -LiteralPath $rawRoot -Filter "*.mp4" | Sort-Object Name | ForEach-Object {
    $stem = $_.BaseName
    $processedVideo = Join-Path $processedRoot ($stem + ".mp4")
    $thumbnail = Join-Path $processedRoot ($stem + ".png")
    $scaleFilter = "scale=${frameWidth}:${frameHeight}:force_original_aspect_ratio=decrease:flags=lanczos,pad=${frameWidth}:${frameHeight}:(ow-iw)/2:(oh-ih)/2:black"
    $contentDuration = Get-ContentDuration $_.FullName
    $durationArgument = $contentDuration.ToString("0.######", [Globalization.CultureInfo]::InvariantCulture)

    & $ffmpeg -y -hide_banner -loglevel error -i $_.FullName -t $durationArgument `
        -vf "fps=$FramesPerSecond,$scaleFilter,setpts=N/($FramesPerSecond*TB)" `
        -an -c:v libx264 -preset veryfast -crf 23 -pix_fmt yuv420p -r $FramesPerSecond -fps_mode cfr -movflags +faststart $processedVideo
    if ($LASTEXITCODE -ne 0) {
        throw "FFmpeg failed to create processed video for $($_.FullName)"
    }

    & $ffmpeg -y -hide_banner -loglevel error -i $_.FullName -frames:v 1 -vf $scaleFilter $thumbnail
    if ($LASTEXITCODE -ne 0) {
        throw "FFmpeg failed to create thumbnail for $($_.FullName)"
    }

    $processedVideos += Get-Item -LiteralPath $processedVideo
    Write-Host "Processed ${stem} at ${FramesPerSecond} FPS (${durationArgument}s content)"
}

$processedVideos | Sort-Object Name | ForEach-Object {
    $stem = $_.BaseName
    $targetDirectory = Join-Path $outputRoot $stem
    New-Item -ItemType Directory -Path $targetDirectory -Force | Out-Null

    Get-ChildItem -LiteralPath $targetDirectory -Filter "*.jpg" -ErrorAction SilentlyContinue |
        Remove-Item -Force

    $frameCount = 0
    $frameCountText = & $ffprobe -v error -select_streams v:0 -show_entries stream=nb_frames -of "default=noprint_wrappers=1:nokey=1" $_.FullName
    if ($LASTEXITCODE -ne 0 -or ![int]::TryParse($frameCountText.Trim(), [ref]$frameCount)) {
        throw "FFprobe failed to read frame count for $($_.FullName)"
    }
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
