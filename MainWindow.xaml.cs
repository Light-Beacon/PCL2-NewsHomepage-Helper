using System;
using System.Resources;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Threading;
using static WpfApp1.Debug;

namespace WpfApp1
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        bool initmode = true;
        bool VersionGot = false;
        short versionmode = 0;
        MCVersions VersionList = new MCVersions();
        MCVersion targetVersion;
        MCVersion versionChoosed
        {
            get {
                if (versionmode == 0)
                    return new MCVersion { id = CustomVersionBox.Text, versionType = MCVType.Others };
                return targetVersion;
            }
            set { targetVersion = value; VersionChooseChanged();}
        }

        public delegate void MCVersionHandler();
        public event MCVersionHandler VersionChooseChanged;

        public MainWindow()
        {
            Log("开始初始化");
            InitializeComponent();
            Init();
            initmode = false;   
        }
        
        void Init()
        {
            VersionChooseChanged += OnVerisonChanged;
            Log("初始化完成");
        }
        void OnVerisonChanged()
        {
            if (versionChoosed.versionType == MCVType.Snapshot)
                WikiLinkBox.Text =  "https://minecraft.fandom.com/zh/wiki/" + versionChoosed.id;
            else
                WikiLinkBox.Text =  "https://minecraft.fandom.com/zh/wiki/Java版" + versionChoosed.id;
            MCBBSLinkBox.Text = "https://www.mcbbs.net/thread-";
            MCWebsizeLinkBox.Text = "https://www.minecraft.net/zh-hans/article/";
            HeaderImgLinkBox.Text = "https://www.lightbeacon.top/pnh/newsimgs/1_";
        }

        private string GenerateXAMLCode(string input)
        {
            string output = "";
            string versiontype;
            switch(versionChoosed.versionType)
            {
                case MCVType.Release:
                    versiontype = "正式版";
                    break;
                case MCVType.Pre_Release:
                    versiontype = "预发布版";
                    break;
                case MCVType.Release_Candidate:
                    versiontype = "候选版";
                    break;
                case MCVType.Snapshot:
                    versiontype = "快照";
                    break;
                case MCVType.Experimental_Snaphot:
                    versiontype = "实验性快照";
                    break;
                case MCVType.Aprilfools_Version:
                    versiontype = "愚人节版本";
                    break;
                default:
                    versiontype = "其它版本";
                    break;
            }
            string versionID = versionChoosed.id;
            string headerimagelink = HeaderImgLinkBox.Text;
            string footnote = footnNoteBox.Text;

            ResourceHelper rh = new ResourceHelper();
            MarkdownToXamlConverter converter = new MarkdownToXamlConverter(rh);

            //Begining
            output += rh.GetStr("Begin1");
            output += versiontype;
            output += rh.GetStr("Begin2");
            output += versionID;
            output += rh.GetStr("Begin3");
            //output += rh.GetStr("ImageLinkHead") + headerimagelink;
            output += headerimagelink;
            output += rh.GetStr("Begin4");
            //output += rh.GetStr("ImageLinkHead") + headerimagelink;
            output += headerimagelink;
            output += rh.GetStr("Begin5");
            output += versionChoosed.id.ToUpper();
            output += rh.GetStr("BeginEnd");
            output += "\n";

            output += converter.Convert(input);

            output += rh.GetStr("Link1");
            output += WikiLinkBox.Text;
            output += rh.GetStr("Link2");
            output += MCBBSLinkBox.Text;
            output += rh.GetStr("Link3");
            output += MCWebsizeLinkBox.Text;
            output += rh.GetStr("LinkEnd");

            output += rh.GetStr("FootNoteStart");
            output += footnote;
            output += rh.GetStr("FootNoteEnd");

            output += rh.GetStr("End");
            return output;
        }

        private void GetVersionList()
        {
            if (!VersionGot)
            {
                VersionGot = true;
                DateTime time = DateTime.Now;
                try
                {
                    VersionList.GetVersions();
                }
                catch
                {
                    MessageBox.Show("网络问题，无法获取，正在退出编辑器。", "Error");
                }
                Log("获取版本共花费时间：" + (DateTime.Now - time).ToString());
            }
        }

        private void ChooseVersionChildThread()
        {
            Log("线程创建成功！");
            GetVersionList();
            Dispatcher.BeginInvoke(new Action(delegate
            {
                VersionChooseBox.Items.Clear();
                foreach (MCVersion v in MCVersions.Versions)
                    VersionChooseBox.Items.Add(new TextBlock { Text = v.id });
                VersionChooseBox.SelectedIndex = 0;
            }));
            Log("线程正在结束。");
        }

        private void AutoFillVersionChildThread()
        {
            GetVersionList();
            Dispatcher.BeginInvoke(new Action(delegate
            {
                VersionIDText.Text = VersionList.LatestVersion().id;
                VersionTypeText.Text = VersionList.LatestVersion().versionType.ToString();
                versionChoosed = VersionList.LatestVersion();
            }));
        }
 
        //Interactions on Control
        private void AutoFill_Checked(object sender, RoutedEventArgs e)
        {
            VersionAutoFill.Visibility = Visibility.Visible;
            VersionChoose.Visibility = Visibility.Hidden;
            VersionCustom.Visibility = Visibility.Hidden;
            VersionNotChoose.Visibility = Visibility.Hidden;
            ThreadHelper.RunInNewThread(AutoFillVersionChildThread);
            versionmode = 1;
        }

        private void Choose_Checked(object sender, RoutedEventArgs e)
        {
            VersionChoose.Visibility = Visibility.Visible;
            VersionAutoFill.Visibility = Visibility.Hidden;
            VersionCustom.Visibility = Visibility.Hidden;
            VersionNotChoose.Visibility = Visibility.Hidden;
            ThreadHelper.RunInNewThread(ChooseVersionChildThread);
            versionmode = 2;
        }

        private void VersionChooseBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(!initmode)
            {
                if (VersionChooseBox.SelectedIndex >= 0)
                {
                    VersionTypeText.Text = VersionList.SearchByIndex(VersionChooseBox.SelectedIndex).versionType.ToString();
                    versionChoosed = VersionList.SearchByIndex(VersionChooseBox.SelectedIndex);
                }      
                else
                    VersionTypeText.Text = "请选择版本";
            }
            
        }

        private void CustomVersionType_TextChanged(object sender, TextChangedEventArgs e)
        {
            VersionTypeText.Text = CustomVersionType.Text;
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            VersionChoose.Visibility = Visibility.Hidden;
            VersionAutoFill.Visibility = Visibility.Hidden;
            VersionCustom.Visibility = Visibility.Visible;
            VersionNotChoose.Visibility = Visibility.Hidden;
            versionmode = 0;
        }

        private void WikiButton_Click(object sender, RoutedEventArgs e) // 跳转到对应的Wiki连接
        {
            if(versionChoosed.versionType == MCVType.Snapshot)
                System.Diagnostics.Process.Start("https://minecraft.fandom.com/zh/wiki/" + versionChoosed.id);
            else
                System.Diagnostics.Process.Start("https://minecraft.fandom.com/zh/wiki/Java版" + versionChoosed.id);
        }

        private void ExportCodeButton_Click(object sender, RoutedEventArgs e) // 生成代码
        {
            TextRange range = new TextRange(ArticalCodeBox.Document.ContentStart, ArticalCodeBox.Document.ContentEnd);
            string str = range.Text;
            if (versionChoosed != null)
            {
                string code = GenerateXAMLCode(str);
                Clipboard.SetText(code);
                //MessageBox.Show(code);
                MessageBox.Show("已复制到剪贴板");
            } 
            else
                MessageBox.Show("您还没有选择版本捏。");
        }
    }


    public class ThreadHelper
    {
        //用于将Action在新线程上执行
        public static void RunInNewThread(Action act)
        {
            ThreadStart childref = new ThreadStart(act);
            Thread childThread = new Thread(childref);
            Log("创建线程：" + childThread.ManagedThreadId);
            childThread.Start();
            Log("线程创建成功！");
        }

    }
    
    public class ResourceHelper
    {
        //用于获取Resource.resx中的资源
        public string GetStr(string name)
        {
            ResourceManager resManager = new ResourceManager("NewsHomepageHelper.Resource", typeof(ResourceHelper).Assembly);
            return resManager.GetString(name);
        }
    }
}
