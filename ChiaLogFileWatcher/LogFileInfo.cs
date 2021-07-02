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

        private Func<string, bool> stringMatcherFunc;
        private Action<string> responseAction;
        private DateTime lastWriteTime;
        private bool isDisposed;

        public LogFileInfo(string filePath) : this(new FileInfo(filePath))
        {
        }        

        public LogFileInfo(FileInfo fileInfo)
        {
            this.fileInfo = fileInfo;
            
            this.lastWriteTime = this.fileInfo.LastWriteTime;
            this.LineCount = this.GetLineCount();

            this.timer = new Timer(TimeSpan.FromSeconds(2).TotalMilliseconds);
            this.timer.Elapsed += this.OnTimerFired;
            this.timer.AutoReset = true;
            this.timer.Enabled = true;
        }

        public event Action<FileInfo, IEnumerable<string>> LinesAdded;

        public int LineCount { get; private set; }

        public void WatchFor(Func<string, bool> stringMatcher, Action<string> responseAction)
        {
            this.stringMatcherFunc = stringMatcher;
            this.responseAction = responseAction;
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
