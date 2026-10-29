# IP Switcher (MVP)

Windows desktop application using C# / WPF / .NET 8.0. It stores profiles locally as JSON and applies IPv4 settings through Windows PowerShell NetTCPIP cmdlets.

## Important limitations of this starter

- This is a starter/MVP. Test only on a non-critical PC and a spare network adapter first.
- Static IP application removes existing non-APIPA IPv4 addresses from the selected adapter before adding the new address. Multiple static addresses on the same adapter are therefore not preserved.
- It does not yet implement automatic rollback, IPv6 configuration, static route management, or verification that the entered IP is unused.
- Changing an adapter that carries remote access can disconnect the session.
- The app requests administrator privileges through `app.manifest`.
- The PowerShell script uses `-ExecutionPolicy Bypass` only for this child process; it does not change the machine-wide execution policy.
- Profile data is stored per Windows user at `%LOCALAPPDATA%\IPSwitcher\profiles.json`; profiles are not automatically shared between PCs.

## Requirements

- Visual Studio 2022 with the ".NET desktop development" workload, or .NET 8 SDK.
- Windows 11 recommended. Windows 10 22H2 support ended on 2025-10-14; use only where your organization's support policy permits it.

## Build

Open `IPSwitcher.csproj` in Visual Studio and build `Release`, or run:

```powershell
dotnet restore
dotnet build -c Release
```

## Publish a single-file x64 executable

```powershell
dotnet publish .\IPSwitcher.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

Output is under `bin\Release\net8.0-windows\win-x64\publish\`. Copy the published `IPSwitcher.exe` to test PCs. The self-contained build includes the .NET runtime, so the target PC does not need a separate .NET runtime installation.

## Before deployment

1. Test DHCP -> static -> DHCP on a spare adapter.
2. Confirm static IP, subnet, gateway and DNS values with your network administrator.
3. Test on every Windows build and adapter type in your deployment scope.
4. Sign the executable with your organization's code-signing certificate if available.
5. Consider packaging with MSIX or an MSI installer for managed deployment.
