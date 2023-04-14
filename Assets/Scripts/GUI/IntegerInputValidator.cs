#nullable enable
using UnityEngine;
using TMPro;

namespace MarkovBlocks
{
    [RequireComponent(typeof (TMP_InputField))]
    public class IntegerInputValidator : MonoBehaviour
    {
        [SerializeField] public int MinValue;
        [SerializeField] public int MaxValue;

        private TMP_InputField? input;

        void Start()
        {
            input = GetComponent<TMP_InputField>();
            input.onEndEdit.RemoveAllListeners();
            input.onEndEdit.AddListener(ValidateNumberInput);

        }

        private void ValidateNumberInput(string newText)
        {
            int num;

            if (int.TryParse(newText, out num))
            {
                if (num > MaxValue) // Input value too big
                    input!.text = MaxValue.ToString();
                else if (num < MinValue) // Input value too small
                    input!.text = MinValue.ToString();
                
                // Input value is valid, no need to update
            }
            else // Input is not even an integer, update to min value
                input!.text = MinValue.ToString();
        }
    }
}

