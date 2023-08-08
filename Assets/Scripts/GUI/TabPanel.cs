#nullable enable
using UnityEngine;

namespace MarkovCraft
{
    public class TabPanel : MonoBehaviour
    {
        [SerializeField] private Animator[] tabContentAnimators = { };
        [SerializeField] private int defaultTabIndex = 0;

        private int selectedTabIndex = -1;

        void Start()
        {
            if (tabContentAnimators.Length > 0) // If tab list is not empty
            {
                SelectTab(defaultTabIndex);
            }
        }

        public void SelectTab(int tabIndex)
        {
            if (tabIndex < 0 || tabIndex >= tabContentAnimators.Length)
            {
                Debug.LogWarning($"Invalid tab index: {tabIndex}");
                return;
            }

            if (selectedTabIndex != tabIndex)
            {
                if (selectedTabIndex >= 0 && selectedTabIndex < tabContentAnimators.Length)
                {
                    // Hide previously selected tab content
                    tabContentAnimators[selectedTabIndex].SetBool("Hidden", true);
                }

                // Show selected tab content
                tabContentAnimators[tabIndex].SetBool("Hidden", false);

                // Update selection
                selectedTabIndex = tabIndex;
            }
        }
    }
}