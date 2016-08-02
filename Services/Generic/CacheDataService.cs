using System;
using System.Collections.Generic;
using System.Linq;
using SiteStack.Sitefinity.CacheService.Services.Abstract;
using Telerik.Microsoft.Practices.EnterpriseLibrary.Caching;
using Telerik.Sitefinity.Data;
using Telerik.Microsoft.Practices.EnterpriseLibrary.Caching.Expirations;
using Telerik.Sitefinity.Services;
using Telerik.Microsoft.Practices.EnterpriseLibrary.Logging;
using SiteStack.Sitefinity.CacheService.Data.Abstract;

namespace SiteStack.Sitefinity.CacheService.Services.Generic
{
    /// <summary>
    /// Service that stores and retrieves Sitefinity data from the global site cache
    /// </summary>
    /// <typeparam name="T">Cacheable type</typeparam>
    /// <typeparam name="U">Sitefinity type</typeparam>
    public class CacheDataService : IDataService
    {
        #region Members Variables

        private int _expirationMinutes = 60;
        private object itemLock = new object();

        #endregion

        #region Private Properties

        /// <summary>
        /// Manager responsible for adding and retrieving items from the cache
        /// </summary>
        private ICacheManager CacheManager
        {
            get
            {
                return SystemManager.GetCacheManager(CacheManagerInstance.Global);
            }
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Global value for how long items should remain in the cache
        /// </summary>
        public int DefaultExpirationMinutes
        {
            get
            {
                return _expirationMinutes;
            }

            set
            {
                _expirationMinutes = value;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Retrieves base content items from cache that have been stored as a list
        /// </summary>
        /// <typeparam name="T">Cacheable type</typeparam>
        /// <typeparam name="U">Sitefinity type</typeparam>
        /// <param name="cacheOnFail">Cache from database if cache returns null</param>
        /// <returns>list of cached items</returns>
        public List<T> GetBaseListItems<T, U>(bool cacheOnFail = true, int expirationMinutes = 0) where T : ICacheData<U>
        {
            //create an instance of the cacheable item
            T cacheableDataItem = (T)Activator.CreateInstance(typeof(T));
            
            //pull from the list cache, with content type as the key
            List<T> cachedDataItems = CacheManager[cacheableDataItem.GetContentType<U>().ToString()] as List<T>;

            //if null was returned (nothing was in the cache for this key)
            if (cacheOnFail && cachedDataItems == null)
            {
                //since nothing was pulled from the cache, cache the base set as a list
                if (cacheOnFail && expirationMinutes > 0)
                    CacheBaseDataSetAsList<T, U>(expirationMinutes);
                else if (cacheOnFail)
                    CacheBaseDataSetAsList<T, U>(DefaultExpirationMinutes);
                else
                    CacheBaseDataSetAsList<T, U>();
                
                //return the cached version (even if it's null, gotta stop some time...)
                return CacheManager[cacheableDataItem.GetContentType<U>().ToString()] as List<T>;
            }

            //found the cached items right away, return them
            return cachedDataItems;
        }

        /// <summary>
        /// Retrieves base content items from cache that have been stored as a dictionary
        /// </summary>
        /// <typeparam name="T">Cacheable type</typeparam>
        /// <typeparam name="U">Sitefinity type</typeparam>
        /// <param name="cacheOnFail">Cache from database if cache returns null</param>
        /// <returns>dictionary of cached items</returns>
        public Dictionary<string, T> GetBaseDictionaryItems<T, U>(bool cacheOnFail = true, int expirationMinutes = 0) where T : ICacheData<U>
        {
            //create an instance of the cacheable item
            T cacheableDataItem = (T)Activator.CreateInstance(typeof(T));

            //pull from the dictionary cache, with content type + dict as the key
            Dictionary<string, T> cachedDataItems
                = CacheManager[cacheableDataItem.GetContentType<U>().ToString() + "-dict"] as Dictionary<string, T>;

            //if null was returned (nothing was in the cache for this key)
            if (cacheOnFail && cachedDataItems == null)
            {
                //nothing was in cache, pull from db and try caching
                if (cacheOnFail && expirationMinutes > 0)
                    CacheBaseDataSetAsDictionary<T, U>(expirationMinutes);
                else if (cacheOnFail)
                    CacheBaseDataSetAsDictionary<T, U>(DefaultExpirationMinutes);
                else
                    CacheBaseDataSetAsDictionary<T, U>();
                 
                //return the cached version (even if null)
                return CacheManager[cacheableDataItem.GetContentType<U>().ToString() + "-dict"] as Dictionary<string, T>; 
            }

            //found the cached items easily, return them
            return cachedDataItems;
        }

        /// <summary>
        /// Retrieves content items from cache of a specified key that have been stored as a dictionary of linked lists
        /// </summary>
        /// <typeparam name="T">Cacheable type</typeparam>
        /// <typeparam name="U">Sitefinity type</typeparam>
        /// <param name="key">Cache Key</param>
        /// <param name="failQuery">Query that runs if returned cached items is null</param>
        /// <param name="expirationMinutes"></param>
        /// <returns></returns>
        public Dictionary<string, T> GetLinkedDictionaryItems<T, U>(string key, IQueryable<U> failQuery = null, int expirationMinutes = 0) where T : ICacheDataLinkable<T, U>
        {
            //fetch cached list items
            Dictionary<string, T> cachedItems = (Dictionary<string, T>)CacheManager[key];

            //check if we are supposed to fall back and if the cacheditems is null
            if (failQuery != null && cachedItems == null)
            {
                //cache from the failquery
                if (expirationMinutes > 0)
                    CacheDataSetAsLinkedDictionary<T, U>(failQuery, key, expirationMinutes);
                else
                    CacheDataSetAsLinkedDictionary<T, U>(failQuery, key);

                //return from the cache, even if it's null
                return CacheManager[key] as Dictionary<string, T>;
            }

            //return the list from the cache
            return cachedItems;
        }

        /// <summary>
        /// Cache data set as a dictionary of linked lists
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="U"></typeparam>
        /// <param name="sfContentQuery"></param>
        /// <param name="key"></param>
        /// <param name="expirationMinutes"></param>
        public void CacheDataSetAsLinkedDictionary<T, U>(IQueryable<U> sfContentQuery, string key, int expirationMinutes = 0) where T : ICacheDataLinkable<T, U>
        {
            //create instance of a cache data type
            T cacheDataItem = (T)Activator.CreateInstance(typeof(T));

            //get the content type that we are working with
            Type cacheType = cacheDataItem.GetContentType<U>();

            //get a string of that content type, to be used in the key
            string cacheTypeName = cacheDataItem.GetContentType<U>().ToString();

            //try to fetch these cached items from the cache
            Dictionary<string, T> cachedDictionaryItems = (Dictionary<string, T>)CacheManager[key];

            //as long as nothing is returned from the cache, proceed with caching items
            if (cachedDictionaryItems == null)
            {
                lock (itemLock)
                {
                    cachedDictionaryItems = (Dictionary<string, T>)CacheManager[key];

                    if (cachedDictionaryItems != null)
                        return;

                    //initialize cachedItems to new dictionary
                    cachedDictionaryItems = new Dictionary<string, T>();

                    //if the content type is not null, proceed with pulling in data from database
                    if (cacheType != null)
                    {
                        //initialize a list of cache content items
                        List<U> items = new List<U>();

                        items = sfContentQuery.ToList<U>();

                        //if the list of items is not null and there is 1 or more, begin process for adding them to cache
                        if (items != null && items.Count > 0)
                        {
                            //create list of dependencies
                            List<DataItemCacheDependency> dependencies = new List<DataItemCacheDependency>();

                            //create list of parameters
                            List<ICacheItemExpiration> cacheParameters = new List<ICacheItemExpiration>();

                            //foreach sitefinity content item in items
                            foreach (U item in items)
                            {
                                //create a new instance of a cache object
                                T dataItem = (T)Activator.CreateInstance(typeof(T));

                                //call process item to move over cachecontent item values into the cache object
                                dataItem.ProcessItem(item);

                                string itemKey = dataItem.ItemKey();

                                //try to get an item at the existing item key
                                T existingDataItem = (T)Activator.CreateInstance(typeof(T));
                                bool itemExists = cachedDictionaryItems.TryGetValue(itemKey, out existingDataItem);

                                //if an item exists, add a link to the new one
                                if (itemExists)
                                {
                                    //create iterator for going through linked list
                                    T iterator = existingDataItem;

                                    //while the iterator is able to traverse to the next node
                                    while (iterator.Next != null)
                                    {
                                        //set the iterator to the next node
                                        iterator = iterator.Next;
                                    }

                                    //iterator is at the final node, add a node
                                    iterator.Next = dataItem;

                                    cachedDictionaryItems.Remove(itemKey);
                                    cachedDictionaryItems.Add(itemKey, existingDataItem);
                                }
                                //if an item doesn't exist, add the new one
                                else
                                {
                                    cachedDictionaryItems.Add(itemKey, dataItem);
                                }

                                //create dependency on this items id
                                DataItemCacheDependency dependency
                                    = new DataItemCacheDependency(typeof(U), dataItem.Id);

                                //add dependency
                                dependencies.Add(dependency);
                            }

                            //if the list of cachedItems is not null and there is 1 or more, add them to cache
                            if (cachedDictionaryItems != null && cachedDictionaryItems.Count > 0)
                            {
                                //if more than 1 dependency add them to cacheparameters
                                if (dependencies.Count > 0)
                                    cacheParameters.AddRange(dependencies);

                                //add sliding expiration time to cache parameters
                                if (expirationMinutes > 0)
                                    cacheParameters.Add(new SlidingTime(TimeSpan.FromMinutes(expirationMinutes)));
                                else
                                    cacheParameters.Add(new SlidingTime(TimeSpan.FromMinutes(DefaultExpirationMinutes)));

                                //add items, key, and parameters to cache through CacheManager
                                CacheManager.Add(
                                    key,
                                    cachedDictionaryItems,
                                    CacheItemPriority.Normal,
                                    null,
                                    cacheParameters.ToArray<ICacheItemExpiration>());
                            }
                        }
                        else
                        {
                            cachedDictionaryItems = new Dictionary<string, T>();

                            if (cacheType != null)
                            {
                                //create list of parameters
                                List<ICacheItemExpiration> cacheParameters = new List<ICacheItemExpiration>();

                                //add sliding expiration time to cache parameters
                                if (expirationMinutes > 0)
                                    cacheParameters.Add(new SlidingTime(TimeSpan.FromMinutes(expirationMinutes)));
                                else
                                    cacheParameters.Add(new SlidingTime(TimeSpan.FromMinutes(DefaultExpirationMinutes)));

                                //add items, key, and parameters to cache through CacheManager
                                CacheManager.Add(
                                    key,
                                    cachedDictionaryItems,
                                    CacheItemPriority.Normal,
                                    null,
                                    cacheParameters.ToArray<ICacheItemExpiration>());
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Retrieves content items from cache of a specified key that have been stored as a list
        /// </summary>
        /// <typeparam name="T">Cacheable type</typeparam>
        /// <typeparam name="U">Sitefinity type</typeparam>
        /// <param name="failQuery">Query that runs if returned cached items is null</param>
        /// <param name="key">cache key</param>
        public List<T> GetListItems<T, U>(string key, IQueryable<U> failQuery = null, int expirationMinutes = 0) where T: ICacheData<U>
        {
            //fetch cached list items
            List<T> cachedItems = (List<T>)CacheManager[key];

            //check if we are supposed to fall back and if the cacheditems is null
            if (failQuery != null && cachedItems == null)
            {
                //cache from the failquery
                if(expirationMinutes > 0)
                    CacheDataSetAsList<T, U>(failQuery, key, expirationMinutes);
                else
                    CacheDataSetAsList<T, U>(failQuery, key);

                //return from the cache, even if it's null
                return CacheManager[key] as List<T>;
            }

            //return the list from the cache
            return cachedItems;
        }

        /// <summary>
        /// Retrieves content items from cache of a specified key that have been stored as a dictionary
        /// </summary>
        /// <typeparam name="T">Cacheable type</typeparam>
        /// <typeparam name="U">Sitefinity type</typeparam>
        /// <param name="key">cache key</param>
        /// <param name="failQuery">Query that runs if returned cached items is null</param>
        /// <returns>dictionary of cached items</returns>
        public Dictionary<string, T> GetDictionaryItems<T, U>(string key, IQueryable<U> failQuery = null, int expirationMinutes = 0) where T: ICacheData<U>
        {
            //fetch cached dictionary items
            Dictionary<string, T> cachedItems = (Dictionary<string, T>)CacheManager[key + "-dict"];

            //check if we are supposed to fall back and if the cacheditems is null
            if (failQuery != null && cachedItems == null)
            {
                //cache from the failquery
                if (expirationMinutes > 0)
                    CacheDataSetAsDictionary<T, U>(failQuery, key, expirationMinutes);
                else
                    CacheDataSetAsDictionary<T, U>(failQuery, key);

                //return from the cache, even if it's null
                return CacheManager[key + "-dict"] as Dictionary<string, T>;
            }

            //return the list from the cache
            return cachedItems;
        }

        /// <summary>
        /// Caches provided data set as a list from IQueryable by specified key
        /// </summary>
        /// <typeparam name="T">Cacheable type</typeparam>
        /// <typeparam name="U">Sitefinity type</typeparam>
        /// <param name="sfContentQuery">Sitefintiy data set to be cached</param>
        /// <param name="key">Key associated with set in cache</param>
        public void CacheDataSetAsList<T, U>(IQueryable<U> sfContentQuery, string key, int expirationMinutes = 0) where T: ICacheData<U>
        {
            //create instance of a cacheable data type
            T cacheableDataItem = (T)Activator.CreateInstance(typeof(T));

            //get the content type that we are working with
            Type cacheableType = cacheableDataItem.GetContentType<U>();

            //get a string of that content type, to be used in the key
            string cacheableTypeName = cacheableDataItem.GetContentType<U>().ToString();

            //try to fetch these cached items from the cache
            List<T> cachedItems = (List<T>)CacheManager[key];

            //as long as nothing is returned from the cache, proceed with caching items
            if (cachedItems == null)
            {
                lock (itemLock)
                {
                    // Check again if nothing is in the cache.
                    cachedItems = (List<T>)CacheManager[key];

                    // Return if there are items in the cache.
                    if (cachedItems != null)
                        return;

                    //initialize cachedItems to new list
                    cachedItems = new List<T>();

                    //if the content type is not null, proceed with pulling in data from database
                    if (cacheableType != null)
                    {
                        //initialize a list of cacheable content items
                        List<U> items = new List<U>();

                        items = sfContentQuery.ToList<U>();

                        //if the list of items is not null and there is 1 or more, begin process for adding them to cache
                        if (items != null && items.Count > 0)
                        {
                            //create list of dependencies
                            List<DataItemCacheDependency> dependencies = new List<DataItemCacheDependency>();
                            
                            //create list of parameters
                            List<ICacheItemExpiration> cacheParameters = new List<ICacheItemExpiration>();

                            //foreach sitefinity content item in items
                            foreach (U item in items)
                            {
                                //create a new instance of a cacheable object
                                T dataItem = (T)Activator.CreateInstance(typeof(T));

                                //call process item to move over cacheablecontent item values into the cacheable object
                                dataItem.ProcessItem(item);

                                //add the new object to the list of cached items
                                cachedItems.Add(dataItem);

                                //create dependency on this items id
                                DataItemCacheDependency dependency
                                    = new DataItemCacheDependency(typeof(U), dataItem.Id);

                                //add dependency
                                dependencies.Add(dependency);
                            }

                            //if the list of cachedItems is not null and there is 1 or more, add them to cache
                            if (cachedItems != null && cachedItems.Count > 0)
                            {
                                //if more than 1 dependency add them to cacheparameters
                                if (dependencies.Count > 0)
                                    cacheParameters.AddRange(dependencies);

                                //add sliding expiration time to cache parameters
                                if(expirationMinutes > 0)
                                    cacheParameters.Add(new SlidingTime(TimeSpan.FromMinutes(expirationMinutes)));
                                else
                                    cacheParameters.Add(new SlidingTime(TimeSpan.FromMinutes(DefaultExpirationMinutes)));

                                //add items, key, and parameters to cache through CacheManager
                                CacheManager.Add(
                                    key,
                                    cachedItems,
                                    CacheItemPriority.Normal,
                                    null,
                                    cacheParameters.ToArray<ICacheItemExpiration>());
                            }
                        }
                        else
                        {
                            cachedItems = new List<T>();

                            //create list of parameters
                            List<ICacheItemExpiration> cacheParameters = new List<ICacheItemExpiration>();

                            //add sliding expiration time to cache parameters
                            if (expirationMinutes > 0)
                                cacheParameters.Add(new SlidingTime(TimeSpan.FromMinutes(expirationMinutes)));
                            else
                                cacheParameters.Add(new SlidingTime(TimeSpan.FromMinutes(DefaultExpirationMinutes)));

                            //add items, key, and parameters to cache through CacheManager
                            CacheManager.Add(
                                key,
                                cachedItems,
                                CacheItemPriority.Normal,
                                null,
                                cacheParameters.ToArray<ICacheItemExpiration>());
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Caches provided data set as a dictionary from IQueryable by specified key
        /// </summary>
        /// <typeparam name="T">Cacheable type</typeparam>
        /// <typeparam name="U">Sitefinity type</typeparam>
        /// <param name="sfContentQuery">Sitefintiy data set to be cached</param>
        /// <param name="key">Key associated with set in cache</param>
        public void CacheDataSetAsDictionary<T, U>(IQueryable<U> sfContentQuery, string key, int expirationMinutes = 0) where T : ICacheData<U>
        {
            T cacheableDataItem = (T)Activator.CreateInstance(typeof(T));
            Type cacheableType = cacheableDataItem.GetContentType<U>();
            string cacheableTypeName = cacheableDataItem.GetContentType<U>().ToString();

            //if (key == null && sfContentQuery == null)
            //    key = cacheableTypeName;

            //adding dict to the end so it does not conflict with other list cache keys
            key = key + "-dict";
            
            //List<T> cachedItems = (List<T>)CacheManager[key];
            Dictionary<string, T> cachedDictionary = (Dictionary<string, T>)CacheManager[key];

            if (cachedDictionary == null)
            {
                lock (itemLock)
                { 
                    if (cacheableType != null)
                    {
                        cachedDictionary = (Dictionary<string, T>)CacheManager[key];

                        if (cachedDictionary != null)
                            return;

                        cachedDictionary = new Dictionary<string, T>();

                        List<U> items = new List<U>();

                        items = sfContentQuery.ToList<U>();

                        if (items != null && items.Count > 0)
                        {
                            List<DataItemCacheDependency> dependencies = new List<DataItemCacheDependency>();
                            List<ICacheItemExpiration> cacheParameters = new List<ICacheItemExpiration>();

                            //for each sitefinity content item in items
                            foreach (U item in items)
                            {
                                //create a new instance of the data item
                                T dataItem = (T)Activator.CreateInstance(typeof(T));

                                //process, move over the sitefinity field values to the cacheable item
                                dataItem.ProcessItem(item);

                                //add it to the dictionary using ItemKey as the key for each item
                                cachedDictionary.Add(dataItem.ItemKey(), dataItem);

                                DataItemCacheDependency dependency
                                    = new DataItemCacheDependency(typeof(U), dataItem.Id);

                                dependencies.Add(dependency);
                            }

                            if (cachedDictionary != null && cachedDictionary.Count > 0)
                            {
                                if (dependencies.Count > 0)
                                    cacheParameters.AddRange(dependencies);

                                if(expirationMinutes > 0)
                                    cacheParameters.Add(new SlidingTime(TimeSpan.FromMinutes(expirationMinutes)));
                                else
                                    cacheParameters.Add(new SlidingTime(TimeSpan.FromMinutes(DefaultExpirationMinutes)));

                                CacheManager.Add(
                                    key,
                                    cachedDictionary,
                                    CacheItemPriority.Normal,
                                    null,
                                    cacheParameters.ToArray<ICacheItemExpiration>());
                            }
                        }
                        else
                        {
                            cachedDictionary = new Dictionary<string, T>();

                            if (cacheableType != null)
                            {
                                //create list of parameters
                                List<ICacheItemExpiration> cacheParameters = new List<ICacheItemExpiration>();

                                //add sliding expiration time to cache parameters
                                if (expirationMinutes > 0)
                                    cacheParameters.Add(new SlidingTime(TimeSpan.FromMinutes(expirationMinutes)));
                                else
                                    cacheParameters.Add(new SlidingTime(TimeSpan.FromMinutes(DefaultExpirationMinutes)));

                                //add items, key, and parameters to cache through CacheManager
                                CacheManager.Add(
                                    key,
                                    cachedDictionary,
                                    CacheItemPriority.Normal,
                                    null,
                                    cacheParameters.ToArray<ICacheItemExpiration>());
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Caches base data set, implemented in cacheable, as dictionary
        /// </summary>
        /// <typeparam name="T">Cacheable type</typeparam>
        /// <typeparam name="U">Sitefinity type</typeparam>
        public void CacheBaseDataSetAsDictionary<T, U>(int expirationMinutes = 0) where T : ICacheData<U>
        {
            T cacheableDataItem = (T)Activator.CreateInstance(typeof(T));
            Type cacheableType = cacheableDataItem.GetContentType<U>();
            string cacheableTypeName = cacheableDataItem.GetContentType<U>().ToString();

            //adding dict to the end so it does not conflict with other list cache keys
            string key = String.Empty;
            key = cacheableDataItem.GetContentType<U>().ToString() + "-dict";

            //List<T> cachedItems = (List<T>)CacheManager[key];
            Dictionary<string, T> cachedDictionary = (Dictionary<string, T>)CacheManager[key];

            if (cachedDictionary == null)
            {
                lock (itemLock)
                {
                    if (cacheableType != null)
                    {
                        cachedDictionary = (Dictionary<string, T>)CacheManager[key];

                        if (cachedDictionary != null)
                            return;

                        cachedDictionary = new Dictionary<string, T>();

                        List<U> items = new List<U>();

                        items = cacheableDataItem.GetBaseSitefinityDataSetQuery<U>().ToList();

                        if (items != null && items.Count > 0)
                        {
                            List<DataItemCacheDependency> dependencies = new List<DataItemCacheDependency>();
                            List<ICacheItemExpiration> cacheParameters = new List<ICacheItemExpiration>();

                            //for each sitefinity content item in items
                            foreach (U item in items)
                            {
                                //create a new instance of the data item
                                T dataItem = (T)Activator.CreateInstance(typeof(T));

                                //process, move over the sitefinity field values to the cacheable item
                                dataItem.ProcessItem(item);

                                //add it to the dictionary using ItemKey as the key for each item
                                cachedDictionary.Add(dataItem.ItemKey(), dataItem);

                                DataItemCacheDependency dependency
                                    = new DataItemCacheDependency(typeof(U), dataItem.Id);

                                dependencies.Add(dependency);
                            }

                            if (cachedDictionary != null && cachedDictionary.Count > 0)
                            {
                                if (dependencies.Count > 0)
                                    cacheParameters.AddRange(dependencies);

                                if(expirationMinutes > 0)
                                    cacheParameters.Add(new SlidingTime(TimeSpan.FromMinutes(expirationMinutes)));
                                else
                                    cacheParameters.Add(new SlidingTime(TimeSpan.FromMinutes(DefaultExpirationMinutes)));
                                

                                CacheManager.Add(
                                    key,
                                    cachedDictionary,
                                    CacheItemPriority.Normal,
                                    null,
                                    cacheParameters.ToArray<ICacheItemExpiration>());
                            }
                        }
                        else
                        {
                            cachedDictionary = new Dictionary<string, T>();

                            if (cacheableType != null)
                            {
                                //create list of parameters
                                List<ICacheItemExpiration> cacheParameters = new List<ICacheItemExpiration>();

                                //add sliding expiration time to cache parameters
                                if (expirationMinutes > 0)
                                    cacheParameters.Add(new SlidingTime(TimeSpan.FromMinutes(expirationMinutes)));
                                else
                                    cacheParameters.Add(new SlidingTime(TimeSpan.FromMinutes(DefaultExpirationMinutes)));

                                //add items, key, and parameters to cache through CacheManager
                                CacheManager.Add(
                                    key,
                                    cachedDictionary,
                                    CacheItemPriority.Normal,
                                    null,
                                    cacheParameters.ToArray<ICacheItemExpiration>());
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Caches base data set, implemented in cacheable, as dictionary
        /// </summary>
        /// <typeparam name="T">Cacheable type</typeparam>
        /// <typeparam name="U">Sitefinity type</typeparam>
        public void CacheBaseDataSetAsList<T, U>(int expirationMinutes = 0) where T : ICacheData<U>
        {
            //create instance of a cacheable data type
            T cacheableDataItem = (T)Activator.CreateInstance(typeof(T));

            //get the content type that we are working with
            Type cacheableType = cacheableDataItem.GetContentType<U>();

            //get a string of that content type, to be used in the key
            string cacheableTypeName = cacheableDataItem.GetContentType<U>().ToString();

            string key = String.Empty;
            key = cacheableDataItem.GetContentType<U>().ToString();

            //try to fetch these cached items from the cache
            List<T> cachedItems = (List<T>)CacheManager[key];

            //as long as nothing is returned from the cache, proceed with caching items
            if (cachedItems == null)
            {
                lock (itemLock)
                {
                    cachedItems = (List<T>)CacheManager[key];

                    if (cachedItems != null)
                        return;

                    //initialize cachedItems to new list
                    cachedItems = new List<T>();

                    //if the content type is not null, proceed with pulling in data from database
                    if (cacheableType != null)
                    {
                        //initialize a list of cacheable content items
                        List<U> items = new List<U>();

                        items = cacheableDataItem.GetBaseSitefinityDataSetQuery<U>().ToList();

                        //if the list of items is not null and there is 1 or more, begin process for adding them to cache
                        if (items != null && items.Count > 0)
                        {
                            //create list of dependencies
                            List<DataItemCacheDependency> dependencies = new List<DataItemCacheDependency>();

                            //create list of parameters
                            List<ICacheItemExpiration> cacheParameters = new List<ICacheItemExpiration>();

                            //foreach sitefinity content item in items
                            foreach (U item in items)
                            {
                                //create a new instance of a cacheable object
                                T dataItem = (T)Activator.CreateInstance(typeof(T));

                                //call process item to move over cacheablecontent item values into the cacheable object
                                dataItem.ProcessItem(item);

                                //add the new object to the list of cached items
                                cachedItems.Add(dataItem);

                                //create dependency on this items id
                                DataItemCacheDependency dependency
                                    = new DataItemCacheDependency(typeof(U), dataItem.Id);

                                //add dependency
                                dependencies.Add(dependency);
                            }

                            //if the list of cachedItems is not null and there is 1 or more, add them to cache
                            if (cachedItems != null && cachedItems.Count > 0)
                            {
                                //if more than 1 dependency add them to cacheparameters
                                if (dependencies.Count > 0)
                                    cacheParameters.AddRange(dependencies);

                                //add sliding expiration time to cache parameters
                                if(expirationMinutes > 0)
                                    cacheParameters.Add(new SlidingTime(TimeSpan.FromMinutes(expirationMinutes)));
                                else
                                    cacheParameters.Add(new SlidingTime(TimeSpan.FromMinutes(DefaultExpirationMinutes)));


                                //add items, key, and parameters to cache through CacheManager
                                CacheManager.Add(
                                    key,
                                    cachedItems,
                                    CacheItemPriority.Normal,
                                    null,
                                    cacheParameters.ToArray<ICacheItemExpiration>());
                            }
                        }
                        else
                        {
                            cachedItems = new List<T>();

                            //create list of parameters
                            List<ICacheItemExpiration> cacheParameters = new List<ICacheItemExpiration>();

                            //add sliding expiration time to cache parameters
                            if (expirationMinutes > 0)
                                cacheParameters.Add(new SlidingTime(TimeSpan.FromMinutes(expirationMinutes)));
                            else
                                cacheParameters.Add(new SlidingTime(TimeSpan.FromMinutes(DefaultExpirationMinutes)));

                            //add items, key, and parameters to cache through CacheManager
                            CacheManager.Add(
                                key,
                                cachedItems,
                                CacheItemPriority.Normal,
                                null,
                                cacheParameters.ToArray<ICacheItemExpiration>());
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Clears all cached list items from the cache via provided key
        /// </summary>
        /// <typeparam name="T">Cacheable type</typeparam>
        /// <typeparam name="U">Sitefinity type</typeparam>
        /// <param name="key">key to clear</param>
        public void ClearCachedListItems<T, U>(string key) where T: ICacheData<U>
        {
            if (!String.IsNullOrEmpty(key))
            {
                //create instance of cacheable data item
                T cacheableDataItem = (T)Activator.CreateInstance(typeof(T));

                //grab the set type for the cacheable item
                Type cacheableType = cacheableDataItem.GetContentType<U>();

                //convert to a string which will be used as the key
                string cacheableTypeName = cacheableDataItem.GetContentType<U>().ToString();

                //clear the cache for this key
                CacheManager.Remove(key);
            }
        }

        /// <summary>
        /// Clears all base list items from the cache
        /// </summary>
        /// <typeparam name="T">Cacheable type</typeparam>
        /// <typeparam name="U">Sitefinity type</typeparam>
        public void ClearCachedListItems<T, U>() where T : ICacheData<U>
        {
            string key = String.Empty;

            //create instance of cacheable data item
            T cacheableDataItem = (T)Activator.CreateInstance(typeof(T));

            //grab the set type for the cacheable item
            Type cacheableType = cacheableDataItem.GetContentType<U>();

            //convert to a string which will be used as the key
            string cacheableTypeName = cacheableDataItem.GetContentType<U>().ToString();

            key = cacheableTypeName;

            //clear the cache for this key
            CacheManager.Remove(key);
        }

        /// <summary>
        /// Clears all cached dictionary items from the cache via provided key
        /// </summary>
        /// <typeparam name="T">Cacheable type</typeparam>
        /// <typeparam name="U">Sitefinity type</typeparam>
        /// <param name="key">optional cache key to clear</param>
        public void ClearCachedDictionaryItems<T, U>(string key) where T: ICacheData<U>
        {
            if (!String.IsNullOrEmpty(key))
            {
                //create instance of cacheable data item
                T cacheableDataItem = (T)Activator.CreateInstance(typeof(T));

                //convert to a string which will be used as the key
                string cacheableTypeName = cacheableDataItem.GetContentType<U>().ToString();

                //if the key provided is null, use the type name
                if (key == null)
                    key = cacheableTypeName;

                //clear the cache for this key
                CacheManager.Remove(key + "-dict");
            }
        }

        /// <summary>
        /// Clears all base dictionary items from the cache
        /// </summary>
        /// <typeparam name="T">Cacheable type</typeparam>
        /// <typeparam name="U">Sitefinity type</typeparam>
        public void ClearCachedDictionaryItems<T, U>() where T : ICacheData<U>
        {
            string key = String.Empty;

            //create instance of cacheable data item
            T cacheableDataItem = (T)Activator.CreateInstance(typeof(T));

            //convert to a string which will be used as the key
            string cacheableTypeName = cacheableDataItem.GetContentType<U>().ToString();

            //use the type name
            key = cacheableTypeName;

            //clear the cache for this key
            CacheManager.Remove(key + "-dict");
        }

        /// <summary>
        /// Adds individual cacheable object to the cache
        /// </summary>
        /// <typeparam name="T">Cacheable type</typeparam>
        /// <typeparam name="U">Sitefinity type</typeparam>
        /// <param name="dataItem">data item to cache</param>
        public void AddToCache<T, U>(T dataItem) where T : ICacheData<U>
        {
            //if the dataitem being passed through is not null
            if (dataItem != null)
            {
                //create cache parameters
                List<ICacheItemExpiration> cacheParams = new List<ICacheItemExpiration>();

                //create cache dependencies
                List<DataItemCacheDependency> dependencies = new List<DataItemCacheDependency>();

                //create dependency on data item id
                DataItemCacheDependency dependency
                    = new DataItemCacheDependency(typeof(T), dataItem.Id);

                //add the dependency to the list of dependencies
                dependencies.Add(dependency);

                //add all dependencies to the cache parameters
                cacheParams.AddRange(dependencies);

                //add sliding time to the cache parameters, default 60 minute
                cacheParams.Add(new SlidingTime(TimeSpan.FromMinutes(DefaultExpirationMinutes)));

                //add data item key, item, priority and parameters to the cache through the cachemanager
                CacheManager.Add(
                    dataItem.ItemKey(),
                    dataItem,
                    CacheItemPriority.Normal,
                    null,
                    cacheParams.ToArray<ICacheItemExpiration>());
            }
        }

        /// <summary>
        /// Caches a single object
        /// </summary>
        /// <typeparam name="T">Type of object being cached</typeparam>
        /// <param name="dataItem">Object being cached</param>
        /// <param name="key">Key for cached object</param>
        public void CacheObject<T>(T dataItem, string key)
        {
            //if the dataitem being passed through is not null
            if (dataItem != null && !string.IsNullOrEmpty(key))
            {
                //create cache parameters
                List<ICacheItemExpiration> cacheParams = new List<ICacheItemExpiration>();

                //create cache dependencies
                List<DataItemCacheDependency> dependencies = new List<DataItemCacheDependency>();

                //add all dependencies to the cache parameters
                cacheParams.AddRange(dependencies);

                //add sliding time to the cache parameters, default 60 minute
                cacheParams.Add(new SlidingTime(TimeSpan.FromMinutes(DefaultExpirationMinutes)));

                //add data item key, item, priority and parameters to the cache through the cachemanager
                CacheManager.Add(
                    key,
                    dataItem,
                    CacheItemPriority.Normal,
                    null,
                    cacheParams.ToArray<ICacheItemExpiration>());
            }
        }

        /// <summary>
        /// Returns a single cache object from provided key
        /// </summary>
        /// <typeparam name="T">Type of object being returned</typeparam>
        /// <param name="key">Key for looking up cached object</param>
        /// <returns>Object of type T</returns>
        public T GetObject<T>(string key)
        {
            //return from the cache, even if it's null
            return (T)CacheManager[key];
        }

        /// <summary>
        /// Clears the cache for a provided cache key
        /// </summary>
        /// <param name="key">Cache key</param>
        public void ClearCachedObject(string key)
        {
            //remove cache at key
            CacheManager.Remove(key);
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Default empty constructor, initializes expiration to 60 minutes
        /// </summary>
        public CacheDataService()
        {
            _expirationMinutes = 60;
        }

        /// <summary>
        /// Constructor that sets initial expiration
        /// </summary>
        /// <param name="expirationMinutes"></param>
        public CacheDataService(int expirationMinutes)
        {
            _expirationMinutes = expirationMinutes;
        }

        #endregion
    }

    #region CacheStructure Enum

    /// <summary>
    /// Enum for tracking what the items are being cached as
    /// </summary>
    public enum CacheStructure
    {
        Dictionary,
        List
    }

    #endregion
}
