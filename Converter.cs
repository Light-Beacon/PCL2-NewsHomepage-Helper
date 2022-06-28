using System;
using static WpfApp1.Debug;

namespace WpfApp1
{
    public class MarkdownToXamlConverter
    {
        private ResourceHelper resourceHelper;
        public MarkdownToXamlConverter(ResourceHelper helper)
        {
            resourceHelper = helper;
        }
        public string Convert(string str)
        {
            string xamlCode = string.Empty;
            string buffer = string.Empty;
            int listDeepth = 0;
            bool skipnext = false;//skip \n behind \r
            foreach (char ch in str)
            {
                if (skipnext)
                {
                    skipnext = false;
                    if (ch == '\n')
                        continue;
                }
                if ((ch == '\r' || ch == '\n') && buffer.Length > 1)
                {
                    if (ch == '\r')
                        skipnext = true;
                    int level = 0;
                    switch (buffer[0])
                    {
                        case '#':
                            //Title
                            while (listDeepth > 0)
                            {
                                xamlCode += resourceHelper.GetStr("ListEnd") + "\n";
                                listDeepth--;
                            }
                            level = GetElementLevel(buffer, '#');
                            if (level == 1)
                            {
                                Log("存在一级标题", 6001);
                            }
                            if (level > 4)
                            {
                                Log("标题等级大于4", 6002);
                            }
                            xamlCode += resourceHelper.GetStr("Title1");
                            xamlCode += buffer.Remove(0, level + 1);
                            xamlCode += resourceHelper.GetStr("Title2");
                            xamlCode += level.ToString();
                            xamlCode += resourceHelper.GetStr("TitleEnd");
                            xamlCode += "\n";
                            break;
                        case '+':
                        case '-':
                        case '*':
                            //List
                            level = Math.Max(GetElementLevel(buffer, '+'), Math.Max(GetElementLevel(buffer, '-'), GetElementLevel(buffer, '*')));
                            while (level > listDeepth)
                            {
                                xamlCode += resourceHelper.GetStr("ListStart") + "\n";
                                listDeepth++;
                            }
                            while (level < listDeepth)
                            {
                                xamlCode += resourceHelper.GetStr("ListEnd") + "\n";
                                listDeepth--;
                            }
                            xamlCode += resourceHelper.GetStr("ListItemStart");
                            xamlCode += buffer.Remove(0, level + 1);
                            xamlCode += resourceHelper.GetStr("ListItemEnd") + "\n";
                            break;
                        default:
                            xamlCode += buffer + "\n";
                            while (listDeepth > 0)
                            {
                                xamlCode += resourceHelper.GetStr("ListEnd") + "\n";
                                listDeepth--;
                            }
                            break;
                    }
                    buffer = string.Empty;
                }
                else
                {
                    buffer += ch;
                }
                while (listDeepth > 0)
                {
                    xamlCode += resourceHelper.GetStr("ListEnd") + "\n";
                    listDeepth--;
                }
            }
            return xamlCode;
        }

        private int GetElementLevel(string str, char targetchr)
        {
            int level = 0;
            foreach (char chr in str)
            {
                if (chr == targetchr)
                    level++;
                else
                {
                    if (chr == ' ')
                        break;
                    else
                        return 0;
                }
            }
            return level;
        }
    }
}