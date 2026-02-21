using System;
using System.Collections.Generic;
using System.Text;

namespace LTU.SearchEngine.Infrastructure.Configurations
{
    public interface IRobotsHandler
    {
        bool IsAllowed(string url);
    }
}
