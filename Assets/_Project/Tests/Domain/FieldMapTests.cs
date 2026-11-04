using System;
using System.Collections.Generic;
using System.Linq;
using MemorialFloor.Domain;
using NUnit.Framework;

namespace MemorialFloor.Domain.Tests
{
    public class FieldMapTests
    {
        /// <summary>2 行 3 列。上面 0 の層に 3 つ、上面 1.5 の層に 1 つ</summary>
        private const string Small =
            "cell 3\n" +
            "origin -3 1.5\n" +
            "\n" +
            "top 1.5\n" +
            "b..\n" +
            "...\n" +
            "\n" +
            "top 0\n" +
            "dd.\n" +
            "..r\n";

        private static FieldBlock At(FieldMap map, int row, int column, char letter)
        {
            return map.Blocks.Single(b => b.Row == row && b.Column == column && b.Letter == letter);
        }

        [Test]
        public void 層の文字を上面の高さで置いた_Cube_に変える()
        {
            FieldMap map = FieldMap.Parse(Small);

            FieldBlock block = At(map, 0, 1, 'd');
            Assert.AreEqual(0f, block.X);
            Assert.AreEqual(-1.5f, block.Y, "上面が 0 なら中心は一辺の半分だけ下");
            Assert.AreEqual(1.5f, block.Z);
        }

        [Test]
        public void 右の列ほど_x_が大きく下の行ほど手前に置く()
        {
            FieldMap map = FieldMap.Parse(Small);

            FieldBlock block = At(map, 1, 2, 'r');
            Assert.AreEqual(3f, block.X);
            Assert.AreEqual(-1.5f, block.Z);
        }

        [Test]
        public void 点のマスには何も置かない()
        {
            FieldMap map = FieldMap.Parse(Small);

            Assert.AreEqual(4, map.Blocks.Count);
            Assert.AreEqual(2, map.Rows);
            Assert.AreEqual(3, map.Columns);
        }

        [Test]
        public void 上の層は上面の高さだけ持ち上がる()
        {
            FieldBlock block = At(FieldMap.Parse(Small), 0, 0, 'b');

            Assert.AreEqual(0f, block.Y);
        }

        [Test]
        public void 同じ高さの層を重ねると同じ場所に_Cube_が2つ並ぶ()
        {
            string text = "cell 3\norigin 0 0\ntop 0\nd\n\ntop 0\nb\n";
            List<FieldBlock> blocks = FieldMap.Parse(text).Blocks.ToList();

            Assert.AreEqual(2, blocks.Count);
            Assert.AreEqual(blocks[0].X, blocks[1].X);
            Assert.AreEqual(blocks[0].Y, blocks[1].Y);
            Assert.AreEqual(blocks[0].Z, blocks[1].Z);
        }

        [Test]
        public void マスに乗らない_Cube_は中心と向きをそのまま持つ()
        {
            string text = "cell 3\npiece d 6.5 -2.05 -5.55 60 0 0\npiece b 1 2 3\n";
            FieldMap map = FieldMap.Parse(text);

            FieldBlock turned = map.Blocks[0];
            Assert.AreEqual('d', turned.Letter);
            Assert.AreEqual(6.5f, turned.X);
            Assert.AreEqual(-2.05f, turned.Y);
            Assert.AreEqual(-5.55f, turned.Z);
            Assert.AreEqual(60f, turned.RotationX);
            Assert.AreEqual(-1, turned.Row, "マスに乗らないので行は無い");

            FieldBlock plain = map.Blocks[1];
            Assert.AreEqual(0f, plain.RotationX, "向きを書かなければ回さない");
            Assert.AreEqual(0f, plain.RotationY);
            Assert.AreEqual(0f, plain.RotationZ);
        }

        [Test]
        public void 書かれていた行を覚えている()
        {
            FieldMap map = FieldMap.Parse(Small);

            Assert.AreEqual(5, At(map, 0, 0, 'b').Line);
            Assert.AreEqual(10, At(map, 1, 2, 'r').Line);
        }

        [Test]
        public void 注釈と空行と_BOM_と_CRLF_は読み飛ばす()
        {
            string text = "﻿# 注釈\r\ncell 3   # 一辺\r\n\r\norigin 0 0\r\ntop 0\r\nd. # 行の後ろの注釈\r\n";
            FieldMap map = FieldMap.Parse(text);

            Assert.AreEqual(1, map.Blocks.Count);
            Assert.AreEqual(2, map.Columns);
        }

        [Test]
        public void 幅のそろわない行は何行目かを添えて断る()
        {
            string text = "cell 3\norigin 0 0\ntop 0\nddd\ndd\n";
            FormatException error = Assert.Throws<FormatException>(() => FieldMap.Parse(text));

            StringAssert.StartsWith("5 行目", error.Message);
        }

        [Test]
        public void 層ごとに行の数が違えば断る()
        {
            string text = "cell 3\norigin 0 0\ntop 0\nd\nd\n\ntop 1.5\nd\n";
            FormatException error = Assert.Throws<FormatException>(() => FieldMap.Parse(text));

            StringAssert.StartsWith("7 行目", error.Message);
        }

        [Test]
        public void 層の外にある行は断る()
        {
            Assert.Throws<FormatException>(() => FieldMap.Parse("cell 3\norigin 0 0\nddd\n"));
        }

        [Test]
        public void 行の無い層は断る()
        {
            Assert.Throws<FormatException>(() => FieldMap.Parse("cell 3\norigin 0 0\ntop 0\n\ntop 1.5\nd\n"));
        }

        [Test]
        public void 知らない語と数でない値は断る()
        {
            Assert.Throws<FormatException>(() => FieldMap.Parse("cell 3\nwall 1 2\n"));
            Assert.Throws<FormatException>(() => FieldMap.Parse("cell three\n"));
        }

        [Test]
        public void 値を書き忘れた語は層の行と取り違えずに断る()
        {
            FormatException error = Assert.Throws<FormatException>(() => FieldMap.Parse("cell 3\norigin\n"));

            StringAssert.Contains("origin", error.Message);
        }

        [Test]
        public void マスの大きさと原点が無いまま層を書けば断る()
        {
            Assert.Throws<FormatException>(() => FieldMap.Parse("origin 0 0\ntop 0\nd\n"));
            Assert.Throws<FormatException>(() => FieldMap.Parse("cell 3\ntop 0\nd\n"));
            Assert.Throws<FormatException>(() => FieldMap.Parse(""));
        }

        [Test]
        public void マスの大きさが_0_以下なら断る()
        {
            Assert.Throws<FormatException>(() => FieldMap.Parse("cell 0\n"));
        }

        [Test]
        public void 点や2文字を記号にした_piece_は断る()
        {
            Assert.Throws<FormatException>(() => FieldMap.Parse("cell 3\npiece . 0 0 0\n"));
            Assert.Throws<FormatException>(() => FieldMap.Parse("cell 3\npiece dd 0 0 0\n"));
            Assert.Throws<FormatException>(() => FieldMap.Parse("cell 3\npiece d 0 0\n"));
        }
    }
}
