using System;
using System.Collections.Generic;

namespace Minerva.Localizations
{
    /// <summary>
    /// Legacy handler adapter backed by the shared multi-region runtime.
    /// </summary>
    public class L10nHandler : IL10nHandler
    {
        #region State

        private L10nRuntime runtime;

        public L10nDataManager Manager => runtime?.Manager;
        public bool IsLoaded => runtime?.HasMainRegion == true;
        public string Region => runtime?.MainRegion ?? string.Empty;
        internal L10nRuntime Runtime => runtime;
        internal string ListDelimiter => runtime?.CurrentData?.ListDelimiter ?? string.Empty;
        internal string WordSpace => runtime?.CurrentData?.WordSpace ?? string.Empty;

        #endregion

        #region Lifecycle

        /// <summary>
        /// Constructs a localization handler adapter.
        /// </summary>
        public L10nHandler()
        {
        }

        /// <summary>
        /// Initializes the shared runtime for this handler.
        /// </summary>
        public void Init(L10nDataManager manager)
        {
            runtime = new L10nRuntime(manager);
        }

        #endregion

        #region Region Loading

        /// <summary>
        /// Loads a region as background data unless no main region exists yet.
        /// </summary>
        public void Load(string region)
        {
            Load(region, false);
        }

        /// <summary>
        /// Loads a region and optionally selects it as the main region.
        /// </summary>
        internal L10nRegionLoadResult Load(string region, bool asMainRegion)
        {
            EnsureRuntime();
            var result = runtime.Load(region, asMainRegion);
            if (result.RegionLoaded)
            {
                UnityEngine.Debug.Log($"Localization Loaded. (Region: {region}, Entry Count: {runtime.ForRegionData(region).EntryCount})");
            }
            return result;
        }

        /// <summary>
        /// Reloads the current main region.
        /// </summary>
        public void Reload()
        {
            EnsureRuntime();
            runtime.ReloadMain();
        }

        #endregion

        #region Raw Content

        /// <summary>
        /// Gets raw content from the main region with shared fallback support.
        /// </summary>
        public string GetRawContent(string key)
        {
            EnsureRuntime();
            return runtime.GetMainRawContent(key);
        }

        /// <summary>
        /// Gets raw content from the main region with shared fallback support.
        /// </summary>
        public string GetRawContent(Key key)
        {
            EnsureRuntime();
            return runtime.GetMainRawContent(key);
        }

        /// <summary>
        /// Gets raw content from the shared fallback region.
        /// </summary>
        public string GetDefaultRawContent(string key)
        {
            EnsureRuntime();
            return runtime.FallbackData?.GetRawContent(key, null);
        }

        /// <summary>
        /// Gets raw content from the shared fallback region.
        /// </summary>
        public string GetDefaultRawContent(Key key)
        {
            EnsureRuntime();
            return runtime.FallbackData?.GetRawContent(key, null);
        }

        #endregion

        #region Key Lookup & Overrides

        /// <summary>
        /// Checks whether the main region contains the key.
        /// </summary>
        public bool Contains(string key, bool fallback)
        {
            EnsureRuntime();
            return runtime.ContainsMain(key, fallback);
        }

        /// <summary>
        /// Checks whether the main region contains the key.
        /// </summary>
        public bool Contains(Key key, bool fallback)
        {
            EnsureRuntime();
            return runtime.ContainsMain(key, fallback);
        }

        /// <summary>
        /// Writes an in-memory override into the main region.
        /// </summary>
        public bool Write(string key, string value)
        {
            EnsureRuntime();
            return runtime.WriteMain(key, value);
        }

        /// <summary>
        /// Writes an in-memory override into the main region.
        /// </summary>
        public bool Write(Key key, string value)
        {
            EnsureRuntime();
            return runtime.WriteMain(key, value);
        }

        /// <summary>
        /// Gets matching key options from the main region.
        /// </summary>
        public bool OptionOf(string partialKey, out string[] result, bool firstLevelOnly = false)
        {
            EnsureRuntime();
            return runtime.OptionOfMain(partialKey, out result, firstLevelOnly);
        }

        /// <summary>
        /// Gets matching key options from the main region.
        /// </summary>
        public bool OptionOf(Key partialKey, out string[] result, bool firstLevelOnly = false)
        {
            EnsureRuntime();
            return runtime.OptionOfMain(partialKey, out result, firstLevelOnly);
        }

        /// <summary>
        /// Copies matching key options from the main region.
        /// </summary>
        public bool CopyOptions(string partialKey, List<string> strings, bool firstLevelOnly = false)
        {
            EnsureRuntime();
            return runtime.CopyOptionsMain(partialKey, strings, firstLevelOnly);
        }

        /// <summary>
        /// Copies matching key options from the main region.
        /// </summary>
        public bool CopyOptions(Key partialKey, List<string> strings, bool firstLevelOnly = false)
        {
            EnsureRuntime();
            return runtime.CopyOptionsMain(partialKey, strings, firstLevelOnly);
        }

        #endregion

        #region Private Helpers

        private void EnsureRuntime()
        {
            if (runtime == null)
            {
                throw new NullReferenceException("The localization manager has not yet initialized.");
            }
        }

        #endregion
    }
}
