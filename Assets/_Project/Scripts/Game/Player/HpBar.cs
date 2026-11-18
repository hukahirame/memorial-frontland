using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace MemorialFloor.Game
{
    /// <summary>プレイヤーの体力のバーと、「いま/上限」の字</summary>
    public sealed class HpBar : MonoBehaviour
    {
        [SerializeField] private PlayerHealth _health;
        [SerializeField, FormerlySerializedAs("slider")] private Slider _slider;
        [SerializeField, FormerlySerializedAs("txt")] private TextMeshProUGUI _label;

        private void OnEnable()
        {
            _health.Changed += Show;
        }

        private void OnDisable()
        {
            _health.Changed -= Show;
        }

        //上限は PlayerHealth の Awake で決まる。OnEnable はそれより先に呼ばれることがあるので、始めの表示はここで出す
        private void Start()
        {
            Show();
        }

        private void Show()
        {
            _slider.maxValue = _health.Max;
            _slider.value = _health.Current;
            _label.text = _health.Current + "/" + _health.Max;
        }
    }
}
