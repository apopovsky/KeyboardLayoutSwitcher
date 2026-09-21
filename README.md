# Keyboard Layout Switcher

Pequeña utilidad para Windows que asigna un layout de teclado a cada teclado físico.

Aplicación experimental para Windows. Permite asignar un layout de Windows a cada teclado físico y solicita el cambio del layout de la ventana activa cuando detecta una pulsación proveniente de ese dispositivo.

## Uso

1. Ejecutar `KeyboardLayoutSwitcher.exe`.
2. Seleccionar un teclado físico en la lista de la izquierda. Se muestra su nombre PnP, cuando Windows lo informa, y su identificador estable.
   También se puede escribir en el área `Probar e identificar teclado`: la interfaz Raw Input que generó la tecla se selecciona y queda marcada como `Escribiendo ahora`.
3. Seleccionar el layout instalado en Windows en la lista de la derecha. Cada fila muestra el idioma, el nombre del teclado y su ID.
4. Presionar `Guardar` para crear el mapping. Un mapping guardado siempre queda activo.
5. Usar `Actualizar` para cambiar su layout o `Borrar` para eliminarlo.
6. Activar `Iniciar con Windows` desde la ventana o desde el menú del tray para que arranque automáticamente para el usuario actual.

`Play` y `Pause` controlan globalmente si la aplicación aplica todos los mappings. El mismo control está disponible en el menú del icono del tray y se conserva al reiniciar. El layout asignado aparece marcado como mapeado al volver a seleccionar ese teclado. La configuración se guarda en:

`%LOCALAPPDATA%\KeyboardLayoutSwitcher\config.json`

Si la aplicación no llega a abrir, el detalle de arranque se guarda en:

`%LOCALAPPDATA%\KeyboardLayoutSwitcher\startup-errors.log`

La aplicación no instala ningún driver. El inicio automático se registra únicamente para el usuario actual en `HKCU`, por lo que no requiere permisos de administrador. El identificador completo del dispositivo se utiliza como clave para que la asignación sobreviva a reinicios y reconexiones normales.
Si se mueve el ejecutable a otra carpeta, hay que desactivar y volver a activar `Iniciar con Windows` para actualizar la ruta registrada.

## Limitación conocida de esta primera prueba

El cambio se solicita al detectar la primera tecla del teclado seleccionado. Hay que verificar en la prueba real si Windows procesa esa primera tecla antes o después del cambio de layout; si alguna vez aparece un carácter con el layout anterior, la siguiente iteración deberá interceptar y reenviar esa primera pulsación.

## Idiomas

La interfaz usa el idioma instalado del sistema operativo. Incluye inglés, español, francés, alemán, italiano, portugués, japonés, coreano, chino simplificado/tradicional y ruso; si el idioma no está disponible, usa inglés.

## Compilar

Requiere Windows y .NET 8 SDK.

    dotnet build KeyboardLayoutSwitcher.csproj --configuration Release

Para generar un ejecutable autónomo de 64 bits:

    dotnet publish KeyboardLayoutSwitcher.csproj --configuration Release --runtime win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true --output publish

El ejecutable publicado queda en publish/KeyboardLayoutSwitcher.exe.

## Licencia

MIT. Ver LICENSE.
