using System;
using System.Reflection;
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
    }
}
