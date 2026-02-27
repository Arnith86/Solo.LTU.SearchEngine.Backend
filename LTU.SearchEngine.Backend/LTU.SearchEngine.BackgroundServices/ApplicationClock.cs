using System;
using System.Collections.Generic;
using System.Text;

namespace LTU.SearchEngine.BackgroundServices
{
    public class ApplicationClock : IClock
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}
