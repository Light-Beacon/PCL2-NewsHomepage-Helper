using GalaSoft.MvvmLight;
namespace NewsHomepageHelper.Model
{
    /// <summary>
    /// ProgressBarWindow 的 Model 层
    /// </summary>
    public class ProgressBarModel : ObservableObject
    {
        private int min;
        public int Min
        {
            get { return min; }
            set { min = value; RaisePropertyChanged(); }
        }

        private int max;

        public int Max
        {
            get { return max; }
            set { max = value; RaisePropertyChanged(); }
        }

        private int currentValue;

        public int CurrentValue
        {
            get { return currentValue; }
            set 
            { 
                currentValue = value;
                RaisePropertyChanged(); 
            }
        }

        private string progressBarTip;

        public string ProgressBarTip
        {
            get { return progressBarTip; }
            set { progressBarTip += value + "...\n"; RaisePropertyChanged(); }
        }

        private string processText;
        public string ProcessText
        { 
            get { return processText; }
            set { processText = value; RaisePropertyChanged(); } 
        }

        private string percentText;
        public string PercentText
        { 
            get { return percentText; }
            set { percentText = value; RaisePropertyChanged(); }
        } 
        /// <summary>
        /// 设置当前的进度值
        /// </summary>
        /// <param name="currentValue"></param>
        public void SetCurrentValue(int currentValue)
        {
            CurrentValue = currentValue;
        }

        /// <summary>
        /// 设置当前的进度提示文字
        /// </summary>
        /// <param name="progressBarTip"></param>
        public void SetCurrentValue(string progressBarTip)
        {
            ProgressBarTip = progressBarTip;
        }
    }
}