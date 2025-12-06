#!/bin/bash

# PinoLauncher macOS Build Script (Intel + Silicon)
# Requiere: dotnet SDK, rcodesign (cargo install apple-codesign)

set -e

echo "🍎 Compilando PinoLauncher para macOS..."
cd "$(dirname "$0")"

# Configuración
APP_NAME="PinoLauncher"
APP_VERSION="0.1.1"
BUNDLE_ID="com.mixstudios.pino"
APP_BUNDLE="${APP_NAME}.app"

# Colores para output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

print_step() {
    echo -e "${BLUE}[$1]${NC} $2"
}

print_success() {
    echo -e "${GREEN}✅${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}⚠️${NC} $1"
}

print_error() {
    echo -e "${RED}❌${NC} $1"
}

print_info() {
    echo -e "${CYAN}ℹ️${NC} $1"
}

# Función para seleccionar arquitectura
select_architecture() {
    echo -e "${CYAN}Selecciona la arquitectura de destino:${NC}"
    echo "1) Intel x64 (osx-x64) - Macs Intel"
    echo "2) Apple Silicon ARM64 (osx-arm64) - Macs M1/M2/M3"
    echo "3) Universal (ambas arquitecturas)"
    echo -n "Opción [1-3]: "
    read -r choice
    
    case $choice in
        1)
            ARCHITECTURES=("osx-x64")
            ARCH_NAMES=("Intel_x64")
            ;;
        2)
            ARCHITECTURES=("osx-arm64")
            ARCH_NAMES=("Silicon_ARM64")
            ;;
        3)
            ARCHITECTURES=("osx-x64" "osx-arm64")
            ARCH_NAMES=("Intel_x64" "Silicon_ARM64")
            ;;
        *)
            print_error "Opción inválida. Usando Intel x64 por defecto."
            ARCHITECTURES=("osx-x64")
            ARCH_NAMES=("Intel_x64")
            ;;
    esac
}

# Verificar dependencias
print_step "INIT" "Verificando dependencias..."

if ! command -v dotnet &> /dev/null; then
    print_error "dotnet SDK no encontrado. Instala desde: https://dotnet.microsoft.com/"
    exit 1
fi

if ! command -v rcodesign &> /dev/null; then
    print_error "rcodesign no encontrado. Instala con: cargo install apple-codesign"
    exit 1
fi

print_success "Dependencias verificadas"

# Seleccionar arquitectura
select_architecture

# Limpiar compilaciones anteriores
print_step "CLEAN" "Limpiando compilaciones anteriores..."
dotnet clean --configuration Release
rm -rf bin/Release/net9.0/osx-*
rm -rf *.app
rm -rf *.dmg
print_success "Limpieza completada"

# Compilar para cada arquitectura
for i in "${!ARCHITECTURES[@]}"; do
    ARCH="${ARCHITECTURES[$i]}"
    ARCH_NAME="${ARCH_NAMES[$i]}"
    OUTPUT_DIR="bin/Release/net9.0/$ARCH/publish"
    
    print_step "BUILD-$ARCH" "Compilando para $ARCH ($ARCH_NAME)..."
    
    dotnet publish \
        --configuration Release \
        --runtime "$ARCH" \
        --self-contained true \
        /p:PublishSingleFile=true \
        /p:PublishReadyToRun=false \
        /p:EnableCompressionInSingleFile=true \
        --output "$OUTPUT_DIR"
    
    if [ ! -f "$OUTPUT_DIR/PinoLauncher.Desktop" ]; then
        print_error "Error: No se pudo generar el ejecutable para $ARCH"
        continue
    fi
    
    print_success "Compilación $ARCH completada"
    
    # Crear bundle para esta arquitectura
    if [ ${#ARCHITECTURES[@]} -eq 1 ]; then
        CURRENT_APP_BUNDLE="$APP_BUNDLE"
    else
        CURRENT_APP_BUNDLE="${APP_NAME}_${ARCH_NAME}.app"
    fi
    
    print_step "BUNDLE-$ARCH" "Creando bundle: $CURRENT_APP_BUNDLE..."
    
    mkdir -p "$CURRENT_APP_BUNDLE/Contents/MacOS"
    mkdir -p "$CURRENT_APP_BUNDLE/Contents/Resources"
    
    # Copiar ejecutable
    cp "$OUTPUT_DIR/PinoLauncher.Desktop" "$CURRENT_APP_BUNDLE/Contents/MacOS/$APP_NAME"
    chmod +x "$CURRENT_APP_BUNDLE/Contents/MacOS/$APP_NAME"
    
    # Copiar librerías necesarias
    find "$OUTPUT_DIR" -name "*.dylib" -exec cp {} "$CURRENT_APP_BUNDLE/Contents/MacOS/" \; 2>/dev/null || true
    
    # Copiar icono si existe
    if [ -f "pino.ico" ]; then
        if command -v sips &> /dev/null; then
            sips -s format icns "pino.ico" --out "$CURRENT_APP_BUNDLE/Contents/Resources/AppIcon.icns" 2>/dev/null || {
                cp "pino.ico" "$CURRENT_APP_BUNDLE/Contents/Resources/AppIcon.ico"
            }
        else
            cp "pino.ico" "$CURRENT_APP_BUNDLE/Contents/Resources/AppIcon.ico"
        fi
    fi
    
    # Crear Info.plist
    cat > "$CURRENT_APP_BUNDLE/Contents/Info.plist" << EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleExecutable</key>
    <string>$APP_NAME</string>
    <key>CFBundleIdentifier</key>
    <string>$BUNDLE_ID</string>
    <key>CFBundleName</key>
    <string>$APP_NAME</string>
    <key>CFBundleDisplayName</key>
    <string>PinoLauncher - Minecraft Launcher</string>
    <key>CFBundleVersion</key>
    <string>$APP_VERSION</string>
    <key>CFBundleShortVersionString</key>
    <string>$APP_VERSION</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
    <key>CFBundleSignature</key>
    <string>PINO</string>
    <key>CFBundleInfoDictionaryVersion</key>
    <string>6.0</string>
    <key>LSMinimumSystemVersion</key>
    <string>11.0</string>
    <key>LSRequiresNativeExecution</key>
    <true/>
    <key>NSHighResolutionCapable</key>
    <true/>
    <key>NSSupportsAutomaticGraphicsSwitching</key>
    <true/>
    <key>CFBundleIconFile</key>
    <string>AppIcon</string>
    <key>NSHumanReadableCopyright</key>
    <string>© 2025 Mix Dev Studio</string>
    <key>NSApplicationCategoryType</key>
    <string>public.app-category.games</string>
</dict>
</plist>
EOF
    
    print_success "Bundle $ARCH creado: $CURRENT_APP_BUNDLE"
    
    # Firmar aplicación
    print_step "SIGN-$ARCH" "Firmando $CURRENT_APP_BUNDLE..."
    
    # Buscar certificados
    CERT_FILE=""
    for cert in "developer.p12" "certificate.p12" "signing.p12"; do
        if [ -f "$cert" ]; then
            CERT_FILE="$cert"
            break
        fi
    done
    
    if [ -n "$CERT_FILE" ] && [ "$i" -eq 0 ]; then
        echo -n "Introduce la contraseña del certificado (Enter para omitir): "
        read -s CERT_PASSWORD
        echo
    fi
    
    if [ -n "$CERT_FILE" ] && [ -n "$CERT_PASSWORD" ]; then
        rcodesign sign \
            --p12-file "$CERT_FILE" \
            --p12-password "$CERT_PASSWORD" \
            "$CURRENT_APP_BUNDLE"
        print_success "Aplicación $ARCH firmada con certificado"
    else
        rcodesign sign --ad-hoc "$CURRENT_APP_BUNDLE" || {
            print_warning "No se pudo firmar $CURRENT_APP_BUNDLE"
        }
        if [ "$i" -eq 0 ]; then
            print_warning "Usando firma ad-hoc (solo para desarrollo)"
        fi
    fi
    
    # Crear DMG individual
    DMG_NAME="${APP_NAME}_${APP_VERSION}_macOS_${ARCH_NAME}.dmg"
    print_step "DMG-$ARCH" "Creando $DMG_NAME..."
    
    DMG_TEMP_DIR="dmg_temp_$ARCH"
    mkdir -p "$DMG_TEMP_DIR"
    
    cp -R "$CURRENT_APP_BUNDLE" "$DMG_TEMP_DIR/"
    ln -sf /Applications "$DMG_TEMP_DIR/Applications"
    
    # Crear README específico para la arquitectura
    cat > "$DMG_TEMP_DIR/README.txt" << EOF
PinoLauncher v$APP_VERSION para macOS ($ARCH_NAME)

ARQUITECTURA:
$([ "$ARCH" == "osx-x64" ] && echo "• Intel x64 - Compatible con Macs Intel" || echo "• Apple Silicon ARM64 - Compatible con Macs M1/M2/M3/M4")

INSTALACIÓN:
1. Arrastra PinoLauncher.app a Applications
2. Abre desde Applications → PinoLauncher
3. Si aparece mensaje de seguridad:
   Sistema → Privacidad y Seguridad → Permitir

REQUISITOS:
$([ "$ARCH" == "osx-arm64" ] && echo "• macOS 11.0 Big Sur o superior (Apple Silicon)" || echo "• macOS 10.15 Catalina o superior (Intel)")
• Acceso a Internet

© 2025 PinoLauncher Team
EOF
    
    if command -v hdiutil &> /dev/null; then
        hdiutil create -volname "PinoLauncher $APP_VERSION ($ARCH_NAME)" \
                       -srcfolder "$DMG_TEMP_DIR" \
                       -ov -format UDZO \
                       "$DMG_NAME"
        print_success "DMG $ARCH creado: $DMG_NAME"
    else
        print_warning "No se puede crear DMG para $ARCH (requiere hdiutil)"
    fi
    
    rm -rf "$DMG_TEMP_DIR"
done

# Crear DMG universal si se compilaron ambas arquitecturas
if [ ${#ARCHITECTURES[@]} -eq 2 ]; then
    print_step "UNIVERSAL" "Creando DMG universal..."
    
    UNIVERSAL_DMG="${APP_NAME}_${APP_VERSION}_macOS_Universal.dmg"
    UNIVERSAL_TEMP_DIR="dmg_universal_temp"
    
    mkdir -p "$UNIVERSAL_TEMP_DIR"
    
    # Copiar ambos bundles
    cp -R "${APP_NAME}_Intel_x64.app" "$UNIVERSAL_TEMP_DIR/"
    cp -R "${APP_NAME}_Silicon_ARM64.app" "$UNIVERSAL_TEMP_DIR/"
    
    ln -sf /Applications "$UNIVERSAL_TEMP_DIR/Applications"
    
    cat > "$UNIVERSAL_TEMP_DIR/README.txt" << EOF
PinoLauncher v$APP_VERSION - Paquete Universal para macOS

INCLUYE:
• PinoLauncher_Intel_x64.app - Para Macs Intel
• PinoLauncher_Silicon_ARM64.app - Para Macs Apple Silicon (M1/M2/M3/M4)

INSTALACIÓN:
1. Elige la versión correcta para tu Mac:
   - Intel x64: Macs anteriores a 2020
   - Silicon ARM64: Macs con chip M1/M2/M3/M4
2. Arrastra la app correspondiente a Applications
3. Elimina la otra versión si no la necesitas

Si no sabes qué versión usar:
Apple Menu → About This Mac → Chip

© 2025 PinoLauncher Team
EOF
    
    if command -v hdiutil &> /dev/null; then
        hdiutil create -volname "PinoLauncher $APP_VERSION Universal" \
                       -srcfolder "$UNIVERSAL_TEMP_DIR" \
                       -ov -format UDZO \
                       "$UNIVERSAL_DMG"
        print_success "DMG Universal creado: $UNIVERSAL_DMG"
    fi
    
    rm -rf "$UNIVERSAL_TEMP_DIR"
fi

print_success "¡Compilación para macOS completada!"
echo
echo "📦 Archivos generados:"
for i in "${!ARCHITECTURES[@]}"; do
    ARCH="${ARCHITECTURES[$i]}"
    ARCH_NAME="${ARCH_NAMES[$i]}"
    if [ ${#ARCHITECTURES[@]} -eq 1 ]; then
        echo "  • $APP_BUNDLE ($ARCH_NAME)"
        echo "  • ${APP_NAME}_${APP_VERSION}_macOS_${ARCH_NAME}.dmg"
    else
        echo "  • ${APP_NAME}_${ARCH_NAME}.app ($ARCH_NAME)"
        echo "  • ${APP_NAME}_${APP_VERSION}_macOS_${ARCH_NAME}.dmg"
    fi
done

if [ ${#ARCHITECTURES[@]} -eq 2 ]; then
    echo "  • ${APP_NAME}_${APP_VERSION}_macOS_Universal.dmg (Ambas arquitecturas)"
fi

echo
echo "🚀 Información de compatibilidad:"
echo "  • Intel x64: Todos los Macs (incluye emulación Rosetta en Silicon)"
echo "  • Silicon ARM64: Solo Macs M1/M2/M3/M4 (rendimiento nativo optimizado)"
echo "  • Universal: Incluye ambas versiones para máxima compatibilidad"
echo
print_success "Construcción macOS completada"