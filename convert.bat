@echo off
setlocal enabledelayedexpansion

rem 同じディレクトリの .cs ファイルをループで処理
for %%f in (*.cs) do (
    rem 出力ファイル名を "cs_ファイル名.md" に設定
    set "filename=cs_%%~nf.md"
    
    echo Converting %%f to !filename!...
    
    rem コピーを実行
    copy "%%f" "!filename!" > nul
)

echo.
echo 変換が完了しました。
pause