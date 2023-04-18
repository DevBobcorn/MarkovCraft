#nullable enable
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

namespace MarkovCraft
{
    public class Welcome : MonoBehaviour
    {
        [SerializeField] TMP_Text? VersionText;
        [SerializeField] VersionHolder? VersionHolder;

        private void UpdateVersionText()
        {
            if (VersionHolder == null) return;

            var verName = VersionHolder.Versions[VersionHolder.SelectedVersion].Name;

            if (VersionText != null)
                VersionText.text = $"< {verName} >";
        }

        void Start()
        {
            if (VersionHolder == null) return;

            if (VersionHolder.Versions.Length <= 0)
                return;
            
            VersionHolder.SelectedVersion = 0;
            
            UpdateVersionText();
        }

        public void PrevVersion()
        {
            if (VersionHolder == null) return;

            var count = VersionHolder.Versions.Length;

            if (count > 0)
                VersionHolder.SelectedVersion = (VersionHolder.SelectedVersion - 1 + count) % count;
            
            UpdateVersionText();
        }

        public void NextVersion()
        {
            if (VersionHolder == null) return;

            var count = VersionHolder.Versions.Length;

            if (count > 0)
                VersionHolder.SelectedVersion = (VersionHolder.SelectedVersion + 1) % count;
            
            UpdateVersionText();
        }

        public void EnterMarkov()
        {
            SceneManager.LoadScene("Scenes/Markov", LoadSceneMode.Single);

        }
    }
}