using UnityEditor.AssetImporters;
using UnityEngine;
using System.IO;
using Minerva.Module;
using UnityEditor;
using System.Linq;

namespace Minerva.Localizations
{
    /// <summary>
    /// Auto import .lang file
    /// </summary>
    [ScriptedImporter(1, "lang")]
    public class LanguageFileImporter : ScriptedImporter
    {
        [DisplayIf(nameof(masterFile), result = false)]
        public string region;
        public string tag;
        public LanguageFile masterFile;

        LanguageFile file;

        public override void OnImportAsset(AssetImportContext ctx)
        {
            if (!file)
            {
                file = LanguageFile.NewLangFile(ctx.assetPath);
            }

            string[] lines = File.ReadAllLines(ctx.assetPath);
            file.ImportFromYaml(lines);
            file.Tag = tag;
            file.SetMasterFile(masterFile);

            var plainText = new TextAsset(string.Join('\n', lines));
            plainText.name = Path.GetFileNameWithoutExtension(ctx.assetPath);
            ctx.AddObjectToAsset("LangFile", file);
            ctx.AddObjectToAsset("PlainText", plainText);
            ctx.SetMainObject(file);

            var settings = LocalizationSettings.GetOrCreateSettings();
            if (settings.manager) settings.manager.RebuildKeyList();
        }
    }
}