using CobMvc.Core.Common;
using System;
using Xunit;
using Shouldly;
using System.Threading.Tasks;

namespace CobMvc.Test
{
    public class CommonHelperTest
    {
        [Fact]
        public void TestCombineUrl()
        {
            UriHelper.Combine("base", "a/b").ShouldBe("base/a/b");
            UriHelper.Combine("/base", "a/b").ShouldBe("/base/a/b");
            UriHelper.Combine("/base/other", "a/b").ShouldBe("/base/other/a/b");
            UriHelper.Combine("/base", "/a/b").ShouldBe("/a/b");
            UriHelper.Combine("/base/other", "/a/b").ShouldBe("/a/b");

            UriHelper.Combine("/base", "a/b", "c").ShouldBe("/base/a/b/c");

            UriHelper.Combine("/base", "/a/b", "c").ShouldBe("/a/b/c");

            Should.Throw<NotSupportedException>(() => UriHelper.Combine("/base/a", "../a"));
        }

        [Fact]
        public void TestNetHelper()
        {
            NetHelper.IsLoopBack("localhost").ShouldBe(true);
            NetHelper.IsLoopBack("127.0.0.1").ShouldBe(true);
            NetHelper.IsLoopBack("::1").ShouldBe(true);
            NetHelper.IsLoopBack("+").ShouldBe(true);

            NetHelper.ChangeToExternal("localhost").ShouldNotBe("127.0.0.1");
        }

        [Fact]
        public void TestIsValueTypeOrString()
        {
            "1".IsValueTypeOrString().ShouldBe(true);
            1.IsValueTypeOrString().ShouldBe(true);
            new Object().IsValueTypeOrString().ShouldBe(false);
        }

        [Fact]
        public void TestTaskHelper()
        {
            TaskHelper.GetUnderlyingType(typeof(Task<int>), out bool isTask).ShouldBe(typeof(int));
            isTask.ShouldBe(true);

            TaskHelper.GetUnderlyingType(typeof(Task), out isTask).ShouldBe(null);
            isTask.ShouldBe(true);

            TaskHelper.GetUnderlyingType(typeof(int), out isTask).ShouldBe(typeof(int));
            isTask.ShouldBe(false);

            TaskHelper.ConvertToGeneric(typeof(int), Task.FromResult<object>(1)).ShouldBeAssignableTo(typeof(Task<int>));

            TaskHelper.GetResult(Task.FromResult(1)).ShouldBe(1);
            TaskHelper.GetResult(Task.CompletedTask).ShouldBe(null);
        }

        [Fact]
        public void TestToHex()
        {
            new byte[] { 1, 0x03, 0x0F, 0x10, 0x1a, 0xaf }.ToHex().ShouldBe("01030f101aaf");

            "aspark".ToMD5().ShouldBe("209e985ef22b12160f39d0dddae0bb95");
        }
    }
}
