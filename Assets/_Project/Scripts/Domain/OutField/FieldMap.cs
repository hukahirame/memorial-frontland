using System;
using System.Collections.Generic;
using System.Globalization;

namespace MemorialFloor.Domain
{
    /// <summary>地図に書かれた Cube 1 つ。位置は中心、向きは度</summary>
    public readonly struct FieldBlock
    {
        public FieldBlock(char letter, float x, float y, float z,
                          float rotationX, float rotationY, float rotationZ,
                          int row, int column, int line)
        {
            Letter = letter;
            X = x;
            Y = y;
            Z = z;
            RotationX = rotationX;
            RotationY = rotationY;
            RotationZ = rotationZ;
            Row = row;
            Column = column;
            Line = line;
        }

        /// <summary>パレットの記号</summary>
        public char Letter { get; }

        public float X { get; }

        public float Y { get; }

        public float Z { get; }

        public float RotationX { get; }

        public float RotationY { get; }

        public float RotationZ { get; }

        /// <summary>マスの行。上の行ほど奥。マスに乗らない Cube は -1</summary>
        public int Row { get; }

        /// <summary>マスの列。左の列ほど x が小さい。マスに乗らない Cube は -1</summary>
        public int Column { get; }

        /// <summary>地図の何行目に書かれていたか。1 から数える</summary>
        public int Line { get; }
    }

    /// <summary>
    /// 地形の地図。1 文字を 1 マスとした層を、上面の高さごとに重ねる。
    /// マスに乗らない Cube は、中心と向きを 1 行ずつ書く。書き方は docs/systems/outfield.md の「地図」
    /// </summary>
    public sealed class FieldMap
    {
        /// <summary>何も置かないマス</summary>
        public const char Empty = '.';

        private readonly List<FieldBlock> _blocks;

        private FieldMap(float cell, float originX, float originZ, int rows, int columns, List<FieldBlock> blocks)
        {
            Cell = cell;
            OriginX = originX;
            OriginZ = originZ;
            Rows = rows;
            Columns = columns;
            _blocks = blocks;
        }

        /// <summary>マスの一辺。Cube の一辺でもある</summary>
        public float Cell { get; }

        /// <summary>左上のマスの中心の x</summary>
        public float OriginX { get; }

        /// <summary>左上のマスの中心の z</summary>
        public float OriginZ { get; }

        /// <summary>層の行の数。層が無ければ 0</summary>
        public int Rows { get; }

        /// <summary>層の列の数。層が無ければ 0</summary>
        public int Columns { get; }

        public IReadOnlyList<FieldBlock> Blocks
        {
            get { return _blocks; }
        }

        /// <summary>読む。書き方が違えば、何行目かを添えて FormatException を投げる</summary>
        public static FieldMap Parse(string text)
        {
            Reader reader = new Reader();
            string[] lines = (text ?? string.Empty).Replace("\r", "").Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                reader.Read(Strip(lines[i]), i + 1);
            }

            return reader.Finish();
        }

        /// <summary># から後ろは注釈。先頭の BOM と前後の空白も落とす</summary>
        private static string Strip(string line)
        {
            int comment = line.IndexOf('#');
            if (comment >= 0) line = line.Substring(0, comment);

            return line.Trim('﻿').Trim();
        }

        /// <summary>1 行ずつ読み進める。層の途中かどうかを持つ</summary>
        private sealed class Reader
        {
            private readonly List<FieldBlock> _blocks = new List<FieldBlock>();

            private float? _cell;
            private float? _originX;
            private float _originZ;
            private int _rows = -1;
            private int _columns = -1;

            private bool _inLayer;
            private float _top;
            private int _row;
            private int _layerLine;

            public void Read(string line, int number)
            {
                if (line.Length == 0)
                {
                    CloseLayer();
                    return;
                }

                // 空白を含まない行は層の 1 行。語の行は値を伴うので空白を含む
                if (line.IndexOf(' ') < 0 && line.IndexOf('\t') < 0 && !IsWord(line))
                {
                    ReadRow(line, number);
                    return;
                }

                CloseLayer();

                string[] words = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                switch (words[0])
                {
                    case "cell":
                        Expect(words, 2, number);
                        if (_cell.HasValue) throw Error(number, "cell が 2 度ある");

                        float cell = Number(words[1], number);
                        if (cell <= 0f) throw Error(number, "cell は 0 より大きくする");

                        _cell = cell;
                        break;

                    case "origin":
                        Expect(words, 3, number);
                        if (_originX.HasValue) throw Error(number, "origin が 2 度ある");

                        _originX = Number(words[1], number);
                        _originZ = Number(words[2], number);
                        break;

                    case "top":
                        Expect(words, 2, number);
                        if (!_cell.HasValue) throw Error(number, "top より前に cell が要る");
                        if (!_originX.HasValue) throw Error(number, "top より前に origin が要る");

                        _top = Number(words[1], number);
                        _inLayer = true;
                        _row = 0;
                        _layerLine = number;
                        break;

                    case "piece":
                        ReadPiece(words, number);
                        break;

                    default:
                        throw Error(number, "知らない語 " + words[0]);
                }
            }

            public FieldMap Finish()
            {
                CloseLayer();

                if (!_cell.HasValue) throw new FormatException("cell が無い");

                return new FieldMap(_cell.Value, _originX ?? 0f, _originZ,
                                    Math.Max(_rows, 0), Math.Max(_columns, 0), _blocks);
            }

            private void ReadRow(string line, int number)
            {
                if (!_inLayer) throw Error(number, "top の無いところに層の行がある");

                if (_columns < 0) _columns = line.Length;
                if (line.Length != _columns)
                    throw Error(number, "幅が " + line.Length + "。ほかの行は " + _columns);

                float cell = _cell.Value;

                for (int column = 0; column < line.Length; column++)
                {
                    char letter = line[column];
                    if (letter == Empty) continue;

                    _blocks.Add(new FieldBlock(letter,
                        _originX.Value + column * cell, _top - cell / 2f, _originZ - _row * cell,
                        0f, 0f, 0f, _row, column, number));
                }

                _row++;
            }

            /// <summary>piece 記号 x y z。向きの x y z を続けてもよい</summary>
            private void ReadPiece(string[] words, int number)
            {
                if (words.Length != 5 && words.Length != 8)
                    throw Error(number, "piece は 記号 x y z か、それに向きの x y z を足した形");
                if (!_cell.HasValue) throw Error(number, "piece より前に cell が要る");
                if (words[1].Length != 1 || words[1][0] == Empty)
                    throw Error(number, "piece の記号は . 以外の 1 文字");

                bool turned = words.Length == 8;

                _blocks.Add(new FieldBlock(words[1][0],
                    Number(words[2], number), Number(words[3], number), Number(words[4], number),
                    turned ? Number(words[5], number) : 0f,
                    turned ? Number(words[6], number) : 0f,
                    turned ? Number(words[7], number) : 0f,
                    -1, -1, number));
            }

            /// <summary>層を閉じる。どの層も同じ行の数にそろえる</summary>
            private void CloseLayer()
            {
                if (!_inLayer) return;

                _inLayer = false;

                if (_row == 0) throw Error(_layerLine, "top の後に層の行が無い");

                if (_rows < 0) _rows = _row;
                if (_row != _rows)
                    throw Error(_layerLine, "この層は " + _row + " 行。ほかの層は " + _rows + " 行");
            }

            /// <summary>値を書き忘れた語の行を、層の行と取り違えないため</summary>
            private static bool IsWord(string line)
            {
                return line == "cell" || line == "origin" || line == "top" || line == "piece";
            }

            private static void Expect(string[] words, int count, int number)
            {
                if (words.Length != count)
                    throw Error(number, words[0] + " の後に値が " + (count - 1) + " つ要る");
            }

            private static float Number(string word, int number)
            {
                float value;
                if (float.TryParse(word, NumberStyles.Float, CultureInfo.InvariantCulture, out value)) return value;

                throw Error(number, "数ではない " + word);
            }

            private static FormatException Error(int number, string message)
            {
                return new FormatException(number + " 行目: " + message);
            }
        }
    }
}
