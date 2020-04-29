#if UNITY_EDITOR

using System;
using Path = System.IO.Path;

using UnityEngine;
using UnityEditor;

namespace ModIO.UI.EditorCode
{
    [CustomPropertyDrawer(typeof(PluginSettings.VariableDirectoryAttribute))]
    public class VariableDirectoryDrawer : PropertyDrawer
    {
        public const float BUTTON_WIDTH = 22;

        // ---------[ GUI FUNCTIONALITY ]---------
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // render base field
            Rect basePropertyRect = position;
            basePropertyRect.height = position.height - EditorGUIUtility.singleLineHeight;
            EditorGUI.PropertyField(basePropertyRect, property, label);

            // draw unwrapped directory
            string dir = property.stringValue;
            dir = PluginSettings.ReplaceDirectoryVariables(dir,
                                                           PluginSettings.GAME_ID);

            Rect previewRect = position;
            previewRect.width = position.width - BUTTON_WIDTH;
            previewRect.y = basePropertyRect.height + basePropertyRect.y;
            previewRect.height = EditorGUIUtility.singleLineHeight;

            Rect buttonRect = previewRect;
            buttonRect.x = previewRect.xMax;
            buttonRect.width = BUTTON_WIDTH;

            using(new EditorGUI.DisabledScope(true))
            {
                // NOTE(@jackson): The second param of GUIContent is a tooltip
                EditorGUI.LabelField(previewRect, new GUIContent(dir, dir));
            }

            bool directoryIsValid = false;
            try
            {
                string testDir = dir;

                while(!directoryIsValid && !string.IsNullOrEmpty(testDir))
                {
                    testDir = Path.GetDirectoryName(testDir);
                    directoryIsValid = LocalDataStorage.GetDirectoryExists(testDir);
                }
            }
            catch
            {
                directoryIsValid = false;
            }

            using(new EditorGUI.DisabledScope(!directoryIsValid))
            {
                string toolTip = null;
                if(directoryIsValid)
                {
                    toolTip = "Locate directory";
                }
                else
                {
                    toolTip = "Invalid directory";
                }

                if(GUI.Button(buttonRect, new GUIContent("...", toolTip)))
                {
                    if(!LocalDataStorage.GetDirectoryExists(dir))
                    {
                        LocalDataStorage.CreateDirectory(dir);
                    }

                    if(dir.StartsWith(Application.dataPath))
                    {
                        string assetDir = dir.Substring(Application.dataPath.Length);
                        assetDir = "Assets" + assetDir;

                        var folderObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetDir);
                        int folderId = folderObject.GetInstanceID();

                        EditorGUIUtility.PingObject(folderId);
                        UnityEditor.Selection.activeObject = folderObject;
                    }
                    else
                    {
                        EditorUtility.RevealInFinder(dir);
                    }
                }
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float baseHeight = base.GetPropertyHeight(property, label);
            return (baseHeight + EditorGUIUtility.singleLineHeight);
        }
    }
}

#endif // UNITY_EDITOR
