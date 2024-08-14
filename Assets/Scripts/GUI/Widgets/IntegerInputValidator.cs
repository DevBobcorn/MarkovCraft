#nullable enable
using UnityEngine;
using UnityEngine.Events;
using TMPro;

namespace MarkovCraft
{
    [RequireComponent(typeof (TMP_InputField))]
    public class IntegerInputValidator : MonoBehaviour
    {
        [SerializeField] public int MinValue = int.MinValue;
        [SerializeField] public int MaxValue = int.MaxValue;
        [SerializeField] public UnityEvent<int>? OnValidateValue = new();

        private TMP_InputField? input;

        void Start()
        {
            input = GetComponent<TMP_InputField>();
            input.onEndEdit.AddListener(ValidateNumberInput);
        }

        private void ValidateNumberInput(string newText)
        {
            if (int.TryParse(newText, out int value))
            {
                if (value > MaxValue) // Input value too big
                {
                    input!.text = MaxValue.ToString();
                    OnValidateValue!.Invoke(MaxValue);
                }
                else if (value < MinValue) // Input value too small
                {
                    input!.text = MinValue.ToString();
                    OnValidateValue!.Invoke(MinValue);
                }
                else
                {
                    // Input value is valid, no need to update
                    OnValidateValue!.Invoke(value);
                }
            }
            else // Input is not even an integer, update to min value
            {
                input!.text = MinValue.ToString();
                OnValidateValue!.Invoke(MinValue);
            }
        }
    }
}

