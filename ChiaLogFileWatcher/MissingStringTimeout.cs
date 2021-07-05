namespace ChiaLogFileWatcher
{
    using System;

    public class MissingStringTimeout
    {
        public MissingStringTimeout(
            Func<string, bool> stringMatcher, 
            TimeSpan timeoutDuration,
            Action timeoutReachedAction)
        {
            this.StringMatcher = stringMatcher;
            this.TimeoutDuration = timeoutDuration;
            this.TimeoutReachedAction = timeoutReachedAction;
            this.LastOccurrenceTime = DateTime.Now;
        }

        public Func<string, bool> StringMatcher { get; }
        public DateTime LastOccurrenceTime { get; set; }
        public TimeSpan TimeoutDuration { get; }
        public bool TimeoutOccurred { get; set; }
        public Action TimeoutReachedAction { get; }

        public TimeSpan TimeSinceLastOccurrence
        {
            get
            {
                return DateTime.Now.Subtract(this.LastOccurrenceTime);
            }
        }

        public bool IsTimeoutReached
        {
            get
            {
                return this.TimeSinceLastOccurrence >= this.TimeoutDuration;
            }
        }
    }
}
