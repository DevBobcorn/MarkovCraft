using System.IO;
using UnityEngine;

namespace MarkovCraft
{
    public class PathHelper
    {
        public static string GetRootDirectory()
        {
            return Directory.GetParent(Application.dataPath).FullName;
        }

        public static string GetPacksDirectory()
        {
            return Directory.GetParent(Application.dataPath).FullName + @"\Resource Packs";
        }

        public static string GetPackDirectoryNamed(string packName)
        {
            return Directory.GetParent(Application.dataPath).FullName + @$"\Resource Packs\{packName}";
        }

        public static string GetExtraDataDirectory()
        {
            return Directory.GetParent(Application.dataPath).FullName + @"\Extra Data";
        }

        public static string GetExtraDataFile(string fileName)
        {
            return Directory.GetParent(Application.dataPath).FullName + @$"\Extra Data\{fileName}";
        }

    }
}