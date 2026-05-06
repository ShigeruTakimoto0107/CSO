@echo off
set CSC_PATH=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe
set TARGET_EXE=pst.exe

echo [BUILD] Compiling C-S-O Project...

if not exist "%CSC_PATH%" (
    echo Error: csc.exe not found.
    pause
    exit /b
)

:: bin フォルダが無ければ作成
if not exist ".\bin" (
    echo [INFO] Creating bin directory...
    mkdir ".\bin"
)

:: 参照の追加とUTF-8対応を明示
"%CSC_PATH%" /out:.\bin\%TARGET_EXE% /nologo /optimize /codepage:65001 /r:System.dll .\src\*.cs

if %ERRORLEVEL% equ 0 (
    echo [SUCCESS] %TARGET_EXE% has been built.
    echo [INFO] Running %TARGET_EXE%...
    .\bin\%TARGET_EXE%
) else (
    echo [FAILED] Compilation error.
)

pause
