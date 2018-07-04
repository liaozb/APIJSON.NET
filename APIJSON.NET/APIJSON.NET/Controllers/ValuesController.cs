namespace APIJSON.NET.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Data.SqlClient;
    using System.Web;
    using Dapper;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Options;
    using Newtonsoft.Json.Linq;
    [Route("api/[controller]")]
    [ApiController]
    public class JsonController : ControllerBase
    {
        private DapperOptions _options;
        public JsonController(IOptions<DapperOptions> options)
        {
            this._options = options.Value;
        }

        /// <summary>
        /// 查询
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        [HttpPost("/Query")]
        public ActionResult Query([FromBody]string json)
        {
            json = HttpUtility.UrlDecode(json);
            JObject ht = new JObject();
            ht.Add("code", "200");
            ht.Add("msg", "success");
            try
            {
                JObject jobject = JObject.Parse(json);
                foreach (var item in jobject)
                {
                    string key = item.Key.Trim();
                    if (key.Equals("[]"))
                    {
                        var htt = new JArray();
                        var jb = JObject.Parse(item.Value.ToString());
                        int page = jb["page"] == null ? 0 : int.Parse(jb["page"].ToString()), count = jb["count"] == null ? 0 : int.Parse(jb["count"].ToString())
                             , query = jb["query"] == null ? 0 : int.Parse(jb["query"].ToString());
                        jb.Remove("page");
                        jb.Remove("count");
                        List<string> tables = new List<string>();
                        List<string> where = new List<string>();
                        foreach (var t in jb)
                        {
                            tables.Add(t.Key);
                            where.Add(t.Value.ToString());
                        }
                        if (tables.Count > 0)
                        {
                            string table = tables[0];
                            var template = JsonToSql.GetSqlBuilder(table, page, count, where[0], null);
                            foreach (var dd in DapperDBHelp.Query(_options.ConnectionString, template.RawSql, template.Parameters))
                            {
                                var zht = new JObject();
                                zht.Add(table, JToken.FromObject(dd));
                                for (int i = 1; i < tables.Count; i++)
                                {
                                    string subtable = tables[i];

                                    if (tables[i].EndsWith("[]"))
                                    {
                                        subtable = tables[i].Replace("[]", "");
                                        var jbb = JObject.Parse(where[i]);
                                        page = jbb["page"] == null ? 0 : int.Parse(jbb["page"].ToString());
                                        count = jbb["count"] == null ? 0 : int.Parse(jbb["count"].ToString());
                                        template = JsonToSql.GetSqlBuilder(subtable, page, count, jbb[subtable].ToString(), zht);
                                        var lt = new JArray();
                                        foreach (var d in DapperDBHelp.Query(_options.ConnectionString, template.RawSql, template.Parameters))
                                        {
                                            lt.Add(JToken.FromObject(d));
                                        }
                                        zht.Add(tables[i], lt);
                                    }
                                    else
                                    {
                                        template = JsonToSql.GetSqlBuilder(subtable, 0, 0, where[i].ToString(), zht);
                                        var df = DapperDBHelp.QueryFirstOrDefault(_options.ConnectionString, template.RawSql, template.Parameters);
                                        if (df != null)
                                        {
                                            zht.Add(subtable, JToken.FromObject(df));
                                        }

                                    }
                                }
                                htt.Add(zht);
                            }
                        }
                        ht.Add("[]", htt);
                    }
                    else if (key.EndsWith("[]"))
                    {
                        var builder = new SqlBuilder();
                        var htt = new JArray();
                        var jb = JObject.Parse(item.Value.ToString());
                        int page = jb["page"] == null ? 0 : int.Parse(jb["page"].ToString()), count = jb["count"] == null ? 0 : int.Parse(jb["count"].ToString());
                        jb.Remove("page");
                        jb.Remove("count");
                        foreach (var t in jb)
                        {
                            var template = JsonToSql.GetSqlBuilder(t.Key, page, count, t.Value.ToString(), null);
                            foreach (var d in DapperDBHelp.Query(_options.ConnectionString, template.RawSql, template.Parameters))
                            {
                                htt.Add(JToken.FromObject(d));
                            }
                        }
                        ht.Add(key, htt);
                    }
                    else
                    {
                        var template = JsonToSql.GetSqlBuilder(key, 0, 0, item.Value.ToString(), ht);
                        var df = DapperDBHelp.QueryFirstOrDefault(_options.ConnectionString, template.RawSql, template.Parameters);
                        if (df != null)
                        {
                            ht.Add(key, JToken.FromObject(df));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ht["code"] = "500";
                ht["msg"] = ex.Message;

            }
            return Ok(ht);
        }
        /// <summary>
        /// 新增
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        [HttpPost("/add")]
        public ActionResult Add([FromBody]string json)
        {
            json = HttpUtility.UrlDecode(json);
            JObject ht = new JObject();
            ht.Add("code", "200");
            ht.Add("msg", "success");
            try
            {
                JObject jobject = JObject.Parse(json);
                var sb = new System.Text.StringBuilder(100);
                foreach (var item in jobject)
                {
                    string key = item.Key.Trim();
                    sb.Append($"insert into [{key}](");
                    var val = new System.Text.StringBuilder(100);
                    val.Append($")values(");
                    var p = new DynamicParameters();
                    foreach (var f in JObject.Parse(item.Value.ToString()))
                    {
                        sb.Append($"{f.Key},");
                        val.Append($"@{f.Key},");
                        p.Add($"@{f.Key}", f.Value.ToString());
                    }
                    string sql = sb.ToString().TrimEnd(',') + val.ToString().TrimEnd(',') + ");SELECT CAST(SCOPE_IDENTITY() as int);";

                    using (var sqlConnection = new SqlConnection(_options.ConnectionString))
                    {
                        sqlConnection.Open();
                        int id = sqlConnection.ExecuteScalar<int>(sql, p);
                        ht.Add(key, JToken.FromObject(new { code = 200, msg = "success", id }));
                    }
                }

            }
            catch (Exception ex)
            {

                ht["code"] = "500";
                ht["msg"] = ex.Message;
            }
            return Ok(ht);
        }
        /// <summary>
        /// 修改
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        [HttpPost("/edit")]
        public ActionResult Edit([FromBody]string json)
        {
            json = HttpUtility.UrlDecode(json);
            JObject ht = new JObject();
            ht.Add("code", "200");
            ht.Add("msg", "success");
            try
            {
                JObject jobject = JObject.Parse(json);
                foreach (var item in jobject)
                {
                    string key = item.Key.Trim();
                    var value = JObject.Parse(item.Value.ToString());
                    var sb = new System.Text.StringBuilder(100);

                    sb.Append($"update [{key}] set ");
                    if (!value.ContainsKey("id"))
                    {
                        ht["code"] = "500";
                        ht["msg"] = "未传主键id";
                        break;
                    }
                    var p = new DynamicParameters();
                    foreach (var f in value)
                    {
                        if (f.Key.ToLower() != "id")
                        {
                            sb.Append($"{f.Key}=@{f.Key},");
                        }

                        p.Add($"@{f.Key}", f.Value.ToString());
                    }
                    string sql = sb.ToString().TrimEnd(',') + " where id=@id;";
                    using (var sqlConnection = new SqlConnection(_options.ConnectionString))
                    {
                        sqlConnection.Open();
                        sqlConnection.Execute(sql, p);
                        ht.Add(key, JToken.FromObject(new { code = 200, msg = "success", id = value["id"].ToString() }));
                    }
                }
            }
            catch (Exception ex)
            {

                ht["code"] = "500";
                ht["msg"] = ex.Message;
            }
            return Ok(ht);
        }
        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        [HttpPost("/remove")]
        public ActionResult Remove([FromBody]string json)
        {
            json = HttpUtility.UrlDecode(json);
            JObject ht = new JObject();
            ht.Add("code", "200");
            ht.Add("msg", "success");
            try
            {
                JObject jobject = JObject.Parse(json);
                foreach (var item in jobject)
                {
                    string key = item.Key.Trim();
                    var value = JObject.Parse(item.Value.ToString());
                    var sb = new System.Text.StringBuilder(100);

                    sb.Append($"delete [{key}] where");
                    if (!value.ContainsKey("id"))
                    {
                        ht["code"] = "500";
                        ht["msg"] = "未传主键id";
                        break;
                    }
                    var p = new DynamicParameters();
                    foreach (var f in value)
                    {
                        sb.Append($"{f.Key}=@{f.Key},");

                        p.Add($"@{f.Key}", f.Value.ToString());
                    }
                    string sql = sb.ToString().TrimEnd(',');
                    using (var sqlConnection = new SqlConnection(_options.ConnectionString))
                    {
                        sqlConnection.Open();
                        sqlConnection.Execute(sql, p);
                        ht.Add(key, JToken.FromObject(new { code = 200, msg = "success", id = value["id"].ToString() }));
                    }
                }
            }
            catch (Exception ex)
            {

                ht["code"] = "500";
                ht["msg"] = ex.Message;
            }
            return Ok(ht);
        }
    }
}