using UnityEngine;
using UnityEngine.SceneManagement;

public static class PerformanceBootstrap
{
    public struct RuntimeQualityProfile
    {
        public int QualityIndex;
        public int VSyncCount;
        public int PixelLightCount;
        public float ShadowDistance;
        public float LodBias;
        public float TreeDistance;
        public float BillboardDistance;
        public float DetailDistance;
        public float DetailDensity;
        public LightShadows AdditionalLightShadows;
        public LightRenderMode AdditionalLightRenderMode;
    }

    private static RuntimeQualityProfile currentProfile;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ApplySafePerformanceProfile()
    {
        ApplyQualityProfile(QualitySettings.GetQualityLevel());

        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    public static void ApplyQualityProfile(int requestedQualityIndex)
    {
        int safeIndex = Mathf.Clamp(requestedQualityIndex, 0, Mathf.Max(0, QualitySettings.names.Length - 1));
        currentProfile = BuildProfile(safeIndex);

        if (QualitySettings.GetQualityLevel() != currentProfile.QualityIndex)
        {
            QualitySettings.SetQualityLevel(currentProfile.QualityIndex, true);
        }

        QualitySettings.vSyncCount = currentProfile.VSyncCount;
        QualitySettings.shadowDistance = currentProfile.ShadowDistance;
        QualitySettings.lodBias = currentProfile.LodBias;
        QualitySettings.pixelLightCount = currentProfile.PixelLightCount;
        Application.targetFrameRate = 60;

        ApplySceneLevelOptimizations();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplySceneLevelOptimizations();
    }

    private static void ApplySceneLevelOptimizations()
    {
        Light[] lights = Object.FindObjectsOfType<Light>(true);
        for (int i = 0; i < lights.Length; i++)
        {
            Light light = lights[i];
            if (light == null)
            {
                continue;
            }

            if (light.type != LightType.Directional)
            {
                light.shadows = currentProfile.AdditionalLightShadows;
                light.renderMode = currentProfile.AdditionalLightRenderMode;
            }
        }

        Terrain[] terrains = Object.FindObjectsOfType<Terrain>(true);
        for (int i = 0; i < terrains.Length; i++)
        {
            Terrain terrain = terrains[i];
            if (terrain == null)
            {
                continue;
            }

            terrain.treeDistance = currentProfile.TreeDistance;
            terrain.treeBillboardDistance = currentProfile.BillboardDistance;
            terrain.detailObjectDistance = currentProfile.DetailDistance;
            terrain.detailObjectDensity = currentProfile.DetailDensity;
        }
    }

    private static RuntimeQualityProfile BuildProfile(int qualityIndex)
    {
        int clamped = Mathf.Clamp(qualityIndex, 0, Mathf.Max(0, QualitySettings.names.Length - 1));

        if (clamped <= 1)
        {
            return new RuntimeQualityProfile
            {
                QualityIndex = clamped,
                VSyncCount = 0,
                PixelLightCount = 1,
                ShadowDistance = 12f,
                LodBias = 0.35f,
                TreeDistance = 70f,
                BillboardDistance = 30f,
                DetailDistance = 10f,
                DetailDensity = 0.05f,
                AdditionalLightShadows = LightShadows.None,
                AdditionalLightRenderMode = LightRenderMode.ForceVertex
            };
        }

        if (clamped == 2)
        {
            return new RuntimeQualityProfile
            {
                QualityIndex = clamped,
                VSyncCount = 0,
                PixelLightCount = 1,
                ShadowDistance = 20f,
                LodBias = 0.5f,
                TreeDistance = 120f,
                BillboardDistance = 60f,
                DetailDistance = 20f,
                DetailDensity = 0.15f,
                AdditionalLightShadows = LightShadows.None,
                AdditionalLightRenderMode = LightRenderMode.ForceVertex
            };
        }

        if (clamped == 3)
        {
            return new RuntimeQualityProfile
            {
                QualityIndex = clamped,
                VSyncCount = 0,
                PixelLightCount = 2,
                ShadowDistance = 45f,
                LodBias = 0.8f,
                TreeDistance = 180f,
                BillboardDistance = 90f,
                DetailDistance = 40f,
                DetailDensity = 0.35f,
                AdditionalLightShadows = LightShadows.Hard,
                AdditionalLightRenderMode = LightRenderMode.Auto
            };
        }

        return new RuntimeQualityProfile
        {
            QualityIndex = clamped,
            VSyncCount = 1,
            PixelLightCount = 4,
            ShadowDistance = 90f,
            LodBias = 1f,
            TreeDistance = 260f,
            BillboardDistance = 140f,
            DetailDistance = 65f,
            DetailDensity = 0.6f,
            AdditionalLightShadows = LightShadows.Hard,
            AdditionalLightRenderMode = LightRenderMode.Auto
        };
    }
}
