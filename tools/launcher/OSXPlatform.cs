using System.IO;

namespace Launcher
{
    public sealed class OSXPlatform : IRuntimePlatform
    {
        public string GameBinaryDownloadFilename => "NineChronicles-alpha-2-macOS.tar.gz";

        public string OpenCommand => "open";

        public string ExecutableGameBinaryPath(string gameBinaryPath) =>
            Path.Combine(gameBinaryPath, "MacOS", "Nine Chronicles.app", "Contents", "MacOS", "Nine Chronicles");
    }
}
