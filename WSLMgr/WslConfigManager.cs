using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;

namespace WSLMgr
{
    public class WslConfigManager
    {
        private static readonly string ConfigFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".wslconfig"
        );

        public string Memory { get; set; } = string.Empty;
        public string Processors { get; set; } = string.Empty;
        public string Swap { get; set; } = string.Empty;
        public bool LocalhostForwarding { get; set; } = true;
        public bool DnsTunneling { get; set; } = false;
        public bool GuiApplications { get; set; } = true;
        public bool NestedVirtualization { get; set; } = true;

        public static WslConfigManager Load()
        {
            var manager = new WslConfigManager();
            if (!File.Exists(ConfigFilePath))
            {
                return manager;
            }

            try
            {
                var lines = File.ReadAllLines(ConfigFilePath);
                string currentSection = string.Empty;

                foreach (var rawLine in lines)
                {
                    string line = rawLine.Trim();
                    if (string.IsNullOrEmpty(line) || line.StartsWith("#") || line.StartsWith(";"))
                    {
                        continue;
                    }

                    if (line.StartsWith("[") && line.EndsWith("]"))
                    {
                        currentSection = line.Substring(1, line.Length - 2).Trim().ToLower();
                        continue;
                    }

                    if (currentSection == "wsl2" || currentSection == "experimental")
                    {
                        var parts = line.Split('=', 2);
                        if (parts.Length == 2)
                        {
                            string key = parts[0].Trim().ToLower();
                            string val = parts[1].Trim();

                            switch (key)
                            {
                                case "memory":
                                    manager.Memory = val;
                                    break;
                                case "processors":
                                    manager.Processors = val;
                                    break;
                                case "swap":
                                    manager.Swap = val;
                                    break;
                                case "localhostforwarding":
                                    manager.LocalhostForwarding = ParseBool(val, true);
                                    break;
                                case "dnstunneling":
                                    manager.DnsTunneling = ParseBool(val, false);
                                    break;
                                case "guiapplications":
                                    manager.GuiApplications = ParseBool(val, true);
                                    break;
                                case "nestedvirtualization":
                                    manager.NestedVirtualization = ParseBool(val, true);
                                    break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load .wslconfig: {ex.Message}");
            }

            return manager;
        }

        private static bool ParseBool(string val, bool defaultValue)
        {
            if (bool.TryParse(val, out bool result))
                return result;

            val = val.ToLower();
            if (val == "true" || val == "1" || val == "yes") return true;
            if (val == "false" || val == "0" || val == "no") return false;

            return defaultValue;
        }

        public void Save()
        {
            try
            {
                List<string> fileLines = File.Exists(ConfigFilePath)
                    ? File.ReadAllLines(ConfigFilePath).ToList()
                    : new List<string>();


                UpdateOrAddSetting(fileLines, "wsl2", "memory", Memory);
                UpdateOrAddSetting(fileLines, "wsl2", "processors", Processors);
                UpdateOrAddSetting(fileLines, "wsl2", "swap", Swap);
                UpdateOrAddSetting(fileLines, "wsl2", "localhostForwarding", LocalhostForwarding.ToString().ToLower());
                UpdateOrAddSetting(fileLines, "wsl2", "guiApplications", GuiApplications.ToString().ToLower());
                UpdateOrAddSetting(fileLines, "wsl2", "nestedVirtualization", NestedVirtualization.ToString().ToLower());


                UpdateOrAddSetting(fileLines, "experimental", "dnsTunneling", DnsTunneling.ToString().ToLower());

                File.WriteAllLines(ConfigFilePath, fileLines, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to save .wslconfig: {ex.Message}", ex);
            }
        }

        private void UpdateOrAddSetting(List<string> lines, string targetSection, string key, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {

                RemoveSetting(lines, targetSection, key);
                return;
            }

            int sectionIndex = -1;
            int nextSectionIndex = -1;

            string targetSectionHeader = $"[{targetSection}]";

            for (int i = 0; i < lines.Count; i++)
            {
                string trimmed = lines[i].Trim();
                if (trimmed.Equals(targetSectionHeader, StringComparison.OrdinalIgnoreCase))
                {
                    sectionIndex = i;
                }
                else if (sectionIndex != -1 && trimmed.StartsWith("[") && trimmed.EndsWith("]"))
                {
                    nextSectionIndex = i;
                    break;
                }
            }

            if (sectionIndex == -1)
            {

                if (lines.Count > 0 && !string.IsNullOrWhiteSpace(lines.Last()))
                {
                    lines.Add("");
                }
                lines.Add(targetSectionHeader);
                lines.Add($"{key}={value}");
                return;
            }


            int searchEnd = nextSectionIndex != -1 ? nextSectionIndex : lines.Count;
            for (int i = sectionIndex + 1; i < searchEnd; i++)
            {
                string line = lines[i].Trim();
                if (line.StartsWith("#") || line.StartsWith(";"))
                {
                    continue;
                }

                var parts = line.Split('=', 2);
                if (parts.Length >= 1 && parts[0].Trim().Equals(key, StringComparison.OrdinalIgnoreCase))
                {

                    lines[i] = $"{key}={value}";
                    return;
                }
            }


            int insertIndex = nextSectionIndex != -1 ? nextSectionIndex : lines.Count;
            lines.Insert(insertIndex, $"{key}={value}");
        }

        private void RemoveSetting(List<string> lines, string targetSection, string key)
        {
            int sectionIndex = -1;
            int nextSectionIndex = -1;
            string targetSectionHeader = $"[{targetSection}]";

            for (int i = 0; i < lines.Count; i++)
            {
                string trimmed = lines[i].Trim();
                if (trimmed.Equals(targetSectionHeader, StringComparison.OrdinalIgnoreCase))
                {
                    sectionIndex = i;
                }
                else if (sectionIndex != -1 && trimmed.StartsWith("[") && trimmed.EndsWith("]"))
                {
                    nextSectionIndex = i;
                    break;
                }
            }

            if (sectionIndex == -1) return;

            int searchEnd = nextSectionIndex != -1 ? nextSectionIndex : lines.Count;
            for (int i = sectionIndex + 1; i < searchEnd; i++)
            {
                string line = lines[i].Trim();
                if (line.StartsWith("#") || line.StartsWith(";")) continue;

                var parts = line.Split('=', 2);
                if (parts.Length >= 1 && parts[0].Trim().Equals(key, StringComparison.OrdinalIgnoreCase))
                {
                    lines.RemoveAt(i);
                    return;
                }
            }
        }
    }
}
