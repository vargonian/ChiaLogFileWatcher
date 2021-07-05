namespace ChiaLogFileWatcher
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Timers;

    public class LogFileInfo : IDisposable
    {
        private readonly FileInfo fileInfo;
        private readonly Timer timer;
        private readonly List<MissingStringTimeout> missingStringTimeouts;

        private Func<string, bool> stringMatcherFunc;
        private Action<string> responseAction;
        private DateTime lastWriteTime;
        private bool isDisposed;

        public LogFileInfo(string filePath, TimeSpan checkInterval) : this(new FileInfo(filePath), checkInterval)
        {
        }        

        public LogFileInfo(FileInfo fileInfo, TimeSpan checkInterval)
        {
            this.fileInfo = fileInfo;
            
            this.lastWriteTime = this.fileInfo.LastWriteTime;
            this.LineCount = this.GetLineCount();

            this.timer = new Timer(checkInterval.TotalMilliseconds);
            this.timer.Elapsed += this.OnTimerFired;
            this.timer.AutoReset = true;
            this.timer.Enabled = true;

            this.missingStringTimeouts = new List<MissingStringTimeout>();
        }

        public event Action<FileInfo, IEnumerable<string>> LinesAdded;

        public int LineCount { get; private set; }

        public string FilePath
        {
            get
            {
                return this.fileInfo.FullName;
            }
        }

        public void WatchFor(Func<string, bool> stringMatcher, Action<string> responseAction)
        {
            this.stringMatcherFunc = stringMatcher;
            this.responseAction = responseAction;
        }

        /// <summary>
        /// Ensures that a string pattern appears within a given duration / frequency.
        /// </summary>
        /// <param name="stringMatcher">A Func from which to match an output line.</param>
        /// <param name="timeoutDuration">The TimeSpan after which the timeoutReachedAction will be triggered.</param>
        /// <param name="timeoutReachedAction">An Action to trigger if the specified pattern matcher isn't satisfied withing timeoutDuration.</param>
        public void WatchForMissing(Func<string, bool> stringMatcher, TimeSpan timeoutDuration, Action timeoutReachedAction)
        {
            this.missingStringTimeouts.Add(new MissingStringTimeout(stringMatcher, timeoutDuration, timeoutReachedAction));
        }

        public void Dispose() => this.Dispose(true);

        protected virtual void Dispose(bool isDisposing)
        {
            if (this.isDisposed)
            {
                return;
            }

            if (isDisposing)
            {
                this.timer?.Dispose();
            }

            this.isDisposed = true;
        }

        private void OnTimerFired(object sender, ElapsedEventArgs e)
        {
            this.fileInfo.Refresh();

            if (this.fileInfo.LastWriteTime > this.lastWriteTime)
            {
                this.lastWriteTime = this.fileInfo.LastWriteTime;

                int previousLineCount = this.LineCount;
                this.LineCount = this.GetLineCount();

                if (this.LineCount < previousLineCount)
                {
                    // The file was probably recreated.
                    previousLineCount = 0;
                }

                IEnumerable<string> newLines = this.ReadLinesFrom(previousLineCount);

                foreach (string line in newLines)
                {
                    if (this.stringMatcherFunc != null)
                    {
                        if (this.stringMatcherFunc.Invoke(line))
                        {
                            this.responseAction?.Invoke(line);
                        }
                    }
                }

                this.LinesAdded?.Invoke(this.fileInfo, newLines);
            }

            foreach (MissingStringTimeout missingStringTimeoutInfo in this.missingStringTimeouts)
            {
                if (missingStringTimeoutInfo.TimeoutOccurred)
                {
                    // Do not handle the same timeout more than once.
                    continue;
                }

                if (missingStringTimeoutInfo.IsTimeoutReached)
                {
                    missingStringTimeoutInfo.TimeoutOccurred = true;
                    missingStringTimeoutInfo.TimeoutReachedAction?.Invoke();
                }
            }
        }

        private IEnumerable<string> ReadLinesFrom(int startLineIndex)
        {
            using (FileStream fileStream = this.fileInfo.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (StreamReader streamReader = new StreamReader(fileStream))
            {
                int lineCount = 0;
                string line;
                while ((line = streamReader.ReadLine()) != null)
                {
                    lineCount++;
                    
                    if ((lineCount - 1) >= startLineIndex)
                    {
                        yield return line;
                    }
                }
            }
        }

        private int GetLineCount()
        {                
            using (FileStream fileStream = this.fileInfo.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (StreamReader streamReader = new StreamReader(fileStream))
            {
                int lineCount = 0;
                while (streamReader.ReadLine() != null)
                {
                    lineCount++;
                }
                
                return lineCount;
            }
        }
    }
}
