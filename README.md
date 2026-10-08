# Monitor Profiles

[English](#english) · [Español](#español)

## English

**Download the latest Windows 11 x64 release:**
[MonitorProfiles-win-x64.zip](https://github.com/DavidDSSR/MonitorProfiles/releases/latest/download/MonitorProfiles-win-x64.zip)

Monitor Profiles switches connected screens between saved profiles. It is a native WPF app for Windows 11, with one-click profile switching, a system-tray menu, and a safe preview/revert flow for custom changes.

### Run the app

1. Download and extract `MonitorProfiles-win-x64.zip`.
2. Run `MonitorProfiles.App.exe` from the extracted folder. The ZIP is self-contained; a separate .NET installation is not required.
3. On first launch, identify and assign Monitor 1–4 to your connected screens, then choose **Save and create profiles**.
4. Apply Work, Competitive, or Story with one click. Create custom profiles and use **Preview changes** to test them; the prior display setup returns automatically if you do not confirm within 15 seconds.

The app supports English, Spanish, French, Italian, Japanese, German, and Simplified Chinese, plus System, Light, and Dark appearance. Profiles and preferences are stored locally under `%LocalAppData%\MonitorProfileController\`.

### Build from source

Requirements: Windows 11 x64 and the .NET 10 SDK.

```powershell
dotnet test MonitorProfiles.sln
dotnet run --project src/MonitorProfiles.App/MonitorProfiles.App.csproj
dotnet publish src/MonitorProfiles.App/MonitorProfiles.App.csproj -c Release -r win-x64 --self-contained true
```

### Source and license

The source is available in this repository's `main` branch. You can fork or clone it and make your own changes; the project is licensed under [MIT](LICENSE).

## Español

**Descarga la versión más reciente para Windows 11 x64:**
[MonitorProfiles-win-x64.zip](https://github.com/DavidDSSR/MonitorProfiles/releases/latest/download/MonitorProfiles-win-x64.zip)

Monitor Profiles cambia las pantallas conectadas entre perfiles guardados. Es una aplicación WPF nativa para Windows 11, con aplicación de perfiles en un toque, menú en la bandeja del sistema y una vista previa segura con reversión.

### Ejecutar la aplicación

1. Descarga y extrae `MonitorProfiles-win-x64.zip`.
2. Ejecuta `MonitorProfiles.App.exe` desde la carpeta extraída. El ZIP es autocontenido; no necesitas instalar .NET por separado.
3. En el primer inicio, identifica y asigna Monitor 1–4 a las pantallas conectadas y pulsa **Guardar y crear perfiles**.
4. Aplica Trabajo, Competitivo o Historia con un toque. Puedes crear perfiles personalizados y usar **Probar cambios**; la configuración anterior se restaura automáticamente si no confirmas en 15 segundos.

La app ofrece inglés, español, francés, italiano, japonés, alemán y chino simplificado, además de los temas Sistema, Claro y Oscuro. Los perfiles y las preferencias se guardan localmente en `%LocalAppData%\MonitorProfileController\`.

### Compilar desde el código

Requisitos: Windows 11 x64 y .NET 10 SDK.

```powershell
dotnet test MonitorProfiles.sln
dotnet run --project src/MonitorProfiles.App/MonitorProfiles.App.csproj
dotnet publish src/MonitorProfiles.App/MonitorProfiles.App.csproj -c Release -r win-x64 --self-contained true
```

### Código fuente y licencia

El código está disponible en la rama `main` de este repositorio. Puedes crear un fork o clonarlo para hacer tus propios cambios; el proyecto utiliza la licencia [MIT](LICENSE).
