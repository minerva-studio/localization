using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Minerva.Localizations
{
    internal static class LocalizationSettingsIMGUIRegister
    {
        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            LocalizationSettings instance;
            // First parameter is the path in the Settings window.
            // Second parameter is the scope of this setting: it only appears in the Project Settings window.
            var provider = new SettingsProvider("Project/Localilzation", SettingsScope.Project)
            {
                // By default the last token of the path is used as display name if no label is provided.
                label = "Localilzation",

                // Create the SettingsProvider and initialize its drawing (IMGUI) function in place:
                guiHandler = (searchContext) =>
                {
                    var settings = LocalizationSettings.GetSerializedSettings();
                    //EditorGUILayout.PropertyField(settings.FindProperty(nameof(instance.testScene)), new GUIContent("Test Scene")); 

                    var label = new GUIContent("Localization Manager");
                    EditorGUILayout.PropertyField(settings.FindProperty(nameof(instance.manager)), label);

                    if (EditorGUI.EndChangeCheck())
                    {
                        settings.ApplyModifiedProperties();
                    }
                },

                // Populate the search keywords to enable smart search filtering and label highlighting:
                keywords = new HashSet<string>(new[] { "Localilzation", "Locale" })
            };

            return provider;
        }
    }
}
