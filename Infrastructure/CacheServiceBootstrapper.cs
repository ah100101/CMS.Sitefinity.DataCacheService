using SiteStack.Sitefinity.CacheService.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telerik.Sitefinity.Configuration;

namespace SiteStack.Sitefinity.CacheService.Infrastructure
{
    public class CacheServiceBootstrapper
    {
        public static void Bootstrapper_Initialized(object sender, Telerik.Sitefinity.Data.ExecutedEventArgs e)
        {
            Config.RegisterSection<CacheServiceConfiguration>();
        }
    }
}
