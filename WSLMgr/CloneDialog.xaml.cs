using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;

namespace WSLMgr
{
    public partial class CloneDialog : Window
    {
        public string SourceDistroName { get; }
        public string CloneName => TxtCloneName.Text.Trim();
        public string InstallPath => TxtInstallPath.Text.Trim();

        public CloneDialog(string sourceDistroName)
        {
            InitializeComponent();
            SourceDistroName = sourceDistroName;
            TxtSourceDistro.Text = $"Clone {sourceDistroName}";


            TxtCloneName.Text = $"{sourceDistroName}-clone";


            string defaultWslDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "WSL", $"{sourceDistroName}-clone");
            TxtInstallPath.Text = defaultWslDir;


            CommandBindings.Add(new CommandBinding(SystemCommands.CloseWindowCommand, (s, e) => Close()));
            CommandBindings.Add(new CommandBinding(SystemCommands.MinimizeWindowCommand, (s, e) => WindowState = WindowState.Minimized));
            CommandBindings.Add(new CommandBinding(SystemCommands.MaximizeWindowCommand, (s, e) =>
                WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized));

            TxtCloneName.TextChanged += TxtCloneName_TextChanged;
            TxtCloneName.Focus();
        }

        private void TxtCloneName_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            string cleanName = TxtCloneName.Text.Trim();
            if (!string.IsNullOrEmpty(cleanName))
            {
                string defaultWslDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "WSL", cleanName);
                TxtInstallPath.Text = defaultWslDir;
            }
        }

        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {

            try
            {
                var dialog = new OpenFolderDialog
                {
                    Title = "Select Destination Folder for Clone Rootfs",
                    InitialDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile))
                };

                if (dialog.ShowDialog(this) == true)
                {
                    TxtInstallPath.Text = dialog.FolderName;
                }
            }
            catch (Exception ex)
            {

                var dialog = new SaveFileDialog
                {
                    Title = "Select Destination Folder (Save any dummy file in the directory)",
                    Filter = "Directory|*.directory",
                    FileName = "Select Folder"
                };
                if (dialog.ShowDialog(this) == true)
                {
                    string path = Path.GetDirectoryName(dialog.FileName) ?? string.Empty;
                    if (!string.IsNullOrEmpty(path))
                    {
                        TxtInstallPath.Text = path;
                    }
                }
            }
        }

        private void BtnClone_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(CloneName))
            {
                MessageBox.Show(this, "Please enter a valid clone name.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(InstallPath))
            {
                MessageBox.Show(this, "Please select an installation directory.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
