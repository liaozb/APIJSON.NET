namespace APIJSON.NET
{
    using System;
    using System.Text.RegularExpressions;
    public static class StringExtensions
    {
       
        /// <summary>
        /// 是否有值
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool IsValue(this object str)
        {
            return str != null && !string.IsNullOrEmpty(str.ToString());
        }
        public static string GetParamName(this string param)
        {
            return param + new Random().Next(1, 100);
        }
     
    }
}