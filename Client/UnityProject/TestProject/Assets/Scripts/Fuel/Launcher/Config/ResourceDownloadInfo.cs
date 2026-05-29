namespace Fuel.Launcher.Config
{
    public readonly struct ResourceDownloadInfo
    {
        public readonly int TotalCount;
        public readonly long TotalBytes;

        public ResourceDownloadInfo(int totalCount, long totalBytes)
        {
            TotalCount = totalCount;
            TotalBytes = totalBytes;
        }
    }
}
