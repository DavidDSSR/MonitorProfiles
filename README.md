# Monitor Profiles

Aplicación de escritorio WPF para Windows 11 que detecta pantallas y cambia sus modos mediante perfiles locales.

## Requisitos

- Windows 11 x64.
- .NET 10 SDK para compilar o ejecutar desde el repositorio.

## Ejecutar desde el código

```powershell
dotnet run --project src/MonitorProfiles.App/MonitorProfiles.App.csproj
```

La primera vez, asigna los nombres Monitor 1–4 a las pantallas detectadas. Las pantallas encendidas pueden mostrar un marcador temporal con **Identificar**. Al guardar la asignación, se crean Trabajo, Competitivo e Historia. Los perfiles quedan guardados en `%LocalAppData%\MonitorProfileController\profiles.json` y la app permanece accesible desde la bandeja del sistema al cerrar la ventana.

Los perfiles guardados se aplican con un clic. Para probar cambios sin guardar, usa **Probar cambios**; la configuración previa se restaura automáticamente si no se confirma en 15 segundos.

## Pruebas

```powershell
dotnet test MonitorProfiles.sln
```

## Publicar para Windows x64

```powershell
dotnet publish src/MonitorProfiles.App/MonitorProfiles.App.csproj -c Release -r win-x64 --self-contained true
```

La salida publicable queda bajo `src/MonitorProfiles.App/bin/Release/net10.0-windows/win-x64/publish/`.

## Nota sobre modos de pantalla

La app enumera modos que Windows informa para las pantallas activas. Para una pantalla conectada pero apagada, Windows puede no exponer su catálogo completo; en ese caso el perfil se valida con `SetDisplayConfig` antes de aplicar. Si Windows rechaza una combinación, se muestra el error y se intenta restaurar el estado anterior.
