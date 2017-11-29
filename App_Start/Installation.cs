using CMS.Sitefinity.CacheService.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telerik.Sitefinity.Abstractions;

namespace CMS.Sitefinity.CacheService.App_Start
{
    public class Installation
    {
        public static void Application_Start()
        {
            Bootstrapper.Initialized += CacheServiceBootstrapper.Bootstrapper_Initialized;
        }
    }
}
