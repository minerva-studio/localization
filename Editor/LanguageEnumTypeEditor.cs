using Minerva.Module;
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Minerva.Localizations.Editor
{
    [Obsolete]
    public class LanguageEnumTypeEditor : EditorWindow
    {
        LocalizationDataManager fileManager;
        string typeName = "Amlos.";
        string assemblyName = "Library-of-Meialia";
        string suffixes = "name";
        Type type;
        string referenceCountry;
        string country;
        private Vector2 scrollPos;
        private bool showSecondaryCountry;


        // Add menu item named "My Window" to the Window menu
        //[MenuItem("Window/Localization/Enum Language Manager")]
        public static void ShowWindow()
        {
            //Show existing window instance. If one doesn't exist, make one.
            var window = GetWindow(typeof(LanguageEnumTypeEditor), false, "Language Editor(Enum)");
            window.name = "Language Pack Manager";
        }


        void OnGUI()
        {
            scrollPos = GUILayout.BeginScrollView(scrollPos);
            GUILayout.BeginVertical();
            GUILayout.Space(10);
            GUILayout.Label("Language Manager", EditorStyles.boldLabel);
            fileManager = (LocalizationDataManager)EditorGUILayout.ObjectField("Language File Manager", fileManager, typeof(LocalizationDataManager), false);
            typeName = EditorGUILayout.TextField("Enum Path", typeName);
            assemblyName = EditorGUILayout.TextField("Assembly Name", assemblyName);
            GUILayout.Space(10);
            showSecondaryCountry = GUILayout.Toggle(showSecondaryCountry, "Show Secondary Country");
            suffixes = EditorGUILayout.TextField("Assembly Name", suffixes);

            if (!fileManager)
            {
                fileManager = FindObjectOfType<LocalizationDataManager>();
                EndWindow(); return;
            }

            try
            {
                if (!string.IsNullOrEmpty(assemblyName))
                {
                    type = Type.GetType(typeName + "," + assemblyName, true);
                }
                else type = Type.GetType(typeName, true);
            }
            catch (Exception)
            {
                var current = GUI.contentColor;
                GUI.contentColor = Color.red;
                GUILayout.Label("Enum is not found");
                GUI.contentColor = current;
                EndWindow();
                return;
            }

            GUILayout.Space(EditorGUIUtility.singleLineHeight);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical();
            referenceCountry = EditorGUILayout.TextField("Reference Country/Region", referenceCountry);
            LoadCountry(referenceCountry);
            if (showSecondaryCountry)
            {
                EditorGUILayout.EndVertical();
                EditorGUILayout.BeginVertical();
                country = EditorGUILayout.TextField("Country/Region", country);
                LoadCountry(country);
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            EndWindow();
        }

        private void LoadCountry(string localizationCountry)
        {
            LanguageFile file = fileManager.GetLanguageFile(localizationCountry.ToString());
            foreach (Enum item in Enum.GetValues(type))
            {
                string text = item.ToString();
                GUILayout.Label(text.ToTitleCase(), EditorStyles.boldLabel);
                foreach (var suffix in suffixes.Split(',').Select(s => s.Replace(" ", "")))
                    StringField(GetKey(text, type), suffix, suffix.ToTitleCase(), 20, file);
                GUILayout.Space(10);
            }
        }

        string StringField(string baseKey, string suffix, string Label, int height, LanguageFile item)
        {
            string key = baseKey + suffix;
            string oldValue = item.Get(key);
            var value = EditorGUILayout.TextField(Label, oldValue, GUILayout.Height(height));
            if (value != oldValue)
            {
                EditorUtility.SetDirty(item);
                item.Write(key, value);

            }
            return oldValue;
        }

        private static string GetKey(string name, Type enumType)
        {
            if (enumType == null)
            {
                return "";
            }

            if (enumType.IsEnum)
            {
                return enumType.FullName + "." + name + ".";
            }
            return "";
        }

        private static void EndWindow()
        {
            GUILayout.EndVertical();
            GUILayout.EndScrollView();
        }
    }
}