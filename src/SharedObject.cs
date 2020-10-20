using System;

namespace Atomus.Service
{
    internal class SharedObject
    {
        public string Key { get; set; }
        public object Object { get; set; }
        public DateTime ExpiryDateTime { get; set; }
        public int ReadCount { get; set; } = 0;
        public int MaxReadCount { get; set; } = int.MaxValue;

        public SharedObject() { }

        public SharedObject(string key)
        {
            this.Key = key;
        }
    }
}