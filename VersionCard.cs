using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace WpfApp1
{
    public class VersionCard : ContentCard
    {
        string versionID;
        MCVType versionType;
        string headerImageUrl;
        string mdCode;
        string wikiUrl;
        string mcbbsUrl;
        string websiteUrl;
        string footnote;
        string title;
        string fatherVersion;
        bool latest;
        public override string name => versionID;
        public override string type => "Version";
        public override string path => fatherVersion;
        public override string dataFileName => fatherVersion + "\\" + versionID + ".md";
        public override string GetData()
        {
            return mdCode;
        }
        public bool isLatest
        {
            get { return latest; }
            set
            {
                if (!value)
                {
                    title = versionID;
                    isSwaped = true;
                }  
                else
                {
                    title = "最新" + GetVerTypeStr() + " - " + versionID;
                    isSwaped = false;
                }
                latest = value;
            }
        }

        public override string GetDisplayTitle()
        {
            return title;
        }

        public MCVType GetVersionType()
        {
            return versionType;
        }

        public override string GetCode()
        {
            return GenerateXAMLCode(mdCode);
        }

        private string GetVerTypeStr()
        {
            string versiontype;
            switch (versionType)
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
            return versiontype;
        }

        private string GenerateXAMLCode(string input)
        {
            string output = "";

            ResourceHelper rh = new ResourceHelper();
            MarkdownToXamlConverter converter = new MarkdownToXamlConverter(rh);

            //Begining
            output += rh.GetStr("Begin1");
            output += displayTitle;
            output += versionID;
            output += rh.GetStr("Begin3");
            output += (isSwaped ? "IsSwaped=\"True\"" : "");
            output += rh.GetStr("Begin4");
            output += headerImageUrl;
            output += rh.GetStr("Begin5");
            output += headerImageUrl;
            output += rh.GetStr("Begin6");
            output += versionID.ToUpper();
            output += rh.GetStr("BeginEnd");
            output += "\n";

            output += converter.Convert(input);

            output += rh.GetStr("Link1");
            output += wikiUrl;
            output += rh.GetStr("Link2");
            output += mcbbsUrl;
            output += rh.GetStr("Link3");
            output += websiteUrl;
            output += rh.GetStr("LinkEnd");

            output += rh.GetStr("FootNoteStart");
            output += footnote;
            output += rh.GetStr("FootNoteEnd");

            output += rh.GetStr("End");
            return output;
        }

        public VersionCard(string _version, MCVType _versionType, string _headerImageUrl, string _mdCode, string _wikiUrl, string _mcbbsUrl, string _websiteUrl, string _footnote, string _fatherVersion, bool isLatest = false,bool swaped = true)
        {
            this.versionID = _version;
            this.versionType = _versionType;
            this.headerImageUrl = _headerImageUrl;
            this.mdCode = _mdCode;
            this.wikiUrl = _wikiUrl;
            this.mcbbsUrl = _mcbbsUrl;
            this.websiteUrl = _websiteUrl;
            this.footnote = _footnote;
            this.latest = isLatest;
            this.fatherVersion = _fatherVersion;
            title = versionID;
            isSwaped = swaped;
        }

        public static explicit operator JObject(VersionCard card)
        {
            return card.ToJObject();
        }

        public override JObject ToJObject()
        {
            JObject jobj = new JObject();
            jobj.Add("name", name);
            jobj.Add("type", "version");
            jobj.Add("fatherversion", fatherVersion);
            jobj.Add("versionID", versionID);
            jobj.Add("versionType", versionType.ToString());
            jobj.Add("headerImageUrl", headerImageUrl);
            jobj.Add("wikiUrl", wikiUrl);
            jobj.Add("mcbbsUrl", mcbbsUrl);
            jobj.Add("websiteUrl", websiteUrl);
            jobj.Add("footnote", footnote);
            jobj.Add("mdcode", mdCode);
            return jobj;
        }

        public static explicit operator VersionCard(JObject jobj)
        {
            if (jobj["type"].ToString() != "Version")
                throw new Exception("卡片类型不符:" + jobj["versionID"].ToString());
            VersionCard result = new VersionCard(jobj["versionID"].ToString(), (MCVType)Enum.Parse(typeof(MCVType), jobj["versionType"].ToString()),
                 jobj["headerImageUrl"].ToString(),jobj["mdcode"].ToString(), jobj["wikiUrl"].ToString(), jobj["mcbbsUrl"].ToString(), jobj["websiteUrl"].ToString(), jobj["footnote"].ToString(), jobj["fatherversion"].ToString());
            if(jobj["isswaped"]!=null && jobj["isswaped"].ToString().ToLower()=="true")
                result.isSwaped = true;
            return result;
        }

        public VersionCard(JObject jobj,string rootpath)
        {
            JArray array = JArray.Parse(FileHelper.ReadFile(rootpath + "Versions\\" + jobj["path"].ToString() + ".json"));
            foreach(JObject item in array)
            {
                if(item["name"].ToString() == jobj["name"].ToString())
                {
                    this.versionID = item["versionID"].ToString();
                    this.versionType = (MCVType)Enum.Parse(typeof(MCVType), item["versionType"].ToString());
                    this.headerImageUrl = item["headerImageUrl"].ToString();
                    this.mdCode = FileHelper.ReadFile(rootpath + "Versions\\" + item["filename"]);
                    this.wikiUrl = item["wikiUrl"].ToString();
                    this.mcbbsUrl = item["mcbbsUrl"].ToString(); 
                    this.websiteUrl = item["websiteUrl"].ToString();
                    this.footnote = item["footnote"].ToString();
                    this.latest = false;
                    this.fatherVersion = item["fatherversion"].ToString();
                    title = versionID;
                    if (jobj["isswaped"] != null && jobj["isswaped"].ToString().ToLower() == "true")
                        isSwaped = true;
                    else
                        isSwaped = false;
                    return;
                }
            }
            throw new Exception("无法找到版本信息");
        }
    }
}
