using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;

namespace WSLMgr
{
    public partial class MainWindow : Window
    {
        private List<WslDistro> _installedDistros = new List<WslDistro>();
        private WslConfigManager _wslConfig = new WslConfigManager();

        public MainWindow()
        {
            InitializeComponent();


            CommandBindings.Add(new CommandBinding(SystemCommands.CloseWindowCommand, (s, e) => Close()));
            CommandBindings.Add(new CommandBinding(SystemCommands.MinimizeWindowCommand, (s, e) => WindowState = WindowState.Minimized));
            CommandBindings.Add(new CommandBinding(SystemCommands.MaximizeWindowCommand, (s, e) =>
                WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized));

            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await RefreshAllAsync();
            LoadConfig();
        }




        private async Task RefreshAllAsync()
        {
            TxtHeaderTitle.Text = "Loading WSL...";


            _installedDistros = await Task.Run(() => WslBridge.GetInstalledDistros());
            FilterAndDisplayDistros();


            var onlineCatalog = await Task.Run(() => WslBridge.GetOnlineDistros());


            foreach (var onlineItem in onlineCatalog)
            {
                onlineItem.IsInstalled = _installedDistros.Any(d => d.Name.Equals(onlineItem.Name, StringComparison.OrdinalIgnoreCase));
            }

            ItemsOnlineCatalog.ItemsSource = onlineCatalog;


            TxtDistrosCount.Text = $"{_installedDistros.Count} Distro{(_installedDistros.Count == 1 ? "" : "s")} Installed";

            UpdateHeaderTitle();
        }

        private void FilterAndDisplayDistros()
        {
            string filterText = TxtSearch.Text.Trim();
            if (string.IsNullOrEmpty(filterText))
            {
                ItemsDistros.ItemsSource = _installedDistros;
                PnlEmptyState.Visibility = _installedDistros.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
                ItemsDistros.Visibility = _installedDistros.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                var filtered = _installedDistros
                    .Where(d => d.Name.Contains(filterText, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                ItemsDistros.ItemsSource = filtered;

                PnlEmptyState.Visibility = Visibility.Collapsed;
                ItemsDistros.Visibility = filtered.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void LoadConfig()
        {
            try
            {
                _wslConfig = WslConfigManager.Load();

                TxtWslMemory.Text = _wslConfig.Memory;
                TxtWslProcessors.Text = _wslConfig.Processors;
                TxtWslSwap.Text = _wslConfig.Swap;
                ChkLocalhostForwarding.IsChecked = _wslConfig.LocalhostForwarding;
                ChkGuiApps.IsChecked = _wslConfig.GuiApplications;
                ChkNestedVirtualization.IsChecked = _wslConfig.NestedVirtualization;
                ChkDnsTunneling.IsChecked = _wslConfig.DnsTunneling;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Failed to load .wslconfig: {ex.Message}", "Config Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateHeaderTitle()
        {
            if (TabMain == null || TxtHeaderTitle == null) return;

            switch (TabMain.SelectedIndex)
            {
                case 0:
                    TxtHeaderTitle.Text = "Distro Dashboard";
                    break;
                case 1:
                    TxtHeaderTitle.Text = "Distro Installer Catalog";
                    break;
                case 2:
                    TxtHeaderTitle.Text = "WSL Engine Settings";
                    break;
                case 3:
                    TxtHeaderTitle.Text = "System Setup Doctor";
                    break;
            }
        }

        #region Navigation and Global Actions

        private void Nav_Checked(object sender, RoutedEventArgs e)
        {
            if (TabMain == null || RadDashboard == null || RadInstall == null || RadSettings == null || RadSetup == null) return;

            if (RadDashboard.IsChecked == true) TabMain.SelectedIndex = 0;
            else if (RadInstall.IsChecked == true) TabMain.SelectedIndex = 1;
            else if (RadSettings.IsChecked == true) TabMain.SelectedIndex = 2;
            else if (RadSetup.IsChecked == true) TabMain.SelectedIndex = 3;

            UpdateHeaderTitle();
        }

        private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            await RefreshAllAsync();
        }

        private void BtnShutdownWSL_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(this,
                "Are you sure you want to shut down the entire Windows Subsystem for Linux (WSL)?\n\nThis will terminate ALL running WSL distributions and any command processes inside them.",
                "Shutdown WSL Subsystem",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                WslBridge.ShutdownWsl();
                _ = RefreshAllAsync();
            }
        }

        private void BtnGoToInstaller_Click(object sender, RoutedEventArgs e)
        {
            RadInstall.IsChecked = true;
        }

        #endregion

        #region Dashboard Distro Card Handlers

        private void BtnLaunchDistro_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string distroName)
            {
                WslBridge.LaunchTerminal(distroName);

                _ = Task.Delay(1000).ContinueWith(_ => Dispatcher.Invoke(() => RefreshAllAsync()));
            }
        }

        private async void BtnStopDistro_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string distroName)
            {
                WslBridge.TerminateDistro(distroName);
                await Task.Delay(500);
                await RefreshAllAsync();
            }
        }

        private async void BtnSetDefault_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string distroName)
            {
                WslBridge.SetDefaultDistro(distroName);
                await Task.Delay(500);
                await RefreshAllAsync();
            }
        }

        private void BtnRunCommand_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string distroName)
            {
                var dlg = new RunCommandDialog(distroName) { Owner = this };
                dlg.ShowDialog();
            }
        }

        private async void BtnCloneDistro_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string distroName)
            {
                var dlg = new CloneDialog(distroName) { Owner = this };
                if (dlg.ShowDialog() == true)
                {
                    WslBridge.DuplicateDistro(distroName, dlg.CloneName, dlg.InstallPath);

                    MessageBox.Show(this,
                        $"Cloning process launched in external console. Please wait for the console to complete and then click Refresh.",
                        "Clone In Progress",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    await Task.Delay(2000);
                    await RefreshAllAsync();
                }
            }
        }

        private void BtnExportDistro_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string distroName)
            {
                var saveDlg = new SaveFileDialog
                {
                    Title = $"Export {distroName} backup to Tar file",
                    Filter = "Tar archive (*.tar)|*.tar",
                    FileName = $"{distroName}-backup.tar"
                };

                if (saveDlg.ShowDialog(this) == true)
                {
                    WslBridge.ExportDistro(distroName, saveDlg.FileName);
                    MessageBox.Show(this,
                        "Export launched in external console. Check the console window for progress.",
                        "Export Distro",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
        }

        private void BtnExploreFiles_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string distroName)
            {
                try
                {

                    System.Diagnostics.Process.Start("explorer.exe", $@"\\wsl.localhost\{distroName}\");
                }
                catch (Exception ex)
                {

                    try
                    {
                        System.Diagnostics.Process.Start("explorer.exe", $@"\\wsl$\{distroName}\");
                    }
                    catch
                    {
                        MessageBox.Show(this, $"Failed to open File Explorer for WSL distro: {ex.Message}", "Error Explorer", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private async void BtnDeleteDistro_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string distroName)
            {
                var result = MessageBox.Show(this,
                    $"Are you absolutely sure you want to delete (unregister) {distroName}?\n\nWARNING: This will permanently delete the distribution and ALL files contained inside its virtual drive! This action is irreversible.",
                    "Confirm Distro Deletion",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Error);

                if (result == MessageBoxResult.Yes)
                {
                    WslBridge.UnregisterDistro(distroName);
                    await Task.Delay(500);
                    await RefreshAllAsync();
                }
            }
        }

        #endregion

        #region Installer Tab Handlers

        private async void BtnInstallOnline_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string distroName)
            {
                WslBridge.InstallDistro(distroName);
                MessageBox.Show(this,
                    $"Distro installation launched in external terminal console.\n\nPlease monitor that console window to set up your UNIX username and password once the download completes.\n\nClick 'Refresh' once the console is done to see it on your Dashboard.",
                    "Distro Installation Launched",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                await Task.Delay(2000);
                await RefreshAllAsync();
            }
        }

        private void TxtImportName_TextChanged(object sender, TextChangedEventArgs e)
        {
            string cleanName = TxtImportName.Text.Trim();
            if (!string.IsNullOrEmpty(cleanName))
            {
                string defaultWslDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "WSL", cleanName);
                TxtImportDestDir.Text = defaultWslDir;
            }
        }

        private void BtnBrowseTar_Click(object sender, RoutedEventArgs e)
        {
            var openDlg = new OpenFileDialog
            {
                Title = "Select rootfs Tarball archive",
                Filter = "Tar archive (*.tar;*.tar.gz;*.tgz)|*.tar;*.tar.gz;*.tgz|All files (*.*)|*.*"
            };

            if (openDlg.ShowDialog(this) == true)
            {
                TxtImportTarPath.Text = openDlg.FileName;
            }
        }

        private void BtnBrowseDestDir_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new OpenFolderDialog
                {
                    Title = "Select Destination Folder for Distro Rootfs",
                    InitialDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile))
                };

                if (dialog.ShowDialog(this) == true)
                {
                    TxtImportDestDir.Text = dialog.FolderName;
                }
            }
            catch
            {

                var dialog = new SaveFileDialog
                {
                    Title = "Select Destination Folder (Save dummy file to pick folder)",
                    Filter = "Directory|*.directory",
                    FileName = "Select Folder"
                };
                if (dialog.ShowDialog(this) == true)
                {
                    string path = Path.GetDirectoryName(dialog.FileName) ?? string.Empty;
                    if (!string.IsNullOrEmpty(path))
                    {
                        TxtImportDestDir.Text = path;
                    }
                }
            }
        }

        private async void BtnImportDistro_Click(object sender, RoutedEventArgs e)
        {
            string name = TxtImportName.Text.Trim();
            string tarPath = TxtImportTarPath.Text.Trim();
            string destDir = TxtImportDestDir.Text.Trim();
            int version = CboImportWslVer.SelectedIndex + 1;

            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show(this, "Please enter a valid distribution name.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(tarPath) || !File.Exists(tarPath))
            {
                MessageBox.Show(this, "Please select a valid source rootfs tar file.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(destDir))
            {
                MessageBox.Show(this, "Please select a destination folder.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            WslBridge.ImportDistro(name, destDir, tarPath, version);

            MessageBox.Show(this,
                "Rootfs import launched in external console. Please wait for the process to complete and click Refresh.",
                "Import Distro",
                MessageBoxButton.OK,
                MessageBoxImage.Information);


            TxtImportName.Text = string.Empty;
            TxtImportTarPath.Text = string.Empty;
            TxtImportDestDir.Text = string.Empty;

            await Task.Delay(2000);
            await RefreshAllAsync();
        }

        #endregion

        #region Settings Tab Handlers

        private void BtnSaveSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _wslConfig.Memory = TxtWslMemory.Text.Trim();
                _wslConfig.Processors = TxtWslProcessors.Text.Trim();
                _wslConfig.Swap = TxtWslSwap.Text.Trim();
                _wslConfig.LocalhostForwarding = ChkLocalhostForwarding.IsChecked == true;
                _wslConfig.GuiApplications = ChkGuiApps.IsChecked == true;
                _wslConfig.NestedVirtualization = ChkNestedVirtualization.IsChecked == true;
                _wslConfig.DnsTunneling = ChkDnsTunneling.IsChecked == true;

                _wslConfig.Save();

                MessageBox.Show(this,
                    "WSL Configuration saved successfully to %USERPROFILE%\\.wslconfig!\n\nNote: You must restart WSL via the Shutdown button (Power icon) for these changes to take effect.",
                    "Settings Saved",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Failed to save settings: {ex.Message}", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterAndDisplayDistros();
        }

        #region Setup Doctor Event Handlers

        private void BtnEnableWsl_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                WslBridge.EnableWslFeature();
                MessageBox.Show(this,
                    "WSL optional feature enable command launched in elevated console. Please verify success inside the console window and restart your system if prompted.",
                    "Enable WSL Optional Feature",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Failed to launch feature setup: {ex.Message}", "Doctor Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnEnableVmPlatform_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                WslBridge.EnableVmPlatformFeature();
                MessageBox.Show(this,
                    "Virtual Machine Platform optional feature enable command launched in elevated console. Please verify success inside the console window and restart your system if prompted.",
                    "Enable VM Platform",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Failed to launch platform setup: {ex.Message}", "Doctor Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnSetWsl1_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                WslBridge.SetDefaultWslVersion(1);
                _ = RefreshAllAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Failed to change default version: {ex.Message}", "Doctor Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnSetWsl2_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                WslBridge.SetDefaultWslVersion(2);
                _ = RefreshAllAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Failed to change default version: {ex.Message}", "Doctor Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnWslUpdate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                WslBridge.UpdateWsl();
                MessageBox.Show(this,
                    "WSL Kernel Update package command launched in separate window. Check the console for progress.",
                    "WSL Kernel Update",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Failed to run update: {ex.Message}", "Doctor Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }
}