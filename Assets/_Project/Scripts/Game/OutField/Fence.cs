using System.Collections;
using MemorialFloor.Domain;
using UnityEngine;

namespace MemorialFloor.Game
{
    /// <summary>
    /// 置いた柵。敵もプレイヤーも通さない。敵がぶつかるたびに耐久が減り、削られなくなると時間で戻る。
    /// 耐久が尽きると根元から倒れて通れるようになり、上限の 4 分の 1 を超えるまで戻ると起き上がって、また通さなくなる。
    /// 剣では削れない。片付けるときはプレイヤーが Allmaity ボタンで壊す。残りの耐久はゲージで上に出す
    /// </summary>
    public sealed class Fence : MonoBehaviour
    {
        /// <summary>同じ敵がぶつかり続けたとき、次に削るまでの秒数</summary>
        public const float StrikeSeconds = 1f;

        /// <summary>倒れたときの傾き（度）。根元を軸に、ぶつかった敵と反対の側へ倒れる</summary>
        public const float FallAngle = 80f;

        /// <summary>壊すボタンが出る、柵からの距離。長さと厚みの向きに、この幅だけ当たりを広げて見る</summary>
        public const float ReachMargin = 0.8f;

        /// <summary>倒れきるまでの秒数</summary>
        private const float FallSeconds = 0.25f;

        /// <summary>叩かれて揺れる長さ</summary>
        private const float ShakeSeconds = 0.15f;

        /// <summary>叩かれて厚みの向きへ揺れる幅</summary>
        private const float ShakeWidth = 0.06f;

        /// <summary>壊れてから消えるまでの秒数</summary>
        private const float BurstSeconds = 0.25f;

        /// <summary>壊れる前に膨らむ大きさの倍率</summary>
        private const float BurstSwell = 1.15f;

        /// <summary>耐久が尽きる間際の明るさ。削れるほど元の色からこの明るさへ近づく</summary>
        private const float WornShade = 0.45f;

        /// <summary>立っているときのゲージの色</summary>
        private static readonly Color StandingFill = new Color(0.45f, 0.85f, 0.35f);

        /// <summary>倒れて戻りかけのときのゲージの色</summary>
        private static readonly Color FallenFill = new Color(1f, 0.6f, 0.25f);

        private const string HitSound = "Fantasy_Game_Attack_Weapon_Impact";

        /// <summary>壊れたときと倒れたときの音の高さ。叩いたときと同じ音を低くして重さを出す</summary>
        private const float BreakPitch = 0.7f;

        /// <summary>起き上がったときの音の高さ。叩いたときと同じ音を高くして軽く鳴らす</summary>
        private const float RisePitch = 1.3f;

        private static readonly int ColorId = Shader.PropertyToID("_Color");

        [Tooltip("耐久。ぶつかった敵の攻撃力のぶん減る")]
        [SerializeField] private int durability = 200;

        [Tooltip("見える部分。揺らし、倒し、色を変える。根元が原点")]
        [SerializeField] private Transform shown;

        [Tooltip("当たり。立てる所を探すときも、この大きさを柵の大きさとする")]
        [SerializeField] private BoxCollider body;

        private FenceDurability state;
        private Vector3 rest;
        private Quaternion upright;
        private Renderer[] parts;
        private MaterialPropertyBlock block;
        private Coroutine shaking;

        private BoxCollider reach;
        private WorldGauge gauge;

        /// <summary>倒れる側。1 なら柵の厚みの向き（z）の正へ、-1 なら負へ倒れる</summary>
        private float fallSide = 1f;

        /// <summary>壊されて消えかけている</summary>
        private bool broken;

        /// <summary>当たりの大きさ。x が長さ、y が高さ、z が厚み</summary>
        public Vector3 Size
        {
            get { return body.size; }
        }

        /// <summary>当たりの真ん中。立てた所（足元）から見た位置</summary>
        public Vector3 Center
        {
            get { return body.center; }
        }

        /// <summary>残りの耐久</summary>
        public int Durability
        {
            get { return state.Current; }
        }

        /// <summary>敵に削りきられて倒れている。このあいだは通れて、削られもしない</summary>
        public bool IsFallen
        {
            get { return state.IsFallen && !broken; }
        }

        /// <summary>壊すボタンを出す範囲の当たり。素通りする当たりで、プレイヤーの体が入るとボタンが出る</summary>
        public Collider Reach
        {
            get { return reach; }
        }

        private void Awake()
        {
            state = new FenceDurability(durability);

            rest = shown.localPosition;
            upright = shown.localRotation;
            parts = shown.GetComponentsInChildren<Renderer>();
            block = new MaterialPropertyBlock();

            reach = MakeReach();
            gauge = WorldGauge.Put(transform, StandingFill);
        }

        private void Update()
        {
            if (broken) return;

            float before = state.Ratio;
            bool rose = state.Repair(Time.time, Time.deltaTime);
            if (state.Ratio == before) return;

            Wear();
            ShowDurability();
            if (rose) Rise();
        }

        /// <summary>from に居る敵がぶつかった。耐久が尽きたら、敵と反対の側へ倒れて通れるようになる。倒れているあいだは何もしない</summary>
        public void Ram(int amount, Vector3 from)
        {
            if (state.IsFallen || broken) return;

            bool fell = state.Strike(amount, Time.time);
            Wear();
            ShowDurability();
            if (!fell)
            {
                Jolt();
                return;
            }

            fallSide = transform.InverseTransformPoint(from).z < 0f ? 1f : -1f;
            TempAudio.TempAudioPlay(HitSound, BreakPitch);
            StartCoroutine(Fall());
        }

        /// <summary>プレイヤーが壊した。すぐ通れるようにし、膨らんでから縮んで消える。何も戻らない</summary>
        public void Demolish()
        {
            if (broken) return;

            broken = true;
            TempAudio.TempAudioPlay(HitSound, BreakPitch);
            StartCoroutine(Burst());
        }

        /// <summary>控えから戻す。置き直した直後に呼ぶ。awaySeconds は控えてから経った秒数で、離れていたあいだのぶんも戻す</summary>
        public void Restore(int current, bool fallen, float awaySeconds)
        {
            state.Restore(current, fallen, awaySeconds);
            Wear();
            ShowDurability();

            body.isTrigger = state.IsFallen;
            if (state.IsFallen) Lean();
        }

        /// <summary>削れたぶん暗くする。素材は共有なので、物ごとの上書きで変える</summary>
        private void Wear()
        {
            float shade = Mathf.Lerp(1f, WornShade, 1f - state.Ratio);

            foreach (Renderer part in parts)
            {
                Color color = part.sharedMaterial.color * shade;
                color.a = part.sharedMaterial.color.a;

                part.GetPropertyBlock(block);
                block.SetColor(ColorId, color);
                part.SetPropertyBlock(block);
            }
        }

        private void Jolt()
        {
            TempAudio.TempAudioPlay(HitSound);
            if (shaking != null) StopCoroutine(shaking);
            shaking = StartCoroutine(Shake());
        }

        private IEnumerator Shake()
        {
            for (float t = 0f; t < ShakeSeconds; t += Time.deltaTime)
            {
                float fade = 1f - t / ShakeSeconds;
                shown.localPosition = rest + Vector3.forward * (Mathf.Sin(t * 80f) * ShakeWidth * fade);
                yield return null;
            }

            shown.localPosition = rest;
            shaking = null;
        }

        /// <summary>倒れたらすぐ通れるようにし、根元を軸に倒す。倒れた柵の当たりは、立てる所を探すときにだけ使う</summary>
        private IEnumerator Fall()
        {
            body.isTrigger = true;
            if (shaking != null) StopCoroutine(shaking);
            shaking = null;
            shown.localPosition = rest;

            for (float t = 0f; t < FallSeconds; t += Time.deltaTime)
            {
                float k = t / FallSeconds;
                shown.localRotation = Quaternion.AngleAxis(fallSide * FallAngle * k * k, Vector3.right) * upright;
                yield return null;
            }

            Lean();
        }

        //倒れているあいだは、起き上がるまで寝かせたままにする
        private void Lean()
        {
            shown.localRotation = Quaternion.AngleAxis(fallSide * FallAngle, Vector3.right) * upright;
        }

        //起き上がる耐久を超えるまで戻って起き上がった。また通さなくなる。重なっていた敵やプレイヤーは当たりが押し出す
        private void Rise()
        {
            shown.localRotation = upright;
            body.isTrigger = false;
            TempAudio.TempAudioPlay(HitSound, RisePitch);
            if (shaking != null) StopCoroutine(shaking);
            shaking = StartCoroutine(Shake());
        }

        /// <summary>膨らんでから縮んで消える。当たりは先に消し、すぐ通れるようにする</summary>
        private IEnumerator Burst()
        {
            body.enabled = false;
            reach.enabled = false;
            if (shaking != null) StopCoroutine(shaking);
            shown.localPosition = rest;
            gauge.gameObject.SetActive(false);

            Vector3 size = shown.localScale;
            for (float t = 0f; t < BurstSeconds; t += Time.deltaTime)
            {
                float k = t / BurstSeconds;
                float scale = k < 0.3f
                    ? Mathf.Lerp(1f, BurstSwell, k / 0.3f)
                    : Mathf.Lerp(BurstSwell, 0f, (k - 0.3f) / 0.7f);

                shown.localScale = size * scale;
                yield return null;
            }

            Destroy(gameObject);
        }

        //当たりを長さと厚みの向きへ広げた、素通りする当たり。柵と同じレイヤーに置き、地面を探す光線には当たらない
        private BoxCollider MakeReach()
        {
            var zone = new GameObject("Reach");
            zone.layer = gameObject.layer;
            zone.transform.SetParent(transform, false);

            var box = zone.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.center = body.center;
            box.size = body.size + new Vector3(ReachMargin * 2f, 0f, ReachMargin * 2f);
            return box;
        }

        private void ShowDurability()
        {
            gauge.Show(state.Ratio, state.IsFallen ? FallenFill : StandingFill);
        }
    }
}
