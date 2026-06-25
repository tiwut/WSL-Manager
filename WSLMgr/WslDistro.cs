using System;

namespace WSLMgr
{
    public class WslDistro
    {
        public string Name { get; set; } = string.Empty;
        public string State { get; set; } = "Stopped";
        public int WslVersion { get; set; } = 2;
        public bool IsDefault { get; set; }


        public string Guid { get; set; } = string.Empty;
        public string BasePath { get; set; } = string.Empty;
        public int DefaultUid { get; set; }

        public bool IsRunning => State.Equals("Running", StringComparison.OrdinalIgnoreCase);

        public string StatusColor => IsRunning ? "#10B981" : "#64748B";

        public string DisplayName => Name;

        public string DisplayVersion => $"WSL {WslVersion}";
    }

    public class WslOnlineDistro
    {
        public string Name { get; set; } = string.Empty;
        public string FriendlyName { get; set; } = string.Empty;
        public bool IsInstalled { get; set; }
        public string StatusText => IsInstalled ? "Installed" : "Install";
    }
}
