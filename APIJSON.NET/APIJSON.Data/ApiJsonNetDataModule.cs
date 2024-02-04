using System;
using System.Collections.Generic;
using System.Text;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace APIJSON.Data;
[DependsOn(
    typeof(AbpAutofacModule))]
public class ApiJsonNetDataModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {

    }
}
