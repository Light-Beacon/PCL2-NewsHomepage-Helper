using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewsHomepageHelper
{
    public class ArticalPart
    {
        public string ExportCode;
    }

    public class ArticalHeader : ArticalPart
    {

    }

    public class ArticalList : ArticalPart
    {

    }

    public class ArticalListItem : ArticalPart
    {

    }

    public class ArticalLinks : ArticalPart
    {

    }

    public class ArticalFootNote : ArticalPart
    {

    }

    public class ArticalEnd : ArticalPart
    {
        private string ExportCode()
        {
            return Resource.End;
        }

    }
}
