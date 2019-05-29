using System;
using System.Linq;

namespace APIJSON.NET
{
    /// <summary>
    /// 自定义方法
    /// </summary>
    public class FuncList
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public string Merge(object a, object b)
        {
            return a.ToString() + b.ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public object MergeObj(object a, object b)
        {
            return new { a, b };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public bool isContain(object a, object b)
        {
            return a.ToString().Split(',').Contains(b);
        }
    }
}
