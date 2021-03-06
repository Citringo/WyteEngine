﻿using UnityEngine;
using System.Collections;
using UnityEngine.Tilemaps;

/// <summary>
/// キャラクターコントローラー２D
/// アタッチした時以下を追加
/// ・Animator
/// ・Rigidbody2D
/// ・BoxCollider
/// ・SpriteRenderer
/// </summary>
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(AudioSource))]
public class CharacterController2D : BaseBehaviour
{
	[Header("GROUND_LAYER")]
	public LayerMask groundLayer;
	[Header("CHARACTER_IMAGE_DIR")]
	public CharaImageDir charaImageDir;

	[Header("ANIMATION_NAME")]
	public string animNameJump = "Jump";
	public string animNameIdle = "Idle";
	public string animNameWalk = "Walk";

	[Header("CHARACTER_STATUS")]
	public float charaScale = 1.0f;
	public float charaHead = 1.0f;
	public float charaFoot = -1.0f;
	public float charaWidth = 1.0f;
	public float charaWidth2 = 1.0f;
	public float charaGravityScale = 1.0f;
	public float charaMoveSpeed = 5.0f;
	public float charaDashMultiplier = 2.0f;
	public float charaJumpScale = 10.0f;
	public float charaCeilingBouness = -1.0f;

	[Header("KEY_CONFIG")]
	public bool isInput = true;
	public float jumpThreshold = .5f;

	[Header("AUDIO")]
	public AudioClip ClipJump;
	public AudioClip ClipLand;
	public AudioClip ClipCeil;

	private Rigidbody2D rigid;
	private Animator animator;
	private AudioSource asource;
	private bool isDeath = false;
	private float lastDir = 1.0f;
	private float jumpTimer = 0;
	private string nowAnim;
	private bool prevIsGrounded, prevIsCeiling;

	/// <summary>
	/// キャラの向き
	/// </summary>
	public enum CharaImageDir
	{
		Right,
		Left
	}

	/// <summary>
	/// 開始処理
	/// </summary>
	void Start()
	{
		Init();
	}

	/// <summary>
	/// 更新処理
	/// </summary>
	void Update()
	{
		Animation();
		if (isInput)
			InputKey();
		prevIsCeiling = IsCeiling();
		prevIsGrounded = IsGrounded();
	}

	/// <summary>
	/// 初期化
	/// </summary>
	void Init()
	{
		rigid = gameObject.GetComponent<Rigidbody2D>();
		animator = gameObject.GetComponent<Animator>();
		asource = gameObject.GetComponent<AudioSource>();

		rigid.freezeRotation = true;
		rigid.gravityScale = charaGravityScale;
	}

	/// <summary>
	/// シーンビューにキャラ頭の線と床の線を表示させる
	/// </summary>
	void OnDrawGizmos()
	{
		Gizmos.color = IsGrounded() ? Color.red : Color.blue;
		Gizmos.DrawLine(FloorA, FloorB);

		var ceilingA = transform.position + new Vector3(-(charaWidth / 2), charaHead);
		var ceilingB = transform.position + new Vector3((charaWidth / 2), charaHead);

		Gizmos.color = IsCeiling() ? Color.red : Color.blue;
		Gizmos.DrawLine(CeilingA, CeilingB);

		var kickLA = transform.position + new Vector3(-(charaWidth / 2), charaHead);
		var kickLB = transform.position + new Vector3(-(charaWidth / 2), charaFoot);
		var kickRA = transform.position + new Vector3((charaWidth / 2), charaHead);
		var kickRB = transform.position + new Vector3((charaWidth / 2), charaFoot);

		Gizmos.color = CanKickLeft() ? Color.red : Color.blue;
		Gizmos.DrawLine(KickLA, KickLB);
		Gizmos.color = CanKickRight() ? Color.red : Color.blue;
		Gizmos.DrawLine(KickRA, KickRB);

	}

	private void OnGUI()
	{
		GUILayout.Label(rigid.velocity.ToString());
	}


	/// <summary>
	/// キー入力
	/// </summary>
	void InputKey()
	{
		if (isDeath)
			return;
		if (IsGrounded() && rigid.velocity != Vector2.zero) IsJumping = false;
		if (Input.GetKeyDown(KeyBind.Jump))
			Jump();

		Move(KeyBind.Arrow.x);

	}

	
	/// <summary>
	/// アニメーション制御
	/// </summary>
	void Animation()
	{
		if (isDeath)
			return;
		animator.SetFloat("WalkSpeedMultiplier", DashMultiplier);
		if (IsJumping)
			Play(animNameJump);
		else if (Mathf.Round(rigid.velocity.x) == 0.0f)
			Play(animNameIdle);
		else
			Play(animNameWalk);
	}

	void Play(string anim)
	{
		if (nowAnim == anim) return;
		nowAnim = anim;
		animator.StopPlayback();
		animator.Play(anim);
	}

	public bool IsJumping { get; private set; }

	/// <summary>
	/// ジャンプした時
	/// </summary>
	public void Jump()
	{
		if (IsGrounded())
		{
			var nowVec = rigid.velocity;
			rigid.velocity = new Vector3(nowVec.x, charaJumpScale);
			IsJumping = true;
			asource.PlayOneShot(ClipJump);
		}
		
		/*else if (CanKickLeft())
		{
			rigid
		}
		else if (CanKickRight())
		{
			rigid.AddForce((Vector2.left + Vector2.up) * 3000);
		}*/
		//TODO: プレイヤーの左右移動に加速度を取り入れ、外部からの圧力を無視しないようにする
	}

	public float DashMultiplier => Input.GetKey(KeyBind.Dash) ? charaDashMultiplier : 1;

	/// <summary>
	/// 移動
	/// </summary>
	public void Move(float rightSpeed)
	{

		if (rightSpeed != 0.0f)
			lastDir = (rightSpeed == 0) ? lastDir : rightSpeed;

		if (isDeath)
			return;

		var dir = rightSpeed * charaMoveSpeed * DashMultiplier;

		if (charaImageDir == CharaImageDir.Right)
			transform.localScale = new Vector3(lastDir * charaScale, charaScale);
		else
			transform.localScale = new Vector3(-lastDir * charaScale, charaScale);

		rigid.velocity = new Vector2(dir, rigid.velocity.y);

		if (IsCeiling() && !prevIsCeiling)
			asource.PlayOneShot(ClipCeil);


		if (IsGrounded() && !prevIsGrounded)
			asource.PlayOneShot(ClipLand);

		if (IsJumping && !Input.GetKey(KeyBind.Jump))
			rigid.velocity -= new Vector2(0, charaGravityScale * .4f);

		if (IsCeiling())
			rigid.velocity = new Vector2(rigid.velocity.x, charaCeilingBouness);
	}


	/// <summary>
	/// 地面についているかどうか
	/// </summary>
	/// <returns></returns>
	public bool IsGrounded()
	{
		var hit = Physics2D.Linecast(FloorA, FloorB, groundLayer);
		return hit;
	}

	/// <summary>
	/// 頭が当たったかどうか
	/// </summary>
	/// <returns></returns>
	public bool IsCeiling()
	{
		bool hit = Physics2D.Linecast(CeilingA, CeilingB, groundLayer);
		return hit;
	}

	public bool CanKickLeft() => Physics2D.Linecast(KickLA, KickLB, groundLayer);
	public bool CanKickRight() => Physics2D.Linecast(KickRA, KickRB, groundLayer);


	Vector3 FloorA => transform.position + new Vector3(-(charaWidth / 2), charaFoot);
	Vector3 FloorB => transform.position + new Vector3((charaWidth / 2), charaFoot);

	Vector3 CeilingA => transform.position + new Vector3(-(charaWidth / 2), charaHead);
	Vector3 CeilingB => transform.position + new Vector3((charaWidth / 2), charaHead);


	Vector3 KickLA => transform.position + new Vector3(-(charaWidth2 / 2), charaHead / 2);
	Vector3 KickLB => transform.position + new Vector3(-(charaWidth2 / 2), charaFoot / 2);

	Vector3 KickRA => transform.position + new Vector3((charaWidth2 / 2), charaHead / 2);
	Vector3 KickRB => transform.position + new Vector3((charaWidth2 / 2), charaFoot / 2);

	/// <summary>
	/// 死んだとき
	/// </summary>
	public void Death()
	{
		isDeath = true;
	}
}
