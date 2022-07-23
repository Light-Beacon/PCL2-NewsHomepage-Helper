using NewsHomepageHelper;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Resources;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Threading;
using static WpfApp1.Debug;
using Newtonsoft.Json.Linq;

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

        ResourceHelper rh = new ResourceHelper();
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
            cardList = new ObservableCollection<ContentCard>();
            resList = new ObservableCollection<ContentCard>();
            cardList.CollectionChanged += OnCardListChanged;
            resList.CollectionChanged += OnResCardListChanged;
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
            HeaderImgLinkBox.Text = rh.GetStr("ImageLinkHead");
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

        private string GetMdCode()
        {
            TextRange range = new TextRange(ArticalCodeBox.Document.ContentStart, ArticalCodeBox.Document.ContentEnd);
            return range.Text;
        }

        private void ExportCodeButton_Click(object sender, RoutedEventArgs e) // 生成代码
        {
            
            if (versionChoosed != null)
            {
                string code = new VersionCard(versionChoosed.id,versionChoosed.versionType,HeaderImgLinkBox.Text, GetMdCode(), WikiLinkBox.Text, MCBBSLinkBox.Text, MCWebsizeLinkBox.Text, footnNoteBox.Text,FatherVersionBox.Text,true).GetCode();
                Clipboard.SetText(code);
                //MessageBox.Show(code);
                MessageBox.Show("已复制到剪贴板");
            } 
            else
                MessageBox.Show("您还没有选择版本捏。");
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.Filter = "Markdown文件|.md";
            dlg.Title = "保存原始 Markdown 文件";
            dlg.FileName = versionChoosed.id;
            if (dlg.ShowDialog().Value)
            {
                string filePath = dlg.FileName;
                FileStream fs = new FileStream(filePath,FileMode.Create);
                StreamWriter sw = new StreamWriter(fs, System.Text.Encoding.UTF8);
                TextRange range = new TextRange(ArticalCodeBox.Document.ContentStart, ArticalCodeBox.Document.ContentEnd);
                string str = range.Text;
                sw.Write(str);
                sw.Close();
                fs.Close();
                Debug.Log("Md文件已成功保存至：" + filePath);
                MessageBox.Show("已保存！");
            }
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            
        }
        

        //首页管理
        ObservableCollection<ContentCard> cardList;
        int ctnSelPos
        {
            get { return ContentListBox.Items.IndexOf(ContentListBox.SelectedItem); }
        }

        private void ContentMoveUpBtn_Click(object sender, RoutedEventArgs e)
        {
            if(ctnSelPos != -1)
                cardList.Move(ctnSelPos, Math.Max(ctnSelPos - 1, 0));
        }

        private void ContentMoveDownBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ctnSelPos != -1)
                cardList.Move(ctnSelPos, Math.Min(ctnSelPos + 1, cardList.Count - 1));
        }

        private void ContentDelBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ctnSelPos != -1)
                cardList.RemoveAt(ctnSelPos);
        }

        void OnCardListChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateContentList();
        }

        void UpdateContentList()
        {
            if (ContentListBox.Items != null && ContentListBox.ItemsSource == null)
                ContentListBox.Items.Clear();
            ContentListBox.ItemsSource = cardList.ToList();

            bool isFirstVersionCard = true;
            foreach(ContentCard card in cardList)
            {
                if(card.GetType() == typeof(VersionCard))
                {
                    if (isFirstVersionCard)
                    {
                        ((VersionCard)card).isLatest = true;
                        isFirstVersionCard = false;
                    }
                    else
                        ((VersionCard)card).isLatest = false;
                }
            }
        }

        private void ExportItemButton_Click(object sender, RoutedEventArgs e)
        {
            //仅供测试
            cardList.Add(new VersionCard(versionChoosed.id, versionChoosed.versionType, HeaderImgLinkBox.Text, GetMdCode(), WikiLinkBox.Text, MCBBSLinkBox.Text, MCWebsizeLinkBox.Text, footnNoteBox.Text, FatherVersionBox.Text));
        }

        private void ContentFoldBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ctnSelPos != -1)
                cardList[ctnSelPos].switchSwapStats();
            UpdateContentList();
        }

        private void SeparatorBtn_Click(object sender, RoutedEventArgs e)
        {
            TextBoxWindow tBW = new TextBoxWindow("分隔符标题","分隔符");
            if(tBW.ShowDialog() == true)
            {
                cardList.Insert(ctnSelPos + 1, new Separator(tBW.Answer));
            }
        }

        private void GeneBtn_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(GenerateXAMLCode());
        }

        string GenerateXAMLCode()
        {
            string result = string.Empty;
            foreach(var card in cardList)
            {
                result += card.GetCode();
            }
            return result;
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            SaveContent();
            MessageBox.Show("保存成功！");
        }

        ObservableCollection<ContentCard> resList;

        void OnResCardListChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateResList();
        }

        void UpdateResList()
        {
            if (ResListBox.Items != null && ResListBox.ItemsSource == null)
                ResListBox.Items.Clear();
            ResListBox.ItemsSource = resList.ToList();
        }

        private void ImportBtn_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Filter = "Json 文件|*.json";
            dlg.Title = "打开版本信息文件";
            if (dlg.ShowDialog().Value)
            {
                string filePath =  dlg.FileName;
                FileStream fs = new FileStream(filePath, FileMode.Open);
                StreamReader sr = new StreamReader(fs, System.Text.Encoding.UTF8);
                string str = sr.ReadToEnd();
                sr.Close();
                fs.Close();
                Debug.Log("已读取：" + filePath);
                try
                {
                    JArray jArray = new JArray();
                    jArray = JArray.Parse(str);
                    foreach (JObject card in jArray)
                    {
                        string path = Path.GetDirectoryName(filePath) + "\\" + Path.GetFileNameWithoutExtension(filePath) + "\\" + card["filename"].ToString();
                        if (!File.Exists(path))
                            throw new Exception("路径不存在:" + path);
                        FileStream _fs = new FileStream(path, FileMode.Open);
                        StreamReader _sr = new StreamReader(_fs, System.Text.Encoding.UTF8);
                        string code = _sr.ReadToEnd();
                        card.Add("mdcode",code);
                        resList.Add((VersionCard)card);
                        _sr.Close();
                        _fs.Close();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        public void SaveContent()
        {
            string projectPath= "C:\\Users\\26826\\Desktop\\source\\";
            string projectHPFileName = "News.json";
            JArray array = new JArray();
            JObject pairs;
            foreach(ContentCard card in cardList)
            {
                pairs = new JObject();
                pairs.Add("name", card.name);
                if(card.type != "Separator")
                {
                    string path = card.path;
                    string folder = card.type;
                    folder += 's';  //e.g. Version -> Versions
                    pairs.Add("path", path);
                    string jsonpath = projectPath + folder + "\\" + path + ".json";
                    JArray sources = new JArray();
                    if (File.Exists(jsonpath))
                    {
                        FileStream fs = new FileStream(jsonpath, FileMode.Open);//e.g. .../Versions/1.19.json
                        StreamReader sr = new StreamReader(fs, System.Text.Encoding.UTF8);
                        string temp = sr.ReadToEnd();
                        sources = JArray.Parse(temp);
                        sr.Close();
                        fs.Close();
                    }
                    sources = EditJson(sources, card , projectPath + folder + "\\");
                    FileHelper.WriteFile(jsonpath, sources.ToString());
                }
                pairs.Add("type", card.type);
                if(card.isSwaped)
                    pairs.Add("isswaped", card.isSwaped);
                array.Add(pairs);
            }
            FileHelper.WriteFile(projectPath + projectHPFileName, array.ToString());
        }

        private void LoadBtn_Click(object sender, RoutedEventArgs e)
        {
            loadContent();
        }

        public void loadContent()
        {
            string projectPath = "C:\\Users\\26826\\Desktop\\source\\";
            string projectHPFileName = "News.json";
            cardList.Clear();
            JArray array = JArray.Parse(FileHelper.ReadFile(projectPath + projectHPFileName));
            {
                ContentCard card;
                foreach (JObject obj in array)
                {
                    switch (obj["type"].ToString())
                    {
                        case "Version":
                            card = new VersionCard(obj, projectPath);
                            break;
                        case "Separator":
                            card = (Separator)obj;
                            break;
                        case "Custom":
                            throw new NotImplementedException();
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    cardList.Add(card);
                }
            }

        }

        public JArray EditJson(JArray array,ContentCard card,string path)
        {
            if(!CardInArrayAndEdited(array, card, path))
            {
                JObject valuePairs = card.ToJObject();
                valuePairs.Add("filename", card.dataFileName);
                array.Add(valuePairs);
                FileHelper.WriteFile(path + card.dataFileName, card.GetData());
            }

            return array;
        }

        bool CardInArrayAndEdited(JArray array, ContentCard card, string path)
        {
            for(int i = 0; i < array.Count; i++)
            {
                if (array[i]["name"].ToString() == card.name)
                {
                    string temp = array[i]["filename"].ToString();
                    array[i] = card.ToJObject();
                    array[i]["filename"] = temp;
                    FileHelper.WriteFile(path + temp, card.GetData());
                    return true;
                }
            }
            return false;
        }
    }

    public static class FileHelper
    {
        public static void WriteFile(string path, string ctx)
        {
            if (!File.Exists(path))
            {
                string directoryName = Path.GetDirectoryName(path);
                if (!Directory.Exists(directoryName))
                    Directory.CreateDirectory(directoryName);
                //File.Create(path);
            }
            FileStream fs = new FileStream(path, FileMode.Create);
            StreamWriter sw = new StreamWriter(fs, System.Text.Encoding.UTF8);
            sw.Write(ctx);
            sw.Close();
            fs.Close();
        }

        public static string ReadFile(string path)
        {
            if (!File.Exists(path))
                return null;
            StreamReader sr = new StreamReader(path, System.Text.Encoding.UTF8);
            return sr.ReadToEnd();
        }
    }

    public abstract class ContentCard
    {
        public virtual bool isSwaped { get; set; }
        public virtual string path { get; set; }
        public virtual string name { get; set; }
        public virtual string dataFileName { get => name; }
        public abstract string GetDisplayTitle();
        public abstract string GetCode();
        public abstract JObject ToJObject();
        public abstract string GetData();
        public abstract string type { get; }
        public string displayTitle
        {
            get
            {
                return GetDisplayTitle();
            }
        }
        public string statusText { 
            get
            {
                if (isSwaped)
                    return "▽";
                else
                    return "";
            }
        }
        public void switchSwapStats()
        {
            isSwaped = !isSwaped;
        }
        
    }

    public class CustomPart : ContentCard
    {
        string title;
        string xaml;
        string customPath;
        public override string type { get => "Custom";}
        public override string GetDisplayTitle()
        {
            return title + " (C)";
        }
        public override string GetCode()
        {
            return xaml;
        }
        public override JObject ToJObject()
        {
            JObject jobj = new JObject();
            jobj.Add("type", "custom");
            jobj.Add("title", title);
            if(customPath != null)
                jobj.Add("custompath", customPath);
            return jobj;
        }
        public CustomPart(string _title, string _xaml, string _customPath = null)
        {
            title = _title;
            xaml = _xaml;
            customPath = _customPath;
        }
        public override string GetData()
        {
            return xaml;
        }
    }

    public class Separator : ContentCard
    {
        public override string name { get => title; }
        public override string path { get => throw new NotImplementedException(); }
        public string title;
        public override string type { get => "Separator"; }
        public override string GetCode()
        {
            string output = string.Empty;
            ResourceHelper rh = new ResourceHelper();
            output += rh.GetStr("SeparatorBegin");
            for (int i = 1; i < title.Length; i++)
            {
                output += title[i - 1] + "  ";
            }
            output += title[title.Length - 1];
            output += rh.GetStr("SeparatorEnd");
            return output;
        }

        public override JObject ToJObject()
        {
            JObject jobj = new JObject();
            jobj.Add("type", "separator");
            jobj.Add("title", title);
            return jobj;
        }

        public static explicit operator JObject(Separator separator)
        {
            return separator.ToJObject();
        }
        public static explicit operator Separator(JObject obj)
        {
            return new Separator(obj["name"].ToString());
        }

        public Separator(string _title)
        {
            title = _title;
        }

        public override string GetDisplayTitle()
        {
            return "-- " + title + " --";
        }

        public override bool isSwaped
        {
            get { return false; }
            set { MessageBox.Show("你不可以对分隔符进行折叠！"); }
        }
        public override string GetData()
        {
            throw new NotImplementedException();
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
