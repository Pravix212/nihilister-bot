param(
    [string]$Message = "Update bot"
)

$ErrorActionPreference = "Stop"

Write-Host "`n==> 1. Staging local changes..." -ForegroundColor Cyan
git add .

$status = git status --porcelain
if ($status) {
    Write-Host "==> 2. Committing changes: '$Message'..." -ForegroundColor Cyan
    git commit -m "$Message"
} else {
    Write-Host "==> 2. No changes to commit, continuing..." -ForegroundColor Yellow
}

Write-Host "==> 3. Pushing to GitHub (v6)..." -ForegroundColor Cyan
git push origin v6

Write-Host "==> 4. Updating Droplet & Restarting Bot..." -ForegroundColor Cyan
ssh root@157.230.120.222 "/root/deploy.sh"

Write-Host "`n==> Deploy Complete! Bot is running with latest changes." -ForegroundColor Green
