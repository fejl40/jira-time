using JiraTime.Storage.FileSystem;

namespace JiraTime.Storage;

public static class StorageServiceCollectionExtensions
{
    /// <summary>Registers the local file-based document store. Replace this call to use a real document database.</summary>
    public static IServiceCollection AddFileDocumentStore(this IServiceCollection services, string rootPath)
    {
        services.AddSingleton(new FileDocumentStoreOptions { RootPath = rootPath });
        services.AddSingleton<IDocumentStore, FileDocumentStore>();
        return services;
    }
}
