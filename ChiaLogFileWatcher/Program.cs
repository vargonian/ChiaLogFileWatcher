namespace ChiaLogFileWatcher
{
    using NetCoreAudio;
    using System;
    using System.IO;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using YamlDotNet.Serialization;

    class Program
    {
        static int Main(string[] args)
        {
            if (!TryLoadConfig(out Config config))
            {
                return -1;
            }

            Console.WriteLine("Watching the following log files for proofs:");

            foreach (string logFilePath in config.LogFilePaths)
            {
                Console.WriteLine($"  - {logFilePath}");
            }

            Player soundPlayer = new Player();

            Parallel.ForEach(config.LogFilePaths, logFilePath =>
            {
                using (LogFileInfo logFileInfo = new LogFileInfo(new FileInfo(logFilePath), TimeSpan.FromMinutes(config.CheckIntervalMinutes)))
                {
                    logFileInfo.WatchFor(ContainsFoundProof, (matchingOutputLine) => 
                    { 
                        Console.WriteLine("Matching line: " + matchingOutputLine);
                        soundPlayer.Play(config.ProofFoundAudioFilePath); 
                    });

                    logFileInfo.WatchForMissing(ContainsProofCheck, TimeSpan.FromMinutes(config.ProofsCheckTimeoutMinutes), () =>
                    {
                        Console.WriteLine($"{GetLogTimestamp()} Farmer Stalled (no proof check in {config.ProofsCheckTimeoutMinutes} minutes): {logFileInfo.FilePath}");
                        soundPlayer.Play(config.FarmerStalledAudioFilePath);
                    });

                    while (true)
                    {
                        Thread.Sleep(10);
                    }
                }
            });

            return 0;
        }

        private static string GetLogTimestamp()
        {
            return DateTime.Now.ToString("yyyy’-‘MM’-‘dd’T’HH’:’mm’:’ss.fffffffK");
        }

        private static bool ContainsFoundProof(string outputLine)
        {
            Match match = Regex.Match(outputLine, @"Found [1-9]+ proofs\.");
            return match.Success;
        }

        private static bool ContainsProofCheck(string outputLine)
        {
            Match match = Regex.Match(outputLine, @"Found [0-9]+ proofs\.");
            return match.Success;
        }

        private static bool TryLoadConfig(out Config config)
        {
            config = null;

            const string configFilePath = "config.yaml";

            if (!File.Exists(configFilePath))
            {
                Console.WriteLine($"Could not find {configFilePath}. It must be placed in the same directory as this executable.");
                return false;
            }

            Deserializer deserializer = new Deserializer();
            string yamlText = File.ReadAllText(configFilePath);

            Config loadedConfig = deserializer.Deserialize<Config>(yamlText);

            if (loadedConfig.CheckIntervalMinutes < 0.016 || loadedConfig.CheckIntervalMinutes > 999)
            {
                Console.WriteLine($"ERROR: {nameof(loadedConfig.CheckIntervalMinutes)} must be between 0.016 and 999 (was {loadedConfig.CheckIntervalMinutes}).");
                return false;
            }

            if (string.IsNullOrWhiteSpace(loadedConfig.ProofFoundAudioFilePath))
            {
                Console.WriteLine($"ERROR: No {nameof(loadedConfig.ProofFoundAudioFilePath)} specified in {configFilePath}.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(loadedConfig.FarmerStalledAudioFilePath))
            {
                Console.WriteLine($"ERROR: No {nameof(loadedConfig.FarmerStalledAudioFilePath)} specified in {configFilePath}.");
                return false;
            }

            if (loadedConfig.LogFilePaths == null || loadedConfig.LogFilePaths.Count == 0)
            {
                Console.WriteLine($"ERROR: No log file paths specified in {configFilePath}.");
                return false;
            }

            bool foundMissingLogFileInConfig = false;
            foreach (string logFilePath in loadedConfig.LogFilePaths)
            {
                if (!File.Exists(logFilePath))
                {
                    foundMissingLogFileInConfig = true;
                    Console.WriteLine($"ERROR: Couldn't find log file specified in {configFilePath}: {logFilePath}");
                }
            }

            if (foundMissingLogFileInConfig)
            {
                return false;
            }

            config = loadedConfig;
            return true;
        }
    }
}
