using GalaSoft.MvvmLight;
using NewsHomepageHelper.Model;
using NewsHomepageHelper.View;
using System;
using System.ComponentModel;
using System.Windows;

namespace NewsHomepageHelper.ViewModel
{
    public class ProgressBarWindowViewModel : ViewModelBase
    {
        #region Properties
        private ProgressBarWindow progressBarWindow;
        private ProgressBarModel progressBarModel;

        /// <summary>
        /// 执行等待的内容
        /// </summary>
        public Action<object, DoWorkEventArgs> DoWorkAction { get; set; }

        /// <summary>
        /// 进度发生变化时的内容
        /// </summary>
        public Action<object, ProgressChangedEventArgs> ProgressChangedAction { get; set; }
        
        /// <summary>
        /// 完成任务时
        /// </summary>
        public Action<object, RunWorkerCompletedEventArgs> RunWorkerCompletedAction { get; set; }
        BackgroundWorker worker;
        private int taskCount = 0;
        #endregion

        public ProgressBarModel ProgressBarModel
        {
            get
            {
                return progressBarModel;
            }
            private set
            {
                progressBarModel = value;
                RaisePropertyChanged();
            }
        }

        public ProgressBarWindowViewModel(ProgressBarWindow progressBarWindow, int maxValue, Action<object, DoWorkEventArgs> doWorkAction, Action<object, RunWorkerCompletedEventArgs> runWorkerCompletedAction = null)
        {
            this.progressBarWindow = progressBarWindow;
            ProgressBarModel progressBarModel = new ProgressBarModel();
            ProgressBarModel = progressBarModel;
            worker = new BackgroundWorker();
            taskCount = maxValue;
            DoWorkAction = doWorkAction;
            RunWorkerCompletedAction = runWorkerCompletedAction;
            this.progressBarWindow.ContentRendered += ProgressBarWindow_ContentRendered;
        }

        private void ProgressBarWindow_ContentRendered(object sender, EventArgs e)
        {

            worker.WorkerReportsProgress = true;
            worker.DoWork += Worker_DoWork;
            worker.ProgressChanged += Worker_ProgressChanged;
            worker.RunWorkerCompleted += Worker_RunWorkerCompleted;
            worker.RunWorkerAsync(taskCount);
        }

        /// <summary>
        /// 任务完成时的回调
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            RunWorkerCompletedAction?.Invoke(sender, e);
            progressBarWindow.Close();
        }

        /// <summary>
        /// 当前的执行内容
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            int endNumber = 0;
            if (e.Argument != null)
            {
                endNumber = (int)e.Argument;
            }
            progressBarModel.Min = 0;
            progressBarModel.Max = endNumber;
            DoWorkAction?.Invoke(sender, e);
        }
        /// <summary>
        /// 任务执行过程中的回调函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            string message = e.UserState.ToString();
            ProgressBarModel.CurrentValue = e.ProgressPercentage;
            progressBarModel.ProgressBarTip = e.UserState?.ToString();
            progressBarModel.ProcessText = progressBarModel.CurrentValue.ToString() + "/" + progressBarModel.Max.ToString();
            progressBarModel.PercentText = (progressBarModel.Max != 0) ? (((int)(((double)progressBarModel.CurrentValue / (double)taskCount * 100) + 0.5)).ToString() + "%") : "?%";
        }
    }
}