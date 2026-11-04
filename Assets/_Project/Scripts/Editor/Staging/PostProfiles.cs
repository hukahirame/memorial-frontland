using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace MemorialFloor.Editor
{
    /// <summary>後処理の設定の資産。場所の光と空気と、見た目を試すシーンが使う</summary>
    internal static class PostProfiles
    {
        /// <summary>在れば読み、無ければ作る。作り直すと GUID が変わり、範囲からの参照が切れる</summary>
        public static PostProcessProfile Load(string path)
        {
            PostProcessProfile profile = AssetDatabase.LoadAssetAtPath<PostProcessProfile>(path);
            if (profile != null) return profile;

            profile = ScriptableObject.CreateInstance<PostProcessProfile>();
            AssetDatabase.CreateAsset(profile, path);

            return profile;
        }

        /// <summary>効果を 1 つ、在れば書き換え、無ければ設定の資産の中に足す</summary>
        public static T Effect<T>(PostProcessProfile profile) where T : PostProcessEffectSettings
        {
            if (!profile.TryGetSettings(out T effect))
            {
                effect = ScriptableObject.CreateInstance<T>();
                effect.name = typeof(T).Name;
                effect.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
                AssetDatabase.AddObjectToAsset(effect, profile);
                profile.settings.Add(effect);
            }

            effect.enabled.Override(true);
            EditorUtility.SetDirty(effect);

            return effect;
        }
    }
}
