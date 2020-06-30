namespace APIJSON.NET
{
    using APIJSON.NET.Services;
    using AspectCore.Extensions.Reflection;
    using Newtonsoft.Json.Linq;
    using SqlSugar;
    using System;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.Linq;
    using System.Text.RegularExpressions;

    /// <summary>
    /// 
    /// </summary>
    public class SelectTable
    {
        private readonly IIdentityService _identitySvc;
        private readonly ITableMapper _tableMapper;
        private readonly SqlSugarClient db;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="identityService"></param>
        /// <param name="tableMapper"></param>
        /// <param name="dbClient"></param>
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
        public object ExecFunc(string funcname, object[] param, Type[] types)
        {
            var method = typeof(FuncList).GetMethod(funcname);

            var reflector = method.GetReflector();
            var result = reflector.Invoke(new FuncList(), param);
            return result;
        }

        private string ToSql(string subtable, int page, int count, int query, string json)
        {
            JObject values = JObject.Parse(json);
            page = values["page"] == null ? page : int.Parse(values["page"].ToString());
            count = values["count"] == null ? count : int.Parse(values["count"].ToString());
            query = values["query"] == null ? query : int.Parse(values["query"].ToString());
            values.Remove("page");
            values.Remove("count");
            subtable = _tableMapper.GetTableName(subtable);
            var tb = sugarQueryable(subtable, "*", values,null);
            var xx= tb.Skip((page - 1) * count).Take(10).ToSql();
            return xx.Key;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="subtable"></param>
        /// <param name="page"></param>
        /// <param name="count"></param>
        /// <param name="json"></param>
        /// <param name="dd"></param>
        /// <returns></returns>
        public Tuple<dynamic, int> GetTableData(string subtable, int page, int count, int query, string json, JObject dd)
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
            query = values["query"] == null ? query : int.Parse(values["query"].ToString());
            values.Remove("page");
            values.Remove("count");
            var tb = sugarQueryable(subtable, selectrole, values, dd);
            if (query == 1)//1-总数
                return new Tuple<dynamic, int>(new List<object>(), tb.Count());
            else
            {
                if (count > 0)
                {
                    int total = 0;
                    if (query == 0)//0-对象
                        return new Tuple<dynamic, int>(tb.ToPageList(page, count), total);
                    else
                        //2-以上全部
                        return new Tuple<dynamic, int>(tb.ToPageList(page, count, ref total), total);

                }
                else
                {
                    if (query == 0)
                        return new Tuple<dynamic, int>(tb.ToList(), 0);
                    else
                        return new Tuple<dynamic, int>(tb.ToList(), tb.Count());
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="subtable"></param>
        /// <param name="json"></param>
        /// <param name="dd"></param>
        /// <returns></returns>
        public dynamic GetFirstData(string subtable, string json, JObject dd)
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
                    string param = item.Value.ToString().Substring(item.Value.ToString().IndexOf("(") + 1).TrimEnd(')');
                    var types = new List<Type>();
                    var paramss = new List<object>();
                    foreach (var va in param.Split(','))
                    {
                        types.Add(typeof(object));
                        paramss.Add(tb.Where(it => it.Key.Equals(va)).Select(i => i.Value));
                    }
                    dic[item.Name] = ExecFunc(func, paramss.ToArray(), types.ToArray());
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
        /// 单表查询
        /// </summary>
        /// <param name="queryObj"></param>
        /// <param name="nodeName">返回数据的节点名称  默认为 infos</param>
        /// <returns></returns>
        public JObject QuerySingle(JObject queryObj, string nodeName = "infos")
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

                    if (key.EndsWith("[]"))
                    {
                        total = QuerySingleList(resultObj, item, nodeName);
                    }
                    else if (key.Equals("func"))
                    {
                        ExecFunc(resultObj, item);
                    }
                    else if (key.Equals("total@"))
                    {
                        resultObj.Add("total", total);
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

        /// <summary>
        /// 获取查询语句
        /// </summary>
        /// <param name="queryObj"></param>
        /// <returns></returns>
        public string ToSql(JObject queryObj)
        {
            foreach (var item in queryObj)
            {
                string key = item.Key.Trim();

                if (key.EndsWith("[]"))
                {
                    return  ToSql(item);
                }
            }
            return string.Empty;
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

        //单表查询,返回的数据在指定的NodeName节点
        private int QuerySingleList(JObject resultObj, KeyValuePair<string, JToken> item, string nodeName)
        {
            string key = item.Key.Trim();
            var jb = JObject.Parse(item.Value.ToString());
            int page = jb["page"] == null ? 0 : int.Parse(jb["page"].ToString());
            int count = jb["count"] == null ? 10 : int.Parse(jb["count"].ToString());
            int query = jb["query"] == null ? 0 : int.Parse(jb["query"].ToString());
            int total = 0;

            jb.Remove("page"); jb.Remove("count"); jb.Remove("query");

            var htt = new JArray();
            foreach (var t in jb)
            {
                var datas = GetTableData(t.Key, page, count, query, t.Value.ToString(), null);
                if (query > 0)
                {
                    total = datas.Item2;
                }
                foreach (var data in datas.Item1)
                {
                    htt.Add(JToken.FromObject(data));
                }
            }

            if (!string.IsNullOrEmpty(nodeName))
            {
                resultObj.Add(nodeName, htt);
            }
            else
                resultObj.Add(key, htt);
            return total;
        }

        private string ToSql(KeyValuePair<string, JToken> item)
        {
            string key = item.Key.Trim();
            var jb = JObject.Parse(item.Value.ToString());
            int page = jb["page"] == null ? 0 : int.Parse(jb["page"].ToString());
            int count = jb["count"] == null ? 10 : int.Parse(jb["count"].ToString());
            int query = jb["query"] == null ? 0 : int.Parse(jb["query"].ToString());

            jb.Remove("page"); jb.Remove("count"); jb.Remove("query");
            var htt = new JArray();
            foreach (var t in jb)
            {
                return ToSql(t.Key, page, count, query, t.Value.ToString());
            }

            return string.Empty;
        }
        //单表查询
        private int QuerySingleList(JObject resultObj, KeyValuePair<string, JToken> item)
        {
            string key = item.Key.Trim();
            return QuerySingleList(resultObj, item, key);
        }

        //多列表查询
        private int QueryMoreList(JObject resultObj, KeyValuePair<string, JToken> item)
        {
            int total = 0;

            var jb = JObject.Parse(item.Value.ToString());
            var page = jb["page"] == null ? 0 : int.Parse(jb["page"].ToString());
            var count = jb["count"] == null ? 10 : int.Parse(jb["count"].ToString());
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
                var temp = GetTableData(table, page, count, query, where[0], null);
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
                            foreach (var d in GetTableData(subtable, page, count, query, jbb[subtable].ToString(), zht).Item1)
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
                ProcessColumn(subtable, selectrole, values, tb);
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
                string fieldValue = va.Value.ToString();

                if (vakey.EndsWith("$"))//模糊查询
                {
                    FuzzyQuery(subtable, conModels, va);
                }
                else if (vakey.EndsWith("{}"))//逻辑运算
                {
                    ConditionQuery(subtable, conModels, va);
                }
                else if (vakey.EndsWith("%"))//bwtween查询
                {
                    ConditionBetween(subtable, conModels, va);
                }
                else if (vakey.EndsWith("@") && dd != null) // 关联上一个table
                {
                    string[] str = fieldValue.Split('/');
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
                else if (IsCol(subtable, vakey)) //其他where条件
                {
                    conModels.Add(new ConditionalModel() { FieldName = vakey, ConditionalType = ConditionalType.Equal, FieldValue = fieldValue });
                }
            }
            tb.Where(conModels);

            //排序
            ProcessOrder(subtable, values, tb);

            //分组
            PrccessGroup(subtable, values, tb);

            //Having
            ProcessHaving(values, tb);
            return tb;
        }

        //处理字段重命名 "@column":"toId:parentId"，对应SQL是toId AS parentId，将查询的字段toId变为parentId返回
        private void ProcessColumn(string subtable, string selectrole, JObject values, ISugarQueryable<ExpandoObject> tb)
        {
            var str = new System.Text.StringBuilder(100);
            foreach (var item in values["@column"].ToString().Split(','))
            {
                string[] ziduan = item.Split(':');
                string colName = ziduan[0];
                var ma = new Regex(@"\((\w+)\)").Match(colName);
                //处理max，min这样的函数
                if (ma.Success && ma.Groups.Count > 1)
                {
                    colName = ma.Groups[1].Value;
                }

                //判断列表是否有权限  sum(1)，sum(*),Count(1)这样的值直接有效
                if (colName == "*" || int.TryParse(colName, out int colNumber) || (IsCol(subtable, colName) && _identitySvc.ColIsRole(colName, selectrole.Split(','))))
                {
                    if (ziduan.Length > 1)
                    {
                        if (ziduan[1].Length > 20)
                        {
                            throw new Exception("别名不能超过20个字符");
                        }
                        str.Append(ziduan[0] + " as " + ReplaceSQLChar(ziduan[1]) + ",");
                    }
                    else
                        str.Append(ziduan[0] + ",");

                }
            }
            if (string.IsNullOrEmpty(str.ToString()))
            {
                throw new Exception($"表名{subtable}没有可查询的字段！");
            }
            tb.Select(str.ToString().TrimEnd(','));
        }

        // "@having":"function0(...)?value0;function1(...)?value1;function2(...)?value2..."，
        // SQL函数条件，一般和 @group一起用，函数一般在 @column里声明
        private void ProcessHaving(JObject values, ISugarQueryable<ExpandoObject> tb)
        {
            if (values["@having"].IsValue())
            {
                List<IConditionalModel> hw = new List<IConditionalModel>();
                List<string> havingItems = new List<string>();
                if (values["@having"].HasValues)
                {
                    havingItems = values["@having"].Select(p => p.ToString()).ToList();
                }
                else
                {
                    havingItems.Add(values["@having"].ToString());
                }
                foreach (var item in havingItems)
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
               
              
                tb.Having(string.Join(",", havingItems));
            }
        }

        //"@group":"column0,column1..."，分组方式。如果 @column里声明了Table的id，则id也必须在 @group中声明；其它情况下必须满足至少一个条件:
        //1.分组的key在 @column里声明
        //2.Table主键在 @group中声明
        private void PrccessGroup(string subtable, JObject values, ISugarQueryable<ExpandoObject> tb)
        {
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
        }

        //处理排序 "@order":"name-,id"查询按 name降序、id默认顺序 排序的User数组
        private void ProcessOrder(string subtable, JObject values, ISugarQueryable<ExpandoObject> tb)
        {
            if (values["@order"].IsValue())
            {
                foreach (var item in values["@order"].ToString().Split(','))
                {
                    string col = item.Replace("-", "").Replace("+", "");
                    if (IsCol(subtable, col))
                    {
                        if (item.EndsWith("-"))
                        {
                            tb.OrderBy($"{col} desc");
                        }
                        else if (item.EndsWith("+"))
                        {
                            tb.OrderBy($"{col} asc");
                        }
                        else
                        {
                            tb.OrderBy($"{col}");
                        }
                    }
                }
            }
        }

        //条件查询 "key{}":"条件0,条件1..."，条件为任意SQL比较表达式字符串，非Number类型必须用''包含条件的值，如'a'
        //&, |, ! 逻辑运算符，对应数据库 SQL 中的 AND, OR, NOT。 
        //   横或纵与：同一字段的值内条件默认 | 或连接，不同字段的条件默认 & 与连接。 
        //   ① & 可用于"key&{}":"条件"等 
        //   ② | 可用于"key|{}":"条件", "key|{}":[] 等，一般可省略 
        //   ③ ! 可单独使用，如"key!":Object，也可像&,|一样配合其他功能符使用
        private void ConditionQuery(string subtable, List<IConditionalModel> conModels, KeyValuePair<string, JToken> va)
        {
            string vakey = va.Key.Trim();
            string field = vakey.TrimEnd("{}".ToCharArray());
            if (va.Value.HasValues)
            {
                List<string> inValues = new List<string>();
                foreach (var cm in va.Value)
                {
                    inValues.Add(cm.ToString());
                }

                conModels.Add(new ConditionalModel() { FieldName = field, ConditionalType = field.EndsWith("!") ? ConditionalType.NotIn : ConditionalType.In, FieldValue = string.Join(",", inValues) });

            }
            else
            {
                var ddt = new List<KeyValuePair<WhereType, ConditionalModel>>();
                foreach (var and in va.Value.ToString().Split(','))
                {
                    var model = new ConditionalModel();
                    model.FieldName = field.TrimEnd("&".ToCharArray());//处理&()的查询方式
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

        //"key%":"start,end" => "key%":["start,end"]，其中 start 和 end 都只能为 Boolean, Number, String 中的一种，如 "2017-01-01,2019-01-01" ，["1,90000", "82001,100000"] ，可用于连续范围内的筛选
        private void ConditionBetween(string subtable, List<IConditionalModel> conModels, KeyValuePair<string, JToken> va)
        {
            string vakey = va.Key.Trim();
            string field = vakey.TrimEnd("%".ToCharArray());
            List<string> inValues = new List<string>();

            if (va.Value.HasValues)
            {
                foreach (var cm in va.Value)
                {
                    inValues.Add(cm.ToString());
                }
            }
            else
            {
                inValues.Add(va.Value.ToString());
            }
            for (var i = 0; i < inValues.Count; i++)
            {
                var fileds = inValues[i].Split(',');
                if (fileds.Length == 2)
                {
                    var ddt = new List<KeyValuePair<WhereType, ConditionalModel>>();

                    var leftCondition = new ConditionalModel()
                    {
                        FieldName = field,
                        ConditionalType = ConditionalType.GreaterThanOrEqual,
                        FieldValue = fileds[0]
                    };
                    ddt.Add(new KeyValuePair<WhereType, ConditionalModel>(i == 0 ? WhereType.And : WhereType.Or, leftCondition));
                    var rightCondition = new ConditionalModel()
                    {
                        FieldName = field,
                        ConditionalType = ConditionalType.LessThanOrEqual,
                        FieldValue = fileds[1]
                    };
                    ddt.Add(new KeyValuePair<WhereType, ConditionalModel>(WhereType.And, rightCondition));

                    conModels.Add(new ConditionalCollections() { ConditionalList = ddt });
                }
            }
        }

        //模糊搜索	"key$":"SQL搜索表达式" => "key$":["SQL搜索表达式"]，任意SQL搜索表达式字符串，如 %key%(包含key), key%(以key开始), %k%e%y%(包含字母k,e,y) 等，%表示任意字符
        private void FuzzyQuery(string subtable, List<IConditionalModel> conModels, KeyValuePair<string, JToken> va)
        {
            string vakey = va.Key.Trim();
            string fieldValue = va.Value.ToString();
            var conditionalType = ConditionalType.Like;
            if (IsCol(subtable, vakey.TrimEnd('$')))
            {
                //支持三种like查询
                if (fieldValue.StartsWith("%") && fieldValue.EndsWith("%"))
                {
                    conditionalType = ConditionalType.Like;
                }
                else if (fieldValue.StartsWith("%"))
                {
                    conditionalType = ConditionalType.LikeRight;
                }
                else if (fieldValue.EndsWith("%"))
                {
                    conditionalType = ConditionalType.LikeLeft;
                }
                conModels.Add(new ConditionalModel() { FieldName = vakey.TrimEnd('$'), ConditionalType = conditionalType, FieldValue = fieldValue.TrimEnd("%".ToArray()).TrimStart("%".ToArray()) });
            }
        }

        public string ReplaceSQLChar(string str)
        {
            if (str == String.Empty)
                return String.Empty;
            str = str.Replace("'", "");
            str = str.Replace(";", "");
            str = str.Replace(",", "");
            str = str.Replace("?", "");
            str = str.Replace("<", "");
            str = str.Replace(">", "");
            str = str.Replace("(", "");
            str = str.Replace(")", "");
            str = str.Replace("@", "");
            str = str.Replace("=", "");
            str = str.Replace("+", "");
            str = str.Replace("*", "");
            str = str.Replace("&", "");
            str = str.Replace("#", "");
            str = str.Replace("%", "");
            str = str.Replace("$", "");
            str = str.Replace("\"", "");

            //删除与数据库相关的词
            str = Regex.Replace(str, "delete from", "", RegexOptions.IgnoreCase);
            str = Regex.Replace(str, "drop table", "", RegexOptions.IgnoreCase);
            str = Regex.Replace(str, "truncate", "", RegexOptions.IgnoreCase);
            str = Regex.Replace(str, "xp_cmdshell", "", RegexOptions.IgnoreCase);
            str = Regex.Replace(str, "exec master", "", RegexOptions.IgnoreCase);
            str = Regex.Replace(str, "net localgroup administrators", "", RegexOptions.IgnoreCase);
            str = Regex.Replace(str, "net user", "", RegexOptions.IgnoreCase);
            str = Regex.Replace(str, "-", "", RegexOptions.IgnoreCase);
            str = Regex.Replace(str, "truncate", "", RegexOptions.IgnoreCase);
            return str;
        }
    }
}
