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
        public static bool TryLoadIgnoreFile(string ignoreFilePath, out List<string> ignoredExtensions, out List<string> ignoredPaths)
        {
            string[] ignoreFileLines = null;

            ignoredExtensions = new List<string>();
            ignoredPaths = new List<string>();

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
                else if (line.StartsWith("."))
                {
                    if (!line.Contains("/") && !line.Contains("\\"))
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
                            // This try-catch is here to catch general IO failures and preventing the tool from crashing.
                            // These can be things like access denied, hard drive dead, etc.
                            // If files don't exist, they wont get copied either by the uploader.
                        }
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

        private static void IgnoreDirectoryRecursively(string basePath, string directory, List<string> ignoredPaths)
        {
            foreach (string file in Directory.EnumerateFileSystemEntries(directory.Replace("/", "\\"), "*.*", SearchOption.AllDirectories))
            {
                if (File.Exists(file))
                {
                    ignoredPaths.Add(file.Remove(0, basePath.Length + 1));
                }
                else
                {
                    ignoredPaths.Add(file.Remove(0, basePath.Length + 1) + "\\");
                }
            }
        }
    }
}
