#!/bin/bash

#Variables Generales
MAC_UTILS="mac-utilities"
OUT_FOLDER="bin/Release/net8.0/publish/app_build"
INFO_PLIST="$MAC_UTILS/Info.plist"
ICON="$MAC_UTILS/launcher.icns"
SIGN_TOOL="$MAC_UTILS/rcodesign"
APP_NAME="Launcher"

#Variables de ARM64
ARM64_DIR="bin/Release/net8.0/publish/osx-arm64/."
ARM64_OUT="$OUT_FOLDER/arm64/$APP_NAME.app"

#Variables de x64
X64_DIR="bin/Release/net8.0/publish/osx-x64/."
X64_OUT="$OUT_FOLDER/x64/$APP_NAME.app"

#Variables de decoracion
ROJO='\e[0;31m'
VERDE='\e[0;32m'
AMARILLO='\e[1;33m'
NORMAL='\e[0m'

dotnet publish /p:PublishProfile=MacOS-Arm64

#Declarando la funcion para el firmado del paquete
sign_function () {
    $SIGN_TOOL sign $1
    if [ $(echo $?) == 0 ]
    then
        printf "${VERDE}"
        echo "El paquete $APP_NAME ha sido firmado exitosamente con una firma ADHOC"
        printf "${NORMAL}"
    else
        printf "${ROJO}"
        echo "Ha ocurrido una error al firmar el paquete $APP_NAME"
        printf "${NORMAL}"
    fi
}

result_function () {
    echo ""
    printf "${AMARILLO}"
    echo "RESULTADOS: "

    if [ -d "$1/Contents/_CodeSignature" ]
    then
        printf "${VERDE}"
    else
        printf "${ROJO}"
    fi

    echo "PAQUETE ARM64: $1"

    if [ -d "$2/Contents/_CodeSignature" ]
    then
        printf "${VERDE}"
    else
        printf "${ROJO}"
    fi

    echo "PAQUETE X64: $2"
    printf "${NORMAL}"
}

#Si no existe el folder de salida lo crea
if [ ! -d "$OUT_FOLDER" ]
then
    mkdir "$OUT_FOLDER"
fi

if [ $(echo $?) == 0 ]
then

    #Si ya hay un paquete anterior lo borra antes de proceder
    if [ -d "$ARM64_OUT" ]
    then
        rm -rf "$OUT_FOLDER/arm64"
    fi

    #Creacion de sus respectivos folders
    mkdir "$OUT_FOLDER/arm64"
    mkdir "$ARM64_OUT"
    mkdir "$ARM64_OUT/Contents"
    mkdir "$ARM64_OUT/Contents/MacOS"
    mkdir "$ARM64_OUT/Contents/Resources"

    #Uniendo todo en un solo paquete
    cp "$INFO_PLIST" "$ARM64_OUT/Contents/Info.plist"
    cp "$ICON" "$ARM64_OUT/Contents/Resources/$APP_NAME.icns"
    cp -a "$ARM64_DIR" "$ARM64_OUT/Contents/MacOS"
    find "$ARM64_OUT/Contents/MacOS" -name "*.pdb" -type f -delete

    if [ -d "$ARM64_OUT" ]
    then
        printf "${VERDE}"
        echo "La compilacion y el empaquetado de los archivos ARM64 ha sido un exito"
        printf "${NORMAL}"
        sign_function "$ARM64_OUT"
        printf "${AMARILLO}"
        echo "Paquete: $ARM64_OUT"
        printf "${NORMAL}"
    else
        printf "${ROJO}"
        echo "Ha ocurrido un error al empaquetar los archivos ARM64"
        printf "${NORMAL}"
    fi
else
    printf "${ROJO}"
    echo "La compilacion en ARM64 tuvo un error"
    printf "${NORMAL}"
fi

dotnet publish /p:PublishProfile=MacOS-X64

if [ $(echo $?) == 0 ]
then

    #Si ya hoy un paquete anterior lo borra antes de proceder
    if [ -d "$X64_OUT" ]
    then
        rm -rf "$OUT_FOLDER/x64"
    fi

    #Creacion de sus respectivos folders
    mkdir "$OUT_FOLDER/x64"
    mkdir "$X64_OUT"
    mkdir "$X64_OUT/Contents"
    mkdir "$X64_OUT/Contents/MacOS"
    mkdir "$X64_OUT/Contents/Resources"

    #Uniendo todo en un solo paquete
    cp "$INFO_PLIST" "$X64_OUT/Contents/Info.plist"
    cp "$ICON" "$X64_OUT/Contents/Resources/$APP_NAME.icns"
    cp -a "$X64_DIR" "$X64_OUT/Contents/MacOS"
    find "$X64_OUT/Contents/MacOS" -name "*.pdb" -type f -delete

    if [ -d "$X64_OUT" ]
    then
        printf "${VERDE}"
        echo "La compilacion y el empaquetado de los archivos X64 ha sido un exito"
        printf "${NORMAL}"
        sign_function "$X64_OUT"
        printf "${AMARILLO}"
        echo "Paquete: $X64_OUT"
        printf "${NORMAL}"
    else
        printf "${ROJO}"
        echo "Ha ocurrido un error al empaquetar los archivos x64"
        printf "${NORMAL}"
    fi
else
    printf "${ROJO}"
    echo "La compilacion X64 tuvo un error"
    printf "${NORMAL}"
fi

result_function "$ARM64_OUT" "$X64_OUT"
