using System;
using System.Collections;
using System.Collections.Generic;
using Assets.HeroEditor.Common.Scripts.CharacterScripts;
using MemorialFloor.Domain;
using MemorialFloor.Game;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Player2 : MonoBehaviour
{
    /// <summary>攻撃のスティックの代わりに押すキー。向きは矢印キーで決める</summary>
    public const KeyCode AttackKey = KeyCode.RightShift;

    /// <summary>画面の案内に出す、攻撃のキーの名前。AttackKey を変えたら合わせる</summary>
    public const string AttackKeyName = "右Shift";

    /// <summary>ジャンプボタンの代わりに押すキー。離したときに跳ぶ</summary>
    public const KeyCode JumpKey = KeyCode.Space;

    /// <summary>回避攻撃の音の高さ。剣の振りと同じ音を高くして、軽く鳴らす</summary>
    private const float RollPitch = 1.3f;

    /// <summary>柵を壊す Allmaity ボタンの名前</summary>
    private const string FenceButton = "Fence";

    /// <summary>手の届く範囲にある柵。Allmaity の「柵を壊す」で、いちばん近いものを壊す</summary>
    private readonly List<Fence> fencesInReach = new List<Fence>();

    [SerializeField] private FixedJoystick joystick;
    [SerializeField] private FixedJoystick joystick_atk;
    public static bool cool = false;
    private bool atk_mode = false;
    public bool on_ground = false;
    public static Rigidbody playerrb;
    public static float speed = 1;
    public Vector4 expos;

    /// <summary>これより高く上がったら場の外とみなす。場所に着くたびに入れ替わる</summary>
    private float ceiling = FieldBounds.EscapeHeight;
    [SerializeField] public Character character;
    [SerializeField] private Animator animator;
    [SerializeField] private Transform body;
    [SerializeField] private Transform weapon;

    public static float horizontalKey;
    public static float verticalKey;

    /// <summary>体力。所有者はここ1つ。Slider は表示であって正典ではない</summary>
    public static readonly Health Hp = new Health();

    /// <summary>体力の表示。シーンごとに取り直す</summary>
    public static Slider playerhp;
    [SerializeField] Button jumpbutton;
    [SerializeField] AudioSource footstep;
    public static AudioSource tempaudio;
    public Allmaity allmaity;
    [SerializeField] private AreaTravel travel;
    private bool ATK_singleton = false;

    /// <summary>被弾したあとの無敵。点滅もこの間だけ続く</summary>
    private readonly Invulnerability invulnerability = new Invulnerability();

    /// <summary>出ている攻撃が始まった時刻と全体の長さ。先行入力を受けるかの判定に使う</summary>
    private float attackStartedAt;
    private float attackDuration;

    /// <summary>先行入力。出ている攻撃が終わったら、この倒し方で次を出す</summary>
    private bool hasBufferedAttack;
    private float bufferedHorizontal;
    private float bufferedVertical;

    /// <summary>体の固い当たり。作業台と出口は、これが重なったときだけ数える。倒れているあいだは切られる</summary>
    private Collider solidBody;

    /// <summary>倒れている間と場所を移っている間は、歩くのも振るのも、持ち物を使うのも止める</summary>
    public bool CanAct
    {
        get { return !cool && !travel.IsTraveling; }
    }

    /// <summary>出ている攻撃。出ていなければ null</summary>
    private AttackMove? swinging;

    /// <summary>いまの行動。振っているあいだはその攻撃、構えているあいだは構え歩き。どちらでもなければ null</summary>
    public AttackMove? Doing
    {
        get
        {
            if (swinging.HasValue) return swinging;

            return IsBracing ? AttackMove.StanceWalk : (AttackMove?)null;
        }
    }

    /// <summary>構えているあいだ、いまの向きで離すと出る攻撃。構えていなければ null</summary>
    public AttackMove? Aimed
    {
        get
        {
            if (!IsBracing) return null;

            Vector2 aim = AttackAim(joystick_atk);
            return AttackRule.MoveOf(aim.x, aim.y, body.localScale.x);
        }
    }

    /// <summary>攻撃を押したまま、まだ離していない。このあいだは体の向きを変えずに歩く</summary>
    private bool IsBracing
    {
        get { return CanAct && atk_mode && !ATK_singleton; }
    }

    /// <summary>最後に歩いた向き。止まっても残る。柵はこの向きの先に立てる</summary>
    /// <summary>絵が向いている横の側。右なら 1、左なら -1。絵は左右にしか向かないので、柵はこの側に立てる</summary>
    public float Side
    {
        get { return body.localScale.x < 0f ? -1f : 1f; }
    }

    /// <summary>体の固い当たり。倒れているあいだは切られる</summary>
    public Collider Body
    {
        get { return solidBody; }
    }

    void Start()
    {
        playerrb = GetComponent<Rigidbody>();
        BindHpView(GameObject.FindWithTag("HpSlider").GetComponent<Slider>());
        solidBody = SolidBody();
        KeyHint.Put((RectTransform)jumpbutton.transform, JumpKey.ToString());
        if (joystick_atk.TryGetComponent(out JoystickEffect_ATK effect)) effect.Bind(this);
    }

    private Collider SolidBody()
    {
        foreach (MeshCollider found in GetComponentsInChildren<MeshCollider>())
        {
            if (!found.isTrigger) return found;
        }

        return null;
    }

    /// <summary>着いた場所の、場の範囲と場の外とみなす高さを受け取る</summary>
    public void Bind(Area area)
    {
        expos = area.Exposition;
        ceiling = area.Ceiling;
    }

    /// <summary>いま地面に立っているか。on_ground は Update でしか書き直さないので、置き直した直後は古い</summary>
    public bool Grounded()
    {
        return Physics.Raycast(transform.position + Vector3.down * 0.6f, Vector3.down, 0.2f);
    }

    void Update()
    {
        ForgetGoneFences();

        if (Grounded()) //接地判定
        {
            on_ground = true;
            jumpbutton.interactable = true;
        }
        else { on_ground = false; jumpbutton.interactable = false; }

        //if (Input.GetMouseButtonUp(1)) StartCoroutine(Attack());

        if (weapon != null)  weapon.localEulerAngles = new Vector3(0, 0, AttackAim(joystick_atk).y * 90);

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D))
        {
            //矢印キーは攻撃の向きに使う。GetAxisRaw は矢印も拾うので、WASD を直に読む
            horizontalKey = KeyAxis(KeyCode.D, KeyCode.A);
            verticalKey = KeyAxis(KeyCode.W, KeyCode.S);
        }
        else
        {
            horizontalKey = joystick.Horizontal;
            verticalKey = joystick.Vertical;
        }


        if (Input.GetKeyUp(JumpKey)) Jump();

        //攻撃のキーは攻撃のスティックと同じで、押して構え、離した瞬間の向きへ振る
        if (Input.GetKeyDown(AttackKey)) AttackDown();
        if (Input.GetKeyUp(AttackKey))
        {
            AttackUp();
            StartCoroutine(Attack());
        }

        //緊急脱出用。場の範囲は着いたときに入れ替わるので、移っているあいだは見ない。止めた体には速さも入れられない
        if (!travel.IsTraveling)
        {
            Vector3 pos = transform.position;
            FieldBounds bounds = Exposition.ToBounds(expos, ceiling);
            if (bounds.HasEscaped(pos.x, pos.y, pos.z))
            {
                transform.position = new Vector3(-2, 0.65f, -2);
                playerrb.linearVelocity = Vector3.right * 2;
            }
            else
            {
                //押し戻すのは横だけ。縦まで 0 にすると、壁際で跳んでも宙で止まり、吹き飛ばしも消える
                Nudge nudge = bounds.PushBack(pos.x, pos.z);
                if (!nudge.None) playerrb.linearVelocity = new Vector3(nudge.X * 0.5f, playerrb.linearVelocity.y, nudge.Z * 0.5f);
            }
        }

        if (CanAct)//この中は停止可能
        {
            if (!atk_mode)
            {
                if ((horizontalKey > 0.1) && (body.localScale.x == -1))//右転換
                {
                    body.localScale = Vector3.one;
                    // weapon.localEulerAngles = new Vector3(0, 0, 20);
                    // weapon.localPosition = new Vector3(0.3f, -0.196f, 0.01f);
                    // body.transform.rotation = Quaternion.Euler(0, 0, 0);

                }
                else if ((horizontalKey < -0.1) && (body.localScale.x == 1)) //左転換
                {
                    body.localScale = new Vector3(-1, 1, 1);
                    // weapon.localEulerAngles = new Vector3(0, 180, 20);
                    // weapon.localPosition = new Vector3(-0.3f, -0.196f, 0.01f);
                    // body.transform.rotation = Quaternion.Euler(0, 180, 0);

                }
            }
            else //atk長押し中
            {
            
            }
            Heading heading = MoveRule.Direction(horizontalKey, verticalKey);
            Vector3 temp = new Vector3(heading.X, 0, heading.Z);

            if (!heading.IsStill)//足音
            {
                if(on_ground ||(character.GetState() == CharacterState.Idle))  character.SetState(CharacterState.Walk);
                if (!footstep.loop)  footstep.loop = true;
                if(!footstep.isPlaying)  footstep.Play();
            }
            else if(footstep.loop)
            {
                character.SetState(CharacterState.Idle);
                footstep.loop = false;
                footstep.Stop();
            }
            transform.Translate(speed * temp * Time.deltaTime);
        }
        else
        {
            //歩けないあいだは足音も止める。上の止め方は手を離したときにしか通らない。倒れた姿はそのまま残す
            if (footstep.loop)
            {
                footstep.loop = false;
                footstep.Stop();
            }
            if (!cool && character.GetState() == CharacterState.Walk) character.SetState(CharacterState.Idle);
        }
    }

    public void Jump()
    {
        //ボタンの呼び先は Core に残る古い Player で、移動の入口を持たない。動けるかは本物のプレイヤーで見る
        if (jumpbutton.interactable == false || !playerrb.GetComponent<Player2>().CanAct) return;
        playerrb.AddForce(horizontalKey, playerrb.mass * 4, verticalKey, ForceMode.Impulse);
        jumpbutton.interactable = false;
        Debug.Log("Jump：" + new Vector3(horizontalKey,playerrb.mass * 4, verticalKey));
    }

    //剣や視界確保の当たりもプレイヤーの子で、その出入りもここへ届く。作業台と出口は体の固い当たりが重なったときだけ数える。
    //入るときは外接する箱で見る（入った瞬間の浅い重なりも取りこぼさない）。出るときは形で見る（剣だけが抜けても消さない）
    private void OnTriggerEnter(Collider other)
    {
        if (solidBody == null || !solidBody.enabled || !solidBody.bounds.Intersects(other.bounds)) return;

        // 最終的に、Allmaity.csに衝突した名前を全て送り付ける仕様にするはず
        if (other.gameObject.name == "CraftBench")
        {
            allmaity.Prepare_Button("CraftInventory");
        }
        if (other.TryGetComponent(out Exit exit))
        {
            EnterExit(exit);
        }
        Fence fence = FenceOf(other);
        if (fence != null && !fencesInReach.Contains(fence))
        {
            fencesInReach.Add(fence);
            allmaity.Prepare(FenceButton, "柵を壊す", DemolishFence);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.name == "CraftBench" && !BodyInside(other))
        {
            allmaity.Trash_Button("CraftInventory");
        }
        Fence fence = FenceOf(other);
        if (fence != null && !BodyInside(other))
        {
            fencesInReach.Remove(fence);
            if (fencesInReach.Count == 0) allmaity.Trash_Button(FenceButton);
        }
    }

    /// <summary>other が柵の手の届く範囲の当たりなら、その柵</summary>
    private static Fence FenceOf(Collider other)
    {
        Fence fence = other.GetComponentInParent<Fence>();
        return fence != null && fence.Reach == other ? fence : null;
    }

    /// <summary>手の届く柵のうち、いちばん近いものを壊す。Allmaity の「柵を壊す」から呼ぶ</summary>
    private void DemolishFence()
    {
        ForgetGoneFences();

        Fence nearest = null;
        foreach (Fence fence in fencesInReach)
        {
            if (nearest == null || FlatDistance(fence) < FlatDistance(nearest)) nearest = fence;
        }
        if (nearest == null) return;

        nearest.Demolish();
        fencesInReach.Remove(nearest);
        if (fencesInReach.Count == 0) allmaity.Trash_Button(FenceButton);
    }

    private float FlatDistance(Fence fence)
    {
        Vector3 offset = fence.transform.position - transform.position;
        return new Vector2(offset.x, offset.z).magnitude;
    }

    //場所を移ると、手の届いていた柵は場所ごと消え、出たことは届かない。消えた柵は忘れ、無くなったらボタンも消す
    private void ForgetGoneFences()
    {
        if (fencesInReach.Count == 0 || fencesInReach.RemoveAll(fence => fence == null) == 0) return;

        if (fencesInReach.Count == 0) allmaity.Trash_Button(FenceButton);
    }

    private bool BodyInside(Collider other)
    {
        return solidBody != null && solidBody.enabled && Physics.ComputePenetration(
            solidBody, solidBody.transform.position, solidBody.transform.rotation,
            other, other.transform.position, other.transform.rotation, out _, out _);
    }

    /// <summary>出口に入ったら、受注にかかわらず隣の場所へ移る</summary>
    private void EnterExit(Exit exit)
    {
        travel.Through(exit.Kind, RootsManager.RootIds());
    }

    public void HpChange()
    {
        
    }

    /// <summary>表示を差し替える。上限と初期値は最初に見つけた Slider から一度だけ取る</summary>
    public static void BindHpView(Slider view)
    {
        playerhp = view;
        if (view == null) return;

        if (!Hp.IsReady)
        {
            Hp.SetMax((int)view.maxValue);
            Hp.SetCurrent((int)view.value);
        }

        RefreshHpView();
    }

    /// <summary>Health の値を表示へ書き出す</summary>
    public static void RefreshHpView()
    {
        if (playerhp == null) return;

        playerhp.value = Hp.Current;

        Transform label = playerhp.transform.Find("Text_Value");
        if (label != null)
            label.GetComponent<TextMeshProUGUI>().text = Hp.Current + "/" + Hp.Max;
    }

    public void Player_Hurt(int damage, Vector3 direction) // 現状、ノックバックはほぼ固定
    {
        //場所を移っているあいだは動けないので当てない。倒れて拠点へ戻される途中もここに入る
        if (Hp.IsDead || travel.IsTraveling || invulnerability.IsActive(Time.time)) return;

        invulnerability.Begin(Time.time);
        Hp.Take(damage);
        RefreshHpView();
        playerrb.AddForce(direction.normalized * 100, ForceMode.Impulse);
        StartCoroutine(PlayerHurt_Graphic());
        if (Hp.IsDead) GetComponent<PlayerDeath>().DeathMainProcess();//死
        /*{
            cool = true;
            character.SetState(CharacterState.DeathB);
            transform.GetComponentInChildren<Wink>().DeathEyesClose();
            transform.GetComponentInChildren<MeshCollider>().enabled = false;
            if(transform.localScale.x == -1) transform.position += Vector3.down * 0.3f;
            playerrb.constraints = RigidbodyConstraints.FreezeAll;
            GameObject.Find("BigText").GetComponent<BigText>().Bigtxt_Anim("体力が0になりました","失敗");
        }*/
    }

    private IEnumerator PlayerHurt_Graphic()
    {
        Wink.play_eyeclose = true;
        var renderers = GetComponentsInChildren<SpriteRenderer>();

        //髪や目は色を付けてあるので、白ではなく元の色へ戻す
        var colors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++) colors[i] = renderers[i].color;

        while (invulnerability.IsFlashing(Time.time))
        {
            foreach (var renderer in renderers) renderer.color = new Color(1, 0.2f, 0.2f, renderer.color.a);
            yield return new WaitForSeconds(0.3f);

            for (int i = 0; i < renderers.Length; i++) renderers[i].color = colors[i];
            yield return new WaitForSeconds(0.2f);
        }
    }

    /// <summary>
    /// 倒れて戻される。動けるようになってからしばらく当たらず、被弾のあとと同じく点滅する。
    /// 戻った地点のまわりに攻め込んだ敵が居ても、離れる間を残す。移り始めてから呼ぶ
    /// </summary>
    public void GuardAfterRevive()
    {
        StartCoroutine(GuardUntilRevived());
    }

    private IEnumerator GuardUntilRevived()
    {
        //移り終わったフレームに当たらないよう、数え始めるまでのあいだも無敵にしておく
        invulnerability.Hold();
        while (travel.IsTraveling) yield return null;

        invulnerability.Revived(Time.time);
        yield return PlayerHurt_Graphic();
    }
    /// <summary>攻撃の向き。攻撃のスティックを倒していればその向き、倒していなければ矢印キーの向き。長さは 1 まで</summary>
    public static Vector2 AttackAim(Joystick stick)
    {
        if (stick.Direction != Vector2.zero) return stick.Direction;

        return new Vector2(KeyAxis(KeyCode.RightArrow, KeyCode.LeftArrow), KeyAxis(KeyCode.UpArrow, KeyCode.DownArrow)).normalized;
    }

    private static float KeyAxis(KeyCode positive, KeyCode negative)
    {
        return (Input.GetKey(positive) ? 1f : 0f) - (Input.GetKey(negative) ? 1f : 0f);
    }

    public void AttackDown(){ atk_mode = true; /*speed *= 0.64f;*/ }//いらないかも

    public void AttackUp() { /*speed *= 1.5625f;*/ }//いらないかも


    private void AttackModeFinish()
    {
        atk_mode = false;
        ATK_singleton = false;
        swinging = null;
        animator.transform.localEulerAngles = Vector3.zero;
    }
    public IEnumerator Attack()
    {
        //離した瞬間の向き。スティックから呼ぶと、呼んだ側はこの直後にスティックを 0 に戻す
        Vector2 aim = AttackAim(joystick_atk);
        float horizontal = aim.x;
        float vertical = aim.y;

        if (ATK_singleton)
        {
            if (AttackTiming.IsBuffered(Time.time - attackStartedAt, attackDuration))
            {
                hasBufferedAttack = true;
                bufferedHorizontal = horizontal;
                bufferedVertical = vertical;
            }
            yield break;
        }
        ATK_singleton = true;

        while (true)
        {
            yield return Swing(horizontal, vertical);

            if (!hasBufferedAttack) break;
            hasBufferedAttack = false;
            horizontal = bufferedHorizontal;
            vertical = bufferedVertical;
        }

        AttackModeFinish();
    }

    private IEnumerator Swing(float horizontal, float vertical)
    {
        if (!CanAct) yield break;

        atk_mode = true;
        attackStartedAt = Time.time;
        SideJab sj = GetComponentInChildren<SideJab>();

        AttackKind kind = AttackRule.Choose(horizontal, vertical, sj.transform.localScale.x);
        swinging = AttackRule.MoveOf(horizontal, vertical, sj.transform.localScale.x);

        if (kind == AttackKind.Step)
        {
            attackDuration = AttackTiming.StepSeconds;
            playerrb.AddForce(new Vector3(horizontal * 3, 1, vertical * 3) * playerrb.mass, ForceMode.Impulse);
            yield return new WaitForSeconds(AttackTiming.StepSeconds);
            yield break;
        }

        if (kind == AttackKind.Roll)
        {
            attackDuration = RollRule.Seconds;
            yield return Roll(vertical, sj.transform.localScale.x);
            yield break;
        }

        attackDuration = kind == AttackKind.Thrust ? AttackTiming.ThrustSeconds : AttackTiming.SwingSeconds;
        if (kind == AttackKind.Thrust)
        {
            int swing = AttackRule.ThrustDirection(vertical);
            if (swing != 0) StartCoroutine(sj.SideJabPlay(swing));
            character.Jab();
        }
        else
        {
            character.Slash();
        }
        TempAudio.TempAudioPlay("Fantasy_Game_Attack_Skill_Knife_Throw_B");

        //当たるのは振りの打点のあたりだけ。振りかぶっているあいだに倒れたり移り始めたりしたら出さない
        yield return new WaitForSeconds(AttackTiming.WindupSeconds);
        if (!CanAct) yield break;
        StartCoroutine(weapon.GetComponent<Weapon>().AttackWeaponProcess(AttackTiming.ActiveSeconds));
        yield return new WaitForSeconds(attackDuration - AttackTiming.WindupSeconds);
    }

    //手前か奥へ抜ける。回り始めから少しのあいだ当たらず、回っているあいだは体のまわりに当てる。見た目は RollStaging が受け持つ
    private IEnumerator Roll(float vertical, float facing)
    {
        invulnerability.Dodge(Time.time);
        character.Slash();
        TempAudio.TempAudioPlay("Fantasy_Game_Attack_Skill_Knife_Throw_B", RollPitch);

        float depth = vertical > 0f ? 1f : -1f;
        //抜ける側へ、体の前から回る
        float turn = -360f * depth * Mathf.Sign(facing);
        RollStaging staging = new RollStaging(animator.transform, transform, turn, FeetHeight());
        Weapon blade = weapon.GetComponent<Weapon>();
        float elapsed = 0f;
        float travelled = 0f;
        bool striking = false;

        while (elapsed < RollRule.Seconds && CanAct)
        {
            elapsed = Mathf.Min(elapsed + Time.deltaTime, RollRule.Seconds);
            float progress = elapsed / RollRule.Seconds;

            float reach = RollRule.Travelled(progress) * RollRule.Distance;
            transform.Translate(0f, 0f, (reach - travelled) * depth);
            travelled = reach;

            staging.Pose(progress);

            if (!striking && elapsed >= RollRule.StrikeFrom)
            {
                striking = true;
                StartCoroutine(blade.SpinStrikeProcess(RollRule.StrikeSeconds, RollRule.Radius));
            }

            yield return null;
        }

        staging.Restore();
    }

    /// <summary>足もとの高さ。体の固い当たりの下端</summary>
    private float FeetHeight()
    {
        return solidBody != null ? solidBody.bounds.min.y : transform.position.y;
    }

}
