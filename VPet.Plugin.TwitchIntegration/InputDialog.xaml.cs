using LinePutScript.Localization.WPF;
using System;
using System.Windows;
using System.Windows.Input;

namespace VPet.Plugin.TwitchIntegration
{
    public partial class InputDialog : Window
    {
        public string resultText = "";
        public event EventHandler<string> OnTextEntered;

        public InputDialog(int x)
        {
            this.InitializeComponent();
            this.xCustomText.Text = $"{"CUSTOM TEXT".Translate()} {x + 1}";
        }

        private void OkButton(object sender, RoutedEventArgs e)
        {
            this.resultText = this.InputTextBox.Text;
            OnTextEntered?.Invoke(this, this.resultText);
            this.Close();
        }

        private void Move(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }
    }
}
