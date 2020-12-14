using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaveSwitcher2.Services
{
    class TimeService
    {
        private DateTime _startTime;
        private DateTime _endTime;
        private TimeSpan _duration;

        public void Start()
        {
            _startTime = DateTime.UtcNow;
        }

        public TimeSpan End()
        {
            _endTime = DateTime.UtcNow;
            _duration = _endTime - _startTime;
            return _duration;
        }

        public TimeSpan LastDuration()
        {
            return _duration;
        }
    }
}
