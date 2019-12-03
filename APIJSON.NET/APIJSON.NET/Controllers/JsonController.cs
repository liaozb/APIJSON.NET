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
    using Microsoft.AspNetCore.Cors;
    using System.Threading.Tasks;
    using System.IO;
    using System.Text;
    using System.Net.Http;

    [Route("api/[controller]")]
    [ApiController]
    [EnableCors("localhost")]
    public class JsonController : ControllerBase
    {

        private SelectTable selectTable;
        private DbContext db;
        private readonly IIdentityService _identitySvc;
        private ITableMapper _tableMapper;

        public JsonController(IIdentityService identityService, ITableMapper tableMapper, DbContext _db)
        {
            db = _db;
            _tableMapper = tableMapper;
            _identitySvc = identityService;
            selectTable = new SelectTable(_identitySvc, _tableMapper, _db.Db);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [HttpGet("/test")]
        public ActionResult Test()
        {
            string str = "{\"page\":1,\"count\":3,\"query\":2,\"Org\":{\"@column\":\"Id,Name\"}}";
            var content = new StringContent(str);
            return Ok(content);
        }

        /// <summary>
        /// 查询
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        [HttpPost("/get")]

        public async Task<ActionResult> Query([FromBody] JObject jobject)
        {
            var st = new SelectTable(_identitySvc, _tableMapper, db.Db);
            JObject resultJobj = st.Query(jobject);
            return Ok(resultJobj);
        }

        [HttpPost("/{table}")]
        public async Task<ActionResult> QueryByTable([FromRoute]string table)
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

            if (jobject["query"] != null && jobject["query"].ToString() != "0" && jobject["total@"] == null)
            {
                //自动添加总计数量
                ht.Add("total@", "");
            }

            //每页最大1000条数据
            if (jobject["count"] != null && int.Parse(jobject["count"].ToString()) > 1000)
            {
                throw new Exception("count分页数量最大不能超过1000");
            }

            bool isDebug = (jobject["@debug"] != null && jobject["@debug"].ToString() != "0");
            jobject.Remove("@debug");

            bool hasTableKey = false;
            List<string> ignoreConditions = new List<string> { "page", "count", "query" };
            JObject tableConditions = new JObject();//表的其它查询条件，比如过滤，字段等
            foreach (var item in jobject)
            {
                if (item.Key.Equals(table, StringComparison.CurrentCultureIgnoreCase))
                {
                    hasTableKey = true;
                    break;
                }
                if (!ignoreConditions.Contains(item.Key.ToLower()))
                {
                    tableConditions.Add(item.Key, item.Value);
                }
            }

            foreach (var removeKey in tableConditions)
            {
                jobject.Remove(removeKey.Key);
            }

            if (!hasTableKey)
            {
                jobject.Add(table, tableConditions);
            }

            return await Query(ht);
        }
        /// <summary>
        /// 新增
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        [HttpPost("/add")]
        public ActionResult Add([FromBody]JObject jobject)
        {

            JObject ht = new JObject();
            ht.Add("code", "200");
            ht.Add("msg", "success");
            try
            {



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
                        if (f.Key.ToLower() != "id" && selectTable.IsCol(key, f.Key) && (role.Insert.Column.Contains("*") || role.Insert.Column.Contains(f.Key, StringComparer.CurrentCultureIgnoreCase)))
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
        public ActionResult Edit([FromBody]JObject jobject)
        {
            JObject ht = new JObject();
            ht.Add("code", "200");
            ht.Add("msg", "success");
            try
            {
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
                    foreach (var f in value)
                    {
                        if (f.Key.ToLower() != "id" && selectTable.IsCol(key, f.Key) && (role.Update.Column.Contains("*") || role.Update.Column.Contains(f.Key, StringComparer.CurrentCultureIgnoreCase)))
                        {
                            dt.Add(f.Key, f.Value.ToString());
                        }
                    }
                    db.Db.Updateable(dt).AS(key).Where("id=@id", new { id = value["id"].ToString() }).ExecuteCommand();
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
        public ActionResult Remove([FromBody]JObject jobject)
        {
            JObject ht = new JObject();
            ht.Add("code", "200");
            ht.Add("msg", "success");
            try
            {
                var role = _identitySvc.GetRole();
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