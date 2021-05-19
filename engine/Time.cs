using System;
using WorldSim.API;

namespace WorldSim.Model
{
    public class Time : ITime
    {
        private DateTime _current;
        private int _iteration;
        private DateTime _start;
        private readonly World _world;

        public Time(World world)
        {
            StepUnit = TimeStep.year;
            StepValue = 1;
            _start = new DateTime(1800, 1, 1);
            End = new DateTime(2101, 1, 1);
            _world = world;
            _current = Start;
            _iteration = 0;
        }

        public TimeStep StepUnit { get; set; }
        public int StepValue { get; set; }

        public DateTime Start
        {
            get => _start;
            set
            {
                _start = value;
                _current = _start;
                _iteration = 0;
            }
        }

        public DateTime End { get; set; }

        public DateTime Current
        {
            get => _current;
            set => RunTo(value);
        }

        public int Iteration
        {
            get => _iteration;
            set => IterateTo(value);
        }

        public int LastIteration()
        {
            switch (StepUnit)
            {
                case TimeStep.year:
                    if ((End.Year - Start.Year) % StepValue == 0)
                        return (End.Year - Start.Year) / StepValue;
                    else
                        return 1 + (End.Year - Start.Year) / StepValue;
                case TimeStep.month:
                    var months = (End.Year - Start.Year) * 12;
                    months += End.Month - Start.Month;
                    if (months % StepValue == 0)
                        return months / StepValue;
                    else
                        return 1 + months / StepValue;
                case TimeStep.day:
                    var days = (End - Start).Days; // It's magic
                    if (days % StepValue == 0)
                        return days / StepValue;
                    else
                        return 1 + days / StepValue;
            }

            return 0;
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
                        return 365.0f / StepValue;
                    else
                        return 365.0f / StepValue;
            }

            return 1.0f;
        }

        public void Restart()
        {
            _current = Start;
            _iteration = 0;
            _world?.Restart();
        }

        public void Step()
        {
            switch (StepUnit)
            {
                case TimeStep.month:
                    _current = _current.AddMonths(StepValue);
                    break;
                case TimeStep.day:
                    _current = _current.AddDays(StepValue);
                    break;
                default:
                    _current = _current.AddYears(StepValue);
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
                switch (StepUnit)
                {
                    case TimeStep.month:
                        newCurrent = _current.AddMonths(-StepValue);
                        break;
                    case TimeStep.day:
                        newCurrent = _current.AddDays(-StepValue);
                        break;
                    default:
                        newCurrent = _current.AddYears(-StepValue);
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
                // We need to go back to square one before fast-forwarding
                Restart();

            // Now just fast forward
            while (targetDate > _current) Step();
        }

        public void IterateTo(int iteration)
        {
            iteration = Math.Max(0, iteration);
            if (iteration < _iteration)
                // We need to go back to square one before fast-forwarding
                Restart();

            // Now just fast forward
            while (iteration > _iteration) Step();
        }
    }
}
