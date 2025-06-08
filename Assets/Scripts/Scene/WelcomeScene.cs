#nullable enable
using System.Collections;
using System.IO;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;
using TMPro;

using CraftSharp;
using CraftSharp.Resource;

namespace MarkovCraft
{
    [RequireComponent(typeof (Animator))]
    public class WelcomeScene : MonoBehaviour
    {
        private static readonly int LEFT = Animator.StringToHash("Left");
        private static readonly int RIGHT = Animator.StringToHash("Right");
        private static readonly int HIDDEN = Animator.StringToHash("Hidden");
        private static readonly int ENTER = Animator.StringToHash("Enter");
        
        [SerializeField] private TMP_Text? VersionText, DownloadInfoText;
        [SerializeField] private VersionHolder? VersionHolder;
        [SerializeField] private LocalizedStringTable? L10nTable;
        [SerializeField] private Animator? CubeAnimator;
        [SerializeField] private Button? EnterButton, DownloadButton, ReplayButton;
        [SerializeField] private Button? ResourcePacksButton;

        [SerializeField] private WelcomeScreen? WelcomeScreen;

        private static WelcomeScene? instance;

        public static WelcomeScene Instance
        {
            get {
                if (!instance)
                    instance = FindFirstObjectByType<WelcomeScene>();

                return instance!;
            }
        }

        private Animator? downloadButtonAnimator;
        private bool downloadingRes = false;

        public static string GetL10nString(string key, params object[] p)
        {
            var str = Instance.L10nTable!.GetTable().GetEntry(key);
            return str == null ? $"<{key}>" : string.Format(str.Value, p);
        }

        private void UpdateSelectedVersion()
        {
            if (!VersionHolder) return;

            var verIndex = VersionHolder.SelectedVersion;
            var version = VersionHolder.Versions[verIndex];

            if (VersionText)
                VersionText.text = $"< {version.Name} >";
            
            var newResPath = PathHelper.GetPackFile($"vanilla-{version.ResourceVersion}", "pack.mcmeta");
            var resPresent = File.Exists(newResPath);

            downloadButtonAnimator!.SetBool(HIDDEN, resPresent);

            if (EnterButton)
                EnterButton.interactable = resPresent;
            
            if (ReplayButton)
                ReplayButton.interactable = resPresent;
            
            if (ResourcePacksButton)
                ResourcePacksButton.interactable = resPresent;
        }

        private void Start()
        {
            if (!VersionHolder || !DownloadButton || !DownloadInfoText) return;

            downloadButtonAnimator = DownloadButton.GetComponent<Animator>();
            DownloadInfoText.text = $"v{Application.version}";

            if (VersionHolder.Versions.Length <= 0)
                return;
            
            VersionHolder.SelectedVersion = 0;
            
            UpdateSelectedVersion();
        }

        public string GetResourceVersion()
        {
            return VersionHolder!.Versions[VersionHolder.SelectedVersion].ResourceVersion;
        }

        public int GetResPackFormatInt()
        {
            return VersionHolder!.Versions[VersionHolder.SelectedVersion].ResPackFormatInt;
        }

        public void PrevVersion()
        {
            if (!VersionHolder || downloadingRes) return;

            var count = VersionHolder.Versions.Length;

            if (count > 0)
                VersionHolder.SelectedVersion = (VersionHolder.SelectedVersion - 1 + count) % count;
            
            UpdateSelectedVersion();

            CubeAnimator!.SetTrigger(LEFT);
        }

        public void NextVersion()
        {
            if (!VersionHolder || downloadingRes) return;

            var count = VersionHolder.Versions.Length;

            if (count > 0)
                VersionHolder.SelectedVersion = (VersionHolder.SelectedVersion + 1) % count;
            
            UpdateSelectedVersion();

            CubeAnimator!.SetTrigger(RIGHT);
        }

        public void NextLanguage()
        {
            var locales = LocalizationSettings.AvailableLocales.Locales;
            var selected = LocalizationSettings.SelectedLocale;
            var selectedIndex = locales.IndexOf(selected);
            if (selectedIndex >= 0)
                LocalizationSettings.SelectedLocale = locales[(selectedIndex + 1) % locales.Count];

            Debug.Log($"Locale changed to {LocalizationSettings.SelectedLocale}");
        }

        public void DownloadResource()
        {
            if (!VersionHolder || downloadingRes) return;

            var verIndex = VersionHolder.SelectedVersion;
            var version = VersionHolder.Versions[verIndex];

            downloadingRes = true;
            StartCoroutine(ResourceDownloader.DownloadResource(version.ResourceVersion,
                    (status, progress) => DownloadInfoText!.text = GetL10nString(status) + progress,
                    () => downloadButtonAnimator!.SetBool(HIDDEN, true),
                    succeeded => {
                        downloadButtonAnimator!.SetBool(HIDDEN, succeeded);
                        downloadingRes = false;

                        DownloadInfoText!.text = succeeded ? $"v{Application.version}" :
                                GetL10nString("status.error.download_resource_failure", version.ResourceVersion);

                        // Refresh buttons
                        UpdateSelectedVersion();
                    }));
        }

        private IEnumerator MarkovCoroutine()
        {
            if (WelcomeScreen)
            {
                WelcomeScreen.SetEnterGameTrigger();
            }

            yield return new WaitForSecondsRealtime(0.32F);

            var op = SceneManager.LoadSceneAsync("Scenes/Generation", LoadSceneMode.Single);

            while (op!.progress < 0.9F)
                yield return null;
        }

        public void EnterMarkov()
        {
            if (!VersionHolder || downloadingRes) return;

            StartCoroutine(MarkovCoroutine());
        }

        private IEnumerator ReplayCoroutine()
        {
            GetComponent<Animator>().SetTrigger(ENTER);

            yield return new WaitForSecondsRealtime(0.32F);

            var op = SceneManager.LoadSceneAsync("Scenes/Replay", LoadSceneMode.Single);

            while (op!.progress < 0.9F)
                yield return null;
        }

        public void EnterReplay()
        {
            if (!VersionHolder || downloadingRes) return;

            StartCoroutine(ReplayCoroutine());
        }

        public void QuitApp()
        {
            Application.Quit();
        }
    }
}