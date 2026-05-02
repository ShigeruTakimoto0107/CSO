@echo off
set CSC_PATH=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe
set TARGET_EXE=pst.exe

echo [BUILD] Compiling C-S-O Project...

if not exist "%CSC_PATH%" (
    echo Error: csc.exe not found.
    pause
    exit /b
)

:: éQè∆ÇÃí«â¡Ç∆UTF-8ëŒâûÇñæé¶
"%CSC_PATH%" /out:%TARGET_EXE% /nologo /optimize /codepage:65001 /r:System.dll *.cs

if %ERRORLEVEL% equ 0 (
    echo [SUCCESS] %TARGET_EXE% has been built.
    echo [INFO] Running %TARGET_EXE%...
    %TARGET_EXE%
) else (
    echo [FAILED] Compilation error.
)

pause