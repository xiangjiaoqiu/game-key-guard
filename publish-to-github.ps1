#Requires -Version 5.1
# 游戏快捷键锁 · 一键发布到 GitHub
# 说明：这里故意不用 $ErrorActionPreference='Stop'，因为 git 的提示信息走 stderr，
#       在 PowerShell 5.1 里会被当成致命错误，导致脚本中途退出。

$RepoUrl = 'https://github.com/xiangjiaoqiu/game-key-guard.git'
$Branch  = 'main'
Set-Location -LiteralPath $PSScriptRoot

function Info($m) { Write-Host $m -ForegroundColor Cyan }
function Ok($m)   { Write-Host $m -ForegroundColor Green }
function Warn($m) { Write-Host $m -ForegroundColor Yellow }
function Dim($m)  { Write-Host $m -ForegroundColor DarkGray }

Info "==============================================="
Info "  游戏快捷键锁 · 一键发布到 GitHub"
Info "  目标仓库: $RepoUrl"
Info "==============================================="

$git = (Get-Command git -ErrorAction SilentlyContinue).Source
if (-not $git) {
    Warn "没有找到 git，请先安装 Git for Windows: https://git-scm.com/download/win"
    Read-Host "`n按回车退出"
    exit 1
}

# ---------- 1/5 准备本地仓库 ----------
Info "`n[1/5] 准备本地仓库 ..."
$gitDir = Join-Path $PWD '.git'
$hasHistory = $false
if (Test-Path $gitDir) {
    & $git rev-parse --verify HEAD > $null 2>&1
    if ($LASTEXITCODE -eq 0) {
        $hasHistory = $true
        Dim "    已有本地仓库和提交记录，直接复用。"
    } else {
        Warn "    发现没有提交记录的半成品 .git（之前沙箱环境留下的），清理后重建 ..."
        Remove-Item -LiteralPath $gitDir -Recurse -Force -ErrorAction SilentlyContinue
    }
}
if (-not (Test-Path $gitDir)) {
    & $git init -b $Branch > $null 2>&1
    if ($LASTEXITCODE -ne 0) { & $git init > $null 2>&1 }
    Dim "    已初始化本地仓库（分支 $Branch）。"
}
& $git config user.name  'xiangjiaoqiu' > $null 2>&1
& $git config user.email 'xiangjiaoqiu@users.noreply.github.com' > $null 2>&1

# ---------- 2/5 暂存 ----------
Info "[2/5] 暂存文件 ..."
& $git add -A
$stagedCount = (& $git diff --cached --name-only | Measure-Object).Count
Dim ("    共 " + $stagedCount + " 个文件有变化。")

# ---------- 3/5 提交 ----------
Info "[3/5] 提交 ..."
if ($hasHistory) {
    & $git commit -m ("更新 " + (Get-Date -Format 'yyyy-MM-dd HH:mm')) 2>&1 | ForEach-Object { Dim ("    " + $_) }
} else {
    & $git commit -m '初始提交：游戏快捷键锁 GameKeyGuard' 2>&1 | ForEach-Object { Dim ("    " + $_) }
}

# ---------- 4/5 关联远程 ----------
Info "[4/5] 关联远程仓库 ..."
& $git remote remove origin > $null 2>&1
& $git remote add origin $RepoUrl
& $git branch -M $Branch
Dim "    origin -> $RepoUrl"

# ---------- 5/5 推送 ----------
Info "[5/5] 推送到 GitHub ..."
Dim "    第一次运行会弹出浏览器让你登录 GitHub，授权后会自动继续。"
& $git push -u origin $Branch 2>&1 | ForEach-Object { Dim ("    " + $_) }

if ($LASTEXITCODE -ne 0) {
    Warn "`n推送没有成功。常见原因："
    Warn "  1) 浏览器登录还没完成 —— 关掉窗口重新双击本脚本再试一次"
    Warn "  2) 网络/代理问题 —— 确认能打开 github.com"
    Warn "  3) 远程仓库已有内容 —— 把上面的报错截图发给 Codex，我来处理"
    Read-Host "`n按回车退出"
    exit 1
}

Ok "`n发布成功！仓库地址: https://github.com/xiangjiaoqiu/game-key-guard"
Info "`n本次推送的文件："
& $git ls-files | ForEach-Object { Dim ("    " + $_) }
Write-Host ""