using Enyim.Caching;
using Enyim.Caching.Configuration;
using Enyim.Caching.Memcached;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Aroc.Cached
{
    public class ArocMemcached
    {
        private MemcachedClientConfiguration _config = new MemcachedClientConfiguration();
        /// <summary>
        /// Get Memcached config
        /// </summary>
        public MemcachedClientConfiguration Config
        {
            get { return _config; }
        }

        private MemcachedClient _client = null;
        /// <summary>
        /// Get Memcached Client
        /// </summary>
        public MemcachedClient Client
        {
            get { return _client; }
            private set { _client = value; }
        }

        /// <summary>
        /// constructor
        /// </summary>
        public ArocMemcached()
        {
            Config.Servers.Add(new IPEndPoint(IPAddress.Loopback, 11211));
            Config.Protocol = MemcachedProtocol.Binary;
            Config.Authentication.Type = typeof(PlainTextAuthenticator);
            Config.Authentication.Parameters.Add("userName", "demo");
            Config.Authentication.Parameters.Add("passWord", "demo");

            Client = new MemcachedClient(Config);
        }

        /// <summary>
        /// Store Info
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="dic"></param>
        public void Add<TValue>(IDictionary<string, TValue> dic)
        {
            if (dic == null || dic.Count < 1)
            {
                return;
            }
            foreach (var key in dic.Keys)
            {
                Client.Store(StoreMode.Add, key, dic[key]);
            }
        }

        /*......*/
    }
}
