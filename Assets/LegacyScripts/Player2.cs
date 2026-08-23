using System;
using System.Collections;
using System.Collections.Generic;
using Assets.HeroEditor.Common.Scripts.CharacterScripts;
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

    public static Slider playerhp;
    [SerializeField] Button jumpbutton;
    [SerializeField] AudioSource footstep;
    public static AudioSource tempaudio;
    public Allmaity allmaity;
    //private static bool PL_singleton;
    private bool ATK_singleton = false;

    void Start()
    {
        playerrb = GetComponent<Rigidbody>();
        playerhp = GameObject.FindWithTag("HpSlider").GetComponent<Slider>();
        DontDestroyOnLoad(gameObject);

        /* if (PL_singleton == false)
         {
             DontDestroyOnLoad(gameObject);
             PL_singleton = true;
         }
         else Destroy(gameObject);
        */
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
        bool XL = pos.x > expos.x + 0.4, XS = pos.x < expos.y - 0.4, ZL = pos.z > expos.z + 0.4, ZS = pos.z < expos.w - 0.4;
        if (expos == null) return;
        if ((pos.x > expos.x) && !XL) playerrb.linearVelocity = Vector3.left * 0.5f;
        if ((pos.x < expos.y) && !XS) playerrb.linearVelocity = Vector3.right * 0.5f;
        if ((pos.z > expos.z) && !ZL) playerrb.linearVelocity = Vector3.back * 0.5f;
        if ((pos.z < expos.w) && !ZS) playerrb.linearVelocity = Vector3.forward * 0.5f;
        if (XL || XS || ZL || ZS || (Mathf.Abs(pos.y) > 4))
        {
            transform.position = new Vector3(-2, 0.65f, -2);
            playerrb.linearVelocity = Vector3.right * 2;
        }

        if (cool == false)//この中は停止可能
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
            Vector3 temp = new Vector3(horizontalKey, 0, verticalKey).normalized;

            if (temp.magnitude > 0.2)//足音
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
        if(other.gameObject.tag == "SceneTransition")
        {
            allmaity.Prepare_Button(other.gameObject.name); // 対象objの先頭"ST_"が命名規則
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.name == "CraftBench")
        {
            allmaity.Trash_Button("CraftInventory");
        }
        if (other.gameObject.tag == "SceneTransition")
        {
            allmaity.Trash_Button(other.gameObject.name); // 対象objの先頭"ST_"が命名規則
        }
    }

    public void HpChange()
    {
        
    }

    public void Player_Hurt(int damage, Vector3 direction) // 現状、ノックバックはほぼ固定
    {
        playerhp.value -= damage;
        var t = playerhp.transform.Find("Text_Value").GetComponent<TextMeshProUGUI>();
        t.text = playerhp.value.ToString() + "/" + playerhp.maxValue.ToString();
        playerrb.AddForce(direction.normalized * 100, ForceMode.Impulse);
        StartCoroutine(PlayerHurt_Graphic());
        if (playerhp.value <= 0) GetComponent<PlayerDeath>().DeathMainProcess();//死
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
        for(int i = 0; i < 2; i++)
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
        if (ATK_singleton) yield break;
        ATK_singleton = true;
        SideJab sj = GetComponentInChildren<SideJab>();


        if ((Mathf.Abs(joystick_atk.Horizontal) > 0.5f) && (joystick_atk.Horizontal * sj.transform.localScale.x > 0))//突き
        {
            if (joystick_atk.Vertical > 0.5) StartCoroutine(sj.SideJabPlay(1));
            if (joystick_atk.Vertical < -0.5) StartCoroutine(sj.SideJabPlay(-1));
            character.Jab();
        }
        else if ((Mathf.Abs(joystick_atk.Horizontal) < 0.5f) && (Mathf.Abs(joystick_atk.Vertical) < 0.5f)) //ケサ
        {
            character.Slash();
        }
        else //ステップ
        {
            playerrb.AddForce(new Vector3(joystick_atk.Horizontal * 3, 1, joystick_atk.Vertical * 3) * playerrb.mass, ForceMode.Impulse);
            yield return new WaitForSeconds(0.8f);
            AttackModeFinish();
            yield break;
        }
            TempAudio.TempAudioPlay("Fantasy_Game_Attack_Skill_Knife_Throw_B");

        StartCoroutine(weapon.GetComponent<Weapon>().AttackWeaponProcess(0.7f));
        //while (animator.GetCurrentAnimatorStateInfo(0).IsName("Slash")) { yield return null;}
        yield return new WaitForSeconds(0.7f);

        AttackModeFinish();
    }

}
