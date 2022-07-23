using System;
using System.Windows;
namespace NewsHomepageHelper
{
    /// <summary>
    /// TextBoxWindow.xaml 的交互逻辑
    /// </summary>
    public partial class TextBoxWindow : Window
    {
        public TextBoxWindow(string require,string defaultAnswer = "")
        {
            InitializeComponent();
            RequireTextBlock.Text = "请输入："+ require;
            inputBox.Text = defaultAnswer;
            this.Title = "输入" + require;
        }

        private void confirmButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            inputBox.SelectAll();
            inputBox.Focus();
        }

        public string Answer
        {
            get { return inputBox.Text; }
        }
    }
}
