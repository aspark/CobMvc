using System;
using System.Collections.Generic;
using System.Text;

namespace Cobweb.Core.Config
{
    class DefaultCobConfiguration : ICobConfiguration
    {
        public DefaultCobConfiguration()
        {

        }

        public string Get(string name)
        {
            throw new NotImplementedException();
        }

        public T Get<T>(string name)
        {
            throw new NotImplementedException();
        }
    }
}
