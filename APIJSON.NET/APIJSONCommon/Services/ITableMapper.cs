namespace APIJSON.NET.Services
{
    public interface ITableMapper
    {
        /// <summary>
        /// 表别名获取
        /// </summary>
        /// <param name="oldname"></param>
        /// <returns></returns>
          string GetTableName(string oldname);
    }
}
