namespace APIJSON.NET.Controllers
{
    using ApiJson.Common;
    using ApiJson.Common.Services;
    using Microsoft.AspNetCore.Cors;
    using Microsoft.AspNetCore.Mvc;
    using Newtonsoft.Json.Linq;
    using SqlSugar;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;
    using System.Web;

    [Route("api/[controller]")]
    [ApiController]
    [EnableCors("localhost")]
    public class JsonController : ControllerBase
    {

        private SelectTable _selectTable;
        private DbContext _db;
        private readonly IIdentityService _identitySvc;
        private ITableMapper _tableMapper;
        public JsonController(ITableMapper tableMapper, DbContext db, IIdentityService identityService)
        {
            _db = db;
            _identitySvc = identityService;
            _tableMapper = tableMapper;
            _selectTable = new SelectTable(_identitySvc, _tableMapper, _db.Db);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public ActionResult Test()
        {
            string str = "{\"page\":1,\"count\":3,\"query\":2,\"Org\":{\"@column\":\"Id,Name\"}}";
            var content = new StringContent(str);

            HttpClient hc = new HttpClient();
            var response = hc.PostAsync("http://localhost:89/api/json/org", content).Result;
            string result = (response.Content.ReadAsStringAsync().Result);//result就是返回的结果。
            return Content(result);
        }

        [HttpPost("{table}")]

        public async Task<ActionResult> Query1([FromRoute]string table)
        {
            string json = string.Empty;
            using (StreamReader reader = new StreamReader(Request.Body, Encoding.UTF8))
            {
                json = await reader.ReadToEndAsync();
            }

            json = HttpUtility.UrlDecode(json);
            JObject ht = new JObject();

            JObject jobject = JObject.Parse(json);
            ht.Add(table + "[]", jobject);
            ht.Add("total@", "");

            bool hasTableKey = false;
            foreach (var item in jobject)
            {
                if (item.Key.Equals(table, StringComparison.CurrentCultureIgnoreCase))
                {
                    hasTableKey = true;
                    break;
                }
            }
            if (!hasTableKey)
            {
                jobject.Add(table, new JObject());
            }

            JObject resultJobj = new SelectTable(_identitySvc, _tableMapper, _db.Db).Query(ht);
            return Ok(resultJobj);
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
            JObject resultJobj = new SelectTable(_identitySvc, _tableMapper, _db.Db).Query(json);
            return Ok(resultJobj);
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
                        if (f.Key.ToLower() != "id" && _selectTable.IsCol(key, f.Key) && (role.Insert.Column.Contains("*") || role.Insert.Column.Contains(f.Key, StringComparer.CurrentCultureIgnoreCase)))
                            dt.Add(f.Key, f.Value);
                    }
                    int id = _db.Db.Insertable(dt).AS(key).ExecuteReturnIdentity();
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
                    dt.Add("id", value["id"].ToString());
                    foreach (var f in value)
                    {
                        if (f.Key.ToLower() != "id" && _selectTable.IsCol(key, f.Key) && (role.Update.Column.Contains("*") || role.Update.Column.Contains(f.Key, StringComparer.CurrentCultureIgnoreCase)))
                        {
                            dt.Add(f.Key, f.Value);
                        }
                    }
                    _db.Db.Updateable(dt).AS(key).ExecuteCommand();
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
                    sb.Append($"delete FROM {key} where ");
                    if (role.Delete == null || role.Delete.Table == null)
                    {
                        ht["code"] = "500";
                        ht["msg"] = "delete权限未配置";
                        break;
                    }
                    if (!role.Delete.Table.Contains(key, StringComparer.CurrentCultureIgnoreCase))
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
                    _db.Db.Ado.ExecuteCommand(sql, p);
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