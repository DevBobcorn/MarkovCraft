#nullable enable
using UnityEngine;
using TMPro;

[RequireComponent(typeof (TMP_Text))]
public class FPSDisplay : MonoBehaviour
{
    private TMP_Text? text;

    void Start()
    {
        text = GetComponent<TMP_Text>();

    }

    // Update is called once per frame
    void Update()
    {
        if (text != null)
        {
            text.text = $"FPS:\t{(int)(1 / Time.deltaTime)}";
        }
    }
}
