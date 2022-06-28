using System;

namespace WpfApp1
{
    public static partial class Debug
    {
        public static void Log(string log, int code = 0, string detil = null)
        {

            if (code == 0)
                System.Diagnostics.Debug.WriteLine("[Info]" + DateTime.Now.ToString(" hh:mm:ss:fffffff ") + log);
            else
            {
                System.Diagnostics.Debug.WriteLine("[Error]" + DateTime.Now.ToString(" hh:mm:ss:fffffff ") + "(" + code + ")" + log);
                System.Diagnostics.Debug.Fail("[Error]" + DateTime.Now.ToString(" hh:mm:ss:fffffff ") + "[" + code + "]" + log, detil);
            }

        }
    }
}