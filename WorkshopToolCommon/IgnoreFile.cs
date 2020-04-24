using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Phoenix.WorkshopTool
{
    public class IgnoreFile
    {
        /// <summary>
        /// Tries to open the passed along .wtignore file, and breaks it down into ignored paths and ignored extensions.
        /// </summary>
        /// <param name="ignoreFilePath">Input - Path of the .wtignore file to parse.</param>
        /// <param name="ignoredExtensions">Output - List of extensions to ignore.</param>
        /// <param name="ignoredPaths">Output - List of paths to ignore.</param>
        /// <returns>True if .wtignore file was parsed, false otherwise.</returns>
        public static bool TryLoadIgnoreFile(string ignoreFilePath, out HashSet<string> ignoredExtensions, out HashSet<string> ignoredPaths)
        {
            return TryLoadIgnoreFile(ignoreFilePath, null, out ignoredExtensions, out ignoredPaths);
        }

        /// <summary>
        /// Tries to open the passed along .wtignore file, and breaks it down into ignored paths and ignored extensions.
        /// </summary>
        /// <param name="ignoreFilePath">Input - Path of the .wtignore file to parse.</param>
        /// <param name="relativeApplicablePath">Input - Relative path to apply the ignore file against, if different from <see cref="ignoreFilePath"/>.</param>
        /// <param name="ignoredExtensions">Output - List of extensions to ignore.</param>
        /// <param name="ignoredPaths">Output - List of paths to ignore.</param>
        /// <returns>True if .wtignore file was parsed, false otherwise.</returns>
        public static bool TryLoadIgnoreFile(string ignoreFilePath, string relativeApplicablePath, out HashSet<string> ignoredExtensions, out HashSet<string> ignoredPaths)
        {
            string[] ignoreFileLines = null;

            ignoredExtensions = new HashSet<string>();
            ignoredPaths = new HashSet<string>();

            try
            {
                if (!File.Exists(ignoreFilePath))
                    return false;

                ignoreFileLines = File.ReadAllLines(ignoreFilePath);
            }
            catch
            {
                return false;
            }

            if (ignoreFileLines == null)
                return false;

            ignoredExtensions.Add(".wtignore");

            string modPath = Path.GetDirectoryName(ignoreFilePath);

            if (!string.IsNullOrEmpty(relativeApplicablePath))
                modPath = Path.Combine(modPath, relativeApplicablePath);

            foreach (string ignoreFileLine in ignoreFileLines)
            {
                string line = ignoreFileLine.Trim();
                if (line.Length == 0)
                    continue;

                string linePath = Path.Combine(modPath, line);

                if (line.StartsWith("#"))
                {
                    // Ignore comments
                }
                else if (line.StartsWith(".") || line.StartsWith("*."))
                {
                    if (line.StartsWith("*"))
                    {
                        line = line.TrimStart('*');
                        linePath = Path.Combine(modPath, line);
                    }

                    if (!File.Exists(linePath) && !Directory.Exists(linePath) && !line.Contains("/") && !line.Contains("\\"))
                    {
                        ignoredExtensions.Add(line);
                    }
                    else
                    {
                        try
                        {
                            IgnoreDirectoryRecursively(modPath, linePath, ignoredPaths);
                        }
                        catch
                        {
                            try
                            {
                                if (File.Exists(linePath))
                                {
                                    string caseSensitiveFilename = Directory.GetFiles(Path.GetDirectoryName(linePath), Path.GetFileName(linePath)).FirstOrDefault();
                                    ignoredPaths.Add(caseSensitiveFilename.Remove(0, modPath.Length + 1));
                                }
                            }
                            catch
                            {
                                // This try-catch is here to catch general IO failures and preventing the tool from crashing.
                                // These can be things like access denied, hard drive dead, etc.
                                // If files don't exist, they wont get copied either by the uploader.
                            }
                        }
                    }
                }
                else if(line.StartsWith("*"))
                {
                    try
                    {
                        IgnoreDirectoryRecursively(modPath, modPath, ignoredPaths, line);
                    }
                    catch
                    {
                        // This try-catch is here to catch general IO failures and preventing the tool from crashing.
                        // These can be things like access denied, hard drive dead, etc.
                        // If files don't exist, they wont get copied either by the uploader.
                    }
                }
                else
                {
                    try
                    {
                        IgnoreDirectoryRecursively(modPath, linePath, ignoredPaths);
                    }
                    catch
                    {
                        try
                        {
                            if (File.Exists(linePath))
                            {
                                string caseSensitiveFilename = Directory.GetFiles(Path.GetDirectoryName(linePath), Path.GetFileName(linePath)).FirstOrDefault();
                                ignoredPaths.Add(caseSensitiveFilename.Remove(0, modPath.Length + 1));
                            }
                        }
                        catch
                        {
                            // This try-catch is here to catch general IO failures and preventing the tool from crashing.
                            // These can be things like access denied, hard drive dead, etc.
                            // If files don't exist, they wont get copied either by the uploader.
                        }
                    }
                }
            }

            return true;
        }

        private static void IgnoreDirectoryRecursively(string basePath, string directory, HashSet<string> ignoredPaths, string searchPattern = "*.*")
        {
            if (searchPattern.Any(c => c != '*' && c != '.'))
            {
                searchPattern = searchPattern.Trim('/', '\\', '*');
            }

            foreach (string file in Directory.EnumerateFileSystemEntries(directory.Replace("/", "\\"), searchPattern, SearchOption.AllDirectories))
            {
                if (File.Exists(file))
                {
                    ignoredPaths.Add(file.Remove(0, basePath.Length + 1));
                }
                else
                {
                    ignoredPaths.Add(file.Remove(0, basePath.Length + 1) + "\\");
                    IgnoreDirectoryRecursively(basePath, file, ignoredPaths);
                }
            }
        }
    }
}
