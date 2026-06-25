using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace WSLMgr
{
    public static class WslBridge
    {
        private const string RegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Lxss";




        public static List<WslDistro> GetInstalledDistros()
        {
            var distros = new List<WslDistro>();
            string defaultGuid = string.Empty;


            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RegistryPath))
                {
                    if (key != null)
                    {
                        defaultGuid = key.GetValue("DefaultDistribution")?.ToString() ?? string.Empty;

                        foreach (var subkeyName in key.GetSubKeyNames())
                        {
                            using (var subkey = key.OpenSubKey(subkeyName))
                            {
                                if (subkey != null)
                                {
                                    var name = subkey.GetValue("DistributionName")?.ToString();
                                    if (!string.IsNullOrEmpty(name))
                                    {
                                        var basePath = subkey.GetValue("BasePath")?.ToString() ?? string.Empty;
                                        var wslVersionVal = subkey.GetValue("Version");
                                        int wslVersion = wslVersionVal is int val ? val : 2;
                                        var defaultUidVal = subkey.GetValue("DefaultUid");
                                        int defaultUid = defaultUidVal is int uid ? uid : 0;

                                        distros.Add(new WslDistro
                                        {
                                            Name = name,
                                            Guid = subkeyName,
                                            BasePath = basePath,
                                            WslVersion = wslVersion,
                                            DefaultUid = defaultUid,
                                            IsDefault = subkeyName.Equals(defaultGuid, StringComparison.OrdinalIgnoreCase),
                                            State = "Stopped"
                                        });
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed reading WSL registry: {ex.Message}");
            }


            try
            {
                var wslOutput = RunWslCommand("-l -v");
                if (!string.IsNullOrEmpty(wslOutput))
                {
                    string lowerOutput = wslOutput.ToLower();
                    if (lowerOutput.Contains("no installed") ||
                        lowerOutput.Contains("keine installierten") ||
                        lowerOutput.Contains("not enabled") ||
                        lowerOutput.Contains("not installed") ||
                        lowerOutput.Contains("optional component") ||
                        lowerOutput.Contains("verfÃ¼gt Ã¼ber keine"))
                    {
                        return distros;
                    }

                    var lines = wslOutput.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var rawLine in lines)
                    {
                        var line = CleanUtf16String(rawLine).Trim();
                        if (string.IsNullOrEmpty(line) || line.StartsWith("NAME", StringComparison.OrdinalIgnoreCase) || line.Contains("STATE"))
                        {
                            continue;
                        }

                        bool isDefault = false;
                        if (line.StartsWith("*"))
                        {
                            isDefault = true;
                            line = line.Substring(1).Trim();
                        }

                        var tokens = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        if (tokens.Length >= 2 && tokens.Length <= 4)
                        {
                            string name = tokens[0];
                            string state = tokens[1];
                            int version = 2;
                            if (tokens.Length >= 3 && int.TryParse(tokens[2], out int parsedVer))
                            {
                                version = parsedVer;
                            }


                            var existing = distros.FirstOrDefault(d => d.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                            if (existing != null)
                            {
                                existing.State = state;
                                if (isDefault) existing.IsDefault = true;
                            }
                            else
                            {
                                distros.Add(new WslDistro
                                {
                                    Name = name,
                                    State = state,
                                    WslVersion = version,
                                    IsDefault = isDefault
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed running wsl.exe -l -v: {ex.Message}");
            }

            return distros;
        }




        public static List<WslOnlineDistro> GetOnlineDistros()
        {
            var list = new List<WslOnlineDistro>();
            try
            {
                var output = RunWslCommand("--list --online");
                if (string.IsNullOrEmpty(output))
                {

                    return GetFallbackOnlineDistros();
                }

                var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                bool headerFound = false;

                foreach (var rawLine in lines)
                {
                    var line = CleanUtf16String(rawLine).Trim();
                    if (string.IsNullOrEmpty(line)) continue;

                    if (line.Contains("NAME") && line.Contains("FRIENDLY NAME"))
                    {
                        headerFound = true;
                        continue;
                    }

                    if (!headerFound) continue;



                    var parts = line.Split(new[] { ' ', '\t' }, 2, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 1)
                    {
                        string name = parts[0].Trim();
                        string friendly = parts.Length > 1 ? parts[1].Trim() : name;

                        list.Add(new WslOnlineDistro
                        {
                            Name = name,
                            FriendlyName = friendly
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed getting online distros: {ex.Message}");
                return GetFallbackOnlineDistros();
            }

            if (list.Count == 0)
            {
                return GetFallbackOnlineDistros();
            }

            return list;
        }

        private static List<WslOnlineDistro> GetFallbackOnlineDistros()
        {
            return new List<WslOnlineDistro>
            {
                new WslOnlineDistro { Name = "Ubuntu", FriendlyName = "Ubuntu" },
                new WslOnlineDistro { Name = "Debian", FriendlyName = "Debian GNU/Linux" },
                new WslOnlineDistro { Name = "kali-linux", FriendlyName = "Kali Linux Rolling" },
                new WslOnlineDistro { Name = "SLES-15-SP6", FriendlyName = "SUSE Linux Enterprise 15 SP6" },
                new WslOnlineDistro { Name = "openSUSE-Tumbleweed", FriendlyName = "openSUSE Tumbleweed" },
                new WslOnlineDistro { Name = "OracleLinux_9_5", FriendlyName = "Oracle Linux 9.5" }
            };
        }




        public static string RunWslCommand(string arguments)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "wsl.exe",
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.Unicode,
                    StandardErrorEncoding = Encoding.Unicode
                };

                using (var process = Process.Start(psi))
                {
                    if (process == null) return string.Empty;

                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (!string.IsNullOrEmpty(error) && string.IsNullOrEmpty(output))
                    {
                        return error;
                    }
                    return output;
                }
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }




        private static string CleanUtf16String(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;


            return input.Replace("\0", "").Trim();
        }

        public static void LaunchTerminal(string distroName)
        {
            try
            {

                var wtPsi = new ProcessStartInfo
                {
                    FileName = "wt.exe",
                    Arguments = $"-d . wsl.exe -d {distroName}",
                    UseShellExecute = true
                };
                Process.Start(wtPsi);
            }
            catch
            {

                var cmdPsi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c start wsl.exe -d {distroName}",
                    UseShellExecute = true
                };
                Process.Start(cmdPsi);
            }
        }

        public static void TerminateDistro(string distroName)
        {
            RunWslCommand($"--terminate {distroName}");
        }

        public static void UnregisterDistro(string distroName)
        {
            RunWslCommand($"--unregister {distroName}");
        }

        public static void SetDefaultDistro(string distroName)
        {
            RunWslCommand($"--set-default {distroName}");
        }

        public static void ShutdownWsl()
        {
            RunWslCommand("--shutdown");
        }




        public static void InstallDistro(string distroName)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c start \"Installing {distroName}\" cmd.exe /c \"wsl.exe --install -d {distroName} && echo. && echo Installation Finished. Press any key to close... && pause > nul\"",
                UseShellExecute = true
            };
            Process.Start(psi);
        }





        public static void ImportDistro(string name, string installPath, string tarPath, int version = 2)
        {

            if (!Directory.Exists(installPath))
            {
                Directory.CreateDirectory(installPath);
            }

            var args = $"/c start \"Importing {name}\" cmd.exe /c \"echo Importing rootfs, please wait... && wsl.exe --import {name} \\\"{installPath}\\\" \\\"{tarPath}\\\" --version {version} && echo. && echo Import complete! Press any key to close... && pause > nul\"";
            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = args,
                UseShellExecute = true
            };
            Process.Start(psi);
        }




        public static void ExportDistro(string distroName, string tarPath)
        {
            var args = $"/c start \"Exporting {distroName}\" cmd.exe /c \"echo Exporting distro, please wait... && wsl.exe --export {distroName} \\\"{tarPath}\\\" && echo. && echo Export complete! Press any key to close... && pause > nul\"";
            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = args,
                UseShellExecute = true
            };
            Process.Start(psi);
        }




        public static void DuplicateDistro(string sourceName, string cloneName, string installPath)
        {
            if (!Directory.Exists(installPath))
            {
                Directory.CreateDirectory(installPath);
            }
            string tempTar = Path.Combine(Path.GetTempPath(), $"{sourceName}_clone_temp.tar");

            var args = $"/c start \"Cloning {sourceName} to {cloneName}\" cmd.exe /c \"echo Exporting source distro... && wsl.exe --export {sourceName} \\\"{tempTar}\\\" && echo Importing new clone... && wsl.exe --import {cloneName} \\\"{installPath}\\\" \\\"{tempTar}\\\" && del \\\"{tempTar}\\\" && echo. && echo Clone complete! Press any key to close... && pause > nul\"";
            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = args,
                UseShellExecute = true
            };
            Process.Start(psi);
        }




        public static async Task<string> RunCommandAsync(string distroName, string command)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "wsl.exe",
                        Arguments = $"-d {distroName} -- {command}",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        StandardOutputEncoding = Encoding.UTF8,
                        StandardErrorEncoding = Encoding.UTF8
                    };

                    using (var process = Process.Start(psi))
                    {
                        if (process == null) return "Error starting process";

                        string output = process.StandardOutput.ReadToEnd();
                        string error = process.StandardError.ReadToEnd();
                        process.WaitForExit();

                        if (!string.IsNullOrEmpty(error))
                        {
                            return output + "\nError:\n" + error;
                        }
                        return output;
                    }
                }
                catch (Exception ex)
                {
                    return $"Error: {ex.Message}";
                }
            });
        }




        public static void RunElevatedCommand(string command)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c start \"WSL Setup Doctor\" cmd.exe /c \"{command}\"",
                UseShellExecute = true,
                Verb = "runas"
            };
            Process.Start(psi);
        }

        public static void EnableWslFeature()
        {
            RunElevatedCommand("echo Enabling Windows Subsystem for Linux... && dism.exe /online /enable-feature /featurename:Microsoft-Windows-Subsystem-Linux /all /norestart && echo. && echo Finished! Press any key to close... && pause > nul");
        }

        public static void EnableVmPlatformFeature()
        {
            RunElevatedCommand("echo Enabling Virtual Machine Platform... && dism.exe /online /enable-feature /featurename:VirtualMachinePlatform /all /norestart && echo. && echo Finished! Press any key to close... && pause > nul");
        }

        public static void SetDefaultWslVersion(int version)
        {
            var args = $"/c start \"Set default WSL to version {version}\" cmd.exe /c \"wsl.exe --set-default-version {version} && echo. && echo Version set to WSL {version}! Press any key to close... && pause > nul\"";
            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = args,
                UseShellExecute = true
            };
            Process.Start(psi);
        }

        public static void UpdateWsl()
        {
            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c start \"Updating WSL Kernel\" cmd.exe /c \"echo Updating WSL kernel... && wsl.exe --update && echo. && echo WSL Kernel Updated! Press any key to close... && pause > nul\"",
                UseShellExecute = true
            };
            Process.Start(psi);
        }
    }
}
