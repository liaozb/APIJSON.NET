namespace APIJSON.NET
{
    using Microsoft.Extensions.Options;
    using Newtonsoft.Json.Linq;
    using SqlSugar;
    using System;
    using System.Collections.Generic;
    public class JsonToSql: DbContext
    {
        public JsonToSql(IOptions<DbOptions> options) : base(options)
        {

        }
        /// <summary>
        /// 对应数据表
        /// </summary>
        static Dictionary<string, string> dict = new Dictionary<string, string>
            {
                {"user", "apijson_user"},
            };
        public dynamic GetTableData(string subtable, int page, int count, string json, JObject dd)
        {
            if (!subtable.IsTable())
            {
                throw new Exception($"表名{subtable}不正确！");
            }
            if (dict.ContainsKey(subtable.ToLower()))
            {
                subtable = dict.GetValueOrDefault(subtable.ToLower());
            }
            var tb = Db.Queryable(subtable, "tb");
            JObject values = JObject.Parse(json);
            page = values["page"] == null ? page : int.Parse(values["page"].ToString());
            count = values["count"] == null ? count : int.Parse(values["count"].ToString());
            values.Remove("page");
            values.Remove("count");
            List<IConditionalModel> conModels = new List<IConditionalModel>();
            foreach (var va in values)
            {
                string vakey = va.Key.Trim();
                if (vakey.StartsWith("@"))
                {

                }
                else if (vakey.EndsWith("$"))//模糊查询
                {
                    if (vakey.TrimEnd('$').IsTable())
                    {
                        conModels.Add(new ConditionalModel() { FieldName = va.Key.TrimEnd('$'), ConditionalType = ConditionalType.Like, FieldValue = va.Value.ToString() });
                    }
                }
                else if (vakey.EndsWith("{}"))//逻辑运算
                {
                    string field = va.Key.TrimEnd("{}".ToCharArray());
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
                    string[] str = va.Value.ToString().Split("/");
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
                else if (vakey.IsTable()) //其他where条件
                {
                    conModels.Add(new ConditionalModel() { FieldName = vakey, ConditionalType =   ConditionalType.Equal, FieldValue = va.Value.ToString() });
                }
            }
            tb.Where(conModels);
            //查询字段
            if (values["@column"].IsValue())
            {
                var str = new System.Text.StringBuilder(100);
                foreach (var item in values["@column"].ToString().Split(","))
                {
                    string[] ziduan = item.Split(":");
                    if (ziduan.Length > 1)
                    {
                        if (ziduan[0].IsField() && ziduan[1].IsTable())
                        {

                            str.Append(ziduan[0] + " as " + ziduan[1] + ",");
                        }
                    }
                    else
                    {
                        if (item.IsField())
                        {
                            str.Append(item + ",");
                        }
                    }
                }
                tb.Select(str.ToString().TrimEnd(','));
            }
            else
            {
                tb.Select("*");
            }
            //排序
            if (values["@order"].IsValue())
            {
                foreach (var item in values["@order"].ToString().Split(","))
                {
                    if (item.Replace("-", "").IsTable())
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
            else
            {
                tb.OrderBy("id");
            }
            if (values["@group"].IsValue())
            {
                var str = new System.Text.StringBuilder(100);
                foreach (var and in values["@group"].ToString().Split(','))
                {
                    if (and.IsField())
                    {
                        str.Append(and + ",");
                    }
                }
                tb.GroupBy(str.ToString().TrimEnd(','));
            }
            if (values["@having"].IsValue())
            {
                tb.Having($"{values["@having"].ToString()}");
            }
            if (count > 0)
            {
                return tb.ToPageList(page, count);
            }
            else
            {
                return tb.ToList();
            }
           
        }
    }
}
