@echo off
setlocal EnableExtensions EnableDelayedExpansion
chcp 65001 >nul

echo [BAT_VERSION] stable-final-v2.2.2
echo [BAT_PATH] %~f0

REM ===== Parameters =====
set "PROTO_ROOT=%~1"
set "OUT_ROOT=%~2"
set "PROTOC_EXE=%~3"
set "IMPORT_ROOT=%~4"
set "ENABLE_GRPC=%~5"
set "GRPC_PLUGIN=%~6"
set "CLEAN_OUTPUT=%~7"
set "ONLY_CHANGED=%~8"
set "MD5_PATH=%~9"

REM Check parameter misalignment (empty quote "" causes parameter shift)
REM If CLEAN_OUTPUT is not 0 or 1, parameters are misaligned
if not "%CLEAN_OUTPUT%"=="0" if not "%CLEAN_OUTPUT%"=="1" (
    REM Realign parameters
    set "MD5_PATH=%ONLY_CHANGED%"
    set "ONLY_CHANGED=%CLEAN_OUTPUT%"
    set "CLEAN_OUTPUT=%GRPC_PLUGIN%"
    set "GRPC_PLUGIN="
)
set "STATE_FILE=%MD5_PATH%\.proto_md5.state"
echo [CONFIG] STATE_FILE=%STATE_FILE%

REM ========Create MD5 Directory=======

:: 1. Check if directory exists, create if not
if not exist "%MD5_PATH%" (
    echo Directory not exist, creating: %MD5_PATH%
    mkdir "%MD5_PATH%"
)


if "%PROTO_ROOT%"=="" ( echo [ERROR] PROTO_ROOT empty & exit /b 2 )
if "%OUT_ROOT%"==""   ( echo [ERROR] OUT_ROOT empty   & exit /b 3 )

if "%PROTOC_EXE%"=="" set "PROTOC_EXE=%~dp0protoc.exe"
if "%IMPORT_ROOT%"=="" set "IMPORT_ROOT=%PROTO_ROOT%"
if "%ENABLE_GRPC%"=="" set "ENABLE_GRPC=0"
if "%GRPC_PLUGIN%"=="" set "GRPC_PLUGIN="
if "%CLEAN_OUTPUT%"=="" set "CLEAN_OUTPUT=0"
if "%ONLY_CHANGED%"=="" set "ONLY_CHANGED=0"

if not exist "%PROTO_ROOT%" ( echo [ERROR] PROTO_ROOT not found: "%PROTO_ROOT%" & exit /b 2 )
if not exist "%PROTOC_EXE%" ( echo [ERROR] protoc not found: "%PROTOC_EXE%" & exit /b 3 )
if not exist "%OUT_ROOT%" mkdir "%OUT_ROOT%"

echo [CONFIG] PROTO_ROOT=%PROTO_ROOT%
echo [CONFIG] OUT_ROOT=%OUT_ROOT%
echo [CONFIG] PROTOC_EXE=%PROTOC_EXE%
echo [CONFIG] IMPORT_ROOT=%IMPORT_ROOT%
echo [CONFIG] ENABLE_GRPC=%ENABLE_GRPC%
echo [CONFIG] GRPC_PLUGIN=%GRPC_PLUGIN%
echo [CONFIG] CLEAN_OUTPUT=%CLEAN_OUTPUT%
echo [CONFIG] ONLY_CHANGED=%ONLY_CHANGED%

if "%CLEAN_OUTPUT%"=="1" (
  if exist "%STATE_FILE%" del /q "%STATE_FILE%" >nul 2>nul
  echo [INFO] Cleaning output folder...
  if exist "%OUT_ROOT%" rmdir /s /q "%OUT_ROOT%" >nul 2>nul
  mkdir "%OUT_ROOT%" >nul 2>nul
  if not exist "%OUT_ROOT%" (
    echo [ERROR] Failed to recreate OUT_ROOT: "%OUT_ROOT%"
    exit /b 4
  )
)

set "LISTFILE=%TEMP%\proto_list_%RANDOM%_%RANDOM%.txt"
dir /b /s "%PROTO_ROOT%\*.proto" > "%LISTFILE%"

set /a COUNT_TOTAL=0
for /f "usebackq delims=" %%L in ("%LISTFILE%") do set /a COUNT_TOTAL+=1

echo [INFO] Proto total = !COUNT_TOTAL!
if !COUNT_TOTAL! EQU 0 (
  del /q "%LISTFILE%" >nul 2>nul
  echo [UNITY_SUMMARY] PROTO_GEN TOTAL=0 DONE=0 SKIP=0 ERR=0
  exit /b 0
)

set /a COUNT_CUR=0
set /a COUNT_DONE=0
set /a COUNT_SKIP=0
set /a COUNT_ERR=0

for /f "usebackq delims=" %%F in ("%LISTFILE%") do (
  set /a COUNT_CUR+=1
  set "SRC=%%~fF"
  set "SRC_DIR=%%~dpF"

  set "REL_DIR=!SRC_DIR:%PROTO_ROOT%=!"
  if "!REL_DIR:~0,1!"=="\" set "REL_DIR=!REL_DIR:~1!"

  if "!REL_DIR!"=="" (
    set "OUT_DIR=%OUT_ROOT%"
  ) else (
    set "OUT_DIR=%OUT_ROOT%\!REL_DIR!"
  )
  if "!OUT_DIR:~-1!"=="\" set "OUT_DIR=!OUT_DIR:~0,-1!"
  if not exist "!OUT_DIR!" mkdir "!OUT_DIR!" >nul 2>nul

  echo [PROGRESS] !COUNT_CUR!/!COUNT_TOTAL! %%~nxF
  echo [MAP] SRC="%%~fF" OUT_DIR="!OUT_DIR!"

  if "%ONLY_CHANGED%"=="1" (
    call :should_compile "%%~fF" "!OUT_DIR!"
    if not errorlevel 1 (
      call :compile_one "%%~fF" "!OUT_DIR!" "%%~dpF"
    ) else (
      echo [SKIP] %%~fF
      set /a COUNT_SKIP+=1
    )
  ) else (
    call :compile_one "%%~fF" "!OUT_DIR!" "%%~dpF"
  )
)

del /q "%LISTFILE%" >nul 2>nul

echo:
echo [RESULT] TOTAL=!COUNT_TOTAL! DONE=!COUNT_DONE! SKIP=!COUNT_SKIP! ERR=!COUNT_ERR!
echo [UNITY_SUMMARY] PROTO_GEN TOTAL=!COUNT_TOTAL! DONE=!COUNT_DONE! SKIP=!COUNT_SKIP! ERR=!COUNT_ERR!

if !COUNT_ERR! GTR 0 exit /b 5
exit /b 0


:compile_one
set "SRC=%~1"
set "ONE_OUT=%~2"
set "SRC_DIR=%~3"

call :to_rel "%SRC%" REL_KEY

echo [GEN ] %SRC%

REM Count .cs files before compile (for verification)
set /a CS_BEFORE=0
for /f "delims=" %%C in ('dir /b /a:-d "%ONE_OUT%\*.cs"') do set /a CS_BEFORE+=1

REM Key: proto_path first current file dir, then IMPORT_ROOT / PROTO_ROOT
set "P_INC_A=--proto_path=%SRC_DIR%"
set "P_INC_B=--proto_path=%IMPORT_ROOT%"
set "P_INC_C=--proto_path=%PROTO_ROOT%"

if "%ENABLE_GRPC%"=="1" (
  if not "%GRPC_PLUGIN%"=="" (
    echo [CMD] "%PROTOC_EXE%" %P_INC_A% %P_INC_B% %P_INC_C% --csharp_out="%ONE_OUT%" --grpc_out="%ONE_OUT%" --plugin=protoc-gen-grpc="%GRPC_PLUGIN%" "%SRC%"
    "%PROTOC_EXE%" %P_INC_A% %P_INC_B% %P_INC_C% --csharp_out="%ONE_OUT%" --grpc_out="%ONE_OUT%" --plugin=protoc-gen-grpc="%GRPC_PLUGIN%" "%SRC%"
  ) else (
    echo [CMD] "%PROTOC_EXE%" %P_INC_A% %P_INC_B% %P_INC_C% --csharp_out="%ONE_OUT%" --grpc_out="%ONE_OUT%" "%SRC%"
    "%PROTOC_EXE%" %P_INC_A% %P_INC_B% %P_INC_C% --csharp_out="%ONE_OUT%" --grpc_out="%ONE_OUT%" "%SRC%"
  )
) else (
  echo [CMD] "%PROTOC_EXE%" %P_INC_A% %P_INC_B% %P_INC_C% --csharp_out="%ONE_OUT%" "%SRC%"
  "%PROTOC_EXE%" %P_INC_A% %P_INC_B% %P_INC_C% --csharp_out="%ONE_OUT%" "%SRC%"
)

if errorlevel 1 (
  echo [ERR ] %SRC%
  set /a COUNT_ERR+=1
  exit /b 0
)

REM Count .cs files after compile
set /a CS_AFTER=0
for /f "delims=" %%C in ('dir /b /a:-d "%ONE_OUT%\*.cs"') do set /a CS_AFTER+=1

if %CS_AFTER% LSS 1 (
  echo [ERR ] No .cs generated in "%ONE_OUT%" for "%SRC%"
  set /a COUNT_ERR+=1
) else (
  if %CS_AFTER% GTR %CS_BEFORE% (
    echo [OK  ] %SRC% ^(new cs: %CS_BEFORE% -^> %CS_AFTER%^)
  ) else (
    echo [OK  ] %SRC% ^(cs exists: %CS_AFTER%^)
  )
  set /a COUNT_DONE+=1
  call :update_state "%SRC%" "%REL_KEY%"
)

exit /b 0


:should_compile
setlocal EnableExtensions DisableDelayedExpansion
set "SC_SRC=%~1"
set "SC_OUT=%~2"
set "SC_MD5="
set "SC_OLD="
set "SC_KEY="

call :to_rel "%SC_SRC%" SC_KEY

REM If output missing, force compile
dir /b /a:-d "%SC_OUT%\*.cs" >nul 2>nul
if errorlevel 1 (
  echo [STATE_MISS] %SC_KEY% ^(reason:output_missing^)
  endlocal & exit /b 0
)

REM Get MD5 (skip first line)
set "CERTUTIL_OUT=%TEMP%\certutil_%RANDOM%_%RANDOM%.tmp"
certutil -hashfile "%SC_SRC%" MD5 > "%CERTUTIL_OUT%" 2>nul
for /f "skip=1 delims=" %%H in ('type "%CERTUTIL_OUT%"') do (
  if not defined SC_MD5 set "SC_MD5=%%H"
)
del /q "%CERTUTIL_OUT%" >nul 2>nul

if not defined SC_MD5 (
  echo [STATE_MISS] %SC_KEY% ^(reason:md5_unavailable^)
  endlocal & exit /b 0
)

setlocal EnableDelayedExpansion
set "SC_MD5=!SC_MD5: =!"
endlocal & set "SC_MD5=%SC_MD5%"

if exist "%STATE_FILE%" (
  for /f "usebackq tokens=1,* delims=|" %%A in ("%STATE_FILE%") do (
    if /I "%%~A"=="%SC_KEY%" (
      set "SC_OLD=%%~B"
      goto :sc_old_ok
    )
  )
)
:sc_old_ok

if /I "%SC_MD5%"=="%SC_OLD%" (
  echo [STATE_HIT ] %SC_KEY%
  endlocal & exit /b 1
) else (
  echo [STATE_MISS] %SC_KEY% ^(reason:md5_changed_or_new^)
  endlocal & exit /b 0
)


:update_state
REM Param1: Absolute SRC  Param2: Relative KEY(optional)
setlocal EnableExtensions DisableDelayedExpansion
set "US_SRC=%~1"
set "US_KEY=%~2"
set "US_MD5="
set "US_STATE=%STATE_FILE%"

if "%US_KEY%"=="" (
  call :to_rel "%US_SRC%" US_KEY
)

set "CERTUTIL_OUT=%TEMP%\certutil_%RANDOM%_%RANDOM%.tmp"
certutil -hashfile "%US_SRC%" MD5 > "%CERTUTIL_OUT%" 2>nul
for /f "skip=1 delims=" %%H in ('type "%CERTUTIL_OUT%"') do (
  if not defined US_MD5 set "US_MD5=%%H"
)
del /q "%CERTUTIL_OUT%" >nul 2>nul

if not defined US_MD5 (
  echo [WARN] cannot get MD5 for "%US_SRC%"
  endlocal & exit /b 0
)

setlocal EnableDelayedExpansion
set "US_MD5=!US_MD5: =!"
endlocal & set "US_MD5=%US_MD5%"

if not exist "%US_STATE%" type nul > "%US_STATE%"

set "TMP_STATE=%US_STATE%.tmp"
break > "%TMP_STATE%"

for /f "usebackq tokens=1,* delims=|" %%A in ("%US_STATE%") do (
  if /I not "%%~A"=="%US_KEY%" (
    >>"%TMP_STATE%" echo %%~A^|%%~B
  )
)

>>"%TMP_STATE%" echo %US_KEY%^|%US_MD5%
move /Y "%TMP_STATE%" "%US_STATE%" >nul

echo [STATE] %US_KEY% ^| %US_MD5%
endlocal & exit /b 0

:to_rel
REM Usage: call :to_rel "absolute proto path" OUT_VAR
setlocal EnableExtensions EnableDelayedExpansion
set "ABS=%~1"
set "REL=!ABS:%PROTO_ROOT%\=!"
if /I "!REL!"=="!ABS!" (
  REM Not under PROTO_ROOT, use original path
  set "REL=%~1"
)
endlocal & set "%~2=%REL%"
exit /b 0
