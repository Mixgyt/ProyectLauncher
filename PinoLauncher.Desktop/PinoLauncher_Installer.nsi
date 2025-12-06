;PinoLauncher Installer Script
;Generated for PinoLauncher - Minecraft Launcher

;Include Modern UI
!include "MUI2.nsh"

;--------------------------------
;General Configuration

;Name and file
Name "PinoLauncher"
OutFile "PinoLauncher_Setup.exe"

;Default installation folder
InstallDir "$PROGRAMFILES\Pino Launcher"

;Get installation folder from registry if available
InstallDirRegKey HKCU "Software\PinoLauncher" ""

;Request application privileges for Windows Vista/7/8/10/11
RequestExecutionLevel admin

;--------------------------------
;Variables

Var StartMenuFolder

;--------------------------------
;Interface Settings

!define MUI_ABORTWARNING
!define MUI_ICON "pino_settings.ico"
!define MUI_UNICON "pino_settings.ico"

;--------------------------------
;Pages

!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_LICENSE "License.txt"
!insertmacro MUI_PAGE_DIRECTORY

;Start Menu Folder Page Configuration
!define MUI_STARTMENUPAGE_REGISTRY_ROOT "HKCU" 
!define MUI_STARTMENUPAGE_REGISTRY_KEY "Software\PinoLauncher" 
!define MUI_STARTMENUPAGE_REGISTRY_VALUENAME "Start Menu Folder"

!insertmacro MUI_PAGE_STARTMENU Application $StartMenuFolder

!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_UNPAGE_WELCOME
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_UNPAGE_FINISH

;--------------------------------
;Languages
 
!insertmacro MUI_LANGUAGE "Spanish"

;--------------------------------
;Installer Sections

Section "PinoLauncher" SecMain

  SetOutPath "$INSTDIR"
  
  ;Archivos principales de la aplicación
  File "bin\Release\net9.0\win-x64\publish\*.*"
  
  ;Crear carpeta para datos de usuario si no existe
  CreateDirectory "$APPDATA\PinoLauncher"
  
  ;Store installation folder
  WriteRegStr HKCU "Software\PinoLauncher" "" $INSTDIR
  
  ;Create uninstaller
  WriteUninstaller "$INSTDIR\Uninstall.exe"
  
  ;Add to Programs and Features
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\PinoLauncher" \
                   "DisplayName" "PinoLauncher - Minecraft Launcher"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\PinoLauncher" \
                   "UninstallString" "$INSTDIR\Uninstall.exe"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\PinoLauncher" \
                   "DisplayIcon" "$INSTDIR\PinoLauncher.Desktop.exe"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\PinoLauncher" \
                   "Publisher" "PinoLauncher Team"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\PinoLauncher" \
                   "DisplayVersion" "0.1.1"
  WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\PinoLauncher" \
                     "NoModify" 1
  WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\PinoLauncher" \
                     "NoRepair" 1
  
  ;Start Menu Shortcuts
  !insertmacro MUI_STARTMENU_WRITE_BEGIN Application
    CreateDirectory "$SMPROGRAMS\$StartMenuFolder"
    CreateShortCut "$SMPROGRAMS\$StartMenuFolder\PinoLauncher.lnk" "$INSTDIR\Launcher.exe" "" "$INSTDIR\Launcher.exe" 0
    CreateShortCut "$SMPROGRAMS\$StartMenuFolder\Desinstalar PinoLauncher.lnk" "$INSTDIR\Uninstall.exe"
  !insertmacro MUI_STARTMENU_WRITE_END
  
  ;Desktop Shortcut
  CreateShortCut "$DESKTOP\Pino Launcher.lnk" "$INSTDIR\Launcher.exe" "" "$INSTDIR\Launcher.exe" 0

SectionEnd

;--------------------------------
;Descriptions

;Language strings
LangString DESC_SecMain ${LANG_SPANISH} "Archivos principales de PinoLauncher"

;Assign language strings to sections
!insertmacro MUI_FUNCTION_DESCRIPTION_BEGIN
  !insertmacro MUI_DESCRIPTION_TEXT ${SecMain} $(DESC_SecMain)
!insertmacro MUI_FUNCTION_DESCRIPTION_END

;--------------------------------
;Uninstaller Section

Section "Uninstall"

  ;Remove files
  Delete "$INSTDIR\*.*"
  RMDir /r "$INSTDIR"
  
  ;Remove Start Menu shortcuts
  !insertmacro MUI_STARTMENU_GETFOLDER Application $StartMenuFolder
  Delete "$SMPROGRAMS\$StartMenuFolder\PinoLauncher.lnk"
  Delete "$SMPROGRAMS\$StartMenuFolder\Desinstalar PinoLauncher.lnk"
  RMDir "$SMPROGRAMS\$StartMenuFolder"
  
  ;Remove Desktop shortcut
  Delete "$DESKTOP\PinoLauncher.lnk"
  
  ;Remove registry keys
  DeleteRegKey HKCU "Software\PinoLauncher"
  DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\PinoLauncher"
  
  ;Note: No eliminamos la carpeta de datos del usuario ($APPDATA\PinoLauncher) 
  ;para preservar configuraciones y datos del usuario

SectionEnd

;--------------------------------
;Functions

Function .onInit
  ;Check if application is already running
  System::Call 'kernel32::CreateMutex(i 0, i 0, t "PinoLauncher_Installer_Mutex") i .r1 ?e'
  Pop $R0
  
  StrCmp $R0 0 +3
    MessageBox MB_OK|MB_ICONEXCLAMATION "El instalador ya se está ejecutando."
    Abort
FunctionEnd