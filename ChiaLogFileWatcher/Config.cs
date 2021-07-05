namespace ChiaLogFileWatcher
{
    using System.Collections.Generic;

    public class Config
    {
        public List<string> LogFilePaths { get; set; }
        public double CheckIntervalMinutes { get; set; }
        public string ProofFoundAudioFilePath { get; set; }
        public string FarmerStalledAudioFilePath { get; set; }
        public double ProofsCheckTimeoutMinutes { get; set; }
    }
}
