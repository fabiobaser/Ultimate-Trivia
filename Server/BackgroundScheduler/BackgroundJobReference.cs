using System;
using Quartz;

namespace BackgroundScheduler
{
    internal class BackgroundJobReference
    {
        public static BackgroundJobReference Create<T>() where T: class, IJob 
            => new BackgroundJobReference{Type = typeof(T)};

        public Type Type { get; set; }
    }
}