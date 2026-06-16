using UnityEngine;

namespace KspWebMap
{
    /// <summary>
    /// Picks the ScaledSpace surface renderer/material for texture export.
    /// Prefers RSS/Kopernicus ScaledMesh2 cubemap bodies over atmosphere/cloud layers.
    /// </summary>
    public static class ScaledBodyMaterialResolver
    {
        public static bool TryResolve(GameObject scaledBody, out Material material)
        {
            material = null;

            if (scaledBody == null)
            {
                return false;
            }

            MeshRenderer[] renderers = scaledBody.GetComponentsInChildren<MeshRenderer>(true);

            if (renderers == null || renderers.Length == 0)
            {
                return false;
            }

            for (int i = 0; i < renderers.Length; i++)
            {
                MeshRenderer renderer = renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                Material candidate = renderer.sharedMaterial;
                if (candidate != null && ScaledMesh2CubemapExporter.HasCompleteCubemapFaces(candidate))
                {
                    material = candidate;
                    return true;
                }
            }

            Material onDemandMaterial;
            if (KopernicusOnDemandTextureLoader.TryResolveOnDemandMaterial(scaledBody, out onDemandMaterial))
            {
                material = onDemandMaterial;
                return true;
            }

            for (int i = 0; i < renderers.Length; i++)
            {
                MeshRenderer renderer = renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                Material candidate = renderer.sharedMaterial;
                if (candidate == null)
                {
                    continue;
                }

                if (HasAssignedStockAlbedo(candidate))
                {
                    material = candidate;
                    return true;
                }
            }

            for (int i = 0; i < renderers.Length; i++)
            {
                MeshRenderer renderer = renderers[i];
                if (renderer != null && renderer.sharedMaterial != null)
                {
                    material = renderer.sharedMaterial;
                    return true;
                }
            }

            return false;
        }

        private static bool HasAssignedStockAlbedo(Material material)
        {
            if (material.HasProperty("_MainTex") && material.GetTexture("_MainTex") != null)
            {
                return true;
            }

            if (material.HasProperty("_ColorMap") && material.GetTexture("_ColorMap") != null)
            {
                return true;
            }

            return material.mainTexture != null;
        }
    }
}
