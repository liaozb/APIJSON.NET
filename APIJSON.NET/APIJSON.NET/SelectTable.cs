namespace APIJSON.NET
{
    using APIJSON.NET.Services;
    using Microsoft.Extensions.Options;
    using Newtonsoft.Json.Linq;
    using SqlSugar;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    public class SelectTable: DbContext
    {
        private readonly IIdentityService _identitySvc;
        private readonly ITableMapper _tableMapper;
        public SelectTable(IOptions<DbOptions> options, IIdentityService identityService, ITableMapper tableMapper) : base(options)
        {
            _identitySvc = identityService;
            _tableMapper = tableMapper;
        }
        public (dynamic,int) GetTableData(string subtable, int page, int count, string json, JObject dd)
        {   
            if (!subtable.IsTable())
            {
                throw new Exception($"表名{subtable}不正确！");
            }
            var role = _identitySvc.GetSelectRole(subtable);
            if (!role.Item1)//没有权限返回异常
            {
                throw new Exception(role.Item2);
            }
            string selectrole = role.Item2;
            subtable = _tableMapper.GetTableName(subtable);
            JObject values = JObject.Parse(json);
            var tb = Db.Queryable(subtable, "tb");
            if (values["@column"].IsValue())
            {
                var str = new System.Text.StringBuilder(100);
                foreach (var item in values["@column"].ToString().Split(","))
                {
                    string[] ziduan = item.Split(":");
                    if (ziduan.Length > 1)
                    {
                        if (_identitySvc.ColIsRole(ziduan[0], selectrole.Split(",")))
                        {

                            str.Append(ziduan[0] + " as " + ziduan[1] + ",");
                        }
                    }
                    else
                    {
                        if (_identitySvc.ColIsRole(item, selectrole.Split(",")))
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
            page = values["page"] == null ? page : int.Parse(values["page"].ToString());
            count = values["count"] == null ? count : int.Parse(values["count"].ToString());
            values.Remove("page");
            values.Remove("count");
            List<IConditionalModel> conModels = new List<IConditionalModel>();
            foreach (var va in values)
            {
                string vakey = va.Key.Trim();
                if (vakey.EndsWith("$"))//模糊查询
                {
                    if (vakey.TrimEnd('$').IsTable())
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
                if (count>0)
                {
                    tb.OrderBy("id");
                }
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
                int total = 0;
                return (tb.ToPageList(page, count,ref total),total);
            }
            else
            {
                return (tb.ToList(),tb.Count());
            }
           
        }
    }
}
