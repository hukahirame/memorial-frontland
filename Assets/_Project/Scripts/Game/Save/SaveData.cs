using System;
using System.Collections.Generic;
using MemorialFloor.Domain;
using UnityEngine;

namespace MemorialFloor.Game
{
    /// <summary>
    /// 保存のファイルの中身。JsonUtility がそのまま読み書きするので、public フィールドで持つ（[D-7] の例外）。
    /// 欄の名前はファイルの鍵で、変えると前の保存が読めなくなる。写すのと戻すのは SaveSystem が行う
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        public int playerhp;

        /// <summary>タイトルで付けた枠の名前。空ならタイトルは「データ」と枠の番号を出す</summary>
        public string saveName = "";

        public string entered_scene;
        public int daytime;
        public int coin;

        /// <summary>立ち位置。entered_scene の場所の原点からの位置</summary>
        public Vector3 respawn;

        public List<ItemSlot> slots = new List<ItemSlot>();

        /// <summary>装備している武器の ID。素手と、これを足す前の保存は空</summary>
        public string equipped = "";

        /// <summary>保存した場所の敵。場所の原点からの位置と、同じ添字の残りの体力。0 と、足りない保存の敵は新しく湧いた敵として出す</summary>
        public List<string> enemies = new List<string>();
        public List<Vector3> respawns = new List<Vector3>();
        public List<int> enemyHps = new List<int>();

        //置いた物。同じ添字の7つで1つ。saveObjects はアイテム ID、saveObjectsYaw は y 軸まわりの向き（度）、
        //saveObjectsDurability は残りの耐久（耐久を持たない物は -1）、saveObjectsAge は控えてから保存までの秒数、
        //saveObjectsFallen は柵が倒れているか。足りない保存は、向きを 0、耐久を控えていない（満タンで立つ）、
        //控えたのは保存したとき、立っているとみなす。ゲームを閉じているあいだは数えないよう、控えた時刻ではなく経った秒数で持つ
        public List<string> saveObjects = new List<string>();
        public List<string> saveObjectsScene = new List<string>();
        public List<Vector3> saveObjectsPos = new List<Vector3>();
        public List<float> saveObjectsYaw = new List<float>();
        public List<int> saveObjectsDurability = new List<int>();
        public List<float> saveObjectsAge = new List<float>();
        public List<bool> saveObjectsFallen = new List<bool>();

        //根源は遊ぶうちに動く値だけを持つ。名前や危険度はコードから作り直す
        public List<RootRecord> roots = new List<RootRecord>();
        public List<QuestRecord> quests = new List<QuestRecord>();
        public string ordered_id = "";

        /// <summary>前の形の保存の、拠点へ向かっていた敵の数。読むだけで、書くときは 0</summary>
        public int raiders;

        /// <summary>拠点に攻め込んだ敵</summary>
        public List<RaiderRecord> baseRaiders = new List<RaiderRecord>();

        /// <summary>保留中の調査</summary>
        public List<string> plannedRoots = new List<string>();

        /// <summary>根源とクエストを持つか。持たない保存からは何も戻さず、始めのクエストを出す</summary>
        public bool HasWorld
        {
            get { return roots.Count > 0; }
        }

        /// <summary>置いた物を写す。控えにまだ入れていない物も先に入れる</summary>
        public void RecordPlacements(PlacedObjects placed)
        {
            saveObjects.Clear();
            saveObjectsPos.Clear();
            saveObjectsScene.Clear();
            saveObjectsYaw.Clear();
            saveObjectsDurability.Clear();
            saveObjectsAge.Clear();
            saveObjectsFallen.Clear();

            placed.Flush();
            foreach (string area in placed.Ledger.Areas)
            {
                foreach (Placement placement in placed.Ledger.In(area))
                {
                    saveObjects.Add(placement.ItemId);
                    saveObjectsPos.Add(new Vector3(placement.X, placement.Y, placement.Z));
                    saveObjectsScene.Add(area);
                    saveObjectsYaw.Add(placement.Yaw);
                    saveObjectsDurability.Add(placement.Durability);
                    saveObjectsAge.Add(Time.time - placement.RecordedAt);
                    saveObjectsFallen.Add(placement.Fallen);
                }
            }
        }

        /// <summary>置いた物を控えへ戻す。場所に入ったときに置き直される。位置は場所の原点からの位置のまま控える</summary>
        public void LoadPlacements(PlacementLedger ledger)
        {
            Dictionary<string, List<Placement>> byArea = new Dictionary<string, List<Placement>>();

            for (int i = 0; i < saveObjects.Count; i++)
            {
                string area = saveObjectsScene[i];
                if (!byArea.ContainsKey(area)) byArea[area] = new List<Placement>();

                Vector3 position = saveObjectsPos[i];
                float yaw = i < saveObjectsYaw.Count ? saveObjectsYaw[i] : 0f;
                int durability = i < saveObjectsDurability.Count ? saveObjectsDurability[i] : Placement.NoDurability;
                float age = i < saveObjectsAge.Count ? saveObjectsAge[i] : 0f;
                bool fallen = i < saveObjectsFallen.Count && saveObjectsFallen[i];
                byArea[area].Add(new Placement(saveObjects[i], position.x, position.y, position.z, yaw, durability, Time.time - age, fallen));
            }

            foreach (KeyValuePair<string, List<Placement>> pair in byArea) ledger.Record(pair.Key, pair.Value);
        }
    }

    /// <summary>根源 1 つの、遊ぶうちに動く値</summary>
    [Serializable]
    public sealed class RootRecord
    {
        public string id;
        public int progress;
        public int accumulation;
        public bool hasSpawnPoint;
        public Vector3 spawnPoint;

        public static RootRecord Of(Root root)
        {
            return new RootRecord
            {
                id = root.Id,
                progress = root.Progress,
                accumulation = root.Accumulation,
                hasSpawnPoint = root.HasSpawnPoint,
                spawnPoint = new Vector3(root.SpawnX, root.SpawnY, root.SpawnZ),
            };
        }

        public void Apply(Root root)
        {
            root.Restore(progress, accumulation);
            if (hasSpawnPoint) root.PlaceSpawnPoint(spawnPoint.x, spawnPoint.y, spawnPoint.z);
        }
    }

    /// <summary>拠点に攻め込んだ敵 1 体。位置を持たないものは、拠点の湧き口のまわりに満タンで出る</summary>
    [Serializable]
    public sealed class RaiderRecord
    {
        public bool hasPosition;
        public Vector3 position;
        public int hp;

        public static RaiderRecord Of(Raider raider)
        {
            return new RaiderRecord
            {
                hasPosition = raider.HasPosition,
                position = new Vector3(raider.X, raider.Y, raider.Z),
                hp = raider.Hp,
            };
        }

        public Raider ToRaider()
        {
            return hasPosition ? new Raider(position.x, position.y, position.z, hp) : Raider.Fresh;
        }
    }

    /// <summary>クエスト 1 本。報酬は同じ添字の 2 つで 1 つ</summary>
    [Serializable]
    public sealed class QuestRecord
    {
        public string id;
        public string rootId;
        public string target;
        public int amount;
        public int progress;
        public List<string> rewardKinds = new List<string>();
        public List<int> rewardAmounts = new List<int>();

        public static QuestRecord Of(Quest quest)
        {
            QuestRecord record = new QuestRecord
            {
                id = quest.Id,
                rootId = quest.RootId,
                target = quest.Target,
                amount = quest.Amount,
                progress = quest.Progress,
            };

            foreach (Reward reward in quest.Rewards)
            {
                record.rewardKinds.Add(reward.Kind);
                record.rewardAmounts.Add(reward.Amount);
            }

            return record;
        }

        public Quest ToQuest()
        {
            List<Reward> rewards = new List<Reward>();
            for (int i = 0; i < rewardKinds.Count && i < rewardAmounts.Count; i++)
                rewards.Add(new Reward(rewardKinds[i], rewardAmounts[i]));

            Quest quest = new Quest(id, rootId, target, amount, rewards);
            quest.SetProgress(progress);

            return quest;
        }
    }
}
