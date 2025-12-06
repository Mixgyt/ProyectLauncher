@echo off
echo Compilando PinoLauncher para Release...
cd /d "%~dp0"

echo.
echo [1/3] Limpiando compilaciones anteriores...
dotnet clean --configuration Release

echo.
echo [2/3] Publicando aplicación (ejecutable único)...
dotnet publish --configuration Release --runtime win-x64 --self-contained true /p:PublishSingleFile=true /p:PublishReadyToRun=true /p:EnableCompressionInSingleFile=true --output "bin\Release\net9.0\win-x64\publish"
ren "bin\Release\net9.0\win-x64\publish\PinoLauncher.Desktop.exe" "Launcher.exe"
echo.
rm
echo [3/3] Creando instalador NSIS...
if exist "C:\Program Files (x86)\NSIS\makensis.exe" (
    "C:\Program Files (x86)\NSIS\makensis.exe" PinoLauncher_Installer.nsi
    echo.
    echo ✓ Instalador creado: PinoLauncher_Setup.exe
) else (
    echo ⚠️  NSIS no encontrado en la ruta estándar
    echo    Instala NSIS desde: https://nsis.sourceforge.io/
    echo    O ejecuta manualmente: makensis.exe PinoLauncher_Installer.nsi
)

echo.
echo Proceso completado. Presiona cualquier tecla para continuar...
pause >nul