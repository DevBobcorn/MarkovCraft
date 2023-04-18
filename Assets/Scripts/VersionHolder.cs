#nullable enable
using UnityEngine;
using System;

namespace MarkovCraft
{
    [Serializable]
    public struct MCVersion
    {
        public string Name;
        public string DataVersion;
        public string ResourceVersion;
    }

    [CreateAssetMenu(fileName = "VersionHolder", menuName = "MarkovCraft/Version Holder")]
    public class VersionHolder : ScriptableObject
    {
        [SerializeField] public MCVersion[] Versions = new MCVersion[]{ };
        [SerializeField] public int SelectedVersion = -1;
    }
}