namespace ApiJson.Common
{
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
    }
}