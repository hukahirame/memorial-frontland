using System;
using System.IO;
using UnityEngine;

namespace MemorialFloor.Game
{
    /// <summary>
    /// 保存の枠のファイル。枠は 1 から数える。タイトルが枠を並べ、名前を変え、消すのに使う。
    /// 遊ぶあいだの読み書きは SaveSystem が行う
    /// </summary>
    public static class SaveSlots
    {
        /// <summary>保存の枠の数</summary>
        public const int SlotCount = 5;

        /// <summary>タイトルを通らずに始めたときの枠</summary>
        public const int DefaultSlot = 1;

        /// <summary>枠の保存のファイル。1 は枠を分ける前と同じ名前にし、前の保存がそのまま 1 に入る</summary>
        public static string PathOf(int slot)
        {
            string number = slot == 1 ? "" : slot.ToString();
            return Application.persistentDataPath + "/" + ".savedata" + number + ".json";
        }

        /// <summary>枠に、続きから始められる保存があるか</summary>
        public static bool HasSave(int slot)
        {
            return File.Exists(PathOf(slot));
        }

        /// <summary>枠の保存を最後に書いた時刻。保存が無ければ null</summary>
        public static DateTime? SavedAt(int slot)
        {
            return HasSave(slot) ? File.GetLastWriteTime(PathOf(slot)) : (DateTime?)null;
        }

        /// <summary>枠の保存の中身を、戻さずに読む。名前や場所を出すのに使う。保存が無いか読めなければ null</summary>
        public static SaveData Peek(int slot)
        {
            if (!HasSave(slot)) return null;

            try
            {
                return JsonUtility.FromJson<SaveData>(File.ReadAllText(PathOf(slot)));
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// 枠の保存の名前を書き換える。ほかは変えない。書いた時刻も元に戻す。時刻が変わると、タイトルの「つづきから」が
        /// 名前を変えただけの枠を選び、枠に出す日時も遊んだ時刻とずれる。保存が無いか読めなければ何もしない
        /// </summary>
        public static void Rename(int slot, string name)
        {
            SaveData data = Peek(slot);
            if (data == null) return;

            string path = PathOf(slot);
            DateTime written = File.GetLastWriteTime(path);

            data.saveName = name;
            WriteNow(path, JsonUtility.ToJson(data));

            File.SetLastWriteTime(path, written);
        }

        /// <summary>枠の保存を消す。次に始めると、はじめからになる。取り返せないので、呼ぶ側で確かめてから呼ぶ</summary>
        public static void Erase(int slot)
        {
            string path = PathOf(slot);
            if (File.Exists(path)) File.Delete(path);
            if (File.Exists(path + ".tmp")) File.Delete(path + ".tmp");
        }

        /// <summary>書いている途中で落ちても前の保存を壊さないよう、別のファイルに書いてから差し替える</summary>
        public static void WriteNow(string path, string json)
        {
            string tmp = path + ".tmp";
            File.WriteAllText(tmp, json);
            File.Copy(tmp, path, true);
            File.Delete(tmp);
        }
    }
}
