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
            try
            {
                string code = GenerateXAMLCode();
                Clipboard.SetText(code);
                File.WriteAllText(projectPath + "News.xaml", code);
                SaveContent();
                MessageBox.Show("已复制到剪贴板并生成文件");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            
        }

        string GenerateXAMLCode()
        {
            if(projectPath == null)
            {
                loadContentWithDlg();
            }
            string result = string.Empty;
            ResourceHelper resourceHelper = new ResourceHelper();
            result += resourceHelper.GetStr("Mark");
            result += resourceHelper.GetStr("Stut1");
            result += File.ReadAllText(projectPath + "Animation.xaml");
            result += resourceHelper.GetStr("Stut2");
            result += File.ReadAllText(projectPath + "Style.xaml");
            result += resourceHelper.GetStr("Stut3");
            foreach (var card in cardList)
            {
                result += card.GetCode();
            }
            result += resourceHelper.GetStr("Stut4");
            result += File.ReadAllText(projectPath + "RefreshBar.xaml");
            result += File.ReadAllText(projectPath + "Footer.xaml");
            return result;
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            if (projectPath == null)
            {
                Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
                dlg.Filter = "新闻主页工程文件|*.nhpj";
                dlg.Title = "打开新闻主页工程文件";
                if (dlg.ShowDialog().Value)
                {
                    projectPath = Path.GetDirectoryName(dlg.FileName) + "\\";
                    SaveContent();
                }
            }
            else
            {
                SaveContent();
                MessageBox.Show("保存成功！");
            }
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
                        string path = Path.GetDirectoryName(filePath) + "\\" + card["filename"].ToString();
                        if (!File.Exists(path))
                            throw new Exception("路径不存在:" + path);
                        string code = File.ReadAllText(path);
                        card.Add("mdcode",code);
                        resList.Add((VersionCard)card);
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
                    pairs.Add("path", path);
                    if(card.type == "Version")
                    {
                        string folder = card.type;
                        folder += 's';  //e.g. Version -> Versions
                        string jsonpath = projectPath + folder + "\\" + path + ".json";
                        JArray sources = new JArray();
                        if (File.Exists(jsonpath))
                        {
                            string temp = File.ReadAllText(jsonpath);
                            sources = JArray.Parse(temp);
                        }
                        sources = EditJson(sources, card , projectPath + folder + "\\");
                        File.WriteAllText(jsonpath, sources.ToString());
                    }
                }
                pairs.Add("type", card.type);
                if(card.isSwaped)
                    pairs.Add("isswaped", card.isSwaped);
                array.Add(pairs);
            }
            File.WriteAllText(projectPath + projectHPFileName, array.ToString());
        }

        string projectPath = null; 
        private void LoadBtn_Click(object sender, RoutedEventArgs e)
        {
            loadContentWithDlg();
        }

        public void loadContentWithDlg()
        {
            if (projectPath == null)
            {
                Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
                dlg.Filter = "新闻主页工程文件|*.nhpj";
                dlg.Title = "打开新闻主页工程文件";
                if (dlg.ShowDialog().Value)
                {
                    projectPath = Path.GetDirectoryName(dlg.FileName) + "\\";
                    loadContent(projectPath);
                }
            }
            else
                loadContent(projectPath);
        }

        public void loadContent(string projectPath)
        {
            string projectHPFileName = "News.json";
            cardList.Clear();
            JArray array = JArray.Parse(File.ReadAllText(projectPath + projectHPFileName));
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
                            card = new CustomPart(obj, projectPath);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    if (obj["isswaped"] != null && obj["isswaped"].ToString().ToLower() == "true")
                        card.isSwaped = true;
                    else
                        card.isSwaped = false;
                    cardList.Add(card);
                }
            }

        }

        public JArray EditJson(JArray array,ContentCard card,string path)
        {
            if(!CardInArrayAndEdited(array, card, path))
            {
                JObject valuePairs = card.ToJObject(false);
                valuePairs.Add("filename", card.dataFileName);
                array.Add(valuePairs);
                File.WriteAllText(path + card.dataFileName, card.GetData());
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
                    array[i] = card.ToJObject(false);
                    array[i]["filename"] = temp;
                    File.WriteAllText(path + temp, card.GetData());
                    return true;
                }
            }
            return false;
        }

        private void ImportCustomBtn_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Filter = "Xaml 文件|*.xaml";
            dlg.Title = "打开xmal文件";
            if (dlg.ShowDialog().Value)
            {
                string xamlPath = dlg.FileName;
                string dire = Path.GetDirectoryName(xamlPath);
                if (dire.EndsWith("Customs"))
                {
                    Microsoft.Win32.SaveFileDialog dlg2 = new Microsoft.Win32.SaveFileDialog();
                    dlg.Filter = "JSON 文件|*.json";
                    dlg.Title = "保存定义json文件";
                    if (dlg2.ShowDialog().Value)
                    {
                        string jsonPath = dlg2.FileName;
                        JArray array = new JArray();
                        if (File.Exists(jsonPath))
                            array = JArray.Parse(File.ReadAllText(jsonPath));
                        string name = Path.GetFileNameWithoutExtension(xamlPath);
                        foreach (JObject obj in array) //判重
                            if (obj["name"] != null && obj["name"].ToString() == name)
                            {
                                MessageBox.Show("这个识别名已经存在了！");
                            }
                        TextBoxWindow tbw = new TextBoxWindow("卡片标题", name);
                        if (tbw.ShowDialog() == true)
                        {
                            CustomPart part = CustomPart.ConstructFromFile(tbw.Answer, name, dire.Remove(dire.Length - 7), Path.GetFileNameWithoutExtension(jsonPath));
                            array.Add(part.ToJObject(false));
                            File.WriteAllText(jsonPath, array.ToString());
                            resList.Add(part);
                            MessageBox.Show("导入成功！");
                        }
                    }
                }
                else throw new Exception("路径错误");
            }
        }

        private void ThrowButton_Click(object sender, RoutedEventArgs e)
        {
            if(ResListBox.SelectedIndex != -1)
            {
                cardList.Add(resList[ResListBox.SelectedIndex]);
            }
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
        public abstract JObject ToJObject(bool withdata = true);
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
        public override string type { get => "Custom";}
        public override string GetDisplayTitle()
        {
            return title + " (C)";
        }
        public override string GetCode()
        {
            return xaml;
        }
        public override JObject ToJObject(bool withdata = true)
        {
            JObject jobj = new JObject();
            jobj.Add("type", "custom");
            jobj.Add("title", title);
            jobj.Add("name", name);
            if(withdata)
                jobj.Add("xaml", xaml);
            jobj.Add("path", path);
            return jobj;
        }
        public CustomPart(string _name,string _title, string _xaml, string _path)
        {
            name = _name;
            title = _title;
            xaml = _xaml;
            path = _path;
        }
        public override string GetData()
        {
            return xaml;
        }

        public override bool isSwaped
        {
            get { return false; }
            set { if (value) MessageBox.Show("你不可以对自定义部分进行折叠！"); }
        }

        public CustomPart(JObject jobj, string rootpath)
        {
            JArray array = JArray.Parse(File.ReadAllText(rootpath + "Customs\\" + jobj["path"].ToString() + ".json"));
            foreach (JObject item in array)
            {
                if (item["name"].ToString() == jobj["name"].ToString())
                {
                    this.title = item["title"].ToString();
                    this.xaml = File.ReadAllText(rootpath + "Customs\\" + item["name"] + ".xaml");
                    this.name = item["name"].ToString();
                    this.path = item["path"].ToString();
                    return;
                }
            }
            throw new Exception("无法找到信息");
        }

        public static CustomPart ConstructFromFile(string title,string name,string rootpath,string path)
        {
            if (!File.Exists(rootpath + "Customs\\" + name + ".xaml"))
                throw new IOException("构建CustomPart时不存在该文件");
            return new CustomPart(name,title,File.ReadAllText(rootpath + "Customs\\" + name + ".xaml"),path);
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

        public override JObject ToJObject(bool _=true)
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
            set { if(value) MessageBox.Show("你不可以对分隔符进行折叠！"); }
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
