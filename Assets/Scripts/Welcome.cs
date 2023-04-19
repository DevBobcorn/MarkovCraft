#nullable enable
using System.Collections;
using UnityEngine;
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

            CubeAnimator?.SetTrigger("Left");
        }

        public void NextVersion()
        {
            if (VersionHolder == null) return;

            var count = VersionHolder.Versions.Length;

            if (count > 0)
                VersionHolder.SelectedVersion = (VersionHolder.SelectedVersion + 1) % count;
            
            UpdateVersionText();

            CubeAnimator?.SetTrigger("Right");
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
            StartCoroutine(MarkovCoroutine());
        }
    }
}