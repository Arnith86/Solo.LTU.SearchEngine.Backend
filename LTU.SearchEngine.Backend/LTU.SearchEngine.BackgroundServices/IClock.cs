using System;
using System.Collections.Generic;
using System.Text;

namespace LTU.SearchEngine.BackgroundServices
{
    public interface IClock
    {
        DateTime UtcNow { get; }
    }
}
