using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Shouldly;
using CobMvc.Core.Common;
using System.Threading.Tasks;

namespace CobMvc.Test
{
    public class AutoResetSignalTest
    {
        [Fact]
        public async Task TestReset()
        {
            var reset = new AutoResetSignal(1000);
            reset.Set();
            reset.Value.ShouldBe(true);
            reset.FireCount.ShouldBe(1);

            await Task.Delay(1000);

            reset.Value.ShouldBe(false);
            reset.FireCount.ShouldBe(1);

            reset.Set();
            reset.Value.ShouldBe(true);
            reset.FireCount.ShouldBe(2);

            await Task.Delay(1000);
            reset.Value.ShouldBe(true);
            reset.FireCount.ShouldBe(2);

            reset.Reset();

            reset.Value.ShouldBe(false);
            reset.FireCount.ShouldBe(2);
        }

        [Fact]
        public async Task TestThreshouldReset()
        {
            var reset = new ThresholdAutoResetSignal(3, 1000);

            reset.Set();
            reset.Value.ShouldBe(false);
            reset.FireCount.ShouldBe(0);

            reset.Set();
            reset.Value.ShouldBe(false);
            reset.FireCount.ShouldBe(0);

            reset.Set();
            reset.Value.ShouldBe(true);
            reset.FireCount.ShouldBe(1);

            await Task.Delay(1000);

            reset.Value.ShouldBe(false);
            reset.FireCount.ShouldBe(1);
        }
    }
}
