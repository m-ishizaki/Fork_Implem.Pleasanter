namespace Implem.ParameterAccessor.Parts
{
    public class BinaryStorage
    {
        public string Provider;
        public string Path;
        public bool Attachments;
        public bool Images;
        public decimal LimitQuantity;
        public decimal LimitSize;
        public decimal LimitTotalSize;
        public decimal MinQuantity;
        public decimal MaxQuantity;
        public decimal MinSize;
        public decimal MaxSize;
        public decimal TotalMinSize;
        public decimal TotalMaxSize;
        public decimal? ThumbnailLimitSize;
        public decimal? ImageLimitSize;
        public bool UseStorageSelect;
        public string DefaultBinaryStorageProvider;
        public string TemporaryBinaryStorageProvider;
        public decimal LocalFolderMinSize;
        public decimal LocalFolderMaxSize;
        public decimal LocalFolderTotalMinSize;
        public decimal LocalFolderTotalMaxSize;
        public decimal ThumbnailMinSize;
        public decimal ThumbnailMaxSize;
        public decimal LocalFolderLimitSize;
        public decimal LocalFolderLimitTotalSize;
        public bool RestoreLocalFiles;

        public bool IsLocal()
        {
            return Provider == "Local";
        }

        public bool IsAzureFiles()
        {
            return Provider == "AzureFiles";
        }

        public string GetSiteImageProvider()
        {
            return Provider;
        }
    }
}