using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SoulKnight3D.Editor
{
    public sealed class ToyWebGLPostprocessor : IPostprocessBuildWithReport
    {
        public int callbackOrder => int.MaxValue;

        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.WebGL)
            {
                return;
            }

            string streamingAssetsPath = Path.GetFullPath(
                Path.Combine(report.summary.outputPath, "StreamingAssets"));
            if (!Directory.Exists(streamingAssetsPath))
            {
                return;
            }

            RenameAddressableBundles(streamingAssetsPath);
            RenameResKitBundles(streamingAssetsPath);
            RemoveBuildOnlyFiles(streamingAssetsPath);
            Debug.Log("Prepared WebGL StreamingAssets for hosts with extension allowlists such as Bilibili Toy.");
        }

        private static void RenameAddressableBundles(string streamingAssetsPath)
        {
            string addressablesPath = Path.Combine(streamingAssetsPath, "aa");
            if (!Directory.Exists(addressablesPath))
            {
                return;
            }

            foreach (string path in Directory.GetFiles(addressablesPath, "*.bundle", SearchOption.AllDirectories))
            {
                RenameWithUnityWebExtension(path);
            }
        }

        private static void RenameResKitBundles(string streamingAssetsPath)
        {
            string resKitPath = Path.Combine(streamingAssetsPath, "AssetBundles", "WebGL");
            if (!Directory.Exists(resKitPath))
            {
                return;
            }

            foreach (string path in Directory.GetFiles(resKitPath, "*", SearchOption.TopDirectoryOnly))
            {
                string extension = Path.GetExtension(path);
                if (string.IsNullOrEmpty(extension) ||
                    extension.Equals(".bin", StringComparison.OrdinalIgnoreCase))
                {
                    RenameWithUnityWebExtension(path);
                }
            }
        }

        private static void RemoveBuildOnlyFiles(string streamingAssetsPath)
        {
            foreach (string path in Directory.GetFiles(streamingAssetsPath, "*.manifest", SearchOption.AllDirectories))
            {
                File.Delete(path);
            }

            string addressablesLinkPath = Path.GetFullPath(
                Path.Combine(streamingAssetsPath, "aa", "AddressablesLink"));
            if (Directory.Exists(addressablesLinkPath) &&
                IsChildPath(streamingAssetsPath, addressablesLinkPath))
            {
                Directory.Delete(addressablesLinkPath, true);
            }
        }

        private static void RenameWithUnityWebExtension(string sourcePath)
        {
            string destinationPath = sourcePath + ".unityweb";
            if (File.Exists(destinationPath))
            {
                File.Delete(destinationPath);
            }

            File.Move(sourcePath, destinationPath);
        }

        private static bool IsChildPath(string parentPath, string candidatePath)
        {
            string normalizedParent = Path.GetFullPath(parentPath)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string normalizedCandidate = Path.GetFullPath(candidatePath);
            return normalizedCandidate.StartsWith(normalizedParent, StringComparison.OrdinalIgnoreCase);
        }
    }
}
