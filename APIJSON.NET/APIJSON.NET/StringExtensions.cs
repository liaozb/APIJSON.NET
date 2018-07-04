namespace APIJSON.NET
{
    using System;
    using System.Text.RegularExpressions;
    public static class StringExtensions
    {
        /// <summary>
        /// 是否合法表名（大写字母数字下划线 长度在1-15之间）
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool IsTable(this string str)
        {
            return Regex.IsMatch(str, @"^[a-zA-Z][a-zA-Z0-9_]{1,15}$");
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool IsField(this string str)
        {
            return Regex.IsMatch(str, @"^[a-zA-Z][a-zA-Z0-9_()]{1,15}$");
        }
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