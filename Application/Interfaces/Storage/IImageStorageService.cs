using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Interfaces.Storage;

public interface IImageStorageService
{
    Task<bool> ValidateImageAsync(Stream imageStream, string fileExtension, CancellationToken ct = default);
    Task<string> SaveImageAsync(Stream imageStream, string targetFileName, string contentType, CancellationToken ct = default);
    Task<bool> DeleteImageAsync(string fileUrlOrName, CancellationToken ct = default);
}
