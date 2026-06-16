using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace KspWebMap
{
    /// <summary>
    /// RSS/Kopernicus bodies (gas giants, moons, etc.) store ScaledSpace albedo OnDemand.
    /// Textures are not assigned until LoadTextures() — same approach as SCANsat.
    /// </summary>
    public static class KopernicusOnDemandTextureLoader
    {
        private const string LogPrefix = "[KspWebMap]";
        private const string KopernicusAssemblyName = "Kopernicus";
        private const string ScaledSpaceOnDemandTypeName = "Kopernicus.OnDemand.ScaledSpaceOnDemand";
        private const string OnDemandStorageTypeName = "Kopernicus.OnDemand.OnDemandStorage";

        private static Type _onDemandType;
        private static MethodInfo _loadTexturesMethod;
        private static MethodInfo _unloadTexturesMethod;
        private static FieldInfo _isLoadedField;
        private static FieldInfo _scaledRendererField;
        private static PropertyInfo _entriesProperty;
        private static FieldInfo _entryKeysField;
        private static FieldInfo _entryPathsField;
        private static PropertyInfo _entryKeyProperty;
        private static PropertyInfo _entryPathProperty;
        private static MethodInfo _enableBodyMethod;
        private static MethodInfo _disableBodyMethod;
        private static bool _initialized;
        private static bool _available;

        public static bool IsAvailable
        {
            get
            {
                EnsureInitialized();
                return _available;
            }
        }

        public static bool HasOnDemandComponent(GameObject scaledBody)
        {
            EnsureInitialized();
            return _available && FindOnDemandComponent(scaledBody) != null;
        }

        /// <summary>
        /// Kopernicus OnDemand Entries paths + source file sizes — stable across KSP sessions.
        /// </summary>
        public static bool TryGetStableSourceFingerprint(GameObject scaledBody, out string fingerprint)
        {
            fingerprint = null;
            EnsureInitialized();

            if (!_available || scaledBody == null)
            {
                return false;
            }

            MonoBehaviour onDemand = FindOnDemandComponent(scaledBody);
            if (onDemand == null)
            {
                return false;
            }

            List<KeyValuePair<string, string>> entries = CollectOnDemandEntries(onDemand);
            if (entries == null || entries.Count == 0)
            {
                return false;
            }

            entries.Sort(CompareEntryKeys);

            StringBuilder builder = new StringBuilder(128);
            builder.Append("ondemand");

            MeshRenderer renderer = GetScaledRenderer(onDemand);
            Material material = renderer != null ? renderer.sharedMaterial : null;
            Shader shader = material != null ? material.shader : null;
            builder.Append("|shader=").Append(shader != null ? shader.name : "null");

            for (int i = 0; i < entries.Count; i++)
            {
                KeyValuePair<string, string> entry = entries[i];
                builder.Append('|');
                builder.Append(entry.Key);
                builder.Append('=');
                builder.Append(entry.Value);
                AppendSourceFileLength(builder, entry.Value);
            }

            fingerprint = builder.ToString();
            return true;
        }

        public static bool TryResolveOnDemandMaterial(GameObject scaledBody, out Material material)
        {
            material = null;
            EnsureInitialized();

            if (!_available || scaledBody == null)
            {
                return false;
            }

            MonoBehaviour onDemand = FindOnDemandComponent(scaledBody);
            if (onDemand == null)
            {
                return false;
            }

            MeshRenderer renderer = GetScaledRenderer(onDemand);
            if (renderer == null || renderer.sharedMaterial == null)
            {
                return false;
            }

            material = renderer.sharedMaterial;
            return true;
        }

        public static bool TryCaptureAlbedo(
            GameObject scaledBody,
            string bodyName,
            Func<Material, Texture2D> captureFromMaterial,
            out Texture2D captured,
            out Material loadedMaterial)
        {
            captured = null;
            loadedMaterial = null;
            EnsureInitialized();

            if (!_available || scaledBody == null || captureFromMaterial == null)
            {
                return false;
            }

            bool enableBodyCalled = false;
            bool scaledBodyWasActive = scaledBody.activeSelf;

            try
            {
                if (!string.IsNullOrEmpty(bodyName))
                {
                    enableBodyCalled = TryEnableBody(bodyName);
                }

                if (!scaledBody.activeSelf)
                {
                    scaledBody.SetActive(true);
                }

                MonoBehaviour onDemand = FindOnDemandComponent(scaledBody);
                if (onDemand == null)
                {
                    return false;
                }

                bool wasLoaded = GetIsLoaded(onDemand);

                try
                {
                    if (!wasLoaded)
                    {
                        _loadTexturesMethod.Invoke(onDemand, null);
                    }

                    MeshRenderer renderer = GetScaledRenderer(onDemand);
                    if (renderer == null || renderer.sharedMaterial == null)
                    {
                        return false;
                    }

                    loadedMaterial = renderer.sharedMaterial;
                    captured = captureFromMaterial(loadedMaterial);
                    return captured != null;
                }
                finally
                {
                    if (!wasLoaded)
                    {
                        try
                        {
                            _unloadTexturesMethod.Invoke(onDemand, null);
                        }
                        catch
                        {
                            // Best-effort unload after export.
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning(LogPrefix + " Kopernicus OnDemand capture failed for "
                    + (bodyName ?? scaledBody.name) + ": " + ex.Message);
                return false;
            }
            finally
            {
                if (!scaledBodyWasActive)
                {
                    scaledBody.SetActive(false);
                }

                if (enableBodyCalled && !string.IsNullOrEmpty(bodyName))
                {
                    TryDisableBody(bodyName);
                }
            }
        }

        private static bool TryEnableBody(string bodyName)
        {
            if (_enableBodyMethod == null || string.IsNullOrEmpty(bodyName))
            {
                return false;
            }

            try
            {
                _enableBodyMethod.Invoke(null, new object[] { bodyName });
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void TryDisableBody(string bodyName)
        {
            if (_disableBodyMethod == null || string.IsNullOrEmpty(bodyName))
            {
                return;
            }

            try
            {
                _disableBodyMethod.Invoke(null, new object[] { bodyName });
            }
            catch
            {
                // Best-effort cleanup.
            }
        }

        private static void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;

            try
            {
                for (int i = 0; i < AssemblyLoader.loadedAssemblies.Count; i++)
                {
                    AssemblyLoader.LoadedAssembly loaded = AssemblyLoader.loadedAssemblies[i];
                    if (loaded == null || loaded.assembly == null)
                    {
                        continue;
                    }

                    if (!string.Equals(
                            loaded.assembly.GetName().Name,
                            KopernicusAssemblyName,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    _onDemandType = loaded.assembly.GetType(ScaledSpaceOnDemandTypeName);
                    Type storageType = loaded.assembly.GetType(OnDemandStorageTypeName);
                    if (storageType != null)
                    {
                        _enableBodyMethod = storageType.GetMethod(
                            "EnableBody",
                            BindingFlags.Static | BindingFlags.Public);
                        _disableBodyMethod = storageType.GetMethod(
                            "DisableBody",
                            BindingFlags.Static | BindingFlags.Public);
                    }

                    break;
                }

                if (_onDemandType == null)
                {
                    return;
                }

                _loadTexturesMethod = _onDemandType.GetMethod(
                    "LoadTextures",
                    BindingFlags.Instance | BindingFlags.Public);
                _unloadTexturesMethod = _onDemandType.GetMethod(
                    "UnloadTextures",
                    BindingFlags.Instance | BindingFlags.Public);
                _isLoadedField = _onDemandType.GetField(
                    "isLoaded",
                    BindingFlags.Instance | BindingFlags.Public);
                _scaledRendererField = _onDemandType.GetField(
                    "scaledRenderer",
                    BindingFlags.Instance | BindingFlags.Public);
                _entriesProperty = _onDemandType.GetProperty(
                    "Entries",
                    BindingFlags.Instance | BindingFlags.Public);
                _entryKeysField = _onDemandType.GetField(
                    "entryKeys",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                _entryPathsField = _onDemandType.GetField(
                    "entryPaths",
                    BindingFlags.Instance | BindingFlags.NonPublic);

                Type entryType = _onDemandType.Assembly.GetType("Kopernicus.OnDemand.OnDemandTextureEntry");
                if (entryType != null)
                {
                    _entryKeyProperty = entryType.GetProperty(
                        "Key",
                        BindingFlags.Instance | BindingFlags.Public);
                    _entryPathProperty = entryType.GetProperty(
                        "Path",
                        BindingFlags.Instance | BindingFlags.Public);
                }

                _available = _loadTexturesMethod != null
                    && _unloadTexturesMethod != null
                    && _isLoadedField != null;
            }
            catch
            {
                _available = false;
            }
        }

        private static MonoBehaviour FindOnDemandComponent(GameObject scaledBody)
        {
            MonoBehaviour[] behaviours = scaledBody.GetComponentsInChildren<MonoBehaviour>(true);

            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour != null && _onDemandType.IsInstanceOfType(behaviour))
                {
                    return behaviour;
                }
            }

            return null;
        }

        private static bool GetIsLoaded(MonoBehaviour onDemand)
        {
            if (_isLoadedField == null || onDemand == null)
            {
                return false;
            }

            object value = _isLoadedField.GetValue(onDemand);
            return value is bool && (bool)value;
        }

        private static MeshRenderer GetScaledRenderer(MonoBehaviour onDemand)
        {
            if (_scaledRendererField != null)
            {
                object value = _scaledRendererField.GetValue(onDemand);
                MeshRenderer renderer = value as MeshRenderer;
                if (renderer != null)
                {
                    return renderer;
                }
            }

            return onDemand.GetComponent<MeshRenderer>();
        }

        private static int CompareEntryKeys(KeyValuePair<string, string> left, KeyValuePair<string, string> right)
        {
            return string.Compare(left.Key, right.Key, StringComparison.Ordinal);
        }

        private static List<KeyValuePair<string, string>> CollectOnDemandEntries(MonoBehaviour onDemand)
        {
            List<KeyValuePair<string, string>> entries = new List<KeyValuePair<string, string>>();

            if (_entriesProperty != null)
            {
                object value = _entriesProperty.GetValue(onDemand, null);
                IList list = value as IList;
                if (list != null && list.Count > 0)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        KeyValuePair<string, string> entry = ReadOnDemandEntry(list[i]);
                        if (!string.IsNullOrEmpty(entry.Key) && !string.IsNullOrEmpty(entry.Value))
                        {
                            entries.Add(entry);
                        }
                    }
                }
            }

            if (entries.Count == 0)
            {
                AppendSerializedEntries(onDemand, entries);
            }

            return entries;
        }

        private static void AppendSerializedEntries(
            MonoBehaviour onDemand,
            List<KeyValuePair<string, string>> entries)
        {
            if (_entryKeysField == null || _entryPathsField == null)
            {
                return;
            }

            IList keys = _entryKeysField.GetValue(onDemand) as IList;
            IList paths = _entryPathsField.GetValue(onDemand) as IList;
            if (keys == null || paths == null)
            {
                return;
            }

            int count = Math.Min(keys.Count, paths.Count);
            for (int i = 0; i < count; i++)
            {
                string key = keys[i] as string;
                string path = paths[i] as string;
                if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(path))
                {
                    entries.Add(new KeyValuePair<string, string>(key, path));
                }
            }
        }

        private static KeyValuePair<string, string> ReadOnDemandEntry(object entry)
        {
            if (entry == null)
            {
                return new KeyValuePair<string, string>(string.Empty, string.Empty);
            }

            if (_entryKeyProperty != null && _entryPathProperty != null)
            {
                string key = _entryKeyProperty.GetValue(entry, null) as string;
                string path = _entryPathProperty.GetValue(entry, null) as string;
                return new KeyValuePair<string, string>(key ?? string.Empty, path ?? string.Empty);
            }

            return new KeyValuePair<string, string>(string.Empty, string.Empty);
        }

        private static void AppendSourceFileLength(StringBuilder builder, string gameDataRelativePath)
        {
            if (string.IsNullOrEmpty(gameDataRelativePath))
            {
                return;
            }

            try
            {
                string normalized = gameDataRelativePath.Replace('/', Path.DirectorySeparatorChar);
                string fullPath = Path.Combine(KSPUtil.ApplicationRootPath, "GameData", normalized);
                if (File.Exists(fullPath))
                {
                    builder.Append(':').Append(new FileInfo(fullPath).Length);
                }
            }
            catch
            {
                // Best-effort invalidation signal only.
            }
        }
    }
}
