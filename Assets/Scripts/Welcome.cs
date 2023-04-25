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
        [SerializeField] TMP_Text? VersionText, DownloadInfoText;
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
            if (VersionHolder == null || DownloadButton == null || DownloadInfoText == null) return;

            downloadButtonAnimator = DownloadButton.GetComponent<Animator>();
            var buttonText = DownloadButton.GetComponentInChildren<TMP_Text>();
            buttonText.text = "Download";

            DownloadInfoText.text = string.Empty;

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
            StartCoroutine(ResourceDownloader.DownloadResource(version.ResourceVersion, DownloadInfoText!,
                    () => DownloadButton!.GetComponentInChildren<TMP_Text>().text = "Downloading...",
                    (succeeded) => {
                        DownloadButton!.GetComponentInChildren<TMP_Text>().text = "Download";
                        downloadingRes = false;

                        DownloadInfoText!.text = succeeded ? string.Empty :
                                $"Failed to download resources for {version.ResourceVersion}. Please try again.";

                        // Refresh buttons
                        UpdateSelectedVersion();
                    }));
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


    }
}