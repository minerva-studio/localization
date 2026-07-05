using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Minerva.Localizations
{
    /// <summary>
    /// Reports what changed after a region load request.
    /// </summary>
    internal readonly struct L10nRegionLoadResult
    {
        public readonly string Region;
        public readonly bool RegionLoaded;
        public readonly bool MainRegionChanged;
        public readonly bool IsFallbackRegion;

        /// <summary>
        /// Creates a value describing the load operation outcome.
        /// </summary>
        public L10nRegionLoadResult(string region, bool regionLoaded, bool mainRegionChanged, bool isFallbackRegion)
        {
            Region = region;
            RegionLoaded = regionLoaded;
            MainRegionChanged = mainRegionChanged;
            IsFallbackRegion = isFallbackRegion;
        }
    }

    /// <summary>
    /// Owns shared localization data, loaded region data, and the main region.
    /// </summary>
    internal sealed class L10nRuntime
    {
        #region State

        private readonly Dictionary<string, L10nRegionData> loadedRegions = new(StringComparer.Ordinal);
        private readonly L10nDataManager manager;
        private L10nRegionData fallback;
        private string mainRegion = string.Empty;

        public L10nDataManager Manager => manager;
        public string FallbackRegion => manager ? manager.defaultRegion ?? string.Empty : string.Empty;
        public string MainRegion => mainRegion ?? string.Empty;
        public bool HasMainRegion => !string.IsNullOrEmpty(mainRegion);
        public string[] LoadedRegions => loadedRegions.Keys.ToArray();
        public L10nRegionData FallbackData => fallback;
        public L10nRegionData MainData => HasMainRegion && loadedRegions.TryGetValue(mainRegion, out var data) ? data : null;
        public L10nRegionData CurrentData => MainData ?? fallback;

        #endregion

        #region Lifecycle

        /// <summary>
        /// Creates the runtime and loads the shared fallback region once.
        /// </summary>
        public L10nRuntime(L10nDataManager manager)
        {
            this.manager = manager ? manager : throw new ArgumentNullException(nameof(manager));
            LoadFallback();
        }

        #endregion

        #region Region Loading

        /// <summary>
        /// Loads a non-fallback region, optionally selecting it as the main region.
        /// </summary>
        public L10nRegionLoadResult Load(string region, bool asMainRegion)
        {
            region ??= string.Empty;
            if (IsFallbackRegion(region))
            {
                if (asMainRegion)
                {
                    Debug.LogWarning($"Fallback region '{region}' cannot be used as the main localization region.");
                }

                return new L10nRegionLoadResult(region, false, false, true);
            }

            bool wasLoaded = loadedRegions.ContainsKey(region);
            EnsureRegionLoaded(region);

            bool shouldSetMain = asMainRegion || !HasMainRegion;
            bool mainChanged = shouldSetMain && SetMainRegion(region);
            return new L10nRegionLoadResult(region, !wasLoaded, mainChanged, false);
        }

        /// <summary>
        /// Sets the main region after ensuring the region is loaded.
        /// </summary>
        public bool SetMainRegion(string region)
        {
            region ??= string.Empty;
            if (IsFallbackRegion(region))
            {
                Debug.LogWarning($"Fallback region '{region}' cannot be used as the main localization region.");
                return false;
            }

            EnsureRegionLoaded(region);
            if (mainRegion == region)
            {
                return false;
            }

            mainRegion = region;
            return true;
        }

        /// <summary>
        /// Unloads a loaded background region when it is not fallback or main.
        /// </summary>
        public bool Unload(string region)
        {
            region ??= string.Empty;
            if (IsFallbackRegion(region))
            {
                Debug.LogWarning($"Fallback region '{region}' cannot be unloaded.");
                return false;
            }
            if (mainRegion == region)
            {
                Debug.LogWarning($"Main region '{region}' cannot be unloaded.");
                return false;
            }

            return loadedRegions.Remove(region);
        }

        /// <summary>
        /// Gets a public region translator, loading the region if needed.
        /// </summary>
        public RegionL10n ForRegion(string region)
        {
            GetRegionDataForLookup(region);
            return new RegionL10n(this, region);
        }

        /// <summary>
        /// Gets the loaded data for a region, loading it when needed.
        /// </summary>
        public L10nRegionData ForRegionData(string region)
        {
            return GetRegionDataForLookup(region);
        }

        /// <summary>
        /// Reloads the main region if one exists.
        /// </summary>
        public bool ReloadMain()
        {
            if (!HasMainRegion)
            {
                return false;
            }

            MainData.Reload();
            return true;
        }

        /// <summary>
        /// Reloads the requested region when it is currently available.
        /// </summary>
        public bool ReloadRegion(string region)
        {
            region ??= string.Empty;
            if (IsFallbackRegion(region))
            {
                fallback.Reload();
                return true;
            }

            if (!loadedRegions.TryGetValue(region, out var data))
            {
                return false;
            }

            data.Reload();
            return true;
        }

        /// <summary>
        /// Reloads fallback and every loaded non-fallback region.
        /// </summary>
        public void ReloadAllLoadedRegions()
        {
            fallback.Reload();
            foreach (var data in loadedRegions.Values)
            {
                data.Reload();
            }
        }

        /// <summary>
        /// Determines whether a region is already loaded or is the shared fallback.
        /// </summary>
        public bool IsRegionLoaded(string region)
        {
            region ??= string.Empty;
            return IsFallbackRegion(region) || loadedRegions.ContainsKey(region);
        }

        /// <summary>
        /// Determines whether a region is the configured shared fallback.
        /// </summary>
        public bool IsFallbackRegion(string region)
        {
            return string.Equals(region ?? string.Empty, FallbackRegion, StringComparison.Ordinal);
        }

        #endregion

        #region Raw Content

        /// <summary>
        /// Gets raw content through the current main region, falling back to the shared fallback.
        /// </summary>
        public string GetMainRawContent(string key) => CurrentData?.GetRawContent(key, fallback);

        /// <summary>
        /// Gets raw content through the current main region, falling back to the shared fallback.
        /// </summary>
        public string GetMainRawContent(Key key) => CurrentData?.GetRawContent(key, fallback);

        /// <summary>
        /// Gets raw content through a specific region, falling back to the shared fallback.
        /// </summary>
        public string GetRawContent(string region, string key) => GetRegionDataForLookup(region).GetRawContent(key, fallback);

        /// <summary>
        /// Gets raw content through a specific region, falling back to the shared fallback.
        /// </summary>
        public string GetRawContent(string region, Key key) => GetRegionDataForLookup(region).GetRawContent(key, fallback);

        #endregion

        #region Key Lookup & Overrides

        /// <summary>
        /// Checks whether the current main region contains a key.
        /// </summary>
        public bool ContainsMain(string key, bool includeFallback) => CurrentData?.Contains(key, includeFallback ? fallback : null) == true;

        /// <summary>
        /// Checks whether the current main region contains a key.
        /// </summary>
        public bool ContainsMain(Key key, bool includeFallback) => CurrentData?.Contains(key, includeFallback ? fallback : null) == true;

        /// <summary>
        /// Checks whether a specific region contains a key.
        /// </summary>
        public bool Contains(string region, string key, bool includeFallback) => GetRegionDataForLookup(region).Contains(key, includeFallback ? fallback : null);

        /// <summary>
        /// Checks whether a specific region contains a key.
        /// </summary>
        public bool Contains(string region, Key key, bool includeFallback) => GetRegionDataForLookup(region).Contains(key, includeFallback ? fallback : null);

        /// <summary>
        /// Writes an in-memory override into the current main region.
        /// </summary>
        public bool WriteMain(string key, string value)
        {
            var data = MainData;
            if (data == null) return false;
            data.Write(key, value);
            return true;
        }

        /// <summary>
        /// Writes an in-memory override into the current main region.
        /// </summary>
        public bool WriteMain(Key key, string value)
        {
            var data = MainData;
            if (data == null) return false;
            data.Write(key, value);
            return true;
        }

        /// <summary>
        /// Writes an in-memory override into a specific loaded region.
        /// </summary>
        public bool Write(string region, string key, string value)
        {
            var data = GetRegionDataForLookup(region);
            if (data == fallback) return false;
            data.Write(key, value);
            return true;
        }

        /// <summary>
        /// Writes an in-memory override into a specific loaded region.
        /// </summary>
        public bool Write(string region, Key key, string value)
        {
            var data = GetRegionDataForLookup(region);
            if (data == fallback) return false;
            data.Write(key, value);
            return true;
        }

        /// <summary>
        /// Gets matching key options from the current main region.
        /// </summary>
        public bool OptionOfMain(string partialKey, out string[] result, bool firstLevelOnly) => CurrentData.OptionOf(partialKey, out result, firstLevelOnly);

        /// <summary>
        /// Gets matching key options from the current main region.
        /// </summary>
        public bool OptionOfMain(Key partialKey, out string[] result, bool firstLevelOnly) => CurrentData.OptionOf(partialKey, out result, firstLevelOnly);

        /// <summary>
        /// Copies matching key options from the current main region.
        /// </summary>
        public bool CopyOptionsMain(string partialKey, List<string> strings, bool firstLevelOnly) => CurrentData.CopyOptions(partialKey, strings, firstLevelOnly);

        /// <summary>
        /// Copies matching key options from the current main region.
        /// </summary>
        public bool CopyOptionsMain(Key partialKey, List<string> strings, bool firstLevelOnly) => CurrentData.CopyOptions(partialKey, strings, firstLevelOnly);

        #endregion

        #region Private Helpers

        private void LoadFallback()
        {
            if (string.IsNullOrEmpty(FallbackRegion))
            {
                fallback = null;
                Debug.LogException(new NullReferenceException("Default localization region is not configured."));
                return;
            }

            fallback = new L10nRegionData(manager, FallbackRegion);
        }

        private L10nRegionData EnsureRegionLoaded(string region)
        {
            if (string.IsNullOrEmpty(region))
            {
                throw new ArgumentException("Localization region cannot be empty.", nameof(region));
            }
            if (loadedRegions.TryGetValue(region, out var data))
            {
                return data;
            }

            data = new L10nRegionData(manager, region);
            loadedRegions.Add(region, data);
            return data;
        }

        private L10nRegionData GetRegionDataForLookup(string region)
        {
            region ??= string.Empty;
            if (IsFallbackRegion(region) || string.IsNullOrEmpty(region))
            {
                return fallback;
            }

            return EnsureRegionLoaded(region);
        }

        #endregion
    }
}
