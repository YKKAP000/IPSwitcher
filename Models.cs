namespace IPSwitcher;

public sealed class NetworkAdapter
{
    public string Name { get; set; } = "";
    public string Status { get; set; } = "";
    public int InterfaceIndex { get; set; }
    public override string ToString() => $"{Name} (ifIndex {InterfaceIndex}) - {Status}";
}

public sealed class IpProfile
{
    public string Name { get; set; } = "";
    public string AdapterName { get; set; } = "";
    public bool UseDhcp { get; set; }
    public string IpAddress { get; set; } = "";
    public string SubnetMask { get; set; } = "255.255.255.0";
    public string Gateway { get; set; } = "";
    public string Dns1 { get; set; } = "";
    public string Dns2 { get; set; } = "";
}
