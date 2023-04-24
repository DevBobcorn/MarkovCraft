#nullable enable
using System.Collections;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Net;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace MarkovCraft
{
    [RequireComponent(typeof (Animator))]
    public class Welcome : MonoBehaviour
    {
        [SerializeField] TMP_Text? VersionText;
        [SerializeField] VersionHolder? VersionHolder;
        [SerializeField] Animator? CubeAnimator;
        [SerializeField] Button? EnterButton, DownloadButton;

        private Animator? downloadButtonAnimator;
        private bool downloadingRes = false;

        private void UpdateSelectedVersion()
        {
            if (VersionHolder == null) return;

            var verIndex = VersionHolder.SelectedVersion;
            var version = VersionHolder.Versions[verIndex];

            if (VersionText != null)
                VersionText.text = $"< {version.Name} >";
            
            var newResPath = PathHelper.GetPackDirectoryNamed($"vanilla-{version.ResourceVersion}");
            var resPresent = Directory.Exists(newResPath);

            downloadButtonAnimator?.SetBool("Hidden", resPresent);
            if (EnterButton != null)
                EnterButton.interactable = resPresent;

        }

        void Start()
        {
            if (VersionHolder == null || DownloadButton == null) return;

            downloadButtonAnimator = DownloadButton.GetComponent<Animator>();
            var buttonText = DownloadButton.GetComponentInChildren<TMP_Text>();
            buttonText.text = "Download";

            if (VersionHolder.Versions.Length <= 0)
                return;
            
            VersionHolder.SelectedVersion = 0;
            
            UpdateSelectedVersion();
        }

        public void PrevVersion()
        {
            if (VersionHolder == null || downloadingRes) return;

            var count = VersionHolder.Versions.Length;

            if (count > 0)
                VersionHolder.SelectedVersion = (VersionHolder.SelectedVersion - 1 + count) % count;
            
            UpdateSelectedVersion();

            CubeAnimator?.SetTrigger("Left");
        }

        public void NextVersion()
        {
            if (VersionHolder == null || downloadingRes) return;

            var count = VersionHolder.Versions.Length;

            if (count > 0)
                VersionHolder.SelectedVersion = (VersionHolder.SelectedVersion + 1) % count;
            
            UpdateSelectedVersion();

            CubeAnimator?.SetTrigger("Right");
        }

        public void DownloadResource()
        {
            if (VersionHolder == null || downloadingRes) return;

            var verIndex = VersionHolder.SelectedVersion;
            var version = VersionHolder.Versions[verIndex];

            downloadingRes = true;
            StartCoroutine(DownloadResource(version.ResourceVersion));
        }

        private IEnumerator MarkovCoroutine()
        {
            GetComponent<Animator>().SetTrigger("Enter");

            yield return new WaitForSecondsRealtime(0.32F);

            var op = SceneManager.LoadSceneAsync("Scenes/Markov", LoadSceneMode.Single);

            while (op.progress < 0.9F)
                yield return null;
        }

        public void EnterMarkov()
        {
            if (VersionHolder == null || downloadingRes) return;

            StartCoroutine(MarkovCoroutine());
        }

        private IEnumerator DownloadResource(string resVersion)
        {
            Debug.Log($"Downloading resource [{resVersion}]");

            var buttonText = DownloadButton!.GetComponentInChildren<TMP_Text>();
            buttonText.text = "Downloading...";

            yield return null;

            Task<string>? downloadTask = null;
            var webClient = new WebClient();

            // Download version manifest
            downloadTask = webClient.DownloadStringTaskAsync("https://launchermeta.mojang.com/mc/game/version_manifest.json");
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
                    while (!downloadTask.IsCompleted) yield return null;

                    if (downloadTask.IsCompletedSuccessfully)
                    {
                        var infoJson = Json.ParseJson(downloadTask.Result);
                        var clientJarInfo = infoJson.Properties["downloads"].Properties["client"];

                        var jarUri = clientJarInfo.Properties["url"].StringValue;
                        Debug.Log($"Client jar url: {jarUri}");
                        // Download jar file
                        var tempJarPath = PathHelper.GetPackDirectoryNamed("temp.jar");
                        var jardownloadTask = webClient.DownloadFileTaskAsync(jarUri, tempJarPath);
                        while (!jardownloadTask.IsCompleted) yield return null;
                        if (jardownloadTask.IsCompletedSuccessfully) // Jar downloaded, unzip it
                        {
                            var targetFolder = PathHelper.GetPackDirectoryNamed($"vanilla-{resVersion}");
                            var zipFile = ZipFile.OpenRead(tempJarPath);

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
                Debug.Log("Failed to download version manifest.");
            
            // Dispose web client
            webClient.Dispose();

            yield return null;

            buttonText.text = "Download";
            downloadingRes = false;

            // Refresh buttons
            UpdateSelectedVersion();
        }
    }
}