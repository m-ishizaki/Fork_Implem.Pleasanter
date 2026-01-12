using Implem.DefinitionAccessor;
using Implem.Pleasanter.Libraries.Services;

namespace Implem.Pleasanter.Models
{
    /// <summary>
    /// Factory class to create appropriate file storage service based on configuration
    /// </summary>
    public static class FileStorageServiceFactory
    {
        private static IAzureFilesService _azureFilesService;
        private static IFileStorageService _localFileStorageService;
        private static IFileStorageService _azureFilesStorageService;

        /// <summary>
        /// Gets the appropriate file storage service based on BinaryStorage provider configuration
        /// </summary>
        public static IFileStorageService GetService()
        {
            if (Parameters.BinaryStorage.IsAzureFiles())
            {
                if (_azureFilesStorageService == null)
                {
                    if (_azureFilesService == null)
                    {
                        _azureFilesService = new AzureFilesService(
                            Parameters.AzureFilesSettings.ConnectionString,
                            Parameters.AzureFilesSettings.MaxRetries);
                    }
                    _azureFilesStorageService = new AzureFilesStorageService(_azureFilesService);
                }
                return _azureFilesStorageService;
            }
            else
            {
                if (_localFileStorageService == null)
                {
                    _localFileStorageService = new LocalFileStorageService();
                }
                return _localFileStorageService;
            }
        }

        /// <summary>
        /// Resets the cached service instances (useful for testing or configuration changes)
        /// </summary>
        public static void Reset()
        {
            _azureFilesService = null;
            _localFileStorageService = null;
            _azureFilesStorageService = null;
        }
    }
}
