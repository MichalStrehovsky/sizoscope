using System.IO.Compression;

#nullable enable

namespace sizoscope
{
    /// <summary>
    /// Resolves an input file path that may be a .zip archive containing .mstat and .dgml files.
    /// When the input is a .zip, the relevant files are extracted to a temp directory.
    /// Implements IDisposable to clean up temp files.
    /// </summary>
    internal sealed class ResolvedFile : IDisposable
    {
        /// <summary>
        /// The resolved path to the .mstat file (either the original path or the extracted temp path).
        /// </summary>
        public string MstatPath { get; }

        /// <summary>
        /// The temp directory created for extraction, or null if no extraction was needed.
        /// </summary>
        private readonly string? _tempDir;

        private ResolvedFile(string mstatPath, string? tempDir)
        {
            MstatPath = mstatPath;
            _tempDir = tempDir;
        }

        /// <summary>
        /// Opens an input file. If it's a .zip archive, extracts the first .mstat file
        /// (and any companion .scan.dgml.xml file) to a temp directory.
        /// Otherwise, returns the path as-is.
        /// </summary>
        public static ResolvedFile Open(string path)
        {
            if (string.Equals(Path.GetExtension(path), ".zip", StringComparison.OrdinalIgnoreCase))
            {
                return ExtractFromZip(path);
            }

            return new ResolvedFile(path, null);
        }

        private static ResolvedFile ExtractFromZip(string zipPath)
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "sizoscope_" + Guid.NewGuid().ToString("N")[..8]);
            Directory.CreateDirectory(tempDir);

            try
            {
                using var archive = ZipFile.OpenRead(zipPath);

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

                // Extract the .mstat file
                string mstatDest = Path.Combine(tempDir, mstatEntry.Name);
                mstatEntry.ExtractToFile(mstatDest);

                // Look for a companion .scan.dgml.xml file
                string dgmlName = Path.ChangeExtension(mstatEntry.Name, "scan.dgml.xml");
                foreach (var entry in archive.Entries)
                {
                    if (string.Equals(entry.Name, dgmlName, StringComparison.OrdinalIgnoreCase) && entry.Length > 0)
                    {
                        entry.ExtractToFile(Path.Combine(tempDir, entry.Name));
                        break;
                    }
                }

                return new ResolvedFile(mstatDest, tempDir);
            }
            catch
            {
                // Clean up on failure
                try { Directory.Delete(tempDir, true); } catch { }
                throw;
            }
        }

        public void Dispose()
        {
            if (_tempDir != null)
            {
                try { Directory.Delete(_tempDir, true); } catch { }
            }
        }
    }
}
