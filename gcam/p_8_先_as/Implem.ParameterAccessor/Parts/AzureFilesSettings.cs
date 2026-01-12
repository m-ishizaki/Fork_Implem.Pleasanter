namespace Implem.ParameterAccessor.Parts
{
    public class AzureFilesSettings
    {
        public string ConnectionString;
        public string FileShareName;
        public bool EnableAzureFiles;
        public int MaxRetries;

        public AzureFilesSettings()
        {
            MaxRetries = 3;
            EnableAzureFiles = false;
            FileShareName = "pleasanter-files";
        }

        public bool IsEnabled()
        {
            return EnableAzureFiles && !string.IsNullOrEmpty(ConnectionString);
        }
    }
}
