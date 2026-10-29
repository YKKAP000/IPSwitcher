# IP Switcher

Windowsのネットワークアダプターに対して、IPv4設定を簡単に切り替えるためのデスクトップアプリです。DHCPと固定IPを切り替え、設定をプロファイルとして保存できます。画面表示は日本語と英語を選択できます。

English documentation: [README.en.md](README.en.md)

## 主な機能

- ネットワークアダプターの一覧表示と更新
- IPv4のDHCP／固定IP設定
- サブネットマスク、デフォルトゲートウェイ、優先DNS、代替DNSの設定
- IP設定プロファイルの保存、読み込み、削除
- ライト／ダークテーマの切り替え（選択したテーマを保存）
- 日本語／英語の切り替え（選択した言語を保存）
- ゲートウェイとDNSの詳細欄を折りたたみ

## 動作環境

- Windows 10／11（Windows 11を推奨）
- 開発・ビルドには .NET 8 SDK、または対応するVisual Studioと「.NETデスクトップ開発」ワークロード
- IP設定の変更には管理者権限が必要です。起動時にWindowsの確認画面が表示されます。

## ビルド

リポジトリのプロジェクトフォルダーで実行します。

```powershell
dotnet restore .\IPSwitcher.csproj
dotnet build .\IPSwitcher.csproj -c Release
```

またはVisual Studioで `IPSwitcher.csproj` を開き、Release構成でビルドします。

## Windows 64bit向けに公開

.NETランタイムを含めた単一ファイルとして発行できます。配布先のPCに.NETランタイムを別途インストールする必要はありません。

```powershell
dotnet publish .\IPSwitcher.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -o .\publish\win-x64
```

発行された `publish\win-x64` フォルダーをZIPにして配布します。利用者はZIPを展開し、`IPSwitcher.exe` を実行してください。初回起動時にSmartScreenの警告が表示される場合があります。公開配布では、コード署名や利用者が確認できる配布元の用意を検討してください。

## プロファイルとテーマ設定

プロファイルとテーマ設定は、Windowsユーザーごとに次のフォルダーへ保存されます。

```text
%LOCALAPPDATA%\IPSwitcher\
```

プロファイルはJSON形式の `profiles.json` に保存されます。別のPCやWindowsユーザーとは自動的に共有されません。

## 注意事項と現在の制限

- このアプリはIPv4の設定を扱います。IPv6や静的ルートの設定には対応していません。
- 固定IPを適用すると、選択したアダプターにある既存の非APIPA IPv4アドレスを削除してから新しいアドレスを設定します。複数の固定IPを併用している環境では、既存の追加アドレスが保持されません。
- 入力したIPアドレスがネットワーク内で重複していないかの事前確認や、自動ロールバックには対応していません。
- リモート接続に使用中のアダプターを変更すると、接続が切れることがあります。
- まず影響のないPCや予備のネットワークアダプターで動作を確認し、設定値が不明な場合はネットワーク管理者に確認してください。

## ライセンス

このリポジトリには、現時点で `LICENSE` ファイルが含まれていません。ライセンスが追加されるまでは、ソースコードの再利用・改変・再配布の許可条件は定められていません。
