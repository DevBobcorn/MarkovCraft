#nullable enable
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace MarkovCraft
{
    public class OptionsPanel : MonoBehaviour
    {
        [SerializeField] private Light? directionalLight;

        [SerializeField] private CanvasGroup? canvasGroup;

        [SerializeField] private TMP_Text? directionText;
        [SerializeField] private Slider? directionSlider;
        [SerializeField] private TMP_Text? elevAngleText;
        [SerializeField] private Slider? elevAngleSlider;
        [SerializeField] private TMP_Text? intensityText;
        [SerializeField] private Slider? intensitySlider;

        void Start()
        {
            intensitySlider!.onValueChanged.AddListener((val) =>
            {
                intensityText!.text = GameScene.GetL10nString("options.text.intensity", val);
                directionalLight!.intensity = val;
            });

            directionSlider!.onValueChanged.AddListener((val) =>
            {
                directionText!.text = GameScene.GetL10nString("options.text.direction", val);
                directionalLight!.transform.eulerAngles = new Vector3(
                        directionalLight.transform.eulerAngles.x, val, 0F);
            });

            elevAngleSlider!.onValueChanged.AddListener((val) =>
            {
                elevAngleText!.text = GameScene.GetL10nString("options.text.elevangle", val);
                directionalLight!.transform.eulerAngles = new Vector3(
                        val, directionalLight.transform.eulerAngles.y, 0F);
            });

            intensitySlider!.value = directionalLight!.intensity;
            intensityText!.text = GameScene.GetL10nString("options.text.intensity", intensitySlider.value);

            directionSlider!.value = directionalLight.transform.eulerAngles.y;
            directionText!.text = GameScene.GetL10nString("options.text.direction", directionSlider.value);

            elevAngleSlider!.value = directionalLight.transform.eulerAngles.x;
            elevAngleText!.text = GameScene.GetL10nString("options.text.elevangle", elevAngleSlider.value);
        }

        public void ShowPanel()
        {
            canvasGroup!.alpha = 1F;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        public void HidePanel()
        {
            canvasGroup!.alpha = 0F;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }
}