using System;
using WorldSim.API;

namespace WorldSim.Model
{
    public class Time : ITime
    {
        private World _world;
        private DateTime _current;
        private Int32 _iteration;

        public Time(World world)
        {
            this.StepUnit = TimeStep.year;
            this.StepValue = 1;
            this.Start = new DateTime(1800, 1, 1);
            this.End = new DateTime(2101, 1, 1);
            _world = world;
            _current = this.Start;
            _iteration = 0;
        }

        public TimeStep StepUnit { get; set; }
        public Int32 StepValue { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }

        public DateTime Current
        {
            get => _current;
            set { RunTo(value); }
        }

        public Int32 Iteration
        {
            get => _iteration;
            set { IterateTo(value); }
        }

        public float GetAnnualDivider()
        {
            switch (StepUnit)
            {
                case TimeStep.year:
                    return 1.0f / StepValue;
                case TimeStep.month:
                    return 12.0f / StepValue;
                case TimeStep.day:
                    if (DateTime.IsLeapYear(Current.Year))
                    {
                        return 365.0f / StepValue;
                    }
                    else
                    {
                        return 365.0f / StepValue;
                    }
            }

            return 1.0f;
        }

        public void Restart()
        {
            _current = this.Start;
            _iteration = 0;
            _world?.Restart();
        }

        public void Step()
        {
            switch (this.StepUnit)
            {
                case TimeStep.month:
                    _current = _current.AddMonths(this.StepValue);
                    break;
                case TimeStep.day:
                    _current = _current.AddDays(this.StepValue);
                    break;
                default:
                    _current = _current.AddYears(this.StepValue);
                    break;
            }

            _iteration++;
            _world?.Step(this);
        }

        public void StepBack()
        {
            if (_iteration > 0)
            {
                DateTime newCurrent;
                switch (this.StepUnit)
                {
                    case TimeStep.month:
                        newCurrent = _current.AddMonths(-this.StepValue);
                        break;
                    case TimeStep.day:
                        newCurrent = _current.AddDays(-this.StepValue);
                        break;
                    default:
                        newCurrent = _current.AddYears(-this.StepValue);
                        break;
                }

                RunTo(newCurrent);
            }
        }

        public bool Reached(DateTime to)
        {
            return Current >= to;
        }

        public bool ReachedIteration(int to)
        {
            return Iteration >= to;
        }

        public bool Done()
        {
            return Reached(End);
        }

        public void RunTo(DateTime targetDate)
        {
            if (targetDate < _current)
            {
                // We need to go back to square one before fast-forwarding
                Restart();
            }

            // Now just fast forward
            while (targetDate > _current)
            {
                Step();
            }
        }

        public void IterateTo(Int32 iteration)
        {
            iteration = Math.Max(0, iteration);
            if (iteration < _iteration)
            {
                // We need to go back to square one before fast-forwarding
                Restart();
            }

            // Now just fast forward
            while (iteration > _iteration)
            {
                Step();
            }
        }
    }
}
