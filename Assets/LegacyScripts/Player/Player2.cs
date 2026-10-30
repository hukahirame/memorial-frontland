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
    [SerializeField] private FixedJoystick joystick;
    [SerializeField] private FixedJoystick joystick_atk;
    public static bool cool = false;
    private bool atk_mode = false;
    public bool on_ground = false;
    public static Rigidbody playerrb;
    public static float speed = 1;
    public Vector4 expos;
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

    void Start()
    {
        playerrb = GetComponent<Rigidbody>();
        BindHpView(GameObject.FindWithTag("HpSlider").GetComponent<Slider>());
    }

    void Update()
    {

        if (Physics.Raycast(transform.position + Vector3.down * 0.6f, Vector3.down, 0.2f)) //接地判定
        {
            on_ground = true;
            jumpbutton.interactable = true;
        }
        else { on_ground = false; jumpbutton.interactable = false; }

        //if (Input.GetMouseButtonUp(1)) StartCoroutine(Attack());

        if (weapon != null)  weapon.localEulerAngles = new Vector3(0, 0, joystick_atk.Vertical * 90);

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D))
        {
            horizontalKey = Input.GetAxis("Horizontal");
            verticalKey = Input.GetAxis("Vertical");
        }
        else
        {
            horizontalKey = joystick.Horizontal;
            verticalKey = joystick.Vertical;
        }


        if (Input.GetKeyUp(KeyCode.Space)) Jump();

        //緊急脱出用
        Vector3 pos = transform.position;
        FieldBounds bounds = Exposition.ToBounds(expos);
        if (bounds.HasEscaped(pos.x, pos.y, pos.z))
        {
            transform.position = new Vector3(-2, 0.65f, -2);
            playerrb.linearVelocity = Vector3.right * 2;
        }
        else
        {
            Nudge nudge = bounds.PushBack(pos.x, pos.z);
            if (!nudge.None) playerrb.linearVelocity = new Vector3(nudge.X, 0, nudge.Z) * 0.5f;
        }

        if (cool == false && !travel.IsTraveling)//この中は停止可能
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
    }

    public void Jump()
    {
        if (jumpbutton.interactable == false) return;
        playerrb.AddForce(horizontalKey, playerrb.mass * 4, verticalKey, ForceMode.Impulse);
        jumpbutton.interactable = false;
        Debug.Log("Jump：" + new Vector3(horizontalKey,playerrb.mass * 4, verticalKey));
    }

    private void OnTriggerEnter(Collider other)
    {
        // 最終的に、Allmaity.csに衝突した名前を全て送り付ける仕様にするはず
        if (other.gameObject.name == "CraftBench")
        {
            allmaity.Prepare_Button("CraftInventory");
        }
        if (other.TryGetComponent(out Exit exit))
        {
            EnterExit(exit);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.name == "CraftBench")
        {
            allmaity.Trash_Button("CraftInventory");
        }
    }

    /// <summary>出口に入ったら移る。受注が無くて根源へ行けないときは文字だけ出す</summary>
    private void EnterExit(Exit exit)
    {
        Quest ordered = QuestManager.Quests.Find(QuestManager.ordered_id);
        if (travel.Through(exit.Kind, ordered)) return;

        GameObject.Find("MiddleText").GetComponent<MiddleText>().Midtxt_Anim("受注されたクエストがありません");
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
        if (Hp.IsDead || invulnerability.IsActive(Time.time)) return;

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
        while (invulnerability.IsActive(Time.time))
        {
            foreach (var renderer in renderers) renderer.color = new Color(1, 0.2f, 0.2f, renderer.color.a);
            yield return new WaitForSeconds(0.3f);

            foreach (var renderer in renderers) renderer.color = new Color(1, 1, 1,renderer.color.a);
            yield return new WaitForSeconds(0.2f);
        }
    }
    public void AttackDown(){ atk_mode = true; /*speed *= 0.64f;*/ }//いらないかも

    public void AttackUp() { /*speed *= 1.5625f;*/ }//いらないかも


    private void AttackModeFinish()
    {
        atk_mode = false;
        ATK_singleton = false;
        animator.transform.localEulerAngles = Vector3.zero;
    }
    public IEnumerator Attack()
    {
        //離した瞬間の倒し方。呼んだ側はこの直後にスティックを 0 に戻す
        float horizontal = joystick_atk.Horizontal;
        float vertical = joystick_atk.Vertical;

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
        atk_mode = true;
        attackStartedAt = Time.time;
        SideJab sj = GetComponentInChildren<SideJab>();

        AttackKind kind = AttackRule.Choose(horizontal, vertical, sj.transform.localScale.x);

        if (kind == AttackKind.Step)
        {
            attackDuration = AttackTiming.StepSeconds;
            playerrb.AddForce(new Vector3(horizontal * 3, 1, vertical * 3) * playerrb.mass, ForceMode.Impulse);
            yield return new WaitForSeconds(AttackTiming.StepSeconds);
            yield break;
        }

        attackDuration = AttackTiming.SwingSeconds;
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

        //当たるのは振りの打点のあたりだけ
        yield return new WaitForSeconds(AttackTiming.WindupSeconds);
        StartCoroutine(weapon.GetComponent<Weapon>().AttackWeaponProcess(AttackTiming.ActiveSeconds));
        yield return new WaitForSeconds(AttackTiming.SwingSeconds - AttackTiming.WindupSeconds);
    }

}
