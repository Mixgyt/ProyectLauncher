#!/bin/bash

# Script para preparar certificados de firma para macOS
# Ayuda a configurar rcodesign con certificados de desarrollo

set -e

echo "🔐 Configurador de Certificados para PinoLauncher macOS"
echo "=================================================="
echo

# Colores
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
RED='\033[0;31m'
NC='\033[0m'

print_info() {
    echo -e "${BLUE}ℹ️${NC} $1"
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

# Verificar rcodesign
if ! command -v rcodesign &> /dev/null; then
    print_error "rcodesign no encontrado"
    echo "Instala con: cargo install apple-codesign"
    echo "O descarga desde: https://github.com/indygreg/apple-platform-rs"
    exit 1
fi

print_success "rcodesign encontrado"
echo

echo "Opciones de firma disponibles:"
echo "1. Certificado de desarrollador Apple (.p12)"
echo "2. Clave privada personalizada (.pem)"
echo "3. Generar certificado auto-firmado para desarrollo"
echo "4. Solo firma ad-hoc (sin certificado)"
echo

read -p "Selecciona una opción (1-4): " choice

case $choice in
    1)
        echo
        print_info "Opción 1: Certificado de desarrollador Apple"
        echo "Necesitas un certificado .p12 exportado desde Keychain Access"
        echo "o desde tu cuenta de desarrollador de Apple"
        echo
        
        read -p "Ruta al archivo .p12: " p12_path
        if [ ! -f "$p12_path" ]; then
            print_error "Archivo no encontrado: $p12_path"
            exit 1
        fi
        
        # Copiar certificado al directorio del proyecto
        cp "$p12_path" "developer.p12"
        print_success "Certificado copiado como developer.p12"
        
        echo
        print_info "Para usar este certificado, ejecuta:"
        echo "./build_macos.sh"
        echo "Se te pedirá la contraseña del certificado durante la firma"
        ;;
        
    2)
        echo
        print_info "Opción 2: Clave privada personalizada"
        echo "Puedes usar una clave RSA existente o generar una nueva"
        echo
        
        read -p "¿Generar nueva clave privada? (y/n): " gen_key
        
        if [[ $gen_key == "y" || $gen_key == "Y" ]]; then
            # Generar nueva clave privada
            openssl genrsa -out signing.key 2048
            print_success "Clave privada generada: signing.key"
            
            # Generar certificado auto-firmado
            openssl req -new -x509 -key signing.key -out signing.crt -days 365 -subj "/CN=PinoLauncher Developer/O=PinoLauncher Team/C=US"
            print_success "Certificado auto-firmado generado: signing.crt"
            
        else
            read -p "Ruta a la clave privada (.pem o .key): " key_path
            if [ ! -f "$key_path" ]; then
                print_error "Archivo no encontrado: $key_path"
                exit 1
            fi
            
            cp "$key_path" "signing.key"
            print_success "Clave privada copiada como signing.key"
        fi
        
        echo
        print_info "Para usar esta clave, ejecuta:"
        echo "./build_macos.sh"
        ;;
        
    3)
        echo
        print_info "Opción 3: Generar certificado auto-firmado completo"
        
        # Generar clave privada
        openssl genrsa -out development.key 2048
        
        # Información del certificado
        read -p "Nombre del desarrollador [PinoLauncher Developer]: " dev_name
        dev_name=${dev_name:-"PinoLauncher Developer"}
        
        read -p "Organización [PinoLauncher Team]: " org_name
        org_name=${org_name:-"PinoLauncher Team"}
        
        # Generar certificado
        openssl req -new -x509 -key development.key -out development.crt -days 365 \
            -subj "/CN=$dev_name/O=$org_name/C=US"
        
        # Crear archivo P12
        openssl pkcs12 -export -out development.p12 -inkey development.key -in development.crt -name "PinoLauncher Development"
        
        print_success "Certificado auto-firmado generado:"
        echo "  • development.key (clave privada)"
        echo "  • development.crt (certificado)"
        echo "  • development.p12 (bundle PKCS#12)"
        
        # Renombrar para uso automático
        cp development.p12 developer.p12
        
        echo
        print_warning "Este certificado es solo para desarrollo local"
        print_warning "No será confiable en otros sistemas macOS"
        ;;
        
    4)
        echo
        print_info "Opción 4: Solo firma ad-hoc"
        print_warning "La aplicación solo funcionará en el sistema donde se compile"
        print_warning "No se podrá distribuir a otros usuarios"
        
        # Crear archivo marcador
        touch .adhoc-only
        
        print_success "Configurado para firma ad-hoc únicamente"
        ;;
        
    *)
        print_error "Opción inválida"
        exit 1
        ;;
esac

echo
echo "🛠️  Configuración completada"
echo
echo "Próximos pasos:"
echo "1. Ejecuta: chmod +x build_macos.sh"
echo "2. Ejecuta: ./build_macos.sh"
echo
print_info "El script build_macos.sh detectará automáticamente tu configuración"