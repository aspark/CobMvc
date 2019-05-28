using System;
using System.Collections.Generic;
using System.Text;

namespace Cobweb.Core.Config
{
    public interface ICobConfiguration
    {
        string Get(string name);

        T Get<T>(string name);
    }
}
