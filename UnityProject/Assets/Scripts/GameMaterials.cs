using UnityEngine;
using UnityEngine.Rendering;

namespace TikTokLiveGame
{
    public static class GameMaterials
    {
        private static Material clubTemplate;
        private static Material litTemplate;
        private static Material spriteTemplate;
        private static Material circularAvatarTemplate;

        public static Material Club(Color color, Color emission)
        {
            clubTemplate ??= Resources.Load<Material>("Materials/ClubColor");
            Material material = new(clubTemplate);
            material.SetColor("_Color", color);
            material.SetColor("_EmissionColor", emission);
            return material;
        }

        public static Material Lit(Color color, float metallic, float smoothness, Color emission, float detailStrength = 0f, float detailScale = 8f)
        {
            litTemplate ??= Resources.Load<Material>("Materials/ClubLit");
            Material material = new(litTemplate);
            material.SetColor("_Color", color);
            material.SetFloat("_Metallic", Mathf.Clamp01(metallic));
            material.SetFloat("_Glossiness", Mathf.Clamp01(smoothness));
            material.SetColor("_EmissionColor", emission);
            material.SetFloat("_DetailStrength", Mathf.Clamp(detailStrength, 0f, 0.2f));
            material.SetFloat("_DetailScale", Mathf.Max(0.1f, detailScale));
            return material;
        }

        public static Material Sprite()
        {
            spriteTemplate ??= Resources.Load<Material>("Materials/CharacterSprite");
            Material material = new(spriteTemplate);
            material.SetFloat("_ZTest", (float)CompareFunction.Always);
            material.renderQueue = 3200;
            return material;
        }

        public static Material BackgroundSprite()
        {
            spriteTemplate ??= Resources.Load<Material>("Materials/CharacterSprite");
            Material material = new(spriteTemplate);
            material.SetFloat("_ZTest", (float)CompareFunction.LessEqual);
            material.renderQueue = 2990;
            return material;
        }

        public static Material CircularAvatar()
        {
            circularAvatarTemplate ??= Resources.Load<Material>("Materials/CircularAvatar");
            Material material = new(circularAvatarTemplate);
            material.SetFloat("_ZTest", (float)CompareFunction.Always);
            material.renderQueue = 3210;
            return material;
        }
    }
}
