using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;

namespace MadsKristensen.EditorExtensions.Helpers
{
    ///<summary>Caches objects built from files that might change.</summary>
    ///<remarks>Each cache has a limited size, and will re-run the generator if the file changed since it was last requested.</remarks>
    public class FileCache<T>
    {
        const int MaxSize = 10;

        private readonly ConcurrentDictionary<string, CacheEntry> cache = new ConcurrentDictionary<string, CacheEntry>();

        private class CacheEntry
        {
            public CacheEntry(T value, DateTime lastWrite)
            {
                Value = value;
                LastWrite = lastWrite;
            }
            public readonly T Value;
            public readonly DateTime LastWrite;
        }


        private readonly Func<string, T> generator;
        public FileCache(Func<string, T> generator)
        {
            this.generator = generator;
        }

        public T Get(string path)
        {
            bool added = false;

            var retVal = cache.GetOrAdd(path, p =>
            {
                added = true;
                return new CacheEntry(generator(p), File.GetLastWriteTimeUtc(path));
            });

            CacheEntry unused;

            // If the file was touched since we last cached it, evict it
            // from the cache and re-create it.
            if (!added && File.GetLastWriteTimeUtc(path) != retVal.LastWrite)
            {
                // If another thread removed it, fine.
                cache.TryRemove(path, out unused);
                // Either way, just GetOrAdd again.
                return Get(path);
            }

            // Keep trying to remove an item until we succeed, or until another
            // thread shrinks the cache. There is a race condition here; if two
            // threads get past cache.Count > MaxSize at once, but run the next
            // two calls sequentially, we can end up removing two items.  There
            // is nothing particularly bad about that - this is only a cache.
            while (added && cache.Count > MaxSize && !cache.TryRemove(cache.FirstOrDefault().Key, out unused))
                ;
            return retVal.Value;
        }
    }
}
