using System.IO;
using System.Xml;

namespace NewsHomepageHelper
{
    internal static class Formatter
    {
        public static string FormatXAML(string code)
        {
            XmlDocument myXmlDoc = new XmlDocument();
            myXmlDoc.LoadXml(code);
            code = xmlToString(myXmlDoc);
            code = code.Remove(0, 23);//移除XML标记
            code = code.Replace(" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\" xmlns:local=\"clr-namespace:PCL2\"", "");
            return code;
        }

        static string xmlToString(XmlDocument xmlDoc)
        {
            MemoryStream stream = new MemoryStream();
            XmlTextWriter writer = new XmlTextWriter(stream, null);
            writer.Formatting = Formatting.Indented;
            xmlDoc.Save(writer);
            StreamReader sr = new StreamReader(stream, System.Text.Encoding.UTF8);
            stream.Position = 0;
            string xmlString = sr.ReadToEnd();
            sr.Close();
            stream.Close();
            return xmlString;
        }
    }
}
