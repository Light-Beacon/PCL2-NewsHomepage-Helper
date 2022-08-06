using NewsHomepageHelper.ViewModel;
using System;
using System.ComponentModel;
using System.Windows;

namespace NewsHomepageHelper.View
{
    /// <summary>
    /// ProgressBarWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ProgressBarWindow : Window
    {
        public ProgressBarWindowViewModel ProgressBarWindowViewModel { get; set; }
        public ProgressBarWindow(string title, int maxValue, Action<object, DoWorkEventArgs> doWorkAction, Action<object, RunWorkerCompletedEventArgs> runWorkerCompletedAction = null)
        {
            InitializeComponent();
            this.Title = title;
            ProgressBarWindowViewModel progressBarWindowViewModel = new ProgressBarWindowViewModel(this, maxValue, doWorkAction, runWorkerCompletedAction);
            this.ProgressBarWindowViewModel = progressBarWindowViewModel;
            this.DataContext = progressBarWindowViewModel;
        }

        string detailText = "";
        string Detail { 
            get
            {
                return detailText;
            }
            set
            {
                detailText = value;
                DetailBox.Text = detailText;
            } 
        }
    }
}

