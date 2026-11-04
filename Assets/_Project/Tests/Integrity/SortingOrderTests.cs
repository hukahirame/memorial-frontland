using System;
using System.Collections.Generic;
using System.Linq;
using MemorialFloor.Editor;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace MemorialFloor.Tests.Integrity
{
    /// <summary>
    /// 根源3の透ける物の描く順が、プレイヤーと敵（DynamicLayer）と同じ決め方になっているかを見る。
    /// 決め方を変えたのに組み直していない、組んだ物の順を手で変えた、を拾う。
    ///
    /// シーンはプレビューで開く。編集中のシーンと、開いているシーンの並びに触れないため
    /// </summary>
    public class SortingOrderTests
    {
        /// <summary>z が 1 増えるごとの描く順。DynamicLayer と同じ</summary>
        private const float OrderPerDepth = -100f;

        /// <summary>順は部品ごとに真ん中の奥行きで決まる。1 つの部品の草は、真ん中から奥行き 1 より離れていない</summary>
        private const float Tolerance = 100f;

        private static readonly string[] Surfaces = { "MallWaterSurface", "MallShallowSurface" };

        /// <summary>順が外れる主な訳。見た目を試すシーンを組むと部品の prefab が作り直され、根源3の上書きが行き先を失う</summary>
        private const string Rebuild = "見た目を試すシーンを組み直したあとで、根源3を組み直していないかもしれない（Tools/Roots/Build Root3）";

        [Test]
        public void 根源3の木と草は置いた奥行きに合った順で描く()
        {
            InRoot3(renderers =>
            {
                List<SpriteRenderer> sprites = renderers.OfType<SpriteRenderer>().ToList();
                Assert.IsNotEmpty(sprites, "根源3に木も草も無い。下の突き合わせが空回りする");

                foreach (SpriteRenderer sprite in sprites)
                {
                    Assert.AreEqual(sprite.transform.position.z * OrderPerDepth, sprite.sortingOrder, Tolerance,
                        sprite.name + " " + sprite.transform.position + " の描く順が奥行きに合わない。" + Rebuild);
                }
            });
        }

        [Test]
        public void 根源3の水面はほかのどの透ける物よりも先に描く()
        {
            InRoot3(renderers =>
            {
                List<Renderer> seeThrough = renderers
                    .Where(shown => shown.sharedMaterial != null && shown.sharedMaterial.renderQueue > (int)RenderQueue.GeometryLast)
                    .ToList();
                List<Renderer> water = seeThrough.Where(IsWater).ToList();
                Assert.IsNotEmpty(water, "根源3に水面が無い。下の突き合わせが空回りする");

                int first = seeThrough.Where(shown => !IsWater(shown)).Min(shown => shown.sortingOrder);
                foreach (Renderer surface in water)
                {
                    Assert.Less(surface.sortingOrder, first, surface.name + " " + surface.transform.position + " の水面より先に描く透ける物がある。" + Rebuild);
                }
            });
        }

        private static bool IsWater(Renderer shown)
        {
            return Array.IndexOf(Surfaces, shown.sharedMaterial.name) >= 0;
        }

        private static void InRoot3(Action<Renderer[]> check)
        {
            Scene scene = EditorSceneManager.OpenPreviewScene(Root3Builder.ScenePath);

            try
            {
                check(scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Renderer>(true)).ToArray());
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }
    }
}
