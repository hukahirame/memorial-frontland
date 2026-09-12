using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;

namespace MemorialFloor.Tests.Integrity
{
    /// <summary>
    /// シーンとプレハブの参照が生きているかを見る。
    /// YAML の差分は人間が読めないので、目視ではなく不変条件で埋める。
    ///
    /// ファイルは開かずテキストとして読む。シーンを開くとエディタの状態が変わり、
    /// 中断したときに戻らないため。
    /// </summary>
    public class AssetIntegrityTests
    {
        /// <summary>
        /// 見る範囲。third-party と Assets/LegacyScenes/ は追跡対象外で、
        /// 壊れていても直せないため入れない（docs/CONTRIBUTING.md）
        /// </summary>
        private const string Root = "Assets/_Project";

        /// <summary>
        /// 導入時点で既に切れていた参照の控え。増えたら落ち、直ったら減る。
        /// 減る方向にしか動かさない
        /// </summary>
        private const string BaselinePath = "docs/asset-baseline.txt";

        /// <summary>報せる件数の上限。全部出すとメッセージが読めなくなる</summary>
        private const int MaxReported = 20;

        private static readonly Regex Guid = new Regex("guid: ([0-9a-f]{32})");

        /// <summary>スクリプトが外れた MonoBehaviour。参照そのものが消えるとこの形で残る</summary>
        private static readonly Regex ScriptCleared = new Regex("m_Script: {fileID: 0}");

        /// <summary>--- !u!114 &12345 と、その後ろに stripped が付く形</summary>
        private static readonly Regex Anchor = new Regex("^--- !u![0-9]+ &(-?[0-9]+)");

        /// <summary>同じファイルの中を指す参照。guid が続くものは中括弧が閉じないので当たらない</summary>
        private static readonly Regex LocalReference = new Regex("{fileID: (-?[0-9]+)}");

        [Test]
        public void 実在しないGUIDへの参照が増えていない()
        {
            Dictionary<string, string> current = Unresolved();
            HashSet<string> baseline = ReadBaseline();

            if (!File.Exists(BaselinePath))
            {
                WriteBaseline(current.Keys);
                Assert.Fail(BaselinePath + " を作った。" + current.Count +
                            " 件の切れた参照を控えてある。中身を確認すること");
            }

            List<string> added = current.Keys.Where(key => !baseline.Contains(key))
                                             .OrderBy(key => key).ToList();

            Assert.IsEmpty(added, Report(added.Select(key => key + " " + current[key]).ToList(),
                "実在しないアセットを指す参照が増えた。" +
                "消されたアセットか、.meta を伴わずに動かされたアセットがある"));

            List<string> repaired = baseline.Where(key => !current.ContainsKey(key))
                                            .OrderBy(key => key).ToList();

            if (repaired.Count == 0) return;

            WriteBaseline(current.Keys);
            Assert.Fail(Report(repaired,
                "直った参照がある。" + BaselinePath + " から消して書き直した。差分を見ること"));
        }

        [Test]
        public void MissingScriptが残っていない()
        {
            List<string> broken = new List<string>();

            foreach (string path in Targets())
            {
                string[] lines = File.ReadAllLines(path);

                for (int i = 0; i < lines.Length; i++)
                {
                    if (ScriptCleared.IsMatch(lines[i])) broken.Add(Where(path, i));
                }
            }

            Assert.IsEmpty(broken, Report(broken,
                "スクリプトの外れた MonoBehaviour が残っている。" +
                "開いても何も起きないので目視では気づけない"));
        }

        [Test]
        public void ファイルの中を指す参照が同じファイルに在る()
        {
            List<string> broken = new List<string>();

            foreach (string path in Targets())
            {
                string[] lines = File.ReadAllLines(path);
                HashSet<string> anchors = new HashSet<string>();

                foreach (string line in lines)
                {
                    Match anchor = Anchor.Match(line);
                    if (anchor.Success) anchors.Add(anchor.Groups[1].Value);
                }

                for (int i = 0; i < lines.Length; i++)
                {
                    foreach (Match match in LocalReference.Matches(lines[i]))
                    {
                        string id = match.Groups[1].Value;
                        if (id == "0" || anchors.Contains(id)) continue;

                        broken.Add(Where(path, i) + " fileID " + id);
                    }
                }
            }

            Assert.IsEmpty(broken, Report(broken,
                "同じファイルの中に参照先が無い。消された GameObject を指したままの参照がある"));
        }

        /// <summary>
        /// 解決できない参照。鍵は「道 guid」で、値は最初に見つかった行。
        /// 行を鍵に含めないのは、シーンを編集するたびに行がずれて別物になるため
        /// </summary>
        private static Dictionary<string, string> Unresolved()
        {
            Dictionary<string, string> found = new Dictionary<string, string>();

            foreach (string path in Targets())
            {
                string[] lines = File.ReadAllLines(path);

                for (int i = 0; i < lines.Length; i++)
                {
                    foreach (Match match in Guid.Matches(lines[i]))
                    {
                        string guid = match.Groups[1].Value;
                        if (AssetDatabase.GUIDToAssetPath(guid).Length > 0) continue;

                        string key = Slash(path) + " " + guid;
                        if (!found.ContainsKey(key)) found[key] = Where(path, i);
                    }
                }
            }

            return found;
        }

        /// <summary>見るファイル。シーンとプレハブだけ</summary>
        private static IEnumerable<string> Targets()
        {
            if (!Directory.Exists(Root)) yield break;

            foreach (string pattern in new[] { "*.unity", "*.prefab" })
            {
                foreach (string path in Directory.GetFiles(Root, pattern, SearchOption.AllDirectories)
                                                 .OrderBy(p => p, System.StringComparer.Ordinal))
                {
                    yield return path;
                }
            }
        }

        private static HashSet<string> ReadBaseline()
        {
            HashSet<string> keys = new HashSet<string>();
            if (!File.Exists(BaselinePath)) return keys;

            foreach (string raw in File.ReadAllLines(BaselinePath))
            {
                string line = raw.Trim('﻿').Trim();
                if (line.Length == 0 || line.StartsWith("#")) continue;

                keys.Add(line);
            }

            return keys;
        }

        private static void WriteBaseline(IEnumerable<string> keys)
        {
            StringBuilder text = new StringBuilder();
            text.Append("# 切れた参照の控え。AssetIntegrityTests が読み書きする。\n");
            text.Append("#\n");
            text.Append("# 導入した時点で既に切れていたもの。増えるとテストが落ちる。\n");
            text.Append("# 直すと、テストがこのファイルから消して1度だけ落ちる。\n");
            text.Append("# 手で足さないこと。減らすだけ。\n");
            text.Append("#\n");
            text.Append("# <シーンかプレハブの道> <解決できない guid>\n");

            foreach (string key in keys.OrderBy(k => k, System.StringComparer.Ordinal))
                text.Append(key).Append('\n');

            File.WriteAllText(BaselinePath, text.ToString(), new UTF8Encoding(true));
        }

        private static string Slash(string path)
        {
            return path.Replace('\\', '/');
        }

        private static string Where(string path, int index)
        {
            return Slash(path) + ":" + (index + 1);
        }

        private static string Report(List<string> broken, string summary)
        {
            if (broken.Count == 0) return string.Empty;

            string body = string.Join("\n", broken.Take(MaxReported));
            string rest = broken.Count > MaxReported
                ? "\nほか " + (broken.Count - MaxReported) + " 件"
                : string.Empty;

            return summary + "\n" + broken.Count + " 件\n" + body + rest;
        }
    }
}
