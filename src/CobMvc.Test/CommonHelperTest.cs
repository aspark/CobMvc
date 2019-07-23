using CobMvc.Core.Common;
using System;
using Xunit;

namespace CobMvc.Test
{
    public class CommonHelperTest
    {
        [Fact]
        public void TestCombineUrl()
        {
            UriHelper.Combine("", "");
        }
    }
}
