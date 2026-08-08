# Script tu dong commit file da staged theo batch
# Chi can: .\commit-staged.ps1

param(
    [int]$BatchSize = 100,
    [string]$CommitMessage = "Commit files",
    [switch]$PushEachBatch = $true
)

$ErrorActionPreference = "SilentlyContinue"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Auto Commit Staged Files" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Kiem tra git repository
if (-not (Test-Path ".git")) {
    Write-Host "ERROR: Khong phai git repository!" -ForegroundColor Red
    exit 1
}

# Lay danh sach file da staged
Write-Host "Dang quet staged files..." -ForegroundColor Yellow
$stagedFiles = @(git diff --cached --name-only 2>$null)
$totalFiles = $stagedFiles.Count

Write-Host "Tong so file da staged: $totalFiles" -ForegroundColor Green
Write-Host ""

if ($totalFiles -eq 0) {
    Write-Host "Khong co file nao duoc staged!" -ForegroundColor Yellow
    Write-Host "Goi y: Chay 'git add .' de stage files" -ForegroundColor Cyan
    exit 0
}

# Quyet dinh commit theo batch hay tat ca
if ($totalFiles -le $BatchSize) {
    Write-Host "Che do: Commit tat ca $totalFiles files trong 1 lan" -ForegroundColor Cyan
    Write-Host ""
    
    $confirm = Read-Host "Ban co muon tiep tuc? (Y/N)"
    if ($confirm -ne "Y" -and $confirm -ne "y") {
        Write-Host "Da huy!" -ForegroundColor Yellow
        exit 0
    }
    
    Write-Host ""
    Write-Host "Dang commit..." -ForegroundColor Yellow
    
    git commit -m "$CommitMessage" 2>&1 | Out-Null
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Commit thanh cong $totalFiles files!" -ForegroundColor Green
        
        # Push luon neu duoc yeu cau
        if ($PushEachBatch) {
            Write-Host "Dang push..." -ForegroundColor Yellow
            git push 2>&1 | Out-Null
            
            if ($LASTEXITCODE -eq 0) {
                Write-Host "Push thanh cong!" -ForegroundColor Green
            } else {
                Write-Host "Push that bai! Tiep tuc..." -ForegroundColor Red
            }
        }
    } else {
        Write-Host "Commit that bai!" -ForegroundColor Red
        exit 1
    }
} else {
    # Commit theo batch
    $totalBatches = [Math]::Ceiling($totalFiles / $BatchSize)
    Write-Host "File qua nhieu! Se chia thanh $totalBatches batch (moi batch $BatchSize files)" -ForegroundColor Cyan
    Write-Host ""
    
    $confirm = Read-Host "Ban co muon tiep tuc? (Y/N)"
    if ($confirm -ne "Y" -and $confirm -ne "y") {
        Write-Host "Da huy!" -ForegroundColor Yellow
        exit 0
    }
    
    Write-Host ""
    Write-Host "Bat dau commit..." -ForegroundColor Cyan
    Write-Host ""
    
    $successCount = 0
    $errorCount = 0
    
    # Unstage tat ca de xu ly lai
    git reset HEAD 2>&1 | Out-Null
    
    for ($i = 0; $i -lt $totalFiles; $i += $BatchSize) {
        $currentBatch = [Math]::Floor($i / $BatchSize) + 1
        $end = [Math]::Min($i + $BatchSize, $totalFiles)
        
        Write-Host "[$currentBatch/$totalBatches] Dang xu ly files $($i + 1) den $end..." -ForegroundColor Yellow
        
        # Add files trong batch hien tai
        for ($j = $i; $j -lt $end; $j++) {
            git add $stagedFiles[$j] 2>$null
        }
        
        # Commit batch
        $batchMsg = "$CommitMessage - Batch $currentBatch/$totalBatches"
        git commit -m "$batchMsg" 2>&1 | Out-Null
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "  Commit thanh cong!" -ForegroundColor Green
            $successCount++
            
            # Push luon sau moi batch
            if ($PushEachBatch) {
                Write-Host "  Dang push batch $currentBatch..." -ForegroundColor Cyan
                git push 2>&1 | Out-Null
                
                if ($LASTEXITCODE -eq 0) {
                    Write-Host "  Push thanh cong!" -ForegroundColor Green
                } else {
                    Write-Host "  Push that bai! Tiep tuc batch tiep theo..." -ForegroundColor Red
                }
            }
        } else {
            Write-Host "  Commit that bai!" -ForegroundColor Red
            $errorCount++
        }
        
        # Nghi ngan giua cac batch
        if ($currentBatch -lt $totalBatches) {
            Start-Sleep -Milliseconds 300
        }
    }
    
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "Thanh cong: $successCount batch" -ForegroundColor Green
    Write-Host "That bai: $errorCount batch" -ForegroundColor Red
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan

Write-Host ""
Write-Host "Hoan tat!" -ForegroundColor Green