namespace APIJSON.NET
{
    using APIJSON.NET.Services;
    using Microsoft.Extensions.Options;
    using Newtonsoft.Json.Linq;
    using SqlSugar;
    using System;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.Linq;
    using AspectCore.Extensions.Reflection;
    using System.Text.RegularExpressions;

    public class SelectTable
    {
        private readonly IIdentityService _identitySvc;
        private readonly ITableMapper _tableMapper;
        private readonly SqlSugarClient db;

        public SelectTable(IIdentityService identityService, ITableMapper tableMapper, SqlSugarClient dbClient)
        {
            _identitySvc = identityService;
            _tableMapper = tableMapper;
            db = dbClient;
        }
        /// <summary>
        /// 判断表名是否正确
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        public bool IsTable(string table)
        {
            return db.DbMaintenance.GetTableInfoList().Any(it => it.Name.Equals(table, StringComparison.CurrentCultureIgnoreCase));
        }
        /// <summary>
        /// 判断表的列名是否正确
        /// </summary>
        /// <param name="table"></param>
        /// <param name="col"></param>
        /// <returns></returns>
        public bool IsCol(string table, string col)
        {
            return db.DbMaintenance.GetColumnInfosByTableName(table).Any(it => it.DbColumnName.Equals(col, StringComparison.CurrentCultureIgnoreCase));
        }
        /// <summary>
        /// 动态调用方法
        /// </summary>
        /// <param name="funcname"></param>
        /// <param name="param"></param>
        /// <param name="types"></param>
        /// <returns></returns>
        public object ExecFunc(string funcname,object[] param, Type[] types)
        {
            var method = typeof(FuncList).GetMethod(funcname);
     
            var reflector = method.GetReflector();
            var result = reflector.Invoke(new FuncList(), param);
            return result;
        }

        public (dynamic,int) GetTableData(string subtable, int page, int count, string json, JObject dd)
        {   
           
            var role = _identitySvc.GetSelectRole(subtable);
            if (!role.Item1)//没有权限返回异常
            {
                throw new Exception(role.Item2);
            }
            string selectrole = role.Item2;
            subtable = _tableMapper.GetTableName(subtable);
          
            JObject values = JObject.Parse(json);
            page = values["page"] == null ? page : int.Parse(values["page"].ToString());
            count = values["count"] == null ? count : int.Parse(values["count"].ToString());
            values.Remove("page");
            values.Remove("count");
            var tb = sugarQueryable(subtable, selectrole, values, dd);
            if (count > 0)
            {
                int total = 0;
                return (tb.ToPageList(page, count,ref total),total);
            }
            else
            {
                return (tb.ToList(),tb.Count());
            }
        }
        public dynamic GetFirstData(string subtable,  string json, JObject dd)
        {
           
            var role = _identitySvc.GetSelectRole(subtable);
            if (!role.Item1)//没有权限返回异常
            {
                throw new Exception(role.Item2);
            }
            string selectrole = role.Item2;
            subtable = _tableMapper.GetTableName(subtable);
            JObject values = JObject.Parse(json);
            values.Remove("page");
            values.Remove("count");
            var tb = sugarQueryable(subtable, selectrole, values, dd).First();
            var dic = (IDictionary<string, object>)tb;
            foreach (var item in values.Properties().Where(it => it.Name.EndsWith("()")))
            {
                if (item.Value.IsValue())
                {
                    string func = item.Value.ToString().Substring(0, item.Value.ToString().IndexOf("("));
                    string param = item.Value.ToString().Substring(item.Value.ToString().IndexOf("(") + 1).TrimEnd(')') ;
                    var types = new List<Type>();
                    var paramss = new List<object>();
                    foreach (var va in param.Split(','))
                    {
                        types.Add(typeof(object));
                        paramss.Add(tb.Where(it => it.Key.Equals(va)).Select(i => i.Value));
                    }
                  dic[item.Name] =ExecFunc(func, paramss.ToArray(), types.ToArray());
                }
            }
           
            return tb;
            
        }

        /// <summary>
        /// 解析并查询
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public JObject Query(string queryJson)
        {
            JObject resultObj = new JObject();

            try
            {
                JObject queryJobj = JObject.Parse(queryJson);
                resultObj = Query(queryJobj);
            }
            catch (Exception ex)
            {
                resultObj.Add("code", "500");
                resultObj.Add("msg", ex.Message);
            }

            return resultObj;
        }

        /// <summary>
        /// 解析并查询
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public JObject Query(JObject queryObj)
        {
            JObject resultObj = new JObject();
            resultObj.Add("code", "200");
            resultObj.Add("msg", "success");
            try
            {
                int total = 0;
                foreach (var item in queryObj)
                {
                    string key = item.Key.Trim();

                    if (key.Equals("[]"))
                    {
                        total = QueryMoreList(resultObj, item);
                    }
                    else if (key.EndsWith("[]"))
                    {
                        total = QuerySingleList(resultObj, item);
                    }
                    else if (key.Equals("func"))
                    {
                        ExecFunc(resultObj, item);
                    }
                    else if (key.Equals("total@"))
                    {
                        resultObj.Add("total", total);
                    }
                    else
                    {
                        var template = GetFirstData(key, item.Value.ToString(), resultObj);
                        if (template != null)
                        {
                            resultObj.Add(key, JToken.FromObject(template));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                resultObj["code"] = "500";
                resultObj["msg"] = ex.Message;
            }
            return resultObj;
        }

        //单表查询
        private int QuerySingleList(JObject resultObj, KeyValuePair<string, JToken> item)
        {
            string key = item.Key.Trim();
            var jb = JObject.Parse(item.Value.ToString());
            int page = jb["page"] == null ? 0 : int.Parse(jb["page"].ToString());
            int count = jb["count"] == null ? 1 : int.Parse(jb["count"].ToString());
            int query = jb["query"] == null ? 2 : int.Parse(jb["query"].ToString());
            int total = 0;

            jb.Remove("page"); jb.Remove("count"); jb.Remove("query");
            var htt = new JArray();
            foreach (var t in jb)
            {
                var datas = GetTableData(t.Key, page, count, t.Value.ToString(), null);
                if (query > 0)
                {
                    total = datas.Item2;
                }
                foreach (var data in datas.Item1)
                {
                    htt.Add(JToken.FromObject(data));
                }
            }
            resultObj.Add(key, htt);
            return total;
        }

        //多列表查询
        private int QueryMoreList(JObject resultObj, KeyValuePair<string, JToken> item)
        {
            int total = 0;

            var jb = JObject.Parse(item.Value.ToString());
            var page = jb["page"] == null ? 0 : int.Parse(jb["page"].ToString());
            var count = jb["count"] == null ? 0 : int.Parse(jb["count"].ToString());
            var query = jb["query"] == null ? 0 : int.Parse(jb["query"].ToString());
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
                var temp = GetTableData(table, page, count, where[0], null);
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
                            foreach (var d in GetTableData(subtable, page, count, jbb[subtable].ToString(), zht).Item1)
                            {
                                lt.Add(JToken.FromObject(d));
                            }
                            zht.Add(tables[i], lt);
                        }
                        else
                        {
                            var ddf = GetFirstData(subtable, where[i].ToString(), zht);
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
                resultObj.Add("[]", htt);
            }

            return total;
        }

        private void ExecFunc(JObject resultObj, KeyValuePair<string, JToken> item)
        {
            JObject jb = JObject.Parse(item.Value.ToString());
            Type type = typeof(FuncList);

            var dataJObj = new JObject();
            foreach (var f in jb)
            {
                var types = new List<Type>();
                var param = new List<object>();
                foreach (var va in JArray.Parse(f.Value.ToString()))
                {
                    types.Add(typeof(object));
                    param.Add(va);
                }
                dataJObj.Add(f.Key, JToken.FromObject(ExecFunc(f.Key, param.ToArray(), types.ToArray())));
            }
            resultObj.Add("func", dataJObj);
        }

        private ISugarQueryable<ExpandoObject> sugarQueryable(string subtable, string selectrole, JObject values, JObject dd)
        {
            if (!IsTable(subtable))
            {
                throw new Exception($"表名{subtable}不正确！");
            }
            var tb = db.Queryable(subtable, "tb");


            if (values["@column"].IsValue())
            {
                var str = new System.Text.StringBuilder(100);
                foreach (var item in values["@column"].ToString().Split(','))
                {
                    string[] ziduan = item.Split(':');
                    if (ziduan.Length > 1)
                    {
                        if (IsCol(subtable,ziduan[0]) &&_identitySvc.ColIsRole(ziduan[0], selectrole.Split(',')))
                        {

                            str.Append(ziduan[0] + " as " + ziduan[1] + ",");
                        }
                    }
                    else
                    {
                        if (IsCol(subtable, item) && _identitySvc.ColIsRole(item, selectrole.Split(',')))
                        {
                            str.Append(item + ",");
                        }
                    }
                }
                if (string.IsNullOrEmpty(str.ToString()))
                {
                    throw new Exception($"表名{subtable}没有可查询的字段！");
                }
                tb.Select(str.ToString().TrimEnd(','));
            }
            else
            {
                tb.Select(selectrole);
            }
         
            List<IConditionalModel> conModels = new List<IConditionalModel>();
            if (values["identity"].IsValue())
            {
                conModels.Add(new ConditionalModel() { FieldName = values["identity"].ToString(), ConditionalType = ConditionalType.Equal, FieldValue = _identitySvc.GetUserIdentity() });
            }
            foreach (var va in values)
            {
                string vakey = va.Key.Trim();
                if (vakey.EndsWith("$"))//模糊查询
                {
                    if (IsCol(subtable,vakey.TrimEnd('$')))
                    {
                        conModels.Add(new ConditionalModel() { FieldName = vakey.TrimEnd('$'), ConditionalType = ConditionalType.Like, FieldValue = va.Value.ToString() });
                    }
                }
                else if (vakey.EndsWith("{}"))//逻辑运算
                {
                    string field = vakey.TrimEnd("{}".ToCharArray());
                    if (va.Value.HasValues)
                    {
                        conModels.Add(new ConditionalModel() { FieldName = field, ConditionalType = field.EndsWith("!") ? ConditionalType.NotIn : ConditionalType.In, FieldValue = va.Value.ToString() });
                    }
                    else
                    {
                        var ddt = new List<KeyValuePair<WhereType, ConditionalModel>>();
                        foreach (var and in va.Value.ToString().Split(','))
                        {
                            var model = new ConditionalModel();
                            model.FieldName = field;
                            if (and.StartsWith(">="))
                            {
                                model.ConditionalType = ConditionalType.GreaterThanOrEqual;
                                model.FieldValue = and.TrimStart(">=".ToCharArray());
                            }
                            else if (and.StartsWith("<="))
                            {

                                model.ConditionalType = ConditionalType.LessThanOrEqual;
                                model.FieldValue = and.TrimStart("<=".ToCharArray());
                            }
                            else if (and.StartsWith(">"))
                            {
                                model.ConditionalType = ConditionalType.GreaterThan;
                                model.FieldValue = and.TrimStart('>');
                            }
                            else if (and.StartsWith("<"))
                            {
                                model.ConditionalType = ConditionalType.LessThan;
                                model.FieldValue = and.TrimStart('<');
                            }
                            ddt.Add(new KeyValuePair<WhereType, ConditionalModel>((field.EndsWith("&") ? WhereType.And : WhereType.Or), model));
                        }
                        conModels.Add(new ConditionalCollections() { ConditionalList = ddt });
                    }
                }
                else if (vakey.EndsWith("@") && dd != null) // 关联上一个table
                {
                    string[] str = va.Value.ToString().Split('/');
                    string value = string.Empty;
                    if (str.Length == 3)
                    {
                        value = dd[str[1]][str[2]].ToString();
                    }
                    else if (str.Length == 2)
                    {
                        value = dd[str[0]][str[1]].ToString();
                    }

                    conModels.Add(new ConditionalModel() { FieldName = vakey.TrimEnd('@'), ConditionalType = ConditionalType.Equal, FieldValue = value });

                }
                else if (IsCol(subtable,vakey)) //其他where条件
                {
                    conModels.Add(new ConditionalModel() { FieldName = vakey, ConditionalType = ConditionalType.Equal, FieldValue = va.Value.ToString() });
                }
            }
            tb.Where(conModels);

            //排序
            if (values["@order"].IsValue())
            {
                foreach (var item in values["@order"].ToString().Split(','))
                {
                    if (IsCol(subtable,item.Replace("-", "")))
                    {
                        if (item.EndsWith("-"))
                        {
                            tb.OrderBy($"{item.Replace("-", " desc")}");
                        }
                        else
                        {
                            tb.OrderBy($"{item.ToString()}");
                        }
                    }
                }
            }

            if (values["@group"].IsValue())
            {
                var str = new System.Text.StringBuilder(100);
                foreach (var and in values["@group"].ToString().Split(','))
                {
                    if (IsCol(subtable, and))
                    {
                        str.Append(and + ",");
                    }
                }
                tb.GroupBy(str.ToString().TrimEnd(','));
            }
            if (values["@having"].IsValue())
            {
                List<IConditionalModel> hw = new List<IConditionalModel>();
                JArray jArray = JArray.Parse(values["@having"].ToString());
                foreach (var item in jArray)
                {
                    string and = item.ToString();
                    var model = new ConditionalModel();
                    if (and.Contains(">="))
                    {
                        model.FieldName = and.Split(new string[] { ">=" }, StringSplitOptions.RemoveEmptyEntries)[0];
                        model.ConditionalType = ConditionalType.GreaterThanOrEqual;
                        model.FieldValue = and.Split(new string[] { ">=" }, StringSplitOptions.RemoveEmptyEntries)[1];
                    }
                    else if (and.Contains("<="))
                    {
                        
                        model.FieldName = and.Split(new string[] { "<=" }, StringSplitOptions.RemoveEmptyEntries)[0];
                        model.ConditionalType = ConditionalType.LessThanOrEqual;
                        model.FieldValue = and.Split(new string[] { "<=" }, StringSplitOptions.RemoveEmptyEntries)[1];
                    }
                    else if (and.Contains(">"))
                    {
                        model.FieldName = and.Split(new string[] { ">" }, StringSplitOptions.RemoveEmptyEntries)[0];
                        model.ConditionalType = ConditionalType.GreaterThan;
                        model.FieldValue = and.Split(new string[] { ">" }, StringSplitOptions.RemoveEmptyEntries)[1];
                    }
                    else if (and.Contains("<"))
                    {
                        model.FieldName = and.Split(new string[] { "<" }, StringSplitOptions.RemoveEmptyEntries)[0];
                        model.ConditionalType = ConditionalType.LessThan;
                        model.FieldValue = and.Split(new string[] { "<" }, StringSplitOptions.RemoveEmptyEntries)[1];
                    }
                    else if (and.Contains("!="))
                    {
                        model.FieldName = and.Split(new string[] { "!=" }, StringSplitOptions.RemoveEmptyEntries)[0];
                        model.ConditionalType = ConditionalType.NoEqual;
                        model.FieldValue = and.Split(new string[] { "!=" }, StringSplitOptions.RemoveEmptyEntries)[1];
                    }
                    else if (and.Contains("="))
                    {
                        model.FieldName = and.Split(new string[] { "=" }, StringSplitOptions.RemoveEmptyEntries)[0];
                        model.ConditionalType = ConditionalType.Equal;
                        model.FieldValue = and.Split(new string[] { "=" }, StringSplitOptions.RemoveEmptyEntries)[1];
                    }
                    hw.Add(model);
                }

                var d = db.Context.Utilities.ConditionalModelToSql(hw);
                tb.Having(d.Key, d.Value);
            }
            return tb;
        }
    }
}
