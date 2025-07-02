using System.Collections.Generic;

namespace Minerva.Localizations
{
    public interface IL10nHandler
    {
        L10nDataManager Manager { get; }
        bool IsLoaded { get; }
        string Region { get; }

        void Init(L10nDataManager manager);
        void Load(string region);
        void Reload();

        bool Contains(string key, bool fallback);
        bool Contains(Key key, bool fallback);

        bool OptionOf(string partialKey, out string[] result, bool firstLevelOnly = false);
        bool OptionOf(Key partialKey, out string[] result, bool firstLevelOnly = false);

        bool CopyOptions(string partialKey, List<string> strings, bool firstLevelOnly = false);
        bool CopyOptions(Key partialKey, List<string> strings, bool firstLevelOnly = false);

        string GetRawContent(Key key);
        string GetRawContent(string key);

        bool Write(Key key, string value);
        bool Write(string key, string value);
    }
}