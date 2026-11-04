using System.Collections.Generic;
using System.Linq;
using MemorialFloor.Domain;
using MemorialFloor.Editor;
using MemorialFloor.Game;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MemorialFloor.Tests.Integrity
{
    /// <summary>
    /// 地図と、場所のシーンに置いてある地形が食い違っていないかを見る。
    /// 地図を直したのに組み直していない、組んだ Cube を手で動かした、を拾う。
    ///
    /// シーンはプレビューで開く。編集中のシーンと、開いているシーンの並びに触れないため
    /// </summary>
    public class FieldTerrainTests
    {
        /// <summary>位置と大きさの許し。地図は小数 4 桁までしか書かない</summary>
        private const float Tolerance = 0.001f;

        /// <summary>向きの許し（度）</summary>
        private const float AngleTolerance = 0.01f;

        /// <summary>報せる件数の上限。全部出すとメッセージが読めなくなる</summary>
        private const int MaxReported = 20;

        private static IEnumerable<string> Maps()
        {
            return FieldMaps.All();
        }

        [Test]
        public void 地図が1つ以上ある()
        {
            Assert.IsNotEmpty(Maps(), FieldMaps.Folder + " に地図が無い。下の突き合わせが空回りする");
        }

        [TestCaseSource(nameof(Maps))]
        public void 地図から組む地形と場所のシーンの地形が一致する(string mapPath)
        {
            FieldMap map = FieldMaps.Load(mapPath);
            FieldPalette palette = FieldMaps.LoadPalette();
            List<Shape> expected = map.Blocks.Select(block => Expected(block, map.Cell, palette, mapPath)).ToList();

            Scene scene = EditorSceneManager.OpenPreviewScene(FieldMaps.SceneOf(mapPath));

            try
            {
                List<Shape> actual = FieldMaps.Cubes(scene).Select(Actual).ToList();
                List<string> missing = new List<string>();

                foreach (Shape shape in expected)
                {
                    int match = actual.FindIndex(candidate => candidate.Matches(shape));

                    if (match < 0) missing.Add("地図にあってシーンに無い " + shape);
                    else actual.RemoveAt(match);
                }

                List<string> problems = missing.Concat(actual.Select(shape => "シーンにあって地図に無い " + shape)).ToList();

                Assert.IsEmpty(problems, mapPath + " と " + FieldMaps.SceneOf(mapPath) + " の地形が違う。" +
                               problems.Count + " 件\n" + string.Join("\n", problems.Take(MaxReported)));
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        private static Shape Expected(FieldBlock block, float cell, FieldPalette palette, string mapPath)
        {
            FieldPaletteEntry entry = palette.Find(block.Letter);
            Assert.IsNotNull(entry, mapPath + " の " + block.Line + " 行目の記号 " + block.Letter + " がパレットに無い");

            return new Shape(
                "line " + block.Line,
                new Vector3(block.X, block.Y, block.Z),
                Quaternion.Euler(block.RotationX, block.RotationY, block.RotationZ),
                Vector3.one * cell,
                entry.material,
                entry.colliderCenter,
                entry.colliderSize);
        }

        private static Shape Actual(GameObject cube)
        {
            BoxCollider box = cube.GetComponent<BoxCollider>();

            return new Shape(
                cube.name,
                cube.transform.position,
                cube.transform.rotation,
                cube.transform.lossyScale,
                cube.GetComponent<MeshRenderer>().sharedMaterial,
                box != null ? box.center : Vector3.zero,
                box != null ? box.size : Vector3.zero);
        }

        /// <summary>見た目と当たりを決めるもの。どちらから来たかを名前に持つ</summary>
        private sealed class Shape
        {
            private readonly string _name;
            private readonly Vector3 _position;
            private readonly Quaternion _rotation;
            private readonly Vector3 _scale;
            private readonly Material _material;
            private readonly Vector3 _colliderCenter;
            private readonly Vector3 _colliderSize;

            public Shape(string name, Vector3 position, Quaternion rotation, Vector3 scale, Material material,
                         Vector3 colliderCenter, Vector3 colliderSize)
            {
                _name = name;
                _position = position;
                _rotation = rotation;
                _scale = scale;
                _material = material;
                _colliderCenter = colliderCenter;
                _colliderSize = colliderSize;
            }

            public bool Matches(Shape other)
            {
                return Near(_position, other._position)
                    && Quaternion.Angle(_rotation, other._rotation) < AngleTolerance
                    && Near(_scale, other._scale)
                    && _material == other._material
                    && Near(_colliderCenter, other._colliderCenter)
                    && Near(_colliderSize, other._colliderSize);
            }

            public override string ToString()
            {
                return _name + " " + _position.ToString("F3") + " 向き " + _rotation.eulerAngles.ToString("F1") +
                       " 大きさ " + _scale.ToString("F2") + " 素材 " + (_material != null ? _material.name : "無し") +
                       " 当たり " + _colliderCenter.ToString("F2") + " " + _colliderSize.ToString("F2");
            }

            private static bool Near(Vector3 a, Vector3 b)
            {
                return (a - b).sqrMagnitude < Tolerance * Tolerance;
            }
        }
    }
}
