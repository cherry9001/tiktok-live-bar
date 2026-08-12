#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace TikTokLiveGame.Editor
{
    public sealed class DjModelImporter : AssetPostprocessor
    {
        private bool IsDjModel => assetPath.StartsWith("Assets/Resources/DJ/") && assetPath.EndsWith(".fbx");

        private void OnPreprocessModel()
        {
            if (!IsDjModel) return;
            ModelImporter importer = (ModelImporter)assetImporter;
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.importAnimation = true;
            importer.importCameras = false;
            importer.importLights = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
        }

        private void OnPreprocessAnimation()
        {
            if (!IsDjModel) return;
            ModelImporter importer = (ModelImporter)assetImporter;
            ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
            for (int index = 0; index < clips.Length; index++)
            {
                clips[index].name = "DJ Performance";
                clips[index].loopTime = true;
                clips[index].loopPose = true;
                clips[index].keepOriginalPositionXZ = true;
                clips[index].keepOriginalPositionY = true;
                clips[index].keepOriginalOrientation = true;
            }
            importer.clipAnimations = clips;
        }

        private void OnPostprocessModel(GameObject model)
        {
            if (!IsDjModel) return;
            foreach (Renderer modelRenderer in model.GetComponentsInChildren<Renderer>(true))
            {
                modelRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                modelRenderer.receiveShadows = true;
            }
        }
    }
}
#endif
