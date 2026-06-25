using System;
using System.Windows;
using System.Windows.Input;

namespace WSLMgr
{
    public partial class RunCommandDialog : Window
    {
        private readonly string _distroName;

        public RunCommandDialog(string distroName)
        {
            InitializeComponent();
            _distroName = distroName;
            TxtDistroName.Text = $"Inside {distroName}";


            CommandBindings.Add(new CommandBinding(SystemCommands.CloseWindowCommand, (s, e) => Close()));
            CommandBindings.Add(new CommandBinding(SystemCommands.MinimizeWindowCommand, (s, e) => WindowState = WindowState.Minimized));
            CommandBindings.Add(new CommandBinding(SystemCommands.MaximizeWindowCommand, (s, e) =>
                WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized));

            TxtCommand.Focus();
        }

        private async void ExecuteCommand()
        {
            string command = TxtCommand.Text.Trim();
            if (string.IsNullOrEmpty(command)) return;

            TxtPlaceholder.Visibility = Visibility.Collapsed;
            TxtOutput.Text = $"$ {command}\nRunning...";
            BtnRun.IsEnabled = false;
            TxtCommand.IsEnabled = false;

            try
            {
                string result = await WslBridge.RunCommandAsync(_distroName, command);
                TxtOutput.Text = $"$ {command}\n{result}";
            }
            catch (Exception ex)
            {
                TxtOutput.Text = $"$ {command}\nError: {ex.Message}";
            }
            finally
            {
                BtnRun.IsEnabled = true;
                TxtCommand.IsEnabled = true;
                TxtCommand.Focus();
            }
        }

        private void BtnRun_Click(object sender, RoutedEventArgs e)
        {
            ExecuteCommand();
        }

        private void TxtCommand_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == KeyToEnum(Key.Enter))
            {
                ExecuteCommand();
                e.Handled = true;
            }
        }


        private Key KeyToEnum(Key key) => key;

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
