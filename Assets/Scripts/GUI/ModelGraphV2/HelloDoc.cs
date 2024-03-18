using UnityEngine;
using UnityEngine.UIElements;

namespace MarkovCraft
{
    public class HelloDoc : MonoBehaviour
    {
        void Start()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;

            Debug.Log($"Document root: {root.name}");

            var helloButton = root.Q<Button>("test_button");
            helloButton.RegisterCallback<MouseUpEvent>((evt) => {
                Debug.Log("Button clicked!");
            });

            var titleLabel = root.Q<Label>("title_label");
        }
    }
}