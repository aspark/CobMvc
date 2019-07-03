using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CobMvc.Core.Common
{
    /// <summary>
    /// 超时可自动恢复的信号
    /// </summary>
    public class AutoResetSignal
    {
        uint _milliseconds = 0;
        long _resetIntervalTicks = 0;
        public AutoResetSignal(uint milliseconds)
        {
            _milliseconds = milliseconds;

            Reset();
        }

        private volatile int _signal = 0;

        public bool Value { get => _signal == 1; }

        public int FireCount { get; private set; }

        public TimeSpan ResetMaxInterval { get; set; } = TimeSpan.FromMinutes(5);//最长断开5分钟

        public virtual void Set()
        {
            if (Interlocked.CompareExchange(ref _signal, 1, 0) == 0)
            {
                FireCount++;
                //设置恢复时间
                Task.Delay(TimeSpan.FromTicks(_resetIntervalTicks)).ContinueWith(t => {
                    Reset(false);
                });
                _resetIntervalTicks = Math.Min(ResetMaxInterval.Ticks, _resetIntervalTicks * 2);
            }
        }

        public virtual void Reset()
        {
            Reset(true);
        }

        protected virtual void Reset(bool updateInterval)
        {
            _signal = 0;
            if (updateInterval)
                _resetIntervalTicks = _milliseconds * TimeSpan.TicksPerMillisecond;//时间间隔
        }
    }

    /// <summary>
    /// 超过阈值时才标记信号
    /// </summary>
    public sealed class ThresholdAutoResetSignal : AutoResetSignal
    {
        private uint _threshold = 0;
        public ThresholdAutoResetSignal(uint threshold, uint milliseconds) : base(milliseconds)
        {
            _threshold = threshold;
        }

        private volatile int _count = 0;
        public override void Set()
        {
            if (Interlocked.Increment(ref _count) >= _threshold)
            {
                base.Set();
            }
        }

        public bool IsExceeded { get => _count >= _threshold; }

        protected override void Reset(bool updateInterval)
        {
            _count = 0;
            base.Reset(updateInterval);
        }
    }
}
