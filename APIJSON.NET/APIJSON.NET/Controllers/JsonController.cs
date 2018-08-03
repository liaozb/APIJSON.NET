namespace APIJSON.NET.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Web;
    using APIJSON.NET.Models;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Options;
    using Newtonsoft.Json.Linq;
    using SqlSugar;
    using System.Linq;
    using APIJSON.NET.Services;
    using System.Reflection;

    [Route("api/[controller]")]
    [ApiController]
    public class JsonController : ControllerBase
    {

        private SelectTable selectTable;
        private DbContext db;
        private readonly IIdentityService _identitySvc;
        public JsonController(SelectTable _selectTable, DbContext _db,IIdentityService identityService)
        {

            selectTable = _selectTable;
            db = _db;
            _identitySvc = identityService;
        }
        
        /// <summary>
        /// 查询
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        [HttpPost("/get")]

        public ActionResult Query([FromBody]string json)
        {
            json = HttpUtility.UrlDecode(json);
            JObject ht = new JObject();
            ht.Add("code", "200");
            ht.Add("msg", "success");
            try
            {
                JObject jobject = JObject.Parse(json);
                int page = 0, count = 0, query = 0, total = 0;
                foreach (var item in jobject)
                {
                    string key = item.Key.Trim();
                    JObject jb;
                    if (key.Equals("[]"))
                    {
                        jb = JObject.Parse(item.Value.ToString());
                        page = jb["page"] == null ? 0 : int.Parse(jb["page"].ToString());
                        count = jb["count"] == null ? 0 : int.Parse(jb["count"].ToString());
                        query = jb["query"] == null ? 0 : int.Parse(jb["query"].ToString());
                        jb.Remove("page"); jb.Remove("count"); jb.Remove("query");
                        var htt = new JArray();
                        List<string> tables = new List<string>(), where = new List<string>();
                        foreach (var t in jb)
                        {
                            tables.Add(t.Key); where.Add(t.Value.ToString());
                        }
                        if (tables.Count > 0)
                        {
                            string table = tables[0];
                            var temp = selectTable.GetTableData(table, page, count, where[0], null);
                            if (query > 0)
                            {
                                total = temp.Item2;
                            }

                            foreach (var dd in temp.Item1)
                            {
                                var zht = new JObject();
                                zht.Add(table, JToken.FromObject(dd));
                                for (int i = 1; i < tables.Count; i++)
                                {
                                    string subtable = tables[i];
                                    if (subtable.EndsWith("[]"))
                                    {
                                        subtable = subtable.TrimEnd("[]".ToCharArray());
                                        var jbb = JObject.Parse(where[i]);
                                        page = jbb["page"] == null ? 0 : int.Parse(jbb["page"].ToString());
                                        count = jbb["count"] == null ? 0 : int.Parse(jbb["count"].ToString());

                                        var lt = new JArray();
                                        foreach (var d in selectTable.GetTableData(subtable, page, count, jbb[subtable].ToString(), zht).Item1)
                                        {
                                            lt.Add(JToken.FromObject(d));
                                        }
                                        zht.Add(tables[i], lt);
                                    }
                                    else
                                    {
                                        var ddf = selectTable.GetFirstData(subtable, where[i].ToString(), zht);
                                        if (ddf != null)
                                        {
                                            zht.Add(subtable, JToken.FromObject(ddf));

                                        }
                                    }
                                }
                                htt.Add(zht);
                            }

                        }
                        if (query != 1)
                        {
                            ht.Add("[]", htt);
                        }
                    }
                    else if (key.EndsWith("[]"))
                    {
                        jb = JObject.Parse(item.Value.ToString());
                        page = jb["page"] == null ? 0 : int.Parse(jb["page"].ToString());
                        count = jb["count"] == null ? 0 : int.Parse(jb["count"].ToString());
                        query = jb["query"] == null ? 0 : int.Parse(jb["query"].ToString());
                        jb.Remove("page"); jb.Remove("count"); jb.Remove("query");
                        var htt = new JArray();
                        foreach (var t in jb)
                        {
                            foreach (var d in selectTable.GetTableData(t.Key, page, count, t.Value.ToString(), null).Item1)
                            {
                                htt.Add(JToken.FromObject(d));
                            }
                        }
                        ht.Add(key, htt);
                    }
                    else if (key.Equals("func"))
                    {
                        jb = JObject.Parse(item.Value.ToString());
                        Type type = typeof(FuncList);
                        Object obj = Activator.CreateInstance(type);
                        var bb = new JObject();
                        foreach (var f in jb)
                        {
                            var types = new List<Type>();
                            var param = new List<object>();
                            foreach (var va in JArray.Parse(f.Value.ToString()))
                            {
                                types.Add(typeof(object));
                                param.Add(va);
                            }
                            bb.Add(f.Key, JToken.FromObject(selectTable.ExecFunc(f.Key,param.ToArray(), types.ToArray())));
                        }
                        ht.Add("func", bb);
                    }
                    else if (key.Equals("total@"))
                    {
                        ht.Add("total", total);
                    }
                    else
                    {
                        var template = selectTable.GetFirstData(key, item.Value.ToString(), ht);
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
                    var role = _identitySvc.GetRole();
                    if (!role.Insert.Table.Contains(key, StringComparer.CurrentCultureIgnoreCase))
                    {
                        ht["code"] = "500";
                        ht["msg"] = $"没权限添加{key}";
                        break;
                    }
                    var dt = new Dictionary<string, object>();
                    foreach (var f in JObject.Parse(item.Value.ToString()))
                    {
                        if (f.Key.ToLower() != "id" && role.Insert.Column.Contains(f.Key, StringComparer.CurrentCultureIgnoreCase))
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
                    var role = _identitySvc.GetRole();
                    if (!role.Update.Table.Contains(key, StringComparer.CurrentCultureIgnoreCase))
                    {
                        ht["code"] = "500";
                        ht["msg"] = $"没权限修改{key}";
                        break;
                    }
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
                        if (f.Key.ToLower() != "id"&& role.Update.Column.Contains(f.Key, StringComparer.CurrentCultureIgnoreCase))
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
                var role = _identitySvc.GetRole();
                JObject jobject = JObject.Parse(json);
                foreach (var item in jobject)
                {
                    string key = item.Key.Trim();
                    var value = JObject.Parse(item.Value.ToString());
                    var sb = new System.Text.StringBuilder(100);
                    sb.Append($"delete [{key}] where");
                    if (role.Delete==null||role.Delete.Table==null)
                    {
                        ht["code"] = "500";
                        ht["msg"] = "delete权限未配置";
                        break;
                    }
                    if (!role.Delete.Table.Contains(key,StringComparer.CurrentCultureIgnoreCase))
                    {
                        ht["code"] = "500";
                        ht["msg"] = $"没权限删除{key}";
                        break;
                    }
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