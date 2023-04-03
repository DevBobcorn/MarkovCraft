#nullable enable
using UnityEngine;

namespace MarkovBlocks
{
    [CreateAssetMenu(fileName = "MarkovJuniorModel", menuName = "MarkovBlocks/MarkovJuniorModel")]
    public class MarkovJuniorModel : ScriptableObject
    {
        [SerializeField] public string Name = "Apartemazements";
        [SerializeField] public int SizeX = 8;
        [SerializeField] public int SizeY = 8;
        [SerializeField] public int SizeZ = 8;
        [SerializeField] public int Amount = 1;
        [SerializeField] public int Steps = 1000;
        [SerializeField] public bool Animated = true;
        [SerializeField] public string? Seeds = string.Empty;
        [SerializeField] public string[] CustomColors = { };

    }
}