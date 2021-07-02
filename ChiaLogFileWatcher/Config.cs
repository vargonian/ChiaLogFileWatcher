namespace ChiaLogFileWatcher
{
    using System.Collections.Generic;

    public class Config
    {
        public List<string> LogFilePaths { get; set; }
        public string AlertAudioFilePath { get; set; }
    }
}
