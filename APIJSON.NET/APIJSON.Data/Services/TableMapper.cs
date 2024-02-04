using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using Volo.Abp.DependencyInjection;

namespace APIJSON.Data;

public class TableMapper : ITableMapper,ITransientDependency
{
    private readonly Dictionary<string, string> _options= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    public TableMapper(IOptions<Dictionary<string, string>> options)
    {
        foreach (var item in options.Value)
        {
            _options.Add(item.Key, item.Value);
        }
    }
    public string GetTableName(string oldname)
    {
        if (_options.ContainsKey(oldname))
        {
            return _options[oldname];
        }
        return oldname;
    }
}
