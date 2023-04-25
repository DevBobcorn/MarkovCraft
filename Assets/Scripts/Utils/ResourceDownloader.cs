#nullable enable
using System;
using System.Collections;
using System.Threading.Tasks;
using System.Net;
using System.IO.Compression;
using System.IO;
using System.Linq;

using UnityEngine;
using TMPro;

namespace MarkovCraft
{
    public static class ResourceDownloader
    {
        public static IEnumerator DownloadResource(string resVersion, TMP_Text infoText, Action start, Action<bool> complete)
        {
            Debug.Log($"Downloading resource [{resVersion}]");

            start.Invoke();

            yield return null;

            bool succeeded = false;

            Task<string>? downloadTask = null;
            var webClient = new WebClient();

            // Download version manifest
            downloadTask = webClient.DownloadStringTaskAsync("https://launchermeta.mojang.com/mc/game/version_manifest.json");
            infoText.text = "Downloading version manifest...";
            while (!downloadTask.IsCompleted) yield return null;

            if (downloadTask.IsCompletedSuccessfully) // Proceed to resource downloading
            {
                var manifestJson = Json.ParseJson(downloadTask.Result);
                var versionTargets = manifestJson.Properties["versions"].DataArray.Where(x =>
                        x.Properties["id"].StringValue.Equals(resVersion));

                if (versionTargets.Count() > 0)
                {
                    var versionInfoUri = versionTargets.First().Properties["url"].StringValue;
                    downloadTask = webClient.DownloadStringTaskAsync(versionInfoUri);
                    infoText.text = $"Downloading {resVersion} version info...";
                    while (!downloadTask.IsCompleted) yield return null;

                    if (downloadTask.IsCompletedSuccessfully)
                    {
                        var infoJson = Json.ParseJson(downloadTask.Result);
                        var clientJarInfo = infoJson.Properties["downloads"].Properties["client"];

                        var jarUri = clientJarInfo.Properties["url"].StringValue;
                        // Download jar file
                        var tempJarPath = PathHelper.GetPackDirectoryNamed("temp.jar");
                        var jardownloadTask = webClient.DownloadFileTaskAsync(jarUri, tempJarPath);
                        infoText.text = $"Downloading client jar from {jarUri}...";
                        while (!jardownloadTask.IsCompleted) yield return null;
                        if (jardownloadTask.IsCompletedSuccessfully) // Jar downloaded, unzip it
                        {
                            var targetFolder = PathHelper.GetPackDirectoryNamed($"vanilla-{resVersion}");
                            var zipFile = ZipFile.OpenRead(tempJarPath);
                            infoText.text = $"Extracting asset files...";
                            // Extract asset files
                            foreach (var entry in zipFile.Entries.Where(x => x.FullName.StartsWith("assets")))
                            {
                                var entryPath = new FileInfo($@"{targetFolder}\{entry.FullName}");
                                if (!entryPath.Directory.Exists) // Create the folder if not present
                                    entryPath.Directory.Create();
                                entry.ExtractToFile(entryPath.FullName);
                            }
                            
                            if (zipFile.GetEntry("pack.mcmeta") is not null) // Extract pack.mcmeta
                                zipFile.GetEntry("pack.mcmeta").ExtractToFile($@"{targetFolder}\pack.mcmeta");
                            else // Create pack.mcmeta
                            {
                                var metaText = "{ \"pack\": { \"description\": \"Meow~\", \"pack_format\": 4 } }";
                                File.WriteAllText($@"{targetFolder}\pack.mcmeta", metaText);
                            }

                            Debug.Log("Extracted resource files from jar.");

                            // Dispose zip file and clean up
                            zipFile.Dispose();
                            if (File.Exists(tempJarPath))
                                File.Delete(tempJarPath);
                            
                            succeeded = true;
                        }
                        else
                            Debug.LogWarning($"Failed to download client jar: {jardownloadTask.Exception}");
                    }
                    else
                        Debug.LogWarning($"Failed to download version info from {versionInfoUri}.");

                }
                else
                    Debug.LogWarning($"Version [{resVersion}] is not found in manifest!");
            }
            else
                Debug.LogWarning("Failed to download version manifest.");
            
            // Dispose web client
            webClient.Dispose();

            yield return null;

            complete.Invoke(succeeded);
        }
    }
}
