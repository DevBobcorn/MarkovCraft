#nullable enable
using System.Linq;
using UnityEngine;
using TMPro;
using System;

namespace MarkovCraft
{
    public class BlockStateProperty : MonoBehaviour
    {
        [SerializeField] private TMP_Text? keyText;
        [SerializeField] private TMP_Dropdown? valueSelector;
        private string keyName = string.Empty;
        private string[] valueNames = { };
        private Action<string, string>? valueCallback;

        public void SetData(string key, string[] values)
        {
            keyText!.text = key;
            keyName = key;
            valueNames = values;
            valueSelector!.options = values.Select(x => new TMP_Dropdown.OptionData(x)).ToList();
        }

        public void SetCallback(Action<string, string> callback)
        {
            valueCallback = callback;

            valueSelector!.onValueChanged.RemoveAllListeners();
            valueSelector.onValueChanged.AddListener((a) => {
                valueCallback.Invoke(keyName, valueNames[a]);
            });
        }

        public void SelectValue(string value)
        {
            for (int i = 0; i < valueNames.Length; i++)
            {
                if (valueNames[i] == value)
                {
                    valueSelector!.SetValueWithoutNotify(i);
                    break;
                }
            }
        }
    }
}
