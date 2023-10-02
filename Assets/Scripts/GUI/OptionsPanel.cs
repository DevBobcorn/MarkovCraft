#nullable enable
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MarkovCraft
{
    public class OptionsPanel : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer? background;
        [SerializeField] private Light? directionalLight;
        [SerializeField] private Transform? environmentContainer;

        [SerializeField] private CanvasGroup? canvasGroup;
        [SerializeField] private TMP_Dropdown? environmentDropdown;
        [SerializeField] private TMP_Text? directionText;
        [SerializeField] private Slider? directionSlider;
        [SerializeField] private TMP_Text? elevAngleText;
        [SerializeField] private Slider? elevAngleSlider;
        [SerializeField] private TMP_Text? intensityText;
        [SerializeField] private Slider? intensitySlider;

        [SerializeField] private WrappedColorPicker? backgroundColorPicker;

        private static readonly string[] environmentNameKeys = {
            "environment.name.none",
            "environment.name.ground"
        };

        [SerializeField] private GameObject[] environmentPrefabs = { };

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

            backgroundColorPicker!.Initialize(background!.color);
            backgroundColorPicker.onColorChange.AddListener(color => background!.color = color);

            environmentDropdown!.AddOptions(environmentNameKeys.Select(x =>
                    new TMP_Dropdown.OptionData(GameScene.GetL10nString(x))).ToList());

            environmentDropdown!.onValueChanged.AddListener(index =>
            {
                // Clear previous environment
                foreach (Transform child in environmentContainer!)
	                GameObject.Destroy(child.gameObject);
                
                // Create new environment object
                var envPrefab = environmentPrefabs[index];
                if (envPrefab != null)
                {
                    var envObj = GameObject.Instantiate(envPrefab);
                    envObj.transform.SetParent(environmentContainer, false);
                }
            });
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