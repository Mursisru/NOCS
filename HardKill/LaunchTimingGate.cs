using NOCS.Config;

namespace NOCS.HardKill
{
    internal enum LaunchTimingState : byte
    {
        Idle,
        WaitMixedSalvo,
        FireIrPending,
    }

    internal sealed class LaunchTimingGate
    {
        private LaunchTimingState _state = LaunchTimingState.Idle;
        private float _waitRemainingSec;

        internal LaunchTimingState State => _state;

        internal void BeginWait(float deltaSec)
        {
            _waitRemainingSec = deltaSec > 0f ? deltaSec : 0f;
            _state = _waitRemainingSec > 0f ? LaunchTimingState.WaitMixedSalvo : LaunchTimingState.FireIrPending;
        }

        internal void MarkIrPending()
        {
            _state = LaunchTimingState.FireIrPending;
        }

        internal void Tick(float dt)
        {
            if (_state != LaunchTimingState.WaitMixedSalvo)
                return;

            float step = dt;
            if (NocsConfigCache.MaxTimingTickDt > 0f)
                step = step > NocsConfigCache.MaxTimingTickDt ? NocsConfigCache.MaxTimingTickDt : step;

            _waitRemainingSec -= step;
            if (_waitRemainingSec <= 0f)
                _state = LaunchTimingState.FireIrPending;
        }

        internal bool IsIrReady()
        {
            return _state == LaunchTimingState.FireIrPending;
        }

        internal void Reset()
        {
            _state = LaunchTimingState.Idle;
            _waitRemainingSec = 0f;
        }
    }
}
