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
using System.Collections.Generic;
using NewsHomepageHelper.View;
using System.ComponentModel;
using System.Drawing.Imaging;

namespace WpfApp1
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Init
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
        #endregion

        //创建版本
        #region CreateVersionPage
        string headerImgLinkRoot = "https://www.lightbeacon.top/pnh/newsimgs/";
        void OnVerisonChanged()
        {
            AutoFillWikiLink();
            MCBBSLinkBox.Text = "https://www.mcbbs.net/thread-";
            MCWebsizeLinkBox.Text = "https://www.minecraft.net/zh-hans/article/";
            HeaderImgLinkBox.Text = headerImgLinkRoot;
        }

        private void AutoFillWikiLink()
        {
            Dispatcher.BeginInvoke(new Action(delegate
            {
                WikiLinkBox.Text = GetWikiLink();
            }));
        }

        private string GetWikiLink(bool English = false)
        {
            if(English)
            {
                if (versionChoosed.versionType == MCVType.Snapshot || versionChoosed.versionType == MCVType.Release)
                    return "https://minecraft.fandom.com/wiki/Java_Edition_" + versionChoosed.id;
                else
                    throw new Exception("暂不支持");
            }
            else
            {
                if (versionChoosed.versionType == MCVType.Snapshot)
                    return "https://minecraft.fandom.com/zh/wiki/" + versionChoosed.id;
                else
                    return "https://minecraft.fandom.com/zh/wiki/Java版" + versionChoosed.id;
            }
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
            dlg.Filter = "Markdown文件|*.md";
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

        private void ExportItemButton_Click(object sender, RoutedEventArgs e)
        {
            cardList.Add(new VersionCard(versionChoosed.id, versionChoosed.versionType, HeaderImgLinkBox.Text, GetMdCode(), WikiLinkBox.Text, MCBBSLinkBox.Text, MCWebsizeLinkBox.Text, footnNoteBox.Text, FatherVersionBox.Text));
        }

        private void ImportHeaderImgBtn_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Filter = "png图片格式|*.png|jpg图片格式|*.jpg";
            dlg.Title = "读取文件";
            if(dlg.ShowDialog().Value)
            {
                ImportHeaderImg(dlg.FileName);
                MessageBox.Show("导入成功！");
            }
        }

        private void ImportHeaderImg(string sourcePath,bool move = false)
        {
            string imgDire = $"{ProjectPath}{genePath}\\newsimgs\\{FatherVersionBox.Text.Replace('.', '_')}";
            if (!Directory.Exists(imgDire))
                Directory.CreateDirectory(imgDire);
            if (Path.GetExtension(sourcePath) == ".webp")
            {
                MessageBox.Show("检测为Webp文件，即将帮您转换为jpg格式");
                //WebPConvertCopy(filePath, $"{imgDire}\\{versionChoosed.id}.jpg", ImageFormat.Jpeg);
                //HeaderImgLinkBox.Text = $"{headerImgLinkRoot}{FatherVersionBox.Text.Replace('.', '_')}/{versionChoosed.id}.jpg";
            }
            else
            {
                string dist = $"{imgDire}\\{versionChoosed.id}{Path.GetExtension(sourcePath)}";
                HeaderImgLinkBox.Text = $"{headerImgLinkRoot}{FatherVersionBox.Text.Replace('.', '_')}/{versionChoosed.id}{Path.GetExtension(sourcePath)}";//自动填写文件
                if (File.Exists(dist))
                    if (MessageBox.Show("警告！这将覆盖原有文件!是否覆盖?", "警告", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                        File.Delete(dist);
                    else
                        return;
                if (move)
                    File.Move(sourcePath,dist);
                else
                    File.Copy(sourcePath,dist);
                VersionImagePreview.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(dist));//塞进预览框  
            }
        }
        #endregion

        //首页管理
        #region ContentCardList
        ObservableCollection<ContentCard> cardList;
        int ctnSelPos
        {
            get { return ContentListBox.Items.IndexOf(ContentListBox.SelectedItem); }
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ProjectPath == null)
            {
                Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
                dlg.Filter = "新闻主页工程文件|*.nhpj";
                dlg.Title = "打开新闻主页工程文件";
                if (dlg.ShowDialog().Value)
                {
                    ProjectPath = Path.GetDirectoryName(dlg.FileName) + "\\";
                    SaveContent();
                }
            }
            else
            {
                SaveContent();
                MessageBox.Show("保存成功！");
            }
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

        private void EditBtn_Click(object sender, RoutedEventArgs e)
        {
            //ProgressBarWindow pbw = new ProgressBarWindow("正在生成主页代码");
            //ThreadHelper.RunInNewThread(pbw.Show);
            //pbw.Show();
        }

        private void GeneBtn_Click(object sender, RoutedEventArgs e)
        {
            Thread thread = new Thread(GeneCodeThread);
            thread.Start();
        }
        #endregion
        #region ResourseCardList
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
                string filePath = dlg.FileName;
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
                        card.Add("mdcode", code);
                        resList.Add((VersionCard)card);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
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
            if (ResListBox.SelectedIndex != -1)
            {
                cardList.Add(resList[ResListBox.SelectedIndex]);
            }
        }
        #endregion

        string genePath = "PCL2-NewsHomepage";
        public void GeneCodeThread()
        {
            string code = string.Empty;
            Dispatcher.BeginInvoke(new Action(delegate
            {
                code = GenerateXAMLCode();
                
                Clipboard.SetText(code);
                File.WriteAllText($"{ProjectPath}{genePath}\\News.xaml", code);
                SaveContent();
                MessageBox.Show("已复制到剪贴板并生成文件");
            }));
        }

        string GenerateXAMLCode()
        {
            string result = string.Empty;
            ResourceHelper resourceHelper = new ResourceHelper();

            if(projectPath == null)
            {
                MessageBox.Show("您还尚未读取主页，请先读取主页！");
                string _ = ProjectPath;
            }

            int taskSum = cardList.Count + 6;
            int taskNow = 0;
            ProgressBarWindow progressBarWindow = new ProgressBarWindow("生成代码中", taskSum, (sender, e) =>
            {
                BackgroundWorker bgWorker = sender as BackgroundWorker;
                int endNumber = 0;
                if (e.Argument != null)
                {
                    endNumber = (int)e.Argument;
                }
                bgWorker.ReportProgress(taskNow, "生成 新闻主页开头注释");
                result += resourceHelper.GetStr("Mark");//1
                bgWorker.ReportProgress(taskNow++, "生成 新闻主页动画");

                result += resourceHelper.GetStr("Stut1");
                result += File.ReadAllText(ProjectPath + "Animation.xaml");//2
                bgWorker.ReportProgress(taskNow++, "生成 新闻主页样式");

                result += resourceHelper.GetStr("Stut2");
                result += File.ReadAllText(ProjectPath + "Style.xaml");//3
                result += resourceHelper.GetStr("Stut3");
                bgWorker.ReportProgress(taskNow++, "生成 新闻主页卡片表");

                foreach (var card in cardList)
                {
                    bgWorker.ReportProgress(taskNow++, $"生成 {card.name} 中");
                    //StepCompleted(taskSum, taskNow++, "生成 " + card.displayTitle + "卡片中");
                    result += card.GetCode();
                }

                bgWorker.ReportProgress(taskNow++, "生成 刷新栏");
                result += resourceHelper.GetStr("Stut4");
                result += File.ReadAllText(ProjectPath + "RefreshBar.xaml");//4

                bgWorker.ReportProgress(taskNow++, "生成 Footer");
                result += File.ReadAllText(ProjectPath + "Footer.xaml");//5
                result += resourceHelper.GetStr("StutEnd");

                bgWorker.ReportProgress(taskNow++, "格式化代码");
                result = Formatter.FormatXAML(result); //6
                bgWorker.ReportProgress(taskNow++, "生成完毕！");
                Thread.Sleep(500);

            });
            progressBarWindow.ShowDialog();
            return result;
        }

        #region FileIO
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
                    string folder = card.type;
                    folder += 's';  //e.g. Version -> Versions
                    if (card.type == "Version")
                    {
                        string jsonpath = ProjectPath + folder + "\\" + path + ".json";//定义文件位置
                        JArray sources = new JArray();
                        if (File.Exists(jsonpath))
                        {
                            sources = JArray.Parse(File.ReadAllText(jsonpath));
                        }
                        sources = EditJson(sources, card , ProjectPath + folder + "\\");
                        File.WriteAllText(jsonpath, sources.ToString());
                    }
                    if (card.type == "Custom")
                    {
                        string jsonpath = ProjectPath + folder + "\\" + path + ".json";
                        JArray sources = new JArray();
                        if (File.Exists(jsonpath))
                            sources = JArray.Parse(File.ReadAllText(jsonpath));
                        bool needAdd = true;
                        for(int i = 0; i < sources.Count; i++)
                        {
                            if (sources[i]["name"] != null && sources[i]["name"].ToString() == card.name)
                            {
                                sources[i] = card.ToJObject(false);
                                needAdd = false;
                                break;
                            }
                        }
                        if(needAdd)
                            sources.Add(card.ToJObject(false));
                        File.WriteAllText(jsonpath, sources.ToString());
                    }
                }
                pairs.Add("type", card.type);
                if(card.isSwaped)
                    pairs.Add("isswaped", card.isSwaped);
                array.Add(pairs);
            }
            File.WriteAllText(ProjectPath + projectHPFileName, array.ToString());
        }

        string projectPath = null;
        string ProjectPath
        {
            get
            {
                if(projectPath == null)
                {
                    try
                    {
                        Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
                        dlg.Filter = "新闻主页工程文件|*.nhpj";
                        dlg.Title = "打开新闻主页工程文件";
                        if (dlg.ShowDialog().Value)
                        {
                            projectPath = Path.GetDirectoryName(dlg.FileName) + "\\";
                            loadContent(projectPath);
                            MessageBox.Show("读取成功！");
                            ThreadHelper.RunInNewThread(new Action(delegate {
                                Dispatcher.BeginInvoke(new Action(delegate{
                                    VersionCreatePage.IsEnabled = true;
                                }));
                            }));
                            return projectPath;
                        }
                        else
                        {
                            throw new Exception("请打开新闻主页工程文件以继续");
                        }
                    }
                    catch(Exception ex)
                    {
                        throw new Exception(ex.Message);
                    }
                }
                else
                    return projectPath;
            }
            set
            {
                projectPath = value;
            }
        }

        private void LoadBtn_Click(object sender, RoutedEventArgs e)
        {
            loadContent(ProjectPath);
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
                        case "Normal":
                            card = new VersionCard(obj, projectPath);
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
        #endregion

        private void FormatCodeBtn_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(Formatter.FormatXAML(FormatCodeBox.Text));
            MessageBox.Show("已复制到剪贴板！");
        }

        private void AutoFillLinkButton_Click(object sender, RoutedEventArgs e)
        {
            if (FatherVersionBox.Text != "")
                ThreadHelper.RunInNewThread(AutoFillLinkThread);
            else
                MessageBox.Show("请先填写所属版本！");
        }

        private void AutoFillLinkThread()
        {
            Dispatcher.BeginInvoke(new Action(delegate
            {
                AutoFillLink();
            }));
        }

        private void AutoFillLink()
        {
            int taskSum = 12;
            ProgressBarWindow progressBarWindow = new ProgressBarWindow("正在自动填写链接表", taskSum, (sender, e) =>
            {
                BackgroundWorker bgWorker = sender as BackgroundWorker;
                int endNumber = 0;
                if (e.Argument != null)
                {
                    endNumber = (int)e.Argument;
                }
                bgWorker.ReportProgress(0, "自动生成Wiki链接中");
                AutoFillWikiLink();//1
                bgWorker.ReportProgress(1, "获取Wiki信息中");
                string response = string.Empty;
                bool getWikiErr = false;
                try
                {
                    response = HttpHelper.GetHttpStringResponseString(GetWikiLink());//2
                    bgWorker.ReportProgress(3, "获取Wiki信息成功！");
                }
                catch (Exception ex)
                {
                    bgWorker.ReportProgress(2, $"中文Wiki获取失败...{ex}\n正在获取英文Wiki");
                    try
                    {
                        response = HttpHelper.GetHttpStringResponseString(GetWikiLink(true));//2
                        bgWorker.ReportProgress(3, "获取Wiki信息成功！");
                    }
                    catch (Exception ex2)
                    {
                        bgWorker.ReportProgress(3, $"获取失败...{ex2}\n正在跳过");
                        getWikiErr = true;
                    }
                }
                if (!getWikiErr)
                {
                    bool flag = false;
                    AnlyWiki: bgWorker.ReportProgress(3, "解析Wiki信息中");
                    int index = response.IndexOf("https://minecraft.net/article/minecraft");
                    if(index == -1)
                        index = response.IndexOf("https://minecraft.net/zh-hans/article/minecraft");
                    if (index == -1)
                        index = response.IndexOf("https://www.minecraft.net/article/minecraft");
                    if (index == -1)
                        index = response.IndexOf("https://www.minecraft.net/zh-hans/article/minecraft");
                    if (index == -1)
                    {
                        if (flag)
                            getWikiErr = true;
                        else
                            try
                            {
                                bgWorker.ReportProgress(3, "中文Wiki解析失败，正在尝试解析为英文Wiki");
                                response = HttpHelper.GetHttpStringResponseString(GetWikiLink(true));//2
                                bgWorker.ReportProgress(3, "获取英文Wiki信息成功！");
                                flag = true;
                                goto AnlyWiki;
                            }
                            catch (Exception ex)
                            {
                                bgWorker.ReportProgress(3, $"获取失败...{ex}\n正在跳过");
                                getWikiErr =true;
                            }
                    }
                    else
                    {
                        bgWorker.ReportProgress(4, "解析成功！");
                        bgWorker.ReportProgress(4, "自动生成官网链接中");
                        string url = "";
                        for (int i = index; response[i] != '"'; i++)//直到URL结束
                        {
                            url += response[i];
                        }
                        Dispatcher.BeginInvoke(new Action(delegate
                        {
                            MCWebsizeLinkBox.Text = url;
                        }));
                        bgWorker.ReportProgress(5, "官网链接已生成");
                        bgWorker.ReportProgress(5, "获取头图链接中");
                        index = response.IndexOf("infobox-imagearea");
                        if (index == -1) goto skip;
                        response = response.Remove(0, index);
                        index = response.IndexOf("infobox-rows");
                        if (index == -1) goto skip;
                        response =response.Remove(index);
                        index = response.IndexOf("/revision");
                        if (index == -1) goto skip;
                        response = response.Remove(index);
                        index = response.IndexOf("http");
                        if (index == -1) goto skip;
                        response = response.Remove(0, index);
                        bgWorker.ReportProgress(6, $"正在下载Wiki的头图：{response}");
                        string ext = "";
                        for (int i = response.Length - 1; i >= 0; i--)
                        {
                            ext = response[i] + ext;
                            if (response[i] == '.')
                                break;
                        }
                        string tempPath = System.AppDomain.CurrentDomain.BaseDirectory + "temp" + ext;
                        try {
                            HttpHelper.Download(response, tempPath);
                            bgWorker.ReportProgress(7, "下载成功，正在导入");
                            Dispatcher.BeginInvoke(new Action(delegate
                            {
                                ImportHeaderImg(tempPath, true);
                            }));
                            bgWorker.ReportProgress(8, "导入成功！");
                            goto endWiki;
                        }
                        catch (Exception ex) {
                            bgWorker.ReportProgress(9, $"下载头图...失败:{ex}");
                        }
                    skip: bgWorker.ReportProgress(9, "获取头图...跳过");
                    endWiki:;
                    }
                    //HeaderImgLinkBox.Text = headerImgLinkRoot;
                }
                if (getWikiErr)
                {
                   bgWorker.ReportProgress(4, "Wiki解析...跳过");
                   bgWorker.ReportProgress(5, "官网链接填入...跳过");
                    Dispatcher.BeginInvoke(new Action(delegate
                    {
                        MCWebsizeLinkBox.Text = "未生成，请手动复制";
                        HeaderImgLinkBox.Text = "未生成，请手动复制";
                    }));
                    bgWorker.ReportProgress(9, "获取头图...跳过");
                }
                //MCBBS
                bgWorker.ReportProgress(9, "正在获取MCBBS链接");
                try
                {
                    response = HttpHelper.GetHttpStringResponseString("https://www.mcbbs.net/forum-news-1.html");//2
                    response = response.Replace(" ", "");
                    bgWorker.ReportProgress(10, "获取MCBBS信息成功！");
                    bgWorker.ReportProgress(10, "解析MCBBS信息中");
                    int index = response.IndexOf($"Java版{versionChoosed.id}发布");
                    if (index == -1)
                    {
                        bgWorker.ReportProgress(12, $"解析失败...\n正在跳过");
                        Dispatcher.BeginInvoke(new Action(delegate
                        {
                            MCBBSLinkBox.Text = "未生成，请手动复制";
                        }));
                    }
                    else
                    {
                        string url = "";
                        int i;
                        for (i = index - 1; response[i] != '<'; i--) ;
                        for (; response[i] != '"'; i++) ;
                        i++;
                        for (; response[i] != '"'; i++)
                            url += response[i];
                        bgWorker.ReportProgress(11, "解析成功！");
                        Dispatcher.BeginInvoke(new Action(delegate
                        {
                            MCBBSLinkBox.Text = "https://www.mcbbs.net/" + url;
                        }));
                        bgWorker.ReportProgress(12, "MCBBS链接已生成");
                    }
                }
                catch (Exception ex)
                {
                    Dispatcher.BeginInvoke(new Action(delegate
                    {
                        MCBBSLinkBox.Text = "未生成，请手动复制";
                    }));
                    bgWorker.ReportProgress(12, $"获取失败...{ex}\n正在跳过");
                    getWikiErr = true;
                }
                bgWorker.ReportProgress(12, "自动填入结束");
                Thread.Sleep(500);

            });
            progressBarWindow.ShowDialog();
        }
    }

    public abstract class ContentCard
    {
        /// <summary> 获取或设置卡片是否折叠 </summary>
        public virtual bool isSwaped { get; set; }
        /// <summary> 获取或设置卡片的定义位置 </summary>
        public virtual string path { get; set; }
        /// <summary> 获取或设置卡片的ID名 </summary>
        public virtual string name { get; set; }
        /// <summary> 获取或设置卡片数据文件名 </summary>
        public virtual string dataFileName { get => name; }
        /// <summary> 获取卡片在编辑器中显示的名称 </summary>
        public abstract string GetDisplayTitle();
        /// <returns> 卡片XAML代码 </returns>
        public abstract string GetCode();

        /// <summary>
        /// 将 ContentCard 对象转为 Jobject 对象
        /// </summary>
        /// <param name="withdata">转换时是否包含数据（一般只有在保存时不包含）</param>
        public abstract JObject ToJObject(bool withdata = true);
        /// <summary>
        /// 返回该卡片包含数据
        /// </summary>
        /// <returns>卡片内数据</returns>
        public abstract string GetData();
        /// <summary>
        /// 获取卡片种类
        /// </summary>
        public abstract string type { get; }
        /// <summary>
        /// 获取卡片XAML代码
        /// </summary>
        public string displayTitle
        {
            get
            {
                return GetDisplayTitle();
            }
        }
        /// <summary>
        /// 获取卡片折叠状态文字显示
        /// </summary>
        public string statusText { 
            get
            {
                if (isSwaped)
                    return "▽";
                else
                    return "";
            }
        }

        /// <summary>
        /// 切换卡片折叠状态
        /// </summary>
        public void switchSwapStats()
        {
            isSwaped = !isSwaped;
        }

    }

    public class NormalCard : ContentCard
    {
        string title;
        string mdCode;
        public override string type { get => "Normal"; }
        public NormalCard(string Title, string MdCode,string Path, string Name = "", bool swaped = false)
        {
            title = Title;
            mdCode = MdCode;
            path = Path;
            name = (Name=="")?Title:Name;
            isSwaped= swaped;
        }
        public override string GetCode()
        {
            ResourceHelper rh = new ResourceHelper();
            MarkdownToXamlConverter converter = new MarkdownToXamlConverter(rh);
            string output = string.Empty;
            output += converter.Convert(mdCode);
            return output;
        }

        public NormalCard(JObject jobj, string rootpath)
        {
            JArray array = JArray.Parse(File.ReadAllText(rootpath + "Normals\\" + jobj["path"].ToString() + ".json"));
            foreach (JObject item in array)
            {
                if (item["name"].ToString() == jobj["name"].ToString())
                {
                    this.title = item["title"].ToString();
                    this.mdCode = File.ReadAllText(rootpath + "Normals\\" + item["name"] + ".md");
                    this.name = item["name"].ToString();
                    this.path = jobj["path"].ToString();
                    if (jobj["isswaped"] != null && jobj["isswaped"].ToString().ToLower() == "true")
                        isSwaped = true;
                    return;
                }
            }
            throw new Exception("无法找到信息");
        }

        public override JObject ToJObject(bool withdata = true)
        {
            JObject jobj = new JObject();
            jobj.Add("type", "normal");
            jobj.Add("title", title);
            jobj.Add("name", name);
            if (withdata)
                jobj.Add("mdCode", mdCode);
            jobj.Add("path", path);
            return jobj;
        }
        public override string GetData() => mdCode;
        public override string GetDisplayTitle() => title;
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
            //jobj.Add("path", path);
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
                    this.path = jobj["path"].ToString();
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
    public static class ThreadHelper
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

        public static void ShowWindowInNewThread(Window window)
        {
            Dispatcher dispatcher = window.Dispatcher;
            Action act = new Action(delegate
            {
                dispatcher.BeginInvoke(new Action(delegate
                {
                    window.Show();
                }));
            });
            ThreadStart childref = new ThreadStart(act);
            Thread childThread = new Thread(childref);

            Log("创建线程：" + childThread.ManagedThreadId);
            childThread.Start();
            Log("线程创建成功！");
        }

        public static void ShowDialogWindowInNewThread(Window window)
        {
            Dispatcher dispatcher = window.Dispatcher;
            Action act = new Action(delegate
            {
                dispatcher.BeginInvoke(new Action(delegate
                {
                    window.ShowDialog();
                }));
            });
            ThreadStart childref = new ThreadStart(act);
            Thread childThread = new Thread(childref);

            Log("ShowDialogWindowInNewThread 创建线程：" + childThread.ManagedThreadId);
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
