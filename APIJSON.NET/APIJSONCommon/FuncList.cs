using System;
using System.Linq;

namespace APIJSON.NET
{
    /// <summary>
    /// 自定义方法
    /// </summary>
    public class FuncList
    {
        public string Merge(object a, object b)
        {
            return a.ToString() + b.ToString();
        }
        public object MergeObj(object a, object b)
        {
            return new { a, b };
        }
        public bool isContain(object a, object b)
        {
            return a.ToString().Split(',').Contains(b);
        }
    }
}
