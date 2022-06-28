namespace NewsHomepageHelper
{
    public static class StringHelper
    {
        public static bool CharBewteen(char chr, char front, char end)
        {
            if (chr < front || chr > end)
                return false;
            return true;
        }

        public static bool IsNumChr(char chr)
        {
            return CharBewteen(chr, '0', '9');
        }

        public static bool IsLowerChr(char chr)
        {
            return CharBewteen(chr, 'a', 'z');
        }

        public static bool EachCharBewteen(string str, char front, char end)
        {
            if (str.Length <= 0) return true;
            foreach (int chr in str)
            {
                if ((chr < front || chr > end) && chr != ' ')
                {
                    return false;
                }
            }
            return true;
        }

        public static bool IsNumStr(string str)
        {
            return EachCharBewteen(str, '0', '9');
        }

        public static bool IsUpperStr(string str)
        {
            return EachCharBewteen(str, 'A', 'Z');
        }

        public static bool IsLowerStr(string str)
        {
            return EachCharBewteen(str, 'a', 'z');
        }

        public static int Str2Int(string str)
        {
            int temp = 0;
            foreach (int chr in str)
            {
                if (chr < '0' || chr > '9')
                    return temp;
                temp *= 10;
                temp += chr - '1' + 1;
            }
            return temp;
        }
    }
}
