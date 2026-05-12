using Castle.Core.Models;

namespace Castle.Core.Interfaces;

public interface ILibraryScanner
{
    Task<List<Song>> ScanFolderAsync(string folderPath, IProgress<int>? progress = null);
}