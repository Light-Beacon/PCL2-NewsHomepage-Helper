using static WpfApp1.Debug;
using static NewsHomepageHelper.StringHelper;
using System.Net;
using Newtonsoft.Json.Linq;
using System.Windows;
using System.Linq;

namespace WpfApp1
{
    public enum MCVType
    {
        Unkonwn,
        Release,
        Snapshot,
        Pre_Release,
        Release_Candidate,
        Experimental_Snaphot,
        Aprilfools_Version,
        OldAlpha,
        OldBeta,
        Others,
        All
    }

    public class MCVersion
    {
        public string id;
        private MCVType type = 0;
        public MCVType versionType
        {
            get
            {
                AutoFill();
                return type;
            }
            set { type = value; }
        }
        private void AutoFill()
        {
            if (type == MCVType.Unkonwn)
            {
                if (id.Contains("-rc"))
                {
                    type = MCVType.Release_Candidate;
                }
                else if (id.Contains("-pre"))
                {
                    type = MCVType.Pre_Release;
                }
                else if (IsNumChr(id[0]) && IsNumChr(id[1]) && id[2] == 'w' && IsNumChr(id[3]) && IsNumChr(id[4]) && IsLowerChr(id[5]))
                {
                    type = MCVType.Snapshot;
                }
                else
                    type = MCVType.Others;
            }

        }
    }
    public class MCVersions
    {
        static int latestrelease_id;
        static int latestsnapshot_id;
        public static MCVersion[] Versions;
        public MCVersion LatestVersion()
        {
            return Versions[0];
        }
        public MCVersion LastSnapshotVersion()
        {
            return Versions[latestsnapshot_id];
        }
        public MCVersion LastReleaseVersion()
        {
            return Versions[latestrelease_id];
        }
        public MCVersion SearchByIndex(int index)
        {
            return Versions[index];
        }
        public void GetVersions(string url = "https://launchermeta.mojang.com/mc/game/version_manifest.json")
        {
            Log("正在执行：GET " + url);
            HttpWebResponse rsp = HttpHelper.CreateGetHttpResponse(url, 1500, "", new CookieCollection());
            if (!rsp.StatusCode.Equals(HttpStatusCode.OK))
            {
                MessageBox.Show("无法连接到服务器...", "错误");
                return;
            }
            string response = HttpHelper.GetResponseString(rsp);
            Log("已接收来自" + url + "的网络回应");
            JObject Jrsp = JObject.Parse(response);
            JArray Jver = (JArray)Jrsp["versions"];
            Versions = new MCVersion[Jver.Count()];
            int i = 0;
            string _type;
            string _id;
            string _latestrelease = Jrsp["latest"]["release"].ToString();
            string _latestsnapshot = Jrsp["latest"]["snapshot"].ToString();
            foreach (var ver in Jrsp["versions"])
            {
                Versions[i] = new MCVersion();
                _type = Jver[i]["type"].ToString();
                _id = Jver[i]["id"].ToString();
                Versions[i].id = _id;
                if (_id == _latestrelease)
                    latestrelease_id = i;
                else if (_id == _latestsnapshot)
                    latestsnapshot_id = i;
                if (_type == "release")
                    Versions[i].versionType = MCVType.Release;
                else if (_type == "old_alpha")
                    Versions[i].versionType = MCVType.OldAlpha;
                else if (_type == "old_beta")
                    Versions[i].versionType = MCVType.OldBeta;
                //Log("版本读入： "+ _id + " " + _type);
                i++;
            }
            Log("读取成功");
        }
    }

}
