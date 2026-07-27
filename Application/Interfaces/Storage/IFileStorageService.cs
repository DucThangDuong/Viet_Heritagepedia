using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Interfaces.Storage;

public interface IFileStorageService
{
    Task<bool> ValidateMagicBytesAsync(Stream fileStream, string fileExtension, CancellationToken ct = default);
    Task<string> SaveFileAsync(Stream fileStream, string targetFileName, CancellationToken ct = default);
}
