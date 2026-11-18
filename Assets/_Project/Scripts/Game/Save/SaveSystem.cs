using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using MemorialFloor.Domain;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace MemorialFloor.Game
{
    /// <summary>
    /// 遊びの保存。始めるときに枠の保存を読み、最初に場所へ着いたときに戻す。遊ぶあいだは 10 秒おきと、裏に回したとき・閉じるときに書く。
    /// どの枠で始めるかは、常駐シーンを読み込んだ直後（どの Start よりも前）にタイトルが Choose で渡す。渡されなければ 1 の枠
    /// </summary>
    public sealed class SaveSystem : MonoBehaviour
    {
        /// <summary>まとめて書く間隔（実時間の秒）。体力や時刻は毎秒変わるので、変わるたびには書かない</summary>
        private const float AutosaveSeconds = 10f;

        private const string EnemyTag = "Enemy";

        [SerializeField, FormerlySerializedAs("travel")] private AreaTravel _travel;
        [SerializeField, FormerlySerializedAs("placed")] private PlacedObjects _placed;
        [SerializeField, FormerlySerializedAs("health")] private PlayerHealth _health;
        [SerializeField] private PlayerInventory _inventory;
        [SerializeField] private PlayerEquipment _equipment;
        [SerializeField] private Purse _purse;
        [SerializeField] private Sun _sun;
        [SerializeField] private RootsManager _roots;
        [SerializeField] private QuestManager _quests;

        /// <summary>読み書きする枠</summary>
        private int _slot = SaveSlots.DefaultSlot;

        /// <summary>はじめから始める枠に付ける名前。保存が無いまま始めたときに保存へ移す</summary>
        private string _pendingName = "";

        /// <summary>読み書きするか。テストでは切る。保存のファイルは枠ごとに 1 つで、読み書きすると前のテストの状態が次に持ち越される</summary>
        private bool _isEnabled = true;

        /// <summary>枠を開いた（読むかを決めた）か</summary>
        private bool _isOpened;

        /// <summary>読み込んだ保存を、最初に場所へ着いたときに戻す</summary>
        private bool _restorePending;

        /// <summary>
        /// 裏で書いている途中の保存。ファイルに書くのは数ミリ秒かかり、遊んでいるあいだに挟むとそのコマが遅れて引っかかる。
        /// 書くのは前の書き込みが終わってからなので、古い中身があとから新しい中身を上書きすることはない
        /// </summary>
        private Task _pendingWrites = Task.CompletedTask;

        private readonly object _writingGate = new object();

        public SaveData Data { get; private set; } = new SaveData();

        /// <summary>保存してあった場所。保存が無ければ null</summary>
        public string SavedAreaId
        {
            get
            {
                Open();
                return _restorePending ? Data.entered_scene : null;
            }
        }

        /// <summary>戻す根源とクエストがあるか。無ければ始めのクエストを出す</summary>
        public bool HasSavedWorld
        {
            get
            {
                Open();
                return _restorePending && Data.HasWorld;
            }
        }

        /// <summary>シーンのルートに置かれた保存。無ければ null。読み込んだ直後のシーンへ渡すものを渡すのに使う</summary>
        public static SaveSystem In(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                SaveSystem found = root.GetComponentInChildren<SaveSystem>(true);
                if (found != null) return found;
            }

            return null;
        }

        /// <summary>読み書きする枠と、はじめから始めるときに付ける名前。読む前（どの Start よりも前）に呼ぶ</summary>
        public void Choose(int slot, string pendingName)
        {
            _slot = slot;
            _pendingName = pendingName;
        }

        /// <summary>読みも書きもしない。読む前（どの Start よりも前）に呼ぶ。テストで使う</summary>
        public void TurnOff()
        {
            _isEnabled = false;
        }

        private void Start()
        {
            Open();
            if (_isEnabled) StartCoroutine(Autosave());
        }

        private void OnDestroy()
        {
            _travel.Entered -= Restore;

            //タイトルへ戻ると常駐シーンごと消える。タイトルが枠を読む前に書き終える
            FinishWriting();
        }

        //裏に回したアプリは、そのまま落とされることがある。書き終えるまで待ってから手放す
        private void OnApplicationPause(bool paused)
        {
            if (!paused) return;

            TrySave();
            FinishWriting();
        }

        private void OnApplicationQuit()
        {
            TrySave();
            FinishWriting();
        }

        private IEnumerator Autosave()
        {
            while (true)
            {
                yield return new WaitForSecondsRealtime(AutosaveSeconds);
                TrySave();
            }
        }

        /// <summary>枠の保存を読むかを決める。1 度だけ。場所の移動は始める側が SavedAreaId を見て行う。読めない保存は捨てる</summary>
        private void Open()
        {
            if (_isOpened) return;
            _isOpened = true;

            if (_isEnabled && SaveSlots.HasSave(_slot)) Load();

            //はじめから始めた枠には、タイトルで決めた名前を付ける。続きからなら読んだ保存の名前のまま
            if (!_restorePending) Data.saveName = _pendingName;
        }

        private void Load()
        {
            SaveData loaded = null;
            try
            {
                loaded = JsonUtility.FromJson<SaveData>(File.ReadAllText(SaveSlots.PathOf(_slot)));
            }
            catch (Exception e)
            {
                Debug.LogWarning("保存を読めなかったので、はじめから始める：" + e.Message);
            }

            if (loaded == null) return;

            Data = loaded;
            Data.LoadPlacements(_placed.Ledger);

            _restorePending = true;
            _travel.Entered += Restore;
        }

        /// <summary>
        /// 書いてよいときだけ書く。戻し終える前に書くと、保存を始めの状態で上書きする。
        /// 移っている最中と倒れているあいだは、場所と体力が定まらない。設定からタイトルに戻るときも呼ぶ
        /// </summary>
        public void TrySave()
        {
            if (!_isEnabled || !_isOpened || _restorePending) return;
            if (_travel.Current == null || _travel.IsTraveling) return;
            if (_health.IsDead) return;

            Save();
        }

        public void Save()
        {
            Capture(Data);
            Write(SaveSlots.PathOf(_slot), JsonUtility.ToJson(Data));
        }

        /// <summary>いまの遊びを data へ写す</summary>
        public void Capture(SaveData data)
        {
            data.playerhp = _health.Current;
            data.entered_scene = _travel.Where.AreaId;
            data.daytime = _sun.Cycle.ElapsedSeconds;
            data.slots = new List<ItemSlot>(_inventory.Slots);
            data.equipped = _equipment.Equipped.ItemId;
            data.coin = _purse.Coins.Amount;

            //座標は今いる場所の原点からの位置で書く。場所を世界のどこへ置き直しても、前の保存が同じ所を指す
            Area here = _travel.Current;
            data.respawn = Local(here, _health.transform.position);
            CaptureEnemies(data, here);

            data.RecordPlacements(_placed);
            CaptureWorld(data);
        }

        /// <summary>今いる場所の敵。倒れて消える途中のものは書かない。隣の場所も読み込んであるとき、そちらの敵は書かない</summary>
        private static void CaptureEnemies(SaveData data, Area here)
        {
            data.enemies.Clear();
            data.respawns.Clear();
            data.enemyHps.Clear();
            if (here == null) return;

            foreach (GameObject root in here.Scene.GetRootGameObjects())
            {
                foreach (IDamageable enemy in root.GetComponentsInChildren<IDamageable>())
                {
                    Component body = (Component)enemy;
                    if (!body.CompareTag(EnemyTag) || enemy.CurrentHp <= 0) continue;

                    data.enemies.Add(body.name);
                    data.respawns.Add(here.ToLocal(body.transform.position));
                    data.enemyHps.Add(enemy.CurrentHp);
                }
            }
        }

        /// <summary>根源・クエスト・受注・拠点に攻め込んだ敵・保留中の調査を写す</summary>
        public void CaptureWorld(SaveData data)
        {
            data.roots.Clear();
            foreach (Root root in _roots.Roots.All) data.roots.Add(RootRecord.Of(root));

            data.quests.Clear();
            foreach (Quest quest in _quests.Quests.All) data.quests.Add(QuestRecord.Of(quest));

            data.ordered_id = _quests.OrderedId;

            //拠点を読み込んであれば、出ている敵も写す。控えには拠点のシーンが無いあいだの敵しか無い
            data.raiders = 0;
            data.baseRaiders.Clear();
            foreach (Raider raider in _roots.Raid.Kept) data.baseRaiders.Add(RaiderRecord.Of(raider));
            foreach (Area area in _travel.Present)
            {
                RaidGate gate = area.Find<RaidGate>();
                if (gate == null) continue;

                foreach (Raider raider in gate.Survivors()) data.baseRaiders.Add(RaiderRecord.Of(raider));
            }

            data.plannedRoots = new List<string>(_quests.Plan.Pending);
        }

        /// <summary>保存してあった根源とクエストを戻す。最初の場所に着く前に 1 度だけ呼ぶ。着いたときにクエストが始まり、核の位置が決まるため</summary>
        public void RestoreWorld()
        {
            if (HasSavedWorld) RestoreWorld(Data);
        }

        /// <summary>
        /// data の根源・クエスト・受注・拠点に攻め込んだ敵・保留中の調査を戻す。保存のあとで足した根源には始めのクエストを出す。
        /// 根源を持たない保存からは何も戻さない
        /// </summary>
        public void RestoreWorld(SaveData data)
        {
            if (!data.HasWorld) return;

            //作る根源から外れた Id は捨てる
            foreach (RootRecord record in data.roots)
            {
                Root root = _roots.Roots.Find(record.id);
                if (root != null) record.Apply(root);
            }

            _quests.Quests.Clear();
            foreach (QuestRecord record in data.quests) _quests.Quests.TryAdd(record.ToQuest());

            //出さないと、足した根源には 2 回目の夜明けまで受けるものが無い
            foreach (Root root in _roots.Roots.All)
            {
                if (!data.roots.Exists(record => record.id == root.Id)) _quests.IssueFirst(root);
            }

            if (_quests.Quests.Find(data.ordered_id) != null) _quests.Order(data.ordered_id);
            else _quests.CancelOrder();

            _roots.Raid.Clear();
            foreach (RaiderRecord record in data.baseRaiders) _roots.Raid.Keep(record.ToRaider());
            _roots.Raid.Invade(data.raiders);
            _quests.Plan.Restore(data.plannedRoots);
        }

        /// <summary>
        /// 保存した根源の敵を、その根源を読み込んだときに核へ渡す。保存した場所でなければ何もしない。渡したら保存から消す。
        /// 隣として先に読み込んだときも渡す
        /// </summary>
        public void RestoreEnemies(Area area, RootCore core)
        {
            if (area.Id != Data.entered_scene || Data.respawns.Count == 0) return;

            for (int i = 0; i < Data.respawns.Count; i++)
            {
                int hp = i < Data.enemyHps.Count ? Data.enemyHps[i] : 0;
                core.Keep(area.ToWorld(Data.respawns[i]), hp);
            }

            Data.enemies.Clear();
            Data.respawns.Clear();
            Data.enemyHps.Clear();
        }

        /// <summary>
        /// 最初に場所へ着いたとき、体力・時刻・持ち物・所持金を戻す。立ち位置は保存した場所に着いたときだけ戻す。
        /// エディタで場所のシーンから始めると、保存した場所とは違う場所に着く
        /// </summary>
        private void Restore(Area area)
        {
            _travel.Entered -= Restore;
            _restorePending = false;

            _health.SetCurrent(Data.playerhp);
            _sun.Cycle.SetElapsed(Data.daytime);
            _inventory.ReplaceSlots(Data.slots);

            //戻した持ち物から持ち直す。空なら始めの鉄の剣のまま
            if (!string.IsNullOrEmpty(Data.equipped)) _inventory.Wield(Data.equipped);

            _purse.Coins.SetAmount(Data.coin);

            if (area.Id == Data.entered_scene) _health.transform.position = area.ToWorld(Data.respawn);
        }

        private static Vector3 Local(Area here, Vector3 world)
        {
            return here != null ? here.ToLocal(world) : world;
        }

        //中身はここで固めて渡す。WebGL は裏で動かせないので、その場で書く
        private void Write(string path, string json)
        {
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                WriteSafely(path, json);
                return;
            }

            lock (_writingGate)
            {
                _pendingWrites = _pendingWrites.ContinueWith(_ => WriteSafely(path, json), TaskScheduler.Default);
            }
        }

        private static void WriteSafely(string path, string json)
        {
            try
            {
                SaveSlots.WriteNow(path, json);
            }
            catch (Exception e)
            {
                Debug.LogWarning("保存を書けなかった：" + e.Message);
            }
        }

        /// <summary>裏で書いている保存を書き終えるまで待つ</summary>
        private void FinishWriting()
        {
            Task last;
            lock (_writingGate) last = _pendingWrites;
            last.Wait();
        }
    }
}
