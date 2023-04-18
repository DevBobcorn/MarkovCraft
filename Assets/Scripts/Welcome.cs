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

        void Start()
        {
            if (VersionHolder == null) return;

            if (VersionHolder.Versions.Length <= 0)
                return;
            
            VersionHolder.SelectedVersion = 0;
            
            if (VersionText != null)
                VersionText.text = VersionHolder.Versions[VersionHolder.SelectedVersion].Name;
            
            
        }



        public void EnterMarkov()
        {
            SceneManager.LoadScene("Scenes/Markov", LoadSceneMode.Single);

        }
    }
}