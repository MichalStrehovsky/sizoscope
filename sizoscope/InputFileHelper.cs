using System.IO.Compression;

#nullable enable

namespace sizoscope
{
    /// <summary>
    /// Resolves an input file path that may be a .zip archive containing .mstat and .dgml files.
    /// For .zip files, the archive is kept open and streams are read directly from it.
    /// Implements IDisposable to close the archive when done.
    /// </summary>
    internal sealed class ResolvedFile : IDisposable
    {
        /// <summary>The original file path provided by the user.</summary>
        public string OriginalPath { get; }

        /// <summary>The length of the .mstat data in bytes.</summary>
        public long MstatLength { get; }

        private readonly ZipArchive? _archive;
        private readonly ZipArchiveEntry? _mstatEntry;
        private readonly ZipArchiveEntry? _dgmlEntry;
        private readonly string? _filePath; // non-null for plain files

        private ResolvedFile(string originalPath, string filePath)
        {
            OriginalPath = originalPath;
            _filePath = filePath;
            MstatLength = new FileInfo(filePath).Length;
        }

        private ResolvedFile(string originalPath, ZipArchive archive, ZipArchiveEntry mstatEntry, ZipArchiveEntry? dgmlEntry)
        {
            OriginalPath = originalPath;
            _archive = archive;
            _mstatEntry = mstatEntry;
            _dgmlEntry = dgmlEntry;
            MstatLength = mstatEntry.Length;
        }

        /// <summary>
        /// Opens an input file. If it's a .zip archive, opens the archive and locates
        /// the .mstat and companion .scan.dgml.xml entries. Otherwise, treats the file as-is.
        /// </summary>
        public static ResolvedFile Open(string path)
        {
            if (string.Equals(Path.GetExtension(path), ".zip", StringComparison.OrdinalIgnoreCase))
            {
                return OpenZip(path);
            }

            return new ResolvedFile(path, path);
        }

        private static ResolvedFile OpenZip(string zipPath)
        {
            var archive = ZipFile.OpenRead(zipPath);
            try
            {
                // Find the first .mstat entry
                ZipArchiveEntry? mstatEntry = null;
                foreach (var entry in archive.Entries)
                {
                    if (entry.Name.EndsWith(".mstat", StringComparison.OrdinalIgnoreCase) && entry.Length > 0)
                    {
                        mstatEntry = entry;
                        break;
                    }
                }

                if (mstatEntry == null)
                {
                    throw new InvalidOperationException($"No .mstat file found in archive: {zipPath}");
                }

                // Look for a companion .scan.dgml.xml file
                ZipArchiveEntry? dgmlEntry = null;
                string dgmlName = Path.ChangeExtension(mstatEntry.Name, "scan.dgml.xml");
                foreach (var entry in archive.Entries)
                {
                    if (string.Equals(entry.Name, dgmlName, StringComparison.OrdinalIgnoreCase) && entry.Length > 0)
                    {
                        dgmlEntry = entry;
                        break;
                    }
                }

                return new ResolvedFile(zipPath, archive, mstatEntry, dgmlEntry);
            }
            catch
            {
                archive.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Opens a stream to read the .mstat data.
        /// </summary>
        public Stream OpenMstat()
        {
            if (_archive != null)
                return _mstatEntry!.Open();

            return File.OpenRead(_filePath!);
        }

        /// <summary>
        /// Opens a stream to read the .scan.dgml.xml data, or returns null if not available.
        /// </summary>
        public Stream? OpenDgml()
        {
            if (_archive != null)
                return _dgmlEntry?.Open();

            string dgmlPath = Path.ChangeExtension(_filePath!, "scan.dgml.xml");
            if (File.Exists(dgmlPath))
                return File.Open(dgmlPath, FileMode.Open, FileAccess.Read, FileShare.Read);

            return null;
        }

        /// <summary>Whether DGML data is available.</summary>
        public bool HasDgml
        {
            get
            {
                if (_archive != null)
                    return _dgmlEntry != null;

                return File.Exists(Path.ChangeExtension(_filePath!, "scan.dgml.xml"));
            }
        }

        public void Dispose()
        {
            _archive?.Dispose();
        }
    }
}
