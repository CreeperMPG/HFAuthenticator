using System.IO;
using System.Text;
using System.Windows;
using iNKORE.UI.WPF.Modern.Controls;

namespace HFAuthenticator
{
    public partial class LogDialog : ContentDialog
    {
        private string _path;
        public LogDialog(string path)
        {
            InitializeComponent();
            _path = path;
            LoadLog();
        }

        private void LoadLog()
        {
            try
            {
                if (File.Exists(_path))
                {
                    LogTextBox.Text = File.ReadAllText(_path, Encoding.UTF8);
                }
                else
                {
                    LogTextBox.Text = "(无日志)";
                }
            }
            catch
            {
                LogTextBox.Text = "无法读取日志";
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Hide();
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (File.Exists(_path)) File.WriteAllText(_path, string.Empty, Encoding.UTF8);
                LogTextBox.Text = string.Empty;
            }
            catch { }
        }
    }
}
