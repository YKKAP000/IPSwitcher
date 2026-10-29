using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Net.NetworkInformation;
using System.Linq;

namespace IPSwitcher;

public partial class MainWindow : Window
{
    private readonly string _dataDirectory =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "IPSwitcher");
    private string ProfilesPath => Path.Combine(_dataDirectory, "profiles.json");
    private string ThemePath => Path.Combine(_dataDirectory, "theme.txt");
    private List<IpProfile> _profiles = new();

    public MainWindow()
    {
        InitializeComponent();
        Directory.CreateDirectory(_dataDirectory);
        ApplyTheme(LoadDarkThemePreference(), savePreference: false);
        LoadProfilesFromDisk();
        RefreshAdapters();
        UpdateDhcpFields();
    }

    private void RefreshAdapters_Click(object sender, RoutedEventArgs e) => RefreshAdapters();

    private void RefreshAdapters()
    {
        try
        {
            // 以前選択していたアダプター名を保持
            string? previousName =
                (AdapterCombo.SelectedItem as NetworkAdapter)?.Name;

            // .NET標準APIでネットワークアダプターを直接取得
            var items = NetworkInterface
                .GetAllNetworkInterfaces()
                .Where(n =>
                    n.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                    n.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                .OrderBy(n => n.Name)
                .Select(n => new NetworkAdapter
                {
                    Name = n.Name,
                    Status = n.OperationalStatus.ToString(),
                    InterfaceIndex = GetInterfaceIndex(n)
                })
                .ToList();

            AdapterCombo.ItemsSource = items;

            // 以前の選択を維持。存在しなければ先頭を選択
            if (items.Count > 0)
            {
                var selected = items.FirstOrDefault(
                    a => a.Name == previousName);

                AdapterCombo.SelectedItem = selected ?? items[0];
            }

            StatusText.Text =
                $"アダプターを {items.Count} 件取得しました。";
        }
        catch (Exception ex)
        {
            StatusText.Text = "アダプター取得エラー: " + ex.Message;

            MessageBox.Show(
                "ネットワークアダプターの取得に失敗しました。\n" +
                ex.Message,
                "アダプター取得エラー",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private static int GetInterfaceIndex(NetworkInterface adapter)
    {
        try
        {
            return adapter.GetIPProperties().GetIPv4Properties()?.Index ?? 0;
        }
        catch
        {
            return 0;
        }
    }

    private static void AddAdapter(JsonElement e, List<NetworkAdapter> list)
    {
        list.Add(new NetworkAdapter
        {
            Name = e.TryGetProperty("Name", out var n) ? n.ToString() : "",
            Status = e.TryGetProperty("Status", out var s) ? s.ToString() : "",
            InterfaceIndex = e.TryGetProperty("ifIndex", out var i) && int.TryParse(i.ToString(), out var index) ? index : 0
        });
    }

    private void DhcpCheck_Changed(object sender, RoutedEventArgs e) => UpdateDhcpFields();

    private void AdvancedToggle_Click(object sender, RoutedEventArgs e)
    {
        bool expand = AdvancedFieldsPanel.Visibility != Visibility.Visible;
        AdvancedFieldsPanel.Visibility = expand ? Visibility.Visible : Visibility.Collapsed;
        AdvancedToggleButton.Content = expand
            ? "− 詳細設定（ゲートウェイ・DNS）"
            : "＋ 詳細設定（ゲートウェイ・DNS）";
    }

    private void ThemeToggle_Click(object sender, RoutedEventArgs e) =>
        ApplyTheme(!LoadDarkThemePreference(), savePreference: true);

    private bool LoadDarkThemePreference()
    {
        try
        {
            return File.Exists(ThemePath) &&
                   string.Equals(File.ReadAllText(ThemePath).Trim(), "dark", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private void ApplyTheme(bool dark, bool savePreference)
    {
        SetBrush("WindowBackgroundBrush", dark ? "#FF171B22" : "#FFF5F7FA");
        SetBrush("SurfaceBrush", dark ? "#FF222832" : "#FFFFFFFF");
        SetBrush("TextBrush", dark ? "#FFF1F4F8" : "#FF202938");
        SetBrush("MutedTextBrush", dark ? "#FFB0BAC8" : "#FF687386");
        SetBrush("InputBrush", dark ? "#FF2B323E" : "#FFFFFFFF");
        SetBrush("InputBorderBrush", dark ? "#FF4A5565" : "#FFD1D8E0");
        SetBrush("ButtonBrush", dark ? "#FF343D4B" : "#FFE8EDF3");
        SetBrush("StatusBrush", dark ? "#FF252D38" : "#FFE9EEF4");
        ThemeToggleButton.Content = dark ? "☀ ライト" : "🌙 ダーク";
        if (savePreference)
        {
            try
            {
                Directory.CreateDirectory(_dataDirectory);
                File.WriteAllText(ThemePath, dark ? "dark" : "light");
            }
            catch (Exception ex)
            {
                StatusText.Text = "テーマ設定を保存できませんでした: " + ex.Message;
            }
        }
    }

    private void SetBrush(string key, string color) =>
        Resources[key] = new System.Windows.Media.SolidColorBrush(
            (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(color));

    private void UpdateDhcpFields()
    {
        bool dhcp = DhcpCheck?.IsChecked == true;
        if (IpBox == null) return;
        IpBox.IsEnabled = MaskBox.IsEnabled = GatewayBox.IsEnabled = !dhcp;
        // DNS can be specified manually even when IP is DHCP, so keep DNS fields enabled.
    }

    private void SaveProfile_Click(object sender, RoutedEventArgs e)
    {
        if (AdapterCombo.SelectedItem is not NetworkAdapter adapter)
        {
            MessageBox.Show("ネットワークアダプターを選択してください。");
            return;
        }
        string name = ProfileNameBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show("プロファイル名を入力してください。");
            return;
        }

        var profile = ReadForm(adapter.Name, name);
        _profiles.RemoveAll(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
        _profiles.Add(profile);
        SaveProfilesToDisk();
        RefreshProfileList(name);
        StatusText.Text = $"プロファイル「{name}」を保存しました。";
    }

    private IpProfile ReadForm(string adapterName, string name) => new()
    {
        Name = name,
        AdapterName = adapterName,
        UseDhcp = DhcpCheck.IsChecked == true,
        IpAddress = IpBox.Text.Trim(),
        SubnetMask = MaskBox.Text.Trim(),
        Gateway = GatewayBox.Text.Trim(),
        Dns1 = Dns1Box.Text.Trim(),
        Dns2 = Dns2Box.Text.Trim()
    };

    private void LoadProfile_Click(object sender, RoutedEventArgs e)
    {
        if (ProfilesList.SelectedItem is not IpProfile p) return;
        ProfileNameBox.Text = p.Name;
        DhcpCheck.IsChecked = p.UseDhcp;
        IpBox.Text = p.IpAddress;
        MaskBox.Text = p.SubnetMask;
        GatewayBox.Text = p.Gateway;
        Dns1Box.Text = p.Dns1;
        Dns2Box.Text = p.Dns2;

        if (AdapterCombo.ItemsSource is IEnumerable<NetworkAdapter> adapters)
            AdapterCombo.SelectedItem = adapters.FirstOrDefault(a => a.Name == p.AdapterName) ?? AdapterCombo.SelectedItem;

        StatusText.Text = $"プロファイル「{p.Name}」を入力欄に読み込みました。まだネットワーク設定は変更されていません。";
    }

    private void ProfilesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ProfilesList.SelectedItem is IpProfile p)
            ProfileNameBox.Text = p.Name;
    }

    private void DeleteProfile_Click(object sender, RoutedEventArgs e)
    {
        if (ProfilesList.SelectedItem is not IpProfile p) return;
        if (MessageBox.Show($"「{p.Name}」を削除しますか？", "確認", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;
        _profiles.Remove(p);
        SaveProfilesToDisk();
        RefreshProfileList();
        StatusText.Text = $"プロファイル「{p.Name}」を削除しました。";
    }

    private async void Apply_Click(object sender, RoutedEventArgs e)
    {
        if (AdapterCombo.SelectedItem is not NetworkAdapter adapter)
        {
            MessageBox.Show("ネットワークアダプターを選択してください。");
            return;
        }

        bool dhcp = DhcpCheck.IsChecked == true;
        if (!dhcp && (!TryParseIPv4(IpBox.Text, out _) || !TryParseIPv4(MaskBox.Text, out var maskAddress) ||
                      !TryMaskToPrefix(maskAddress, out _)))
        {
            MessageBox.Show("IPアドレスまたはサブネットマスクが正しくありません。");
            return;
        }

        if (!OptionalIPv4Valid(GatewayBox.Text) || !OptionalIPv4Valid(Dns1Box.Text) || !OptionalIPv4Valid(Dns2Box.Text))
        {
            MessageBox.Show("ゲートウェイまたはDNSのIPアドレスが正しくありません。");
            return;
        }

        string summary = dhcp
            ? $"アダプター: {adapter.Name}\nIPアドレス: DHCP（自動取得）\nDNS: {(string.IsNullOrWhiteSpace(Dns1Box.Text) ? "自動" : Dns1Box.Text)}"
            : $"アダプター: {adapter.Name}\nIP: {IpBox.Text}\nマスク: {MaskBox.Text}\nゲートウェイ: {(string.IsNullOrWhiteSpace(GatewayBox.Text) ? "なし" : GatewayBox.Text)}\nDNS: {(string.IsNullOrWhiteSpace(Dns1Box.Text) ? "自動/未指定" : Dns1Box.Text)}";

        if (MessageBox.Show(summary + "\n\nネットワーク通信が一時的に切断される場合があります。\n設定を反映しますか？",
            "ネットワーク設定の変更", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;

        try
        {
            IsEnabled = false;
            StatusText.Text = "設定を反映しています...";
            string script = BuildApplyScript(adapter.InterfaceIndex, dhcp, IpBox.Text.Trim(), MaskBox.Text.Trim(),
                GatewayBox.Text.Trim(), Dns1Box.Text.Trim(), Dns2Box.Text.Trim());
            string result = await Task.Run(() => RunPowerShell(script));
            StatusText.Text = "設定の反映が完了しました。";
            MessageBox.Show("設定変更コマンドが完了しました。Windowsの設定画面でも結果を確認してください。",
                "処理完了", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            StatusText.Text = "設定変更エラー: " + ex.Message;
            MessageBox.Show(ex.Message, "設定変更エラー", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsEnabled = true;
        }
    }

    private static string BuildApplyScript(int ifIndex, bool dhcp, string ip, string mask, string gateway, string dns1, string dns2)
    {
        // Inputs are validated as IPv4 values before they reach this method. Adapter selection is passed as numeric ifIndex.
        var sb = new StringBuilder();
        sb.AppendLine("$ErrorActionPreference = 'Stop'");
        sb.AppendLine($"$idx = {ifIndex}");
        sb.AppendLine("if (-not (Get-NetAdapter -InterfaceIndex $idx -ErrorAction SilentlyContinue)) { throw 'アダプターが見つかりません。' }");

        if (dhcp)
        {
            sb.AppendLine("Set-NetIPInterface -InterfaceIndex $idx -AddressFamily IPv4 -Dhcp Enabled");
            if (string.IsNullOrWhiteSpace(dns1) && string.IsNullOrWhiteSpace(dns2))
                sb.AppendLine("Set-DnsClientServerAddress -InterfaceIndex $idx -ResetServerAddresses");
            else
            {
                var dns = new[] { dns1, dns2 }.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
                string array = string.Join(",", dns.Select(s => $"'{s}'"));
                sb.AppendLine($"Set-DnsClientServerAddress -InterfaceIndex $idx -ServerAddresses @({array})");
            }
            sb.AppendLine("ipconfig /renew");
        }
        else
        {
            TryParseIPv4(mask, out var maskAddr);
            TryMaskToPrefix(maskAddr, out int prefix);
            sb.AppendLine("Set-NetIPInterface -InterfaceIndex $idx -AddressFamily IPv4 -Dhcp Disabled");
            // Remove existing non-APIPA IPv4 addresses on this adapter before assigning the new static address.
            sb.AppendLine("Get-NetIPAddress -InterfaceIndex $idx -AddressFamily IPv4 -ErrorAction SilentlyContinue | Where-Object { $_.IPAddress -notlike '169.254.*' -and $_.PrefixOrigin -ne 'WellKnown' } | Remove-NetIPAddress -Confirm:$false -ErrorAction SilentlyContinue");
            sb.AppendLine($"$ip = '{ip}'");
            sb.AppendLine($"$prefix = {prefix}");
            if (string.IsNullOrWhiteSpace(gateway))
                sb.AppendLine("New-NetIPAddress -InterfaceIndex $idx -IPAddress $ip -PrefixLength $prefix -ErrorAction Stop | Out-Null");
            else
                sb.AppendLine($"New-NetIPAddress -InterfaceIndex $idx -IPAddress $ip -PrefixLength $prefix -DefaultGateway '{gateway}' -ErrorAction Stop | Out-Null");
            if (string.IsNullOrWhiteSpace(dns1) && string.IsNullOrWhiteSpace(dns2))
                sb.AppendLine("Set-DnsClientServerAddress -InterfaceIndex $idx -ResetServerAddresses");
            else
            {
                var dns = new[] { dns1, dns2 }.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
                string array = string.Join(",", dns.Select(s => $"'{s}'"));
                sb.AppendLine($"Set-DnsClientServerAddress -InterfaceIndex $idx -ServerAddresses @({array})");
            }
        }
        sb.AppendLine("Get-NetIPConfiguration -InterfaceIndex $idx | Format-List InterfaceAlias,IPv4Address,IPv4DefaultGateway,DNSServer");
        return sb.ToString();
    }

    private static string RunPowerShell(string script)
    {
        string encoded = Convert.ToBase64String(
            Encoding.Unicode.GetBytes(script));

        var psi = new ProcessStartInfo
        {
            FileName = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.System),
                @"WindowsPowerShell\v1.0\powershell.exe"),

            Arguments = $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -EncodedCommand {encoded}",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException(
                "PowerShellを起動できませんでした。");

        // 標準出力と標準エラーを並行して読み取る
        Task<string> stdoutTask =
            process.StandardOutput.ReadToEndAsync();

        Task<string> stderrTask =
            process.StandardError.ReadToEndAsync();

        // 最大60秒でタイムアウト
        if (!process.WaitForExit(60_000))
        {
            try
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit();
            }
            catch
            {
                // 終了処理中の例外は無視
            }

            throw new TimeoutException(
                "PowerShellの処理が60秒以内に終了しませんでした。");
        }

        // 出力の読み取り完了を待つ
        Task.WaitAll(stdoutTask, stderrTask);

        string stdout = stdoutTask.Result;
        string stderr = stderrTask.Result;

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(stderr)
                    ? stdout
                    : stderr);
        }

        return "完了";
    }

    private static bool TryParseIPv4(string value, out IPAddress address)
    {
        if (IPAddress.TryParse(value, out var parsed) &&
            parsed.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            address = parsed;
            return true;
        }
        address = IPAddress.Any;
        return false;
    }

    private static bool OptionalIPv4Valid(string value) => string.IsNullOrWhiteSpace(value) || TryParseIPv4(value, out _);

    private static bool TryMaskToPrefix(IPAddress mask, out int prefix)
    {
        prefix = 0;
        bool zeroSeen = false;
        foreach (byte b in mask.GetAddressBytes())
        {
            for (int bit = 7; bit >= 0; bit--)
            {
                bool set = (b & (1 << bit)) != 0;
                if (set && zeroSeen) return false;
                if (set) prefix++;
                else zeroSeen = true;
            }
        }
        return true;
    }

    private void LoadProfilesFromDisk()
    {
        try
        {
            if (File.Exists(ProfilesPath))
                _profiles = JsonSerializer.Deserialize<List<IpProfile>>(File.ReadAllText(ProfilesPath)) ?? new();
        }
        catch (Exception ex)
        {
            MessageBox.Show("プロファイルを読み込めませんでした: " + ex.Message);
            _profiles = new();
        }
        RefreshProfileList();
    }

    private void SaveProfilesToDisk()
    {
        Directory.CreateDirectory(_dataDirectory);
        File.WriteAllText(ProfilesPath, JsonSerializer.Serialize(_profiles, new JsonSerializerOptions { WriteIndented = true }));
    }

    private void RefreshProfileList(string? selectName = null)
    {
        ProfilesList.ItemsSource = null;
        ProfilesList.ItemsSource = _profiles.OrderBy(p => p.Name).ToList();
        if (selectName != null)
            ProfilesList.SelectedItem = _profiles.FirstOrDefault(p => p.Name == selectName);
    }
}
