using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : Damageable
{
	[Header("Enemy")]
	// Public
	public EdgeBehaviour			m_OnEdgeAbove;
	public EdgeBehaviour			m_OnEdgeBelow;

	// Serializable
	[SerializeField] float			m_ThinkInterval = 0.5f;
	[SerializeField] float			m_AttackInterval = 1.0f;
	[SerializeField] float			m_SearchDistance = 5f;
	[SerializeField] float			m_StoppingDistance = 1f;
	[SerializeField] float			m_KnockdownDuration = 1f;

	[Header("Sounds")]
	[SerializeField] private AudioClip		m_JumpSound;
	//[SerializeField] private AudioClip		m_DodgeSound;
	//[SerializeField] private AudioClip		m_SaultSound;
	[SerializeField] private AudioClip		m_FootstepSound;
	[SerializeField] private AudioClip		m_ImpactSound;
	[SerializeField] private AudioClip		m_AttackAnticipateSound;
	//[SerializeField] private AudioClip		m_AttackL1Sound;
	//[SerializeField] private AudioClip		m_AttackL2Sound;
	//[SerializeField] private AudioClip		m_AttackH1Sound;
	
	// Components
	private CharController			m_Controller;
	private SpriteRenderer			m_Renderer;
	private Tile					m_Tile;
	private Animator				m_Animator;
	private HurtBox					m_HurtBoxR;
	private HurtBox					m_HurtBoxL;
	private GameObject				m_StatusBar;

	private Damageable				m_Target;
	private float					m_ThinkTimer;
	private float					m_AttackTimer;
	private float					m_KnockdownTimer;

	new void Start ()
	{
		base.Start();

		CheckComponents();

		m_Controller.OnFlinchEnd += Controller_OnFlinchEnd;
		m_Controller.OnFall += Controller_OnFall;
		m_Controller.OnGrounded += Controller_OnGrounded;
		m_Controller.OnJump += Controller_OnJump;

		m_HurtBoxR.OnHurt += HurtBoxR_OnHurt;
		m_HurtBoxL.OnHurt += HurtBoxL_OnHurt;

		m_ThinkTimer = Random.Range(0, m_ThinkInterval);
	}

	private void HurtBoxL_OnHurt(Damageable damageable)
	{
		// Hit effect
		var distort = PoolManager.GetPooledObjectS("Particle Systems", "ImpactDistort1");
		distort.transform.position = damageable.GetCenter();
	}

	private void HurtBoxR_OnHurt(Damageable damageable)
	{
		// Hit effect
		var distort = PoolManager.GetPooledObjectS("Particle Systems", "ImpactDistort1");
		distort.transform.position = damageable.GetCenter();
	}

	private void Controller_OnGrounded()
	{
		GameObject smokePuff;

		if (m_Controller.GetVelocity().x > -0.1f)
		{
			smokePuff = PoolManager.GetPooledObjectS("AnimatedSprites", "SmokePuff1");
			smokePuff.transform.position = transform.position;
			smokePuff.GetComponent<OneShotSprite>().spriteRenderer.flipX = false;
		}

		if (m_Controller.GetVelocity().x < 0.1f)
		{
			smokePuff = PoolManager.GetPooledObjectS("AnimatedSprites", "SmokePuff1");
			smokePuff.transform.position = transform.position;
			smokePuff.GetComponent<OneShotSprite>().spriteRenderer.flipX = true;
		}

		if (m_Controller.isFalling && m_ImpactSound)
			AudioSource.PlayClipAtPoint(m_ImpactSound, transform.position);

	}

	void CheckComponents ()
	{
		if (!m_Controller) m_Controller = GetComponent<CharController>();
		if (!m_Renderer) m_Renderer = transform.Find("Renderer").GetComponent<SpriteRenderer>();
		if (!m_Animator) m_Animator = m_Renderer.gameObject.GetComponent<Animator>();
		if (!m_Tile) m_Tile = GetComponentInChildren<Tile>();
		if (!m_HurtBoxR) m_HurtBoxR = transform.Find("HurtBoxR").GetComponent<HurtBox>();
		if (!m_HurtBoxL) m_HurtBoxL = transform.Find("HurtBoxL").GetComponent<HurtBox>();
		if (!m_StatusBar) m_StatusBar = transform.Find("StatusBar").gameObject;
	}

	void Controller_OnJump ()
	{
		var smokePuff = PoolManager.GetPooledObjectS("AnimatedSprites", "SmokePuff1");
		smokePuff.transform.position = transform.position;
		smokePuff.GetComponent<OneShotSprite>().spriteRenderer.flipX = !m_Controller.IsFacingRight();

		AudioSource.PlayClipAtPoint(m_JumpSound, transform.position);
	}

	void Attack ()
	{
		m_Animator.SetTrigger("Attack");
		m_Controller.isAttacking = true;
		m_Controller.AttackBoost(0.025f, 0.25f);
		m_Controller.SetInputVec(0, 0);
		AudioSource.PlayClipAtPoint(m_AttackAnticipateSound, transform.position);
		m_Controller.kinematicOnGround = true;
	}

	void AbortAttack ()
	{
		m_Controller.kinematicOnGround = false;
		m_Controller.AbortAttack();
	}

	public void AttackHurt ()
	{
		// Activate hurt boxes here
		if (m_Controller.IsFacingRight())
			m_HurtBoxR.Hurt(20);
		else
			m_HurtBoxL.Hurt(20);
	}

	void Update ()
	{
		m_ThinkTimer -= Time.deltaTime;
		if (m_ThinkTimer <= 0 && IsAlive() && !m_Controller.IsImmobilized())
		{
			Think();
			m_ThinkTimer = m_ThinkInterval;
		}

		// Knockdown recovery
		if (IsAlive() && m_Controller.isFalling)
		{
			if (m_Controller.IsGrounded())
				m_KnockdownTimer -= Time.deltaTime;
			else
				m_KnockdownTimer = m_KnockdownDuration;

			if (m_KnockdownTimer <= 0)
			{
				// Recover
				m_Controller.isFalling = false;
				m_Tile.Flash(new Color(1, 1, 0, 0.15f), 0.25f);
			}
		}

		// Movement / attacking
		if (m_Target && IsAlive() && !m_Controller.IsImmobilized() && !m_Controller.isAttacking)
		{
			// Can attack check
			if (m_AttackTimer <= 0 && Vector3.Distance(transform.position, m_Target.transform.position) < m_StoppingDistance)
			{
				Attack();
			}
			else
			{
				m_AttackTimer -= Time.deltaTime;

				// Anticipate target position
				var anticipator = m_Target.GetComponent<PositionAnticipator>();
				//var anticipatedPosition = anticipator ? anticipator.GetAnticipatiedPosition() : m_Target.position;
				var anticipatedPosition = m_Target.transform.position;

				float xDist = anticipatedPosition.x - transform.position.x;

				// Don't continuously run into a wall like an idiot
				if (xDist < 0 && m_Controller.IsWalledL() || xDist > 0 && m_Controller.IsWalledR())
					xDist = 0;

				bool aboveTarget = transform.position.y > anticipatedPosition.y + 0.1f;

				if (Mathf.Abs(xDist) > m_StoppingDistance)
				{
					// I would like to move towards my target
					if ( xDist < 0 && m_Controller.IsGroundedL() ||
						 xDist > 0 && m_Controller.IsGroundedR() ||
						 aboveTarget && m_OnEdgeAbove == EdgeBehaviour.Cross ||
						!aboveTarget && m_OnEdgeBelow == EdgeBehaviour.Cross
					) // Edge cross check
						m_Controller.SetInputVecX(xDist);
					else
					{
						if ( aboveTarget && m_OnEdgeAbove == EdgeBehaviour.Stop ||
							!aboveTarget && m_OnEdgeBelow == EdgeBehaviour.Stop
						) // Edge stop check
							m_Controller.SetInputVecX(0);
						else if ( aboveTarget && m_OnEdgeAbove == EdgeBehaviour.Jump ||
								 !aboveTarget && m_OnEdgeBelow == EdgeBehaviour.Jump
						) // Edge jump check
						{
							m_Controller.SetInputVecX(xDist);
							if (m_Controller.IsGrounded()) // TODO: Move to CharController maybe
								m_Controller.inputJump = true;
						}
					}
				}
				else
					m_Controller.SetInputVecX(0);
			}
		}

		// Update sprite facing direction
		if (IsAlive() && !m_Controller.isFalling)
			m_Renderer.flipX = m_Controller.IsFacingRight();
	}

	void LateUpdate ()
	{
		// Update animator
		if (IsAlive())
		{
			m_Animator.SetFloat("ForwardSpeed", Mathf.Abs(m_Controller.GetInputVec().x));
			if (Mathf.Abs(m_Controller.GetInputVec().x) > Mathf.Epsilon && m_Controller.IsGrounded() && !m_Controller.isAttacking)
				m_Animator.speed = Mathf.Max(Mathf.Abs(m_Controller.GetInputVec().x), 0.5f);

			m_Animator.SetFloat("Combat", m_Target ? 1 : 0);

			m_Animator.SetBool("IsFlinching", m_Controller.IsFlinching());

			// Needed to properly exit attack animations
			m_Animator.SetBool("IsAttacking", m_Controller.isAttacking);
		}

		m_Animator.SetBool("Grounded", m_Controller.IsGrounded());
	}

	void Think ()
	{
		if (m_Target)
		{
			if (!m_Target.IsAlive())
				m_Target = null;
		}
		else
		{
			// Distance-based player search
			var players = FindObjectsOfType<Player>();
			if (players.Length > 0)
			{
				Player closestPlayer = players[0];
				float currDist = Vector3.Distance(transform.position, closestPlayer.transform.position);
				foreach (var player in players)
				{
					var damageable = player.GetComponent<Damageable>();
					if (damageable && !damageable.IsAlive())
						continue;
					float newDist = Vector3.Distance(transform.position, player.transform.position);
					if (newDist < currDist)
					{
						closestPlayer = player;
						currDist = newDist;
					}
				}
				if (currDist <= m_SearchDistance)
				{
					var damageable = closestPlayer.GetComponent<Damageable>();
					if (damageable.IsAlive())
						m_Target = damageable;
				}
			}
		}
	}

	public override void OnDamage()
	{
		if (m_Controller.isAttacking && false) // Uninterruptable attack
		{
			m_Tile.Flash(new Color(1, 1, 0, 1), 0.25f);
		}
		else
		{
			AbortAttack();
			m_Controller.Flinch(0.01f, 0.5f);
			//m_Animator.SetBool("IsFlinching", true);
			transform.GetComponentInChildren<LocalPositionShake>().Shake();
			m_Tile.Flash(new Color(1, 1, 1, 1), 0.25f);
			m_Tile.Flash(new Color(0, 0, 0, 1), 0.25f, 0.05f);
			m_Tile.Flash(new Color(1, 0, 0, 1), 0.25f, 0.1f);
		}
	}

	public override void OnDie()
	{
		// Destroy(gameObject);
		if (!m_Controller.isFalling)
			m_Controller.Launch(Vector3.up * 6, true, false);
		//m_Controller.isFalling = true;
		gameObject.layer = LayerMask.NameToLayer("EnemyDead");

		if (m_StatusBar) m_StatusBar.SetActive(false);

		m_Renderer.color = new Color(0.75f, 0.75f, 0.75f, 1);
		m_Renderer.sortingOrder = 9;

		//var smokePuff = PoolManager.GetPooledObjectS("AnimatedSprites", "SmokePuff2");
		//smokePuff.transform.position = transform.position + Vector3.up * 0.85f;
	}

	private void Controller_OnFlinchEnd()
	{
		//m_Animator.SetBool("IsFlinching", false);
	}

	private void Controller_OnFall(int dir)
	{
		m_Animator.SetInteger("FallDirection", dir);
	}

	public void Footstep ()
	{
		var smokePuff = PoolManager.GetPooledObjectS("AnimatedSprites", "SmokePuff1");
		smokePuff.transform.position = transform.position;
		smokePuff.GetComponent<OneShotSprite>().spriteRenderer.flipX = !m_Controller.IsFacingRight();

		if (m_FootstepSound)
			AudioSource.PlayClipAtPoint(m_FootstepSound, transform.position);
	}

	public void AttackFinished ()
	{
		m_Controller.isAttacking = false;
		m_AttackTimer = m_AttackInterval;
		m_Controller.kinematicOnGround = false;
	}
}
