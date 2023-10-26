using UnityEditor.AssetImporters;
using UnityEngine;
using System.IO;
using Minerva.Module;
using UnityEditor;

namespace Minerva.Localizations
{
    /// <summary>
    /// Auto import, WIP
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
                file = LanguageFile.NewLangFile();
            }

            file.ImportFromYaml(File.ReadAllLines(ctx.assetPath));
            file.Tag = tag;
            file.SetMasterFile(masterFile);

            ctx.AddObjectToAsset("LangFile", file);
            ctx.SetMainObject(file);
        }
    }
}