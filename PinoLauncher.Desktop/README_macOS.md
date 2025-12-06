# PinoLauncher macOS Build Instructions

Este directorio contiene scripts para construir y distribuir PinoLauncher en macOS.

## Archivos incluidos

- `build_macos.sh` - Script principal de construcción y firma
- `setup_macos_signing.sh` - Asistente para configurar certificados de firma
- `README_macOS.md` - Este archivo de documentación

## Requisitos previos

### 1. Instalar dependencias

```bash
# .NET SDK (si no está instalado)
# Descargar desde: https://dotnet.microsoft.com/download

# rcodesign (herramienta de firma)
cargo install apple-codesign

# En macOS, también puedes usar Homebrew:
brew install rcodesign
```

### 2. Configurar certificados de firma (recomendado)

```bash
# Hacer ejecutables los scripts
chmod +x setup_macos_signing.sh
chmod +x build_macos.sh

# Ejecutar el configurador de certificados
./setup_macos_signing.sh
```

## Opciones de firma

### Opción 1: Certificado de desarrollador Apple (recomendado para distribución)
- Requiere cuenta de desarrollador Apple ($99/año)
- Permite distribución fuera del App Store
- Los usuarios no verán advertencias de seguridad

### Opción 2: Certificado auto-firmado
- Gratuito y rápido de configurar
- Solo para desarrollo y pruebas
- Los usuarios verán advertencias de seguridad

### Opción 3: Sin firma (ad-hoc)
- Sin configuración adicional
- Solo funciona en el sistema donde se compila
- No se puede distribuir

## Construcción paso a paso

### 1. Construcción básica
```bash
./build_macos.sh
```

### 2. Salida esperada
El script generará:
- `PinoLauncher.app` - Bundle de aplicación macOS
- `PinoLauncher_0.1.1_macOS.dmg` - Imagen de disco para distribución

## Estructura del bundle generado

```
PinoLauncher.app/
├── Contents/
│   ├── Info.plist          # Metadatos de la aplicación
│   ├── MacOS/
│   │   ├── PinoLauncher    # Ejecutable principal
│   │   └── [librerías]     # Dependencias .NET
│   └── Resources/
│       └── AppIcon.icns    # Icono de la aplicación
```

## Distribución

### Para usuarios finales:
- Distribuye el archivo `.dmg` generado
- Los usuarios simplemente arrastran la app a Applications

### Para desarrolladores:
- Comparte directamente el bundle `.app`
- O comprime el bundle: `tar -czf PinoLauncher.app.tar.gz PinoLauncher.app`

## Resolución de problemas

### Error: "rcodesign not found"
```bash
cargo install apple-codesign
```

### Error: "dotnet not found"
Instala .NET SDK desde: https://dotnet.microsoft.com/download

### Error de firma: "certificate not trusted"
- Usa un certificado de desarrollador Apple válido
- O ejecuta en el mismo Mac donde se compiló (ad-hoc)

### La aplicación no abre en otros Macs
- Verifica que esté firmada correctamente
- Los usuarios pueden necesitar ir a: Sistema → Privacidad y Seguridad → Permitir

## Automatización (CI/CD)

Para automatizar la construcción en GitHub Actions u otros sistemas CI:

```yaml
- name: Build macOS
  run: |
    chmod +x build_macos.sh
    ./build_macos.sh
  env:
    CERT_PASSWORD: ${{ secrets.CERT_PASSWORD }}
```

## Notas importantes

1. **Notarización**: Para distribución sin advertencias, considera notarizar la app con Apple
2. **Permisos**: La aplicación puede necesitar permisos específicos (red, archivos)
3. **Arquitecturas**: Actualmente construye para x64, considera arm64 para Macs Apple Silicon
4. **Dependencias**: Todas las librerías .NET se incluyen en el bundle

## Soporte

Si encuentras problemas:
1. Verifica que todos los prerequisitos están instalados
2. Ejecuta `./setup_macos_signing.sh` para reconfigurar certificados
3. Revisa los logs de construcción para errores específicos