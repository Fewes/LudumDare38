using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ComboTracker
{
	public delegate void ComboFinishedAction(string name);	public event ComboFinishedAction OnComboFinished;

	public float						carryTime = 0.5f;
	public bool							debug;

	Dictionary<string, CombatInput[]>	combos;
	List<CombatInput>					stackedInputs;
	float								timer;

	public ComboTracker ()
	{
		combos			= new Dictionary<string, CombatInput[]>();
		stackedInputs	= new List<CombatInput>();
	}

	public void RegisterCombo (string name, CombatInput[] inputs)
	{
		combos.Add(name, inputs);

		// TODO: Sort combos by length
	}

	public bool StackInput (CombatInput input)
	{
		timer = carryTime;

		stackedInputs.Add(input);

		return EvaluateCombo();
	}

	bool EvaluateCombo()
	{
		foreach (var combo in combos)
		{
			// Input count check
			if (stackedInputs.Count < combo.Value.Length)
				continue;

			// Reverse check the stacked inputs vs the combo
			bool isCombo = true;
			int u = stackedInputs.Count - 1;
			for (int i = combo.Value.Length - 1; i >= 0; )
			{
				if (combo.Value[i] != stackedInputs[u])
				{
					isCombo = false;
					break;
				}
				i--;
				u--;
			}

			if (isCombo)
			{
				if (OnComboFinished != null)
					OnComboFinished(combo.Key);

				if (debug)
				{
					string c = "";

					foreach (var input in combo.Value)
						c += input.ToString() + " + ";

					c = c.Remove(c.Length - 3);

					Debug.Log("Combo " + combo.Key + " inputted: " + c);
				}

				// Found a combo, reset and return
				Reset();
				return true;
			}
		}
		// Not a combo
		return false;
	}

	public void Update (float deltaTime)
	{
		timer -= Time.deltaTime;

		if (timer <= 0)
			Reset();
	}

	public void Reset ()
	{
		stackedInputs.Clear();
	}
}

public enum AttackType
{
	None,
	LightA,
	LightB,
	HeavyA,
	Launcher,
	Pusher
}

public enum CombatInput
{
	Up,
	Down,
	Left,
	Right,
	AttackL,
	AttackH,
	Jump
}

[System.Serializable]
public struct AttackDamage
{
	public int lightAttack;
	public int heavyAttack;
	public int lightComboFinish;
	public int launcher;
	public int slammer;
	public int booster;
	public int pusher;
}

public class Player : Damageable
{
	// Parameters
	[Range(0, 1)]
	[SerializeField] private float			m_CameraDrag = 1f;
	[Range(0, 15)]
	[SerializeField] private float			m_CameraDistance = 5f;
	[SerializeField] private float			m_CameraPanDistance = 5f;

	[SerializeField] private AttackDamage	m_AttackDamage;

	[Header("Sounds")]
	[SerializeField] private AudioClip		m_JumpSound;
	[SerializeField] private AudioClip		m_DodgeSound;
	[SerializeField] private AudioClip		m_SaultSound;
	[SerializeField] private AudioClip		m_FootstepSound;
	[SerializeField] private AudioClip		m_AttackL1Sound;
	[SerializeField] private AudioClip		m_AttackL2Sound;
	[SerializeField] private AudioClip		m_AttackH1Sound;

	// Components
	private Transform						m_Camera;
	private CharController					m_Controller;
	private SpriteRenderer					m_Renderer;
	private Tile							m_Tile;
	private Animator						m_Animator;
	private Transform						m_CameraTarget;
	private HurtBox							m_HurtBoxR;
	private HurtBox							m_HurtBoxL;

	// Input
	private Vector2							m_InputCameraVec;
	private bool							m_WasTriggerDown;

	// Attack variables
	private ComboTracker					m_ComboTracker;
	private AttackType						m_AttackType;
	private float							m_AttackTimer;
	private float							m_MashSpeed = 1;
	private float							m_MashDecay = 2.0f;

	private bool							m_InputUpPrev;
	private bool							m_InputDownPrev;
	private bool							m_InputLeftPrev;
	private bool							m_InputRightPrev;

	// Use this for initialization
	new void Start ()
	{
		base.Start();

		m_Camera = Camera.main.transform;
		m_Controller = GetComponent<CharController>();
		m_Renderer = transform.Find("Renderer").GetComponent<SpriteRenderer>();
		m_Animator = m_Renderer.gameObject.GetComponent<Animator>();
		m_Tile = GetComponentInChildren<Tile>();
		m_CameraTarget = transform.Find("CameraTarget");
		m_HurtBoxR = transform.Find("HurtBoxR").GetComponent<HurtBox>();
		m_HurtBoxL = transform.Find("HurtBoxL").GetComponent<HurtBox>();

		m_ComboTracker	= new ComboTracker();
		//m_ComboTracker.debug = true;
		m_ComboTracker.RegisterCombo("L H L",		new CombatInput[3] { CombatInput.AttackL,	CombatInput.AttackH,	CombatInput.AttackL });
		m_ComboTracker.RegisterCombo("L L H",		new CombatInput[3] { CombatInput.AttackL,	CombatInput.AttackL,	CombatInput.AttackH });
		//m_ComboTracker.RegisterCombo("Launcher",	new CombatInput[3] { CombatInput.Down,		CombatInput.Up,			CombatInput.AttackH });
		m_ComboTracker.RegisterCombo("Launcher",	new CombatInput[3] { CombatInput.AttackL,	CombatInput.Up,		CombatInput.AttackH });
		m_ComboTracker.RegisterCombo("Slammer",		new CombatInput[3] { CombatInput.AttackL,	CombatInput.Down,	CombatInput.AttackH });
		//m_ComboTracker.RegisterCombo("Pusher R",	new CombatInput[3] { CombatInput.Left,		CombatInput.Right,		CombatInput.AttackH });
		//m_ComboTracker.RegisterCombo("Pusher L",	new CombatInput[3] { CombatInput.Right,		CombatInput.Left,		CombatInput.AttackH });
		m_ComboTracker.RegisterCombo("Booster R",	new CombatInput[3] { CombatInput.Right,		CombatInput.Right,		CombatInput.AttackL });
		m_ComboTracker.RegisterCombo("Booster L",	new CombatInput[3] { CombatInput.Left,		CombatInput.Left,		CombatInput.AttackL });
		m_ComboTracker.RegisterCombo("Booster D",	new CombatInput[3] { CombatInput.Down,		CombatInput.Down,		CombatInput.AttackL });
		m_ComboTracker.RegisterCombo("Pusher R",	new CombatInput[3] { CombatInput.AttackL,	CombatInput.Right,	CombatInput.AttackH });
		m_ComboTracker.RegisterCombo("Pusher L",	new CombatInput[3] { CombatInput.AttackL,	CombatInput.Left,	CombatInput.AttackH });
		m_ComboTracker.OnComboFinished += ComboTracker_OnComboFinished;

		m_HurtBoxR.OnHurt += HurtBoxR_OnHurt;
		m_HurtBoxL.OnHurt += HurtBoxL_OnHurt;

		m_Controller.OnAirborne += OnAirborne;
		m_Controller.OnAttackH	+= OnAttackH;
		m_Controller.OnAttackL	+= OnAttackL;
		m_Controller.OnDodge	+= OnDodge;
		m_Controller.OnGrounded	+= OnGrounded;
		m_Controller.OnJump		+= OnJump;
		m_Controller.OnJump		+= OnAirJump;
		m_Controller.OnSault	+= OnSault;
	}

	public Vector3 GetCameraTargetPosition ()
	{
		return m_CameraTarget.position + new Vector3(m_InputCameraVec.x, -m_InputCameraVec.y, 0) * m_CameraPanDistance;
	}

	public float GetCameraDistance ()
	{
		return m_CameraDistance;
	}

	public float GetCameraDrag ()
	{
		return m_CameraDrag;
	}

	private void HurtBoxR_OnHurt(Damageable damageable)
	{
		ResetAirResources();

        // Hit effect 1
        //var impact = PoolManager.GetPooledObjectS("AnimatedSprites", "Impact1");
        //impact.transform.position = (damageable.GetCenter() + GetCenter()) * 0.5f;
        //impact.GetComponent<OneShotSprite>().spriteRenderer.flipX = !m_Controller.IsFacingRight();

        // Hit effect 2
        //var distort = PoolManager.GetPooledObjectS("Particle Systems", "ImpactDistort1");
        //distort.transform.position = damageable.GetCenter();

        // Hit effect 3
        var impact = PoolManager.GetPooledObjectS("AnimatedSprites", "HitFlash");
        impact.transform.position = (damageable.GetCenter() + GetCenter()) * 0.5f;
        impact.transform.rotation = Quaternion.Euler(0, 0, Random.Range(0, 4) * 90f);
        //impact.GetComponent<OneShotSprite>().spriteRenderer.flipX = !m_Controller.IsFacingRight();
    }

	private void HurtBoxL_OnHurt(Damageable damageable)
	{
		ResetAirResources();

        // Hit effect 1
        //var impact = PoolManager.GetPooledObjectS("AnimatedSprites", "Impact1");
        //impact.transform.position = (damageable.GetCenter() + GetCenter()) * 0.5f;
        //impact.GetComponent<OneShotSprite>().spriteRenderer.flipX = !m_Controller.IsFacingRight();

        // Hit effect 2
        //var distort = PoolManager.GetPooledObjectS("Particle Systems", "ImpactDistort1");
        //distort.transform.position = damageable.GetCenter();

        // Hit effect 3
        var impact = PoolManager.GetPooledObjectS("AnimatedSprites", "HitFlash");
        impact.transform.position = damageable.GetCenter();
        impact.GetComponent<OneShotSprite>().spriteRenderer.flipX = !m_Controller.IsFacingRight();
    }

    void ResetAirResources ()
	{
		m_Controller.airResources = 3;
		m_Controller.hasDoubleJumped = false;
		m_Controller.hasDodgedInAir = false;
	}

	// Update is called once per frame
	void Update ()
	{
		// Update timers
		m_AttackTimer -= Time.deltaTime;

		// Update combo
		m_ComboTracker	.Update(Time.deltaTime);

		if (!m_Controller.isAttacking && m_AttackTimer < 0)
		{
			//m_Attack = 0;
			m_AttackType = AttackType.None;
		}

		bool triggerDown = false;

		if (IsAlive())
		{
			// Gather input
			if (!m_Controller.IsImmobilized())
			{
				m_Controller.SetInputVecX(Input.GetAxis("Horizontal"));
				m_Controller.SetInputVecY(Input.GetAxis("Vertical"));
			}
			else
			{
				m_Controller.SetInputVec(0, 0);
			}
			m_InputCameraVec.x = Input.GetAxis("RstickX");
			m_InputCameraVec.y = Input.GetAxis("RstickY");
			if ((m_Controller.IsGrounded() || !m_Controller.hasDoubleJumped || m_Controller.IsWalledL() || m_Controller.IsWalledR())) // TODO: Move to CharController maybe
			{
				m_Controller.inputJump = Input.GetButtonDown("Jump") ? true : m_Controller.inputJump;
			}
			triggerDown = Input.GetAxis("Trigger") < -0.25f;
			m_Controller.inputDodge = (triggerDown && !m_WasTriggerDown) || Input.GetButtonDown("Dodge") ? true : m_Controller.inputDodge;
			m_Controller.inputAttackL = Input.GetButtonDown("Fire1") ? true : m_Controller.inputAttackL;
			m_Controller.inputAttackH = Input.GetButtonDown("Fire2") ? true : m_Controller.inputAttackH;

			bool inputUp	= Input.GetAxis("Vertical") >  0.5;
			bool inputDown	= Input.GetAxis("Vertical") < -0.5;
			bool inputRight	= Input.GetAxis("Horizontal") > 0.5;
			bool inputLeft	= Input.GetAxis("Horizontal") < -0.5;

			if (inputUp    && !m_InputUpPrev)	 m_ComboTracker.StackInput(CombatInput.Up);
			if (inputDown  && !m_InputDownPrev)	 m_ComboTracker.StackInput(CombatInput.Down);
			if (inputRight && !m_InputRightPrev) m_ComboTracker.StackInput(CombatInput.Right);
			if (inputLeft  && !m_InputLeftPrev)	 m_ComboTracker.StackInput(CombatInput.Left);

			m_InputUpPrev    = inputUp;
			m_InputDownPrev  = inputDown;
			m_InputRightPrev = inputRight;
			m_InputLeftPrev  = inputLeft;

			// Update mashing speed
			if (Input.GetButtonDown("Fire1") && m_Controller.isAttacking)
				m_MashSpeed = Mathf.Lerp(m_MashSpeed, 5, .25f);
			else if (m_Controller.isAttacking)
				m_MashSpeed = Mathf.Lerp(m_MashSpeed, 1, Time.deltaTime / m_MashDecay);
			else
				m_MashSpeed = 1;
		}

		// Update sprite facing direction
		m_Renderer.flipX = !m_Controller.IsFacingRight();

		m_WasTriggerDown = triggerDown;
	}

	void Attack (int damage, float delay)
	{
		if (m_Controller.IsFacingRight())
			m_HurtBoxR.Hurt(damage, delay);
		else
			m_HurtBoxL.Hurt(damage, delay);

		m_Animator.SetInteger("AttackNum", (int)m_AttackType);
		m_Animator.SetTrigger("Attack");
	}

	void AttackLaunch (int damage, float delay, Vector3 dir)
	{
		if (m_Controller.IsFacingRight())
			m_HurtBoxR.HurtLaunch(damage, dir, delay);
		else
			m_HurtBoxL.HurtLaunch(damage, dir, delay);

		m_Animator.SetInteger("AttackNum", (int)m_AttackType);
		m_Animator.SetTrigger("Attack");
	}

	void OnAirborne ()
	{
		m_Animator.SetTrigger("Fall");
	}

	void OnGrounded ()
	{
		//transform.FindChild("GroundPuff").GetComponent<ParticleSystem>().Emit(5);

		GameObject smokePuff;

		if (m_Controller.GetVelocity().x > -3f)
		{
			smokePuff = PoolManager.GetPooledObjectS("AnimatedSprites", "SmokePuff1");
			smokePuff.transform.position = transform.position;
			smokePuff.GetComponent<OneShotSprite>().spriteRenderer.flipX = false;
		}

		if (m_Controller.GetVelocity().x < 3f)
		{
			smokePuff = PoolManager.GetPooledObjectS("AnimatedSprites", "SmokePuff1");
			smokePuff.transform.position = transform.position;
			smokePuff.GetComponent<OneShotSprite>().spriteRenderer.flipX = true;
		}

		AudioSource.PlayClipAtPoint(m_FootstepSound, transform.position);
	}

	void OnAirJump ()
	{
		//transform.FindChild("GroundPuff").GetComponent<ParticleSystem>().Emit(10);
	}

	void OnJump ()
	{
		m_Animator.SetTrigger("Jump");

		// Jumping aborts attacks
		AbortAttack();

		var smokePuff = PoolManager.GetPooledObjectS("AnimatedSprites", "SmokePuff1");
		smokePuff.transform.position = transform.position;
		smokePuff.GetComponent<OneShotSprite>().spriteRenderer.flipX = !m_Controller.IsFacingRight();

		AudioSource.PlayClipAtPoint(m_JumpSound, transform.position);
	}

	void OnSault ()
	{
		// Saulting aborts attacks
		AbortAttack();

		IgnoreEnemyCollision(0.5f);

		m_Animator.SetTrigger("Sault");
		AudioSource.PlayClipAtPoint(m_SaultSound, transform.position);
	}

	void OnDodge ()
	{
		// transform.FindChild("GroundPuff").GetComponent<ParticleSystem>().Emit(10);

		var smokePuff = PoolManager.GetPooledObjectS("AnimatedSprites", "SmokePuff1");
		smokePuff.transform.position = transform.position;
		smokePuff.GetComponent<OneShotSprite>().spriteRenderer.flipX = !m_Controller.IsFacingRight();

		AudioSource.PlayClipAtPoint(m_DodgeSound, transform.position);
		m_Controller.inputDodge = false;

		// Abort attack
		AbortAttack();

		IgnoreEnemyCollision(0.5f);

		m_Animator.SetTrigger("Dodge");
	}

	void IgnoreEnemyCollision (float duration)
	{
		gameObject.layer = LayerMask.NameToLayer("PlayerIgnoreEnemies");

		StartCoroutine(EndIgnoreEnemyCollision(duration));
	}

	IEnumerator EndIgnoreEnemyCollision (float duration)
	{
		yield return new WaitForSeconds(duration);

		gameObject.layer = LayerMask.NameToLayer("Player");
	}

	private void ComboTracker_OnComboFinished(string name)
	{
		TextManager.AddWorldText(name, transform.position + Vector3.up * 1.75f);

		if (name == "L H L" || name == "L L H")
		{
			m_Controller.AttackBoost(0.025f, 0.25f);

			AudioSource.PlayClipAtPoint(m_AttackH1Sound, transform.position);
			m_AttackType = AttackType.HeavyA;

			Attack(m_AttackDamage.lightComboFinish, 0.3f);
		}
		else if (name == "Launcher")
		{
			//m_AttackType = AttackType.Launcher;

			m_Controller.AttackBoost(0.025f, 0.25f);

			AudioSource.PlayClipAtPoint(m_AttackH1Sound, transform.position);
			m_AttackType = AttackType.Launcher;

			AttackLaunch(m_AttackDamage.launcher, 0.3f, Vector3.up * m_Controller.jumpForce);
		}
		else if (name == "Slammer")
		{
			//m_AttackType = AttackType.Launcher;

			m_Controller.AttackBoost(0.025f, 0.25f);

			AudioSource.PlayClipAtPoint(m_AttackH1Sound, transform.position);
			m_AttackType = AttackType.HeavyA;

			AttackLaunch(m_AttackDamage.slammer, 0.3f, Vector3.down * 12);
		}
		else if (name == "Booster R" || name == "Booster L")
		{
			m_Controller.AttackBoost(0.75f, 0.2f);

			AudioSource.PlayClipAtPoint(m_AttackH1Sound, transform.position);
			m_AttackType = AttackType.Pusher;

			// Boost counts as air jump
			if (!m_Controller.IsGrounded())
				m_Controller.hasDoubleJumped = true;

			Attack(m_AttackDamage.booster, 0.3f);
		}
		else if (name == "Booster D")
		{
			// m_Controller.AttackBoost(0.75f, 0.2f); // TODO: Extend AttackBoost to accept a directional vector
			
			AudioSource.PlayClipAtPoint(m_AttackH1Sound, transform.position);
			m_AttackType = AttackType.HeavyA;

			Attack(m_AttackDamage.booster, 0.3f);
		}
		else if (name == "Pusher R" || name == "Pusher L")
		{
			m_Controller.AttackBoost(0.025f, 0.25f);
			
			AudioSource.PlayClipAtPoint(m_AttackH1Sound, transform.position);
			m_AttackType = AttackType.Pusher;

			if (name == "Pusher R")
				AttackLaunch(m_AttackDamage.pusher, 0.3f, new Vector3(10, 5, 0));
			else
				AttackLaunch(m_AttackDamage.pusher, 0.3f, new Vector3(-10, 5, 0));
		}
		else
		{
			Debug.LogError("Combo finished but was not handled: " + name);
		}
	}

	void OnAttackL ()
	{
		//if (m_AttackType != AttackType.LightA)
		//{
		//	m_AttackType = AttackType.LightA;
		//	AudioSource.PlayClipAtPoint(m_AttackL1Sound, transform.position);
		//}
		//else
		//{
		//	m_AttackType = AttackType.LightB;
		//	AudioSource.PlayClipAtPoint(m_AttackL2Sound, transform.position);
		//}

		if (!m_ComboTracker.StackInput(CombatInput.AttackL))
		{
			m_AttackType = AttackType.LightA;
			AudioSource.PlayClipAtPoint(m_AttackL1Sound, transform.position);

			//if (m_AttackType == AttackType.LightA)
				TextManager.AddWorldText("L", transform.position + Vector3.up * 1.75f);
			//else
			//	TextManager.AddWorldText("Light B", transform.position + Vector3.up * 1.75f);

			m_Controller.AttackBoost(0.025f, 0.25f);

			Attack(m_AttackDamage.lightAttack, 0.05f);
		}
	}

	void OnAttackH ()
	{
		//m_AttackType = AttackType.HeavyA;
		//AudioSource.PlayClipAtPoint(m_AttackH1Sound, transform.position);

		if (!m_ComboTracker.StackInput(CombatInput.AttackH))
		{
			m_AttackType = AttackType.LightB;
			AudioSource.PlayClipAtPoint(m_AttackL2Sound, transform.position);

			//TextManager.AddWorldText("Heavy A", transform.position + Vector3.up * 1.75f);
			TextManager.AddWorldText("H", transform.position + Vector3.up * 1.75f);

			m_Controller.AttackBoost(0.025f, 0.25f);

			Attack(m_AttackDamage.lightAttack, 0.05f);
			//Attack(m_AttackDamage.heavyAttack, 0.3f);
		}
	}

	void LateUpdate ()
	{
		if (IsAlive())
		{
			// Update animator
			m_Animator.SetFloat("ForwardSpeed", Mathf.Abs(m_Controller.GetInputVec().x));
			if (Mathf.Abs(m_Controller.GetInputVec().x) > Mathf.Epsilon && m_Controller.IsGrounded() && !m_Controller.isAttacking)
				m_Animator.speed = Mathf.Max(Mathf.Abs(m_Controller.GetInputVec().x), 0.5f);
			//else if (m_Controller.isAttacking)
			//	m_Animator.speed = m_MashSpeed;
			//else
			//	m_Animator.speed = 1f;
			m_Animator.SetBool("Grounded", m_Controller.IsGrounded());

			// Needed to properly exit attack animations
			m_Animator.SetBool("IsAttacking", m_Controller.isAttacking);
		}

		// Update camera position
		//Vector3 cameraPos = Vector3.Lerp(m_Camera.position, m_CameraTarget.position + new Vector3(m_InputCameraVec.x, -m_InputCameraVec.y, 0) * m_CameraPanDistance, Time.deltaTime / m_CameraDrag);
		//cameraPos.z = -m_CameraDistance;
		//m_Camera.position = cameraPos;
		//m_Camera.GetComponent<Camera>().orthographicSize = m_CameraDistance;
	}

	void AbortAttack ()
	{
		m_Controller.AbortAttack();
		m_AttackType = AttackType.None;
	}

	public void Footstep ()
	{
		// transform.FindChild("GroundPuff").GetComponent<ParticleSystem>().Emit(1);

		//var smokePuff = PoolManager.GetPooledObjectS("AnimatedSprites", "SmokePuff3");
		//smokePuff.transform.position = transform.position + Vector3.up * 0.15f;
		//smokePuff.GetComponent<OneShotSprite>().velocity = Vector3.up * 0.5f;

		var smokePuff = PoolManager.GetPooledObjectS("AnimatedSprites", "SmokePuff1");
		smokePuff.transform.position = transform.position;
		smokePuff.GetComponent<OneShotSprite>().spriteRenderer.flipX = !m_Controller.IsFacingRight();

		AudioSource.PlayClipAtPoint(m_FootstepSound, transform.position);
	}

	public void AttackFinished ()
	{
		m_Controller.isAttacking = false;
		m_AttackTimer = 0.5f; // Combo carry time
	}

	public void SaultFinished ()
	{
		m_Controller.isSaulting = false;
	}

	public override void OnDamage()
	{
		if (m_Controller.isAttacking && false) // Uninterruptable attack
		{
			m_Tile.Flash(new Color(1, 1, 0, 1), 0.25f);
		}
		else
		{
			m_Controller.AbortAttack();

			m_Controller.Flinch(0.01f, 0.5f);
			// m_Animator.SetBool("IsFlinching", true);
			transform.GetComponentInChildren<LocalPositionShake>().Shake();
			m_Tile.Flash(new Color(1, 0, 0, 1), 0.5f);
		}
	}

	public override void OnDie()
	{
		
	}
}
