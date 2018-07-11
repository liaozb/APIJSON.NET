namespace APIJSON.NET.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Web;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Options;
    using Newtonsoft.Json.Linq;
    using SqlSugar;

    [Route("api/[controller]")]
    [ApiController]
    public class JsonController : ControllerBase
    {
        private DbOptions _options;
        private JsonToSql sqlbuilder;
        private DbContext db;
        public JsonController(IOptions<DbOptions> options, JsonToSql jsonToSql, DbContext _db)
        {
            _options = options.Value;
            sqlbuilder = jsonToSql;
            db = _db;
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
                            var template = sqlbuilder.GetTableData(table, page, count, where[0], null);
                            foreach (var dd in template)
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
                                        template = sqlbuilder.GetTableData(subtable, page, count, jbb[subtable].ToString(), zht);
                                        var lt = new JArray();
                                        foreach (var d in template)
                                        {
                                            lt.Add(JToken.FromObject(d));
                                        }
                                        zht.Add(tables[i], lt);
                                    }
                                    else
                                    {
                                        template = sqlbuilder.GetTableData(subtable, 0, 0, where[i].ToString(), zht);

                                        if (template != null)
                                        {
                                            zht.Add(subtable, JToken.FromObject(template));
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
                      
                        var htt = new JArray();
                        var jb = JObject.Parse(item.Value.ToString());
                        int page = jb["page"] == null ? 0 : int.Parse(jb["page"].ToString()), count = jb["count"] == null ? 0 : int.Parse(jb["count"].ToString());
                        jb.Remove("page");
                        jb.Remove("count");
                        foreach (var t in jb)
                        {
                            var template = sqlbuilder.GetTableData(t.Key, page, count, t.Value.ToString(), null);
                            foreach (var d in template)
                            {
                                htt.Add(JToken.FromObject(d));
                            }
                        }
                        ht.Add(key, htt);
                    }
                    else
                    {
                        var template = sqlbuilder.GetTableData(key, 0, 0, item.Value.ToString(), ht);
                        if (template != null)
                        {
                            ht.Add(key, JToken.FromObject(template));
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

                    var dt = new Dictionary<string, object>();
                    foreach (var f in JObject.Parse(item.Value.ToString()))
                    {
                        dt.Add(f.Key, f.Value);
                    }
                   
                    int id = db.Db.Insertable(dt).AS(key).ExecuteReturnIdentity();
                    ht.Add(key, JToken.FromObject(new { code = 200, msg = "success", id }));

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
                    if (!value.ContainsKey("id"))
                    {
                        ht["code"] = "500";
                        ht["msg"] = "未传主键id";
                        break;
                    }
                    var dt = new Dictionary<string, object>();
                    dt.Add("id", value["id"]);
                    foreach (var f in value)
                    {
                        if (f.Key.ToLower() != "id")
                        {
                            dt.Add(f.Key, f.Value);
                        }
                    }
                    db.Db.Updateable(dt).AS(key).ExecuteCommand();
                    ht.Add(key, JToken.FromObject(new { code = 200, msg = "success", id = value["id"].ToString() }));
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
                    var p = new List<SugarParameter>();
                    foreach (var f in value)
                    {
                        sb.Append($"{f.Key}=@{f.Key},");

                        p.Add(new SugarParameter($"@{f.Key}", f.Value.ToString()));
                    }
                   
                    string sql = sb.ToString().TrimEnd(',');
                    db.Db.Ado.ExecuteCommand(sql, p);
                    ht.Add(key, JToken.FromObject(new { code = 200, msg = "success", id = value["id"].ToString() }));
                   
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