@echo off
chcp 65001 >nul
title 发布 游戏快捷键锁 到 GitHub
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0publish-to-github.ps1"
echo.
pause