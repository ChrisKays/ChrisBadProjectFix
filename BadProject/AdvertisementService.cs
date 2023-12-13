using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Runtime.Caching;
using System.Threading;
using ThirdParty;

namespace Adv
{
    public class AdvertisementService
    {
        private readonly MemoryCache cache;
        private Queue<DateTime> errors;
        private readonly object lockObj = new object();

        private readonly NoSqlAdvProvider mainProvider;
        private readonly int maxErrorCount;

        public AdvertisementService(MemoryCache cache, NoSqlAdvProvider mainProvider, int maxErrorCount)
        {
            this.cache = cache ?? throw new ArgumentNullException(nameof(cache));
            this.mainProvider = mainProvider ?? throw new ArgumentNullException(nameof(mainProvider));
            this.maxErrorCount = maxErrorCount;
            this.errors = new Queue<DateTime>();
        }

        public Advertisement GetAdvertisement(string id)
        {
            lock (lockObj)
            {
                var adv = TryGetFromCache(id) ?? TryGetFromMainProvider(id);

                if (adv == null)
                {
                    adv = TryGetFromBackupProvider(id);
                }

                return adv;
            }
        }

        private Advertisement TryGetFromCache(string id)
        {
            return (Advertisement)cache.Get($"AdvKey_{id}");
        }

        private Advertisement TryGetFromMainProvider(string id)
        {
            var errorCount = CountErrorsInLastHour();

            if (errorCount >= maxErrorCount)
            {
                return null;
            }

            Advertisement adv = null;
            //CM: For some unknown reasons, i'm unable to read fro the config file. Hard coded the retry count for now.
            var retryCount = 3; //int.Parse(ConfigurationManager.AppSettings["RetryCount"]);

            for (var retry = 0; retry < retryCount; retry++)
            {
                try
                {
                    adv = mainProvider.GetAdv(id);
                    break;
                }
                catch
                {
                    Thread.Sleep(1000);
                    errors.Enqueue(DateTime.Now);
                }
            }

            if (adv != null)
            {
                cache.Set($"AdvKey_{id}", adv, DateTimeOffset.Now.AddMinutes(5));
            }

            return adv;
        }

        private Advertisement TryGetFromBackupProvider(string id)
        {
            var adv = SQLAdvProvider.GetAdv(id);

            if (adv != null)
            {
                cache.Set($"AdvKey_{id}", adv, DateTimeOffset.Now.AddMinutes(5));
            }

            return adv;
        }

        private int CountErrorsInLastHour()
        {
            var cutoffTime = DateTime.Now.AddHours(-1);
            errors = new Queue<DateTime>(errors.Where(dat => dat > cutoffTime));

            return errors.Count;
        }
    }

}
