using UnityEditor.AssetImporters;
using UnityEngine;
using System.IO;
using UnityEditor;

namespace Minerva.Localizations
{
    /// <summary>
    /// Auto import .yml file
    /// </summary>
    [ScriptedImporter(1, "yml")]
    public class LanguageFileSourceImporter : ScriptedImporter
    {
        public string tag;
        LanguageFileSource sourceFile;
        LanguageFile file;

        public override void OnImportAsset(AssetImportContext ctx)
        {
            if (!sourceFile)
            {
                sourceFile = LanguageFileSource.NewLangFile();
            }
            if (!file)
            {
                file = LanguageFile.NewLangFile(ctx.assetPath);
            }

            string fileName = Path.GetFileNameWithoutExtension(ctx.assetPath);

            string[] lines = File.ReadAllLines(ctx.assetPath);
            sourceFile.ImportFromYaml(lines);
            sourceFile.tag = tag;

            file.ImportFromYaml(lines);
            file.Tag = tag;
            file.name = $"{fileName}_Default";

            var plainText = new TextAsset(string.Join('\n', lines));
            plainText.name = fileName;
            ctx.AddObjectToAsset("SourceFile", sourceFile);
            ctx.AddObjectToAsset("LangFile", file);
            ctx.AddObjectToAsset("PlainText", plainText);
            ctx.SetMainObject(sourceFile);

            var settings = LocalizationSettings.GetOrCreateSettings();
            if (settings.manager)
            {
                settings.manager.RebuildKeyList();
                settings.manager.UpdateSources(sourceFile.Keys);
            }
        }
    }
}