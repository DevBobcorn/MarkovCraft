#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Xml.Linq;

using CraftSharp;

namespace MarkovCraft
{
    [CustomEditor(typeof (ConfiguredModel))]
    public class ConfiguredModelEditor : Editor
    {
        private static readonly char SP = Path.DirectorySeparatorChar;
        private const string PLACE_HOLDER = "<model/path/here>";
        private const string CONFIGURED_MODEL_FOLDER = "configured_models";

        private string XDocFileName = PLACE_HOLDER;

        public override void OnInspectorGUI()
        {
            GUILayout.Space(10);

            GUILayout.Label($"Load from/Save to (file under {CONFIGURED_MODEL_FOLDER}):");
            XDocFileName = GUILayout.TextField(XDocFileName);

            if (XDocFileName.Equals(PLACE_HOLDER)) // Fill in default xml model path
            {
                var modelObj = this.target as ConfiguredModel;

                if (modelObj is not null)
                    XDocFileName = $"{modelObj.name}.xml";
                else
                    XDocFileName = "model.xml";
            }

            GUILayout.Space(10);

            // XDoc import button
            if (GUILayout.Button("Load from .xml file"))
            {
                var modelObj = this.target as ConfiguredModel;

                if (modelObj is not null)
                {
                    var path = MarkovGlobal.GetDataFile($"{CONFIGURED_MODEL_FOLDER}{SP}{XDocFileName}");
                    
                    if (File.Exists(path))
                    {
                        var xdoc = XDocument.Load(path);
                        ConfiguredModel.UpdateFromXMLDoc(ref modelObj, xdoc);
                        Debug.Log($"Configured model updated from {path}");
                    }
                    else
                        Debug.LogWarning($"File not found at {path}");
                }

                EditorUtility.SetDirty(modelObj);
                AssetDatabase.SaveAssetIfDirty(modelObj);
            }

            // XDoc export button
            if (GUILayout.Button("Save as .xml file"))
            {
                var modelObj = this.target as ConfiguredModel;

                if (modelObj is not null)
                {
                    var path = MarkovGlobal.GetDataFile($"{CONFIGURED_MODEL_FOLDER}{SP}{XDocFileName}");
                    var xdoc = ConfiguredModel.GetXMLDoc(modelObj);
                    xdoc.Save(path);

                    Debug.Log($"Configured model exported to {path}");
                }
            }

            GUILayout.Space(10);

            // Configured model editor
            DrawDefaultInspector();
        }

    }
}
#endif