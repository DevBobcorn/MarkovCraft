#nullable enable
using System;
using UnityEngine;

namespace MarkovBlocks
{
    [Serializable]
    public struct CustomCharRemap
    {
        public char Symbol;
        public string RemapTarget;
        public Color32 RemapColor;
    }

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

        [SerializeField] public CustomCharRemap[] CustomRemapping = { };

    }
}