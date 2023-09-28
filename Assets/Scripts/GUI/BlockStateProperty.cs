#nullable enable
using System.Linq;
using UnityEngine;
using TMPro;

namespace MarkovCraft
{
    public class BlockStateProperty : MonoBehaviour
    {
        [SerializeField] private TMP_Text? keyText;
        [SerializeField] private TMP_Dropdown? valueSelector;

        public void SetData(string key, string[] values)
        {
            keyText!.text = key;

            valueSelector!.options = values.Select(x => new TMP_Dropdown.OptionData(x)).ToList();
        }
    }
}
