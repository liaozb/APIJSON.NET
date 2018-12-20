namespace ApiJson.Common
{
    using SqlSugar;
    public class DbOptions
    {
        public DbType DbType { get; set; }
        public string ConnectionString { get; set; }
    }
}
