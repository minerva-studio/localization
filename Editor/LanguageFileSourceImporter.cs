using UnityEditor.AssetImporters;
using UnityEngine;
using System.IO;

namespace Minerva.Localizations
{
    /// <summary>
    /// Auto import .yml file
    /// </summary>
    [ScriptedImporter(1, "yml")]
    public class LanguageFileSourceImporter : ScriptedImporter
    {
        public string tag;
        LanguageFileSource file;

        public override void OnImportAsset(AssetImportContext ctx)
        {
            if (!file)
            {
                file = LanguageFileSource.NewLangFile();
            }

            string[] lines = File.ReadAllLines(ctx.assetPath);
            file.ImportFromYaml(lines);
            file.tag = tag;

            var plainText = new TextAsset(string.Join('\n', lines));
            plainText.name = file.name;
            ctx.AddObjectToAsset("LangFile", file);
            ctx.AddObjectToAsset("PlainText", plainText);
            ctx.SetMainObject(file);
        }
    }
}