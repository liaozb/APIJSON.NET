using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace APIJSON.NET
{
    public class MethodList
    {
        public string Merge(string a, string b)
        {
            return a + b;
        }
        public object MergeObj(string a, string b)
        {
            return new { a, b };
        }
    }
}
