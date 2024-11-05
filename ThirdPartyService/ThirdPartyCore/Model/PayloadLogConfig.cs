using System;

namespace ThirdPartyCore.Model;

public class PayloadLogConfig
{
    public bool Enabled { get; set; } = false;
    public HashSet<string> ExcludedActions  { get; set; } = new HashSet<string>();
}
