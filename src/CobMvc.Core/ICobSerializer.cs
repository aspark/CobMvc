using System;
using System.Collections.Generic;
using System.Text;

namespace CobMvc.Core
{
    public interface ICobSerializer
    {
        string ContentType { get; }

        object Deserialize(byte[] bs);

        byte[] Serialize(object obj);
    }
}
