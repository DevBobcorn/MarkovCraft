#nullable enable
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MarkovCraft
{
    public class ModelItem : MonoBehaviour
    {
        [SerializeField] private TMP_Text? modelNameText;

        public void SetModelName(string name)
        {
            modelNameText!.text = name;
        }
    }
}