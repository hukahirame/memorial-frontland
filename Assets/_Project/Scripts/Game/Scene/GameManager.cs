using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace MemorialFloor.Game
{
    /// <summary>遊びの始めと終わり。保存を戻すか始めのクエストを出してから最初の場所へ移り、設定の「タイトルに戻る」で保存してタイトルへ戻る</summary>
    public sealed class GameManager : MonoBehaviour
    {
        [SerializeField, FormerlySerializedAs("travel")] private AreaTravel _travel;
        [SerializeField, FormerlySerializedAs("saveSystem")] private SaveSystem _save;
        [SerializeField, FormerlySerializedAs("quests")] private QuestManager _quests;

        [Tooltip("遊ぶ画面の設定。「タイトルに戻る」を受ける")]
        [SerializeField, FormerlySerializedAs("settings")] private SettingsMenu _settings;

        private void Awake()
        {
            _settings.TitleRequested += ToTitle;
        }

        private void Start()
        {
            //保存があれば根源とクエストを戻し、無ければ始めの調査とサブを出す。着いたときにクエストが始まるので、着く前に済ませる
            if (_save.HasSavedWorld) _save.RestoreWorld();
            else _quests.IssueFirst();

            _travel.Begin(_save.SavedAreaId);
        }

        private void OnDestroy()
        {
            _settings.TitleRequested -= ToTitle;
        }

        /// <summary>
        /// 設定の「タイトルに戻る」。書いてよいときなら保存してから、常駐シーンごとタイトルに入れ替える。
        /// 書けないとき（移っている途中、倒れているあいだ）は、10 秒おきの保存の最後のものが残る。遊びの状態は常駐シーンごと捨てる
        /// </summary>
        private void ToTitle()
        {
            _save.TrySave();
            Time.timeScale = 1f;
            SceneManager.LoadScene(AreaTravel.TitleScene);
        }
    }
}
