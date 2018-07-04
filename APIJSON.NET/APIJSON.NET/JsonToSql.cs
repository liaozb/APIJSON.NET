namespace APIJSON.NET
{
    using Dapper;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Linq;
    public static class JsonToSql
    {
        public static SqlBuilder.Template GetSqlBuilder(string subtable, int page, int count, string json, JObject dd)
        {
            if (!subtable.IsTable())
            {
                throw new Exception($"表名{subtable}不正确！");
            }
            JObject values = JObject.Parse(json);
            page = values["page"] == null ? page : int.Parse(values["page"].ToString());
            count = values["count"] == null ? count : int.Parse(values["count"].ToString());
            values.Remove("page");
            values.Remove("count");
            var builder = new SqlBuilder();
            string pagesql = $"select /**select**/ from [{subtable}] /**where**/ /**groupby**/ /**having**/";
            if (count > 0)
            {
                pagesql = $@"select * from (select row_number()over(order by id)rownumber,/**select**/  from [{subtable}] /**where**/ /**groupby**/ /**having**/) a 
                           where rownumber between {(page * count) + 1} and {(page * count) + count}";
            }
            var template = builder.AddTemplate(pagesql);
            //查询字段
            if (values["@column"].IsValue())
            {
                foreach (var item in values["@column"].ToString().Split(","))
                {
                    string[] ziduan = item.Split(":");
                    if (ziduan.Length > 1)
                    {
                        if (ziduan[0].IsField() && ziduan[1].IsTable())
                        {
                            builder.Select(ziduan[0] + " as " + ziduan[1]);
                        }
                    }
                    else
                    {
                        if (item.IsField())
                        {
                            builder.Select(item);
                        }
                    }
                }
            }
            else
            {
                builder.Select("*");
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
                            builder.OrderBy($"{item.Replace("-", " desc")}");
                        }
                        else
                        {
                            builder.OrderBy($"{item.ToString()}");
                        }
                    }
                }
            }
            else
            {

            }
            if (values["@group"].IsValue())
            {
                foreach (var and in values["@group"].ToString().Split(','))
                {
                    if (and.IsField())
                    {
                        builder.GroupBy($"{and}");
                    }
                }
            }
            if (values["@having"].IsValue())
            {
                builder.Having($"{values["@having"].ToString()}");
            }
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
                        vakey = vakey.TrimEnd('$').GetParamName();
                        var p = new DynamicParameters();
                        p.Add($"@{vakey}", va.Value.ToString());
                        builder.Where($"{va.Key.TrimEnd('$')} like @{vakey}", p);
                    }
                }
                else if (vakey.EndsWith("{}"))//逻辑运算
                {
                    string field = va.Key.TrimEnd("{}".ToCharArray());
                    if (va.Value.HasValues)
                    {
                        JArray jArray = JArray.Parse(va.Value.ToString());
                        var p = new DynamicParameters();
                        p.Add($"@{field}", jArray.Select(jv => (string)jv).ToArray());
                        builder.Where($"{field} {(field.EndsWith("!") ? "not" : "")} in @{field}", p);
                    }
                    else
                    {
                        if (field.EndsWith("&"))
                        {
                            field = field.TrimEnd("&".ToCharArray());
                            if (field.IsTable())
                            {
                                foreach (var and in va.Value.ToString().Split(','))
                                {

                                    if (and.StartsWith(">="))
                                    {
                                        if (int.TryParse(and.TrimStart(">=".ToCharArray()), out int opt))
                                        {
                                            builder.Where($"{field}>={opt}");
                                        }
                                    }
                                    else if (and.StartsWith("<="))
                                    {
                                        if (int.TryParse(and.TrimStart("<=".ToCharArray()), out int opt))
                                        {
                                            builder.Where($"{field}<={opt}");
                                        }
                                    }
                                    else if (and.StartsWith(">"))
                                    {
                                        if (int.TryParse(and.TrimStart('>'), out int opt))
                                        {
                                            builder.Where($"{field}>{opt}");
                                        }
                                    }
                                    else if (and.StartsWith("<"))
                                    {
                                        if (int.TryParse(and.TrimStart('<'), out int opt))
                                        {
                                            builder.Where($"{field}<{opt}");
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            field = field.TrimEnd("|".ToCharArray());
                            if (field.IsTable())
                            {
                                foreach (var and in va.Value.ToString().Split(','))
                                {
                                    if (and.StartsWith(">="))
                                    {
                                        if (int.TryParse(and.TrimStart(">=".ToCharArray()), out int opt))
                                        {
                                            builder.OrWhere($"{field}>={opt}");
                                        }
                                    }
                                    else if (and.StartsWith("<="))
                                    {
                                        if (int.TryParse(and.TrimStart("<=".ToCharArray()), out int opt))
                                        {
                                            builder.OrWhere($"{field}<={opt}");
                                        }
                                    }
                                    else if (and.StartsWith(">"))
                                    {
                                        if (int.TryParse(and.TrimStart('>'), out int opt))
                                        {
                                            builder.OrWhere($"{field}>{opt}");
                                        }
                                    }
                                    else if (and.StartsWith("<"))
                                    {
                                        if (int.TryParse(and.TrimStart('<'), out int opt))
                                        {
                                            builder.OrWhere($"{field}<{opt}");
                                        }
                                    }
                                }
                            }
                        }
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
                    var p = new DynamicParameters();
                    p.Add($"@{vakey.TrimEnd('@')}", value);
                    builder.Where($"{vakey.TrimEnd('@')} = @{vakey.TrimEnd('@')}", p);
                }
                else if (vakey.IsTable()) //其他where条件
                {
                    var p = new DynamicParameters();
                    p.Add($"@{vakey}", va.Value.ToString());
                    builder.Where($"{vakey} = @{vakey}", p);
                }
            }
            return template;
        }
    }
}
