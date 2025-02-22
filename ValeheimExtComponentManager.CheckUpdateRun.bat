@echo off
setlocal

echo CheckUpdateRun script started...

:: Define directories
set BASE_DIR=%~dp0
set CUR_DIR=%BASE_DIR%Current
set NEW_DIR=%BASE_DIR%New
set OLD_DIR=%BASE_DIR%Old
set PARAMS_FILE=%BASE_DIR%ValheimExtComponentManager.Recall.Params.TEMP

:: Ensure the params file is deleted if it exists
if exist "%PARAMS_FILE%" (
    echo Unexpected params file found: %PARAMS_FILE%
    del /f /q "%PARAMS_FILE%" 2>nul
    if exist "%PARAMS_FILE%" (
        echo ERROR: Failed to delete params file! Aborting.
        exit /b 1
    )
)

:: Perform initial update check
cd /d "%BASE_DIR%"
call :update_files

:: Switch to the current directory and run the program
cd /d "%CUR_DIR%"
if exist "ValheimExtComponentManager.exe" (
    echo Running ValheimExtComponentManager.exe with params: %*
    "ValheimExtComponentManager.exe" %*
    echo Returned from ValheimExtComponentManager.exe
) else (
    echo ERROR: ValheimExtComponentManager.exe not found in ValheimExtComponentManager directory!
    exit /b 1
)

echo CheckUpdateRun script continuing...

:: Check if params file was created
if exist "%PARAMS_FILE%" (
    set /p PARAMS=<"%PARAMS_FILE%"
    del /f /q "%PARAMS_FILE%" 2>nul
    if exist "%PARAMS_FILE%" (
        echo ERROR: Failed to delete params file! Aborting.
        exit /b 1
    )
)

:: Return to script directory and perform another update check
cd /d "%BASE_DIR%"
call :update_files

:: If parameters were returned, rerun the target exe
if defined PARAMS (
    cd /d "%CUR_DIR%"
    echo Re-running ValheimExtComponentManager.exe with params: %PARAMS%
    "ValheimExtComponentManager.exe" %PARAMS%
    echo Returned from ValheimExtComponentManager.exe
)

echo CheckUpdateRun script done.
exit /b 0

:: =======================
:: UPDATE FILE FUNCTION
:: =======================
:update_files
echo Checking for update directory...

:: If a new version exists, perform an update
if exist "%NEW_DIR%" (
    echo Found update! Replacing current version...

    :: Move old version to backup
    if exist "%CUR_DIR%" (
        echo Backing up current version...
        if exist "%OLD_DIR%" (
            rmdir /s /q "%OLD_DIR%" 2>nul
            if %errorlevel% neq 0 (
                echo ERROR: Failed to remove old backup! Aborting update.
                exit /b 1
            )
        )
        move "%CUR_DIR%" "%OLD_DIR%"
        if %errorlevel% neq 0 (
            echo ERROR: Failed to move current version to backup! Aborting update.
            exit /b 1
        )
    )

    :: Move new version into place
    move "%NEW_DIR%" "%CUR_DIR%"
    if %errorlevel% neq 0 (
        echo ERROR: Failed to move new version into place! Attempting recovery...
        call :attempt_recovery
        exit /b 1
    )
    echo Update complete!
) else (
    echo No update directory found.

    :: Check if recovery is needed when no new version exists
    if not exist "%CUR_DIR%" (
        echo No new version found, but current version is missing. Attempting recovery...
        call :attempt_recovery
    )
)

exit /b

:: =======================
:: RECOVERY FUNCTION
:: =======================
:attempt_recovery
if exist "%OLD_DIR%" (
    echo Attempting to restore previous version...
    move "%OLD_DIR%" "%CUR_DIR%"
    if %errorlevel% neq 0 (
        echo ERROR: Failed to restore previous version! System may be in an inconsistent state.
        exit /b 1
    )
    echo Successfully restored previous version.
) else (
    echo WARNING: No backup version available to restore.
)
exit /b
