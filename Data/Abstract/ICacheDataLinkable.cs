using SiteStack.Sitefinity.CacheService.Services.Abstract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SiteStack.Sitefinity.CacheService.Data.Abstract
{
    public interface ICacheDataLinkable<T, U> : ICacheData<U>
    {
        T Next
        {
            get;
            set;
        }
    }
}
