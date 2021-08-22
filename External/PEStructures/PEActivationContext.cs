using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace System.PEStructures
{
    public class PEActivationContext
    {
        private readonly ILookup<int, PEManifestDirectory> DirectoryCache;
        private readonly XDocument ManifestData;
        private readonly ProcessEx TargetProcess;

        internal PEActivationContext(XDocument manifest, ProcessEx process)
        {
            ManifestData = manifest;
            TargetProcess = process;
            DirectoryCache = GetManifestDirectories().ToLookup(directory => directory.Hash);
        }

        internal string ProbeManifest(string fileName)
        {
            if (ManifestData?.Root is null)
            {
                return null;
            }

            var defaultNamespace = ManifestData.Root.GetDefaultNamespace();
            foreach (var dependency in ManifestData.Descendants(defaultNamespace + "dependency").Elements(defaultNamespace + "dependentAssembly").Elements(defaultNamespace + "assemblyIdentity"))
            {
                // Parse the attributes of the dependency

                var architecture = dependency.Attribute("processorArchitecture")?.Value;
                var language = dependency.Attribute("language")?.Value;
                var name = dependency.Attribute("name")?.Value;
                var token = dependency.Attribute("publicKeyToken")?.Value;
                var version = dependency.Attribute("version")?.Value;

                if (architecture is null || language is null || name is null || token is null || version is null)
                {
                    continue;
                }

                if (architecture == "*")
                {
                    architecture = TargetProcess.GetArchitecture() == Architecture.X86 ? "x86" : "amd64";
                }

                if (language == "*")
                {
                    language = "none";
                }

                // Create a hash for the dependency using the architecture, name and token
                var dependencyHash = string.Join(string.Empty, architecture, name.ToLower(), token).GetHashCode();

                // Query the cache for a matching list of directories
                if (!DirectoryCache.Contains(dependencyHash))
                {
                    continue;
                }

                var matchingDirectories = DirectoryCache[dependencyHash].Where(directory => directory.Language.Equals(language, StringComparison.OrdinalIgnoreCase));

                // Look for the directory that holds the dependency
                var dependencyVersion = new Version(version);

                PEManifestDirectory matchingDirectory;

                if (dependencyVersion.Build == 0 && dependencyVersion.Revision == 0)
                {
                    matchingDirectory = matchingDirectories.Where(directory => directory.Version.Major == dependencyVersion.Major && directory.Version.Minor == dependencyVersion.Minor).OrderByDescending(directory => directory.Version).FirstOrDefault();
                }
                else
                {
                    matchingDirectory = matchingDirectories.FirstOrDefault(directory => directory.Version == dependencyVersion);
                }

                if (matchingDirectory is null)
                {
                    continue;
                }

                var sxsFilePath = Path.Combine(matchingDirectory.Path, fileName);
                if (File.Exists(sxsFilePath))
                {
                    return sxsFilePath;
                }
            }
            return null;
        }

        private IEnumerable<PEManifestDirectory> GetManifestDirectories()
        {
            var architecture = TargetProcess.GetArchitecture() == Architecture.X86 ? "x86" : "amd64";
            var sxsDirectory = new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "WinSxS"));

            foreach (var directory in sxsDirectory.EnumerateDirectories().Where(directory => directory.Name.StartsWith(architecture)))
            {
                var nameComponents = directory.Name.Split(new string[] { "_" }, StringSplitOptions.RemoveEmptyEntries);
                var language = nameComponents[nameComponents.Length - 2];
                var version = new Version(nameComponents[nameComponents.Length - 3]);

                // Create a hash for the directory name, skipping the version, language and hash
                var nameHash = string.Join(string.Empty, nameComponents.Take(nameComponents.Length - 3)).GetHashCode();
                yield return new PEManifestDirectory(nameHash, language, directory.FullName, version);
            }
        }
    }

    public sealed class PEManifestDirectory
    {
        public readonly int Hash;
        public readonly string Language;
        public readonly string Path;
        public readonly Version Version;
        public PEManifestDirectory(int hash, string language, string path, Version version)
        {
            Version = version;
            Hash = hash;
            Language = language;
            Path = path;
        }
    }
}
