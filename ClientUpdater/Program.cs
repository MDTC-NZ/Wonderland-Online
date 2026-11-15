using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;
using System.Diagnostics;

namespace WloClientUpdater
{
    [DataContract]
    sealed class UpdateManifest
    {
        [DataMember(Name = "version", IsRequired = true)]
        public string Version { get; set; }

        [DataMember(Name = "files", IsRequired = true)]
        public List<UpdateFile> Files { get; set; }
    }

    [DataContract]
    sealed class UpdateFile
    {
        [DataMember(Name = "path", IsRequired = true)]
        public string Path { get; set; }

        [DataMember(Name = "size", IsRequired = true)]
        public long Size { get; set; }

        [DataMember(Name = "sha256", IsRequired = true)]
        public string Sha256 { get; set; }
    }

    static class Program
    {
        const string DefaultManifestUrl = "https://wlo.kiaoragaming.xyz:8080/client-updates/manifest.json";
        const string DefaultInstallDirectory = @"C:\Program Files (x86)\Wonderland Online";
        const long MaximumManifestLength = 1048576;
        const long MaximumFileLength = 536870912;

        static int Main(string[] args)
        {
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                string manifestUrl;
                string installDirectory;
                bool checkOnly;
                if (!TryParseArguments(args, out manifestUrl, out installDirectory, out checkOnly))
                    return 2;

                Uri manifestUri = new Uri(manifestUrl, UriKind.Absolute);
                if (manifestUri.Scheme != Uri.UriSchemeHttps && !IsLoopback(manifestUri))
                    throw new InvalidOperationException("Updates must use HTTPS. Plain HTTP is allowed only for localhost testing.");

                string root = Path.GetFullPath(installDirectory);
                string gameExecutable = Path.Combine(root, "Main.exe");
                if (!File.Exists(gameExecutable))
                    throw new FileNotFoundException("The game client Main.exe was not found in the install directory.", gameExecutable);

                UpdateManifest manifest = DownloadManifest(manifestUri);
                ValidateManifest(manifest);

                List<UpdateFile> pending = GetChangedFiles(manifest, root);
                Console.WriteLine("Update version {0}: {1} file(s) to update.", manifest.Version, pending.Count);

                if (checkOnly)
                {
                    VerifyDownloads(manifestUri, pending, root);
                    Console.WriteLine("Update manifest and file hashes are valid; no files were changed.");
                    return 0;
                }

                if (pending.Count > 0)
                    EnsureClientIsClosed();

                foreach (UpdateFile file in pending)
                    ApplyUpdateFile(manifestUri, file, root);

                Console.WriteLine("Client files are up to date.");
                Process.Start(new ProcessStartInfo
                {
                    FileName = gameExecutable,
                    WorkingDirectory = root,
                    UseShellExecute = true
                });
                return 0;
            }
            catch (WebException ex)
            {
                Console.Error.WriteLine("Update download failed: " + ex.Message);
                return 1;
            }
            catch (IOException ex)
            {
                Console.Error.WriteLine("Client update failed: " + ex.Message);
                return 1;
            }
            catch (UnauthorizedAccessException)
            {
                Console.Error.WriteLine("Access denied. Close the game and run the updater with permission to modify its install folder.");
                return 1;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Client update failed: " + ex.Message);
                return 1;
            }
        }

        static bool TryParseArguments(string[] args, out string manifestUrl, out string installDirectory, out bool checkOnly)
        {
            manifestUrl = DefaultManifestUrl;
            installDirectory = DefaultInstallDirectory;
            checkOnly = false;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--manifest-url" && i + 1 < args.Length)
                    manifestUrl = args[++i];
                else if (args[i] == "--install-dir" && i + 1 < args.Length)
                    installDirectory = args[++i];
                else if (args[i] == "--check-only")
                    checkOnly = true;
                else
                {
                    Console.Error.WriteLine("Usage: WloClientUpdater.exe [--manifest-url URL] [--install-dir PATH] [--check-only]");
                    return false;
                }
            }
            return true;
        }

        static UpdateManifest DownloadManifest(Uri manifestUri)
        {
            byte[] data = DownloadBytes(manifestUri, MaximumManifestLength);
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(UpdateManifest));
            using (MemoryStream input = new MemoryStream(data))
                return (UpdateManifest)serializer.ReadObject(input);
        }

        static void ValidateManifest(UpdateManifest manifest)
        {
            if (manifest == null || string.IsNullOrWhiteSpace(manifest.Version) ||
                manifest.Version.Length > 64 || manifest.Files == null)
                throw new InvalidDataException("The update manifest is incomplete.");

            HashSet<string> paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (manifest.Files.Count > 4096)
                throw new InvalidDataException("The update manifest contains too many files.");

            foreach (UpdateFile file in manifest.Files)
            {
                if (file == null || !IsSafeRelativePath(file.Path) ||
                    !paths.Add(file.Path) ||
                    file.Size < 0 || file.Size > MaximumFileLength ||
                    !IsSha256(file.Sha256))
                    throw new InvalidDataException("The update manifest contains an invalid or duplicate file entry.");
            }
        }

        static List<UpdateFile> GetChangedFiles(UpdateManifest manifest, string root)
        {
            List<UpdateFile> pending = new List<UpdateFile>();
            foreach (UpdateFile file in manifest.Files)
            {
                string targetPath = GetTargetPath(root, file.Path);
                EnsureNoReparsePoints(root, Path.GetDirectoryName(targetPath), targetPath);
                if (!File.Exists(targetPath) || !HasExpectedHash(targetPath, file.Sha256))
                    pending.Add(file);
            }
            return pending;
        }

        static void VerifyDownloads(Uri manifestUri, List<UpdateFile> files, string root)
        {
            foreach (UpdateFile file in files)
            {
                string targetPath = GetTargetPath(root, file.Path);
                DownloadAndVerify(manifestUri, file, targetPath, true);
            }
        }

        static void ApplyUpdateFile(Uri manifestUri, UpdateFile file, string root)
        {
            string targetPath = GetTargetPath(root, file.Path);
            string directory = Path.GetDirectoryName(targetPath);
            EnsureNoReparsePoints(root, directory, targetPath);
            Directory.CreateDirectory(directory);
            EnsureNoReparsePoints(root, directory, targetPath);

            string temporaryPath = Path.Combine(directory, ".wlo-update-" + Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                DownloadAndVerify(manifestUri, file, temporaryPath, false);
                if (File.Exists(targetPath))
                    File.Replace(temporaryPath, targetPath, null);
                else
                    File.Move(temporaryPath, targetPath);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }
        }

        static void DownloadAndVerify(Uri manifestUri, UpdateFile file, string outputPath, bool checkOnly)
        {
            Uri fileUri = new Uri(manifestUri, "files/" + EscapePath(file.Path));
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(fileUri);
            request.Method = "GET";
            request.Timeout = 15000;
            request.ReadWriteTimeout = 30000;
            request.AllowAutoRedirect = false;

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                if (response.StatusCode != HttpStatusCode.OK)
                    throw new WebException("The update server returned HTTP " + (int)response.StatusCode + ".");

                Stream destination = checkOnly ? Stream.Null : new FileStream(outputPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                using (destination)
                using (SHA256 sha = SHA256.Create())
                using (CryptoStream hashStream = new CryptoStream(destination, sha, CryptoStreamMode.Write))
                using (Stream source = response.GetResponseStream())
                {
                    byte[] buffer = new byte[65536];
                    long total = 0;
                    int count;
                    while ((count = source.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        total += count;
                        if (total > file.Size || total > MaximumFileLength)
                            throw new InvalidDataException("Downloaded file exceeds the size declared in the manifest: " + file.Path);
                        hashStream.Write(buffer, 0, count);
                    }
                    hashStream.FlushFinalBlock();
                    if (total != file.Size)
                        throw new InvalidDataException("Downloaded file size does not match the manifest: " + file.Path);
                    string actualHash = ToHex(sha.Hash);
                    if (!string.Equals(actualHash, file.Sha256, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException("Downloaded file hash does not match the manifest: " + file.Path);
                }
            }
        }

        static byte[] DownloadBytes(Uri uri, long maximumLength)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.Method = "GET";
            request.Timeout = 15000;
            request.ReadWriteTimeout = 30000;
            request.AllowAutoRedirect = false;

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                if (response.StatusCode != HttpStatusCode.OK)
                    throw new WebException("The update server returned HTTP " + (int)response.StatusCode + ".");
                using (Stream source = response.GetResponseStream())
                using (MemoryStream destination = new MemoryStream())
                {
                    byte[] buffer = new byte[8192];
                    int count;
                    while ((count = source.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        if (destination.Length + count > maximumLength)
                            throw new InvalidDataException("The update manifest is too large.");
                        destination.Write(buffer, 0, count);
                    }
                    return destination.ToArray();
                }
            }
        }

        static string GetTargetPath(string root, string relativePath)
        {
            if (!IsSafeRelativePath(relativePath))
                throw new InvalidDataException("Unsafe update path: " + relativePath);

            string rootPrefix = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
                Path.DirectorySeparatorChar;
            string targetPath = Path.GetFullPath(Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar)));
            if (!targetPath.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Update path escapes the game install directory.");
            return targetPath;
        }

        static bool IsSafeRelativePath(string path)
        {
            if (string.IsNullOrEmpty(path) || path.Length > 240 ||
                path.StartsWith("/", StringComparison.Ordinal) ||
                path.IndexOf('\\') >= 0 || path.IndexOf(':') >= 0)
                return false;

            string[] segments = path.Split('/');
            foreach (string segment in segments)
            {
                if (segment.Length == 0 || segment == "." || segment == ".." ||
                    segment.EndsWith(".", StringComparison.Ordinal) ||
                    segment.EndsWith(" ", StringComparison.Ordinal) ||
                    IsReservedWindowsFileName(segment))
                    return false;
                foreach (char character in segment)
                    if (!char.IsLetterOrDigit(character) && character != ' ' &&
                        character != '.' && character != '_' && character != '-')
                        return false;
            }
            return true;
        }

        static void EnsureNoReparsePoints(string root, string directory, string targetPath)
        {
            string rootPath = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            DirectoryInfo current = new DirectoryInfo(directory);
            while (current != null && current.FullName.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
            {
                if (Directory.Exists(current.FullName) &&
                    (File.GetAttributes(current.FullName) & FileAttributes.ReparsePoint) != 0)
                    throw new IOException("Update path contains a symbolic link or reparse point: " + current.FullName);
                if (string.Equals(current.FullName.TrimEnd(Path.DirectorySeparatorChar), rootPath, StringComparison.OrdinalIgnoreCase))
                    break;
                current = current.Parent;
            }

            if (File.Exists(targetPath) && (File.GetAttributes(targetPath) & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Refusing to overwrite a symbolic link: " + targetPath);
        }

        static bool IsReservedWindowsFileName(string name)
        {
            string baseName = name.Split('.')[0].TrimEnd(' ');
            if (string.Equals(baseName, "CON", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(baseName, "PRN", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(baseName, "AUX", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(baseName, "NUL", StringComparison.OrdinalIgnoreCase))
                return true;
            if (baseName.Length == 4 &&
                (baseName.StartsWith("COM", StringComparison.OrdinalIgnoreCase) ||
                 baseName.StartsWith("LPT", StringComparison.OrdinalIgnoreCase)) &&
                baseName[3] >= '1' && baseName[3] <= '9')
                return true;
            return false;
        }

        static bool HasExpectedHash(string path, string expectedHash)
        {
            using (FileStream input = File.OpenRead(path))
            using (SHA256 sha = SHA256.Create())
                return string.Equals(ToHex(sha.ComputeHash(input)), expectedHash, StringComparison.OrdinalIgnoreCase);
        }

        static void EnsureClientIsClosed()
        {
            if (Process.GetProcessesByName("Main").Length > 0 ||
                Process.GetProcessesByName("aLogin").Length > 0)
                throw new InvalidOperationException("Close Main.exe and aLogin.exe before applying client updates.");
        }

        static bool IsLoopback(Uri uri)
        {
            if (string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase))
                return true;
            System.Net.IPAddress address;
            return System.Net.IPAddress.TryParse(uri.Host, out address) && System.Net.IPAddress.IsLoopback(address);
        }

        static bool IsSha256(string value)
        {
            if (value == null || value.Length != 64)
                return false;
            foreach (char character in value)
                if (!((character >= '0' && character <= '9') ||
                      (character >= 'a' && character <= 'f') ||
                      (character >= 'A' && character <= 'F')))
                    return false;
            return true;
        }

        static string EscapePath(string path)
        {
            string[] segments = path.Split('/');
            for (int i = 0; i < segments.Length; i++)
                segments[i] = Uri.EscapeDataString(segments[i]);
            return string.Join("/", segments);
        }

        static string ToHex(byte[] value)
        {
            StringBuilder result = new StringBuilder(value.Length * 2);
            foreach (byte item in value)
                result.Append(item.ToString("x2"));
            return result.ToString();
        }
    }
}
