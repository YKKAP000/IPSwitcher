# IP Switcher

A Windows desktop application for quickly switching IPv4 settings on network adapters. Switch between DHCP and static IP configuration, and save settings as profiles. The interface is available in Japanese and English.

日本語の説明: [README.md](README.md)

## Features

- List and refresh network adapters
- Configure IPv4 with DHCP or a static address
- Set subnet mask, default gateway, preferred DNS, and alternate DNS
- Save, load, and delete IP configuration profiles
- Switch between light and dark themes; the choice is remembered
- Switch between Japanese and English; the choice is remembered
- Collapse optional gateway and DNS fields

## Requirements

- Windows 10 or 11 (Windows 11 recommended)
- .NET 8 SDK, or Visual Studio with the .NET desktop development workload, to build
- Administrator privileges to apply network settings; Windows displays a permission prompt at startup

## Build

Run these commands from the project directory:

```powershell
dotnet restore .\IPSwitcher.csproj
dotnet build .\IPSwitcher.csproj -c Release
```

Alternatively, open `IPSwitcher.csproj` in Visual Studio and build the Release configuration.

## Publish for 64-bit Windows

Publish a self-contained single-file app. The target computer does not need a separate .NET runtime installation.

```powershell
dotnet publish .\IPSwitcher.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -o .\publish\win-x64
```

Zip the `publish\win-x64` folder for distribution. Users should extract the ZIP and run `IPSwitcher.exe`. Windows SmartScreen may show a warning for a new or unsigned app. For public distribution, consider code signing and providing a verifiable download source.

## Profiles and preferences

Profiles and theme/language preferences are stored per Windows user in:

```text
%LOCALAPPDATA%\IPSwitcher\
```

Profiles are stored in `profiles.json` and are not automatically shared with other PCs or Windows user accounts.

## Notes and current limitations

- The app configures IPv4 only. IPv6 and static routes are not supported.
- Applying a static IP removes existing non-APIPA IPv4 addresses from the selected adapter before assigning the new address. Additional static addresses are not preserved.
- The app does not check whether an entered IP address is already in use and does not automatically roll back a change.
- Changing an adapter used for a remote connection may disconnect that session.
- Test the app on a non-critical PC or spare network adapter first. If you are unsure which values to use, ask your network administrator.

## License

This project is licensed under the MIT License. When using, modifying, or redistributing the software, include the copyright notice and license text from [`LICENSE`](LICENSE).
