using System.IO;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Minerva.Localizations
{
    /// <summary>
    /// Auto import .yml file
    /// </summary>
    [ScriptedImporter(1, "yml")]
    public class LanguageFileSourceImporter : ScriptedImporter
    {
        public bool skipFile;
        public string tag;
        LanguageFileSource sourceFile;
        LanguageFile file;

        public override void OnImportAsset(AssetImportContext ctx)
        {
            string fileName = Path.GetFileNameWithoutExtension(ctx.assetPath);
            string[] lines = File.ReadAllLines(ctx.assetPath);
            var plainText = new TextAsset(string.Join('\n', lines));
            plainText.name = fileName;
            if (skipFile)
            {
                ctx.AddObjectToAsset("PlainText", plainText);
                ctx.SetMainObject(plainText);
                return;
            }

            if (!sourceFile)
            {
                sourceFile = LanguageFileSource.NewLangFile();
            }
            if (!file)
            {
                file = LanguageFile.NewLangFile(ctx.assetPath);
            }

            sourceFile.ImportFromYaml(string.Join("\n", lines));
            sourceFile.tag = tag;

            file.ImportFromYaml(lines);
            file.Tag = tag;
            file.name = $"{fileName}_Default";

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