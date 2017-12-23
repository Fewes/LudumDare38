using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EdgeBehaviour
{
	Cross,
	Stop,
	Jump
}

public enum FallDirection
{
	None,
	Up,
	Down,
	Side
}

public class CharController : MonoBehaviour
{
	// Events
	public delegate void AirborneAction();			public event AirborneAction			OnAirborne;
	public delegate void GroundedAction();			public event GroundedAction			OnGrounded;
	public delegate void JumpAction();				public event JumpAction				OnJump;
	public delegate void AirJumpAction();			public event AirJumpAction			OnAirJump;
	public delegate void SaultAction();				public event SaultAction			OnSault;
	public delegate void DodgeAction();				public event DodgeAction			OnDodge;
	public delegate void AttackLAction();			public event AttackLAction			OnAttackL;
	public delegate void AttackHAction();			public event AttackHAction			OnAttackH;
	public delegate void FlinchAction();			public event FlinchAction			OnFlinch;
	public delegate void DodgeEndAction();			public event DodgeEndAction			OnDodgeEnd;
	public delegate void FlinchEndAction();			public event FlinchEndAction		OnFlinchEnd;
	public delegate void AttackBoostEndAction();	public event AttackBoostEndAction	OnAttackBoostEnd;
	public delegate void FallAction(int dir);		public event FallAction				OnFall;

	// Parameters
	[SerializeField] private float		m_MovementSpeed = 8f;
	[SerializeField] public float		jumpForce = 11f;
	[SerializeField] private float		m_DodgeSpeed = 0.3f;
	[SerializeField] private float		m_DodgeLength = 0.5f;
	[SerializeField] private float		m_AttackBoostSpeed = 0.1f;
	[SerializeField] private float		m_AttackBoostLength = 0.25f;
	[Range(0, 1)]
	[SerializeField] private float		m_AirControl = 0.5f;
	public bool							kinematicOnGround = false;
	[SerializeField] private LayerMask	m_GroundedMask;

	// Components
	private Rigidbody2D					m_Rigidbody;
	private Sensor						m_GroundSensor;
	private Sensor						m_WallSensorR;
	private Sensor						m_WallSensorL;
	private Sensor						m_EdgeSensorR;
	private Sensor						m_EdgeSensorL;

	// Input
	[HideInInspector] private Vector2	inputVec;
	[HideInInspector] public bool		inputJump;
	[HideInInspector] public bool		inputDodge;
	[HideInInspector] public bool		inputAttackL;
	[HideInInspector] public bool		inputAttackH;

	// Public states
	[HideInInspector] public bool		isAttackBoosting;
	[HideInInspector] public bool		isSaulting;
	[HideInInspector] public bool		isAttacking;
	[HideInInspector] public int		airResources;

	[HideInInspector] public bool		hasDoubleJumped;
	[HideInInspector] public bool		hasDodgedInAir;
	[HideInInspector] public bool		hasSaulted;

	// States
	private bool						m_IsFacingRight;
	private bool						m_IsDodging;
	private bool						m_IsDodgingRight;
	private bool						m_IsFlinching;
	private bool						m_IsFlinchingRight;
	private Vector2						m_CurrentVeclocity;
	private FallDirection				m_FallDirection;
	public bool							isFalling;

	// Prev states
	private bool						m_WasGrounded;
	private Vector3						m_LastPos;
	private FallDirection				m_FallDirectionPrev;

	// Timers
	private float						m_DodgeTimer;
	private float						m_AttackBoostTimer;
	private float						m_FlinchLength;
	private float						m_FlinchTimer;
	private float						m_FlinchSpeed;

	public bool IsGrounded ()
	{
		return m_GroundSensor ? m_GroundSensor.State() : true;
	}

	public bool IsGroundedL ()
	{
		return m_EdgeSensorL ? m_EdgeSensorL.State() : true;
	}

	public bool IsGroundedR ()
	{
		return m_EdgeSensorR ? m_EdgeSensorR.State() : true;
	}

	public bool IsWalledR ()
	{
		return m_WallSensorR ? m_WallSensorR.State() : false;
	}

	public bool IsWalledL ()
	{
		return m_WallSensorL ? m_WallSensorL.State() : false;
	}

	public bool IsFacingRight ()
	{
		return m_IsFacingRight;
	}

	public bool IsDodging ()
	{
		return m_IsDodging;
	}

	public bool IsImmobilized ()
	{
		return m_IsFlinching || isFalling;
	}

	public bool IsFlinching ()
	{
		return m_IsFlinching;
	}

	public Vector3 GetVelocity ()
	{
		return m_Rigidbody.velocity;
	}

	void Start ()
	{
		m_Rigidbody = GetComponent<Rigidbody2D>();
		m_GroundSensor = transform.Find("GroundSensor") ? transform.Find("GroundSensor").GetComponent<Sensor>() : null;
		m_WallSensorR = transform.Find("WallSensorR")   ? transform.Find("WallSensorR").GetComponent<Sensor>()  : null;
		m_WallSensorL = transform.Find("WallSensorL")   ? transform.Find("WallSensorL").GetComponent<Sensor>()  : null;
		m_EdgeSensorR = transform.Find("EdgeSensorR")   ? transform.Find("EdgeSensorR").GetComponent<Sensor>()  : null;
		m_EdgeSensorL = transform.Find("EdgeSensorL")   ? transform.Find("EdgeSensorL").GetComponent<Sensor>()  : null;

		airResources = 3;
	}

	public void Flinch (float speed, float length)
	{
		isFalling = false;
		if (OnFlinch != null)
			OnFlinch();
		m_IsFlinching = true;
		m_IsFlinchingRight = !m_IsFacingRight;
		m_FlinchLength = length;
		m_FlinchTimer = m_FlinchLength;

		m_FlinchSpeed = speed;
	}

	public void Launch (Vector3 dir, bool knockOut, bool smoke = true)
	{
		m_IsFlinching = false;
		var vel = m_Rigidbody.velocity;
		vel.x = dir.x;
		vel.y = dir.y;
		m_Rigidbody.velocity = vel;

		if (smoke)
		{
			Vector3 rot = Vector3.zero;
			Vector3 offset = Vector3.zero;
			if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
			{
				offset.y = 0.85f;
				if (dir.x > 0)
					rot.z = 0;
				else
					rot.z = 180;
			}
			else
			{
				if (dir.y > 0)
				{
					rot.z = 90;
				}
				else
				{
					offset.y = 1f;
					rot.z = 270;
				}
			}

			var smokePuff = PoolManager.GetPooledObjectS("AnimatedSprites", "SmokePuff4");
			smokePuff.transform.position = transform.position + offset;
			smokePuff.transform.rotation = Quaternion.Euler(rot);
		}

		// m_JumpTimer = 0.05f;
		m_GroundSensor.Disable(0.1f);

		if (knockOut)
			isFalling = true;
	}
	
	void Update ()
	{
		if (Mathf.Abs(inputVec.x) > Mathf.Epsilon)
			m_IsFacingRight = inputVec.x > 0;

		if (kinematicOnGround)
			m_Rigidbody.isKinematic = IsGrounded();
		else
			m_Rigidbody.isKinematic = false;

		// Dodge start
		if (inputDodge && !IsDodging() && !hasDodgedInAir && IsGrounded())
		{
			m_IsDodgingRight = m_IsFacingRight;
			m_IsDodging = true;
			m_DodgeTimer = m_DodgeLength;

			if (OnDodge != null)
				OnDodge();
		}

		// Attack start
		if ((inputAttackL || inputAttackH) && !isAttacking && !m_IsDodging && !isSaulting && (IsGrounded() || airResources > 0))
		{
			if (inputAttackL)
			{
				inputAttackL = false;

				if (!IsGrounded())
					airResources -= 1;

				if (OnAttackL != null)
					OnAttackL();
			}
			else if (inputAttackH)
			{
				inputAttackH = false;

				if (!IsGrounded())
					airResources -= 1;

				if (OnAttackH != null)
					OnAttackH();
			}

			m_IsDodgingRight = m_IsFacingRight;
			// isAttackBoosting = true;
			// m_AttackBoostTimer = m_AttackBoostLength;

			isAttacking = true;
		}
		else if (!m_GroundSensor.State() && airResources < 1)
		{
			inputAttackL = false;
			inputAttackH = false;
		}
	}

	public void AttackBoost (float speed, float length)
	{
		m_AttackBoostSpeed = speed;
		m_AttackBoostLength = length;

		isAttackBoosting = true;
		m_AttackBoostTimer = m_AttackBoostLength;
		m_IsDodgingRight = m_IsFacingRight;
	}

	public void SetInputVec (Vector2 vec)
	{
		inputVec = vec;

		// Clamp input vector components
		inputVec.x = Mathf.Clamp(inputVec.x, -1, 1);
		inputVec.y = Mathf.Clamp(inputVec.y, -1, 1);
	}

	public void SetInputVec (float x, float y)
	{
		SetInputVec(new Vector2(x, y));
	}

	public void SetInputVecX (float x)
	{
		inputVec.x = x;

		// Clamp input vector components
		inputVec.x = Mathf.Clamp(inputVec.x, -1, 1);
	}

	public void SetInputVecY (float y)
	{
		inputVec.y = y;

		// Clamp input vector components
		inputVec.y = Mathf.Clamp(inputVec.y, -1, 1);
	}

	public Vector2 GetInputVec ()
	{
		return inputVec;
	}

	public void AbortAttack ()
	{
		isAttacking = false;
		isAttackBoosting = false;
	}

	void FixedUpdate ()
	{
		m_DodgeTimer -= Time.fixedDeltaTime;
		m_AttackBoostTimer -= Time.fixedDeltaTime;
		m_FlinchTimer -= Time.fixedDeltaTime;

		UpdateFallDirection();

		// Dodge stop
		if (m_DodgeTimer <= 0 && m_IsDodging)
		{
			m_IsDodging = false;
			m_Rigidbody.velocity = m_CurrentVeclocity;
			if (OnDodgeEnd != null)
				OnDodgeEnd();
		}

		// Attack boost stop
		if (m_AttackBoostTimer <= 0 && isAttackBoosting)
		{
			isAttackBoosting = false;
			m_Rigidbody.velocity = m_CurrentVeclocity;
			if (OnAttackBoostEnd != null)
				OnAttackBoostEnd();
		}

		// Flinch stop
		if (m_FlinchTimer <= 0 && m_IsFlinching)
		{
			m_IsFlinching = false;
			m_Rigidbody.velocity = m_CurrentVeclocity;
			if (OnFlinchEnd != null)
				OnFlinchEnd();
		}

		// Transfer velocity when player stops being grounded
		if ((!IsGrounded() && m_WasGrounded || inputJump) && !isAttacking && m_FallDirection == FallDirection.None)
		{
			m_Rigidbody.velocity = m_CurrentVeclocity;

			if (OnAirborne != null)
				OnAirborne();
		}

		if (IsGrounded() && !m_WasGrounded)
		{
			// Landed
			isSaulting = false;

			airResources = 3;

			if (OnGrounded != null)
				OnGrounded();
		}

		// Jumping
		if (inputJump && !m_IsDodging && !isSaulting)
		{
			int jumpType = 0;

			if (!IsGrounded())
			{
				if (IsWalledR() || IsWalledL())
				{
					// Wall jump
					jumpType = 2;
				}
				else if (!hasDoubleJumped)
				{
					// Air jump
					hasDoubleJumped = true;
					// transform.FindChild("GroundPuff").GetComponent<ParticleSystem>().Emit(10);
					if (OnAirJump != null)
						OnAirJump();
					jumpType = 1;
				}
			}

			var vel = m_Rigidbody.velocity;
			vel.y = jumpForce;
			if (jumpType == 0)
			{
				hasSaulted = false;
			}
			if (jumpType == 2)
			{
				float velX = m_WallSensorL.State() ? m_MovementSpeed : -m_MovementSpeed;
				vel.x = velX;
			}
			else if (Mathf.Abs(inputVec.x) > Mathf.Epsilon)
			{
				float velX = inputVec.x * m_MovementSpeed;
				vel.x = velX;
			}
			m_Rigidbody.velocity = vel;

			m_GroundSensor.Disable(0.1f);

			inputJump = false;

			if (OnJump != null)
				OnJump();
		}

		// Saulting
		if (!inputJump && inputDodge && !IsGrounded() && !hasSaulted)
		{
			var vel = m_Rigidbody.velocity;
			vel.y = Mathf.Max(vel.y, jumpForce * 0.5f);
			m_Rigidbody.velocity = vel;

			isSaulting = true;
			inputDodge = false;

			if (OnSault != null)
				OnSault();

			hasSaulted = true;
		}

		// Movement
		if (m_IsFlinching)
		{
			// Flinch
			m_Rigidbody.MovePosition(m_Rigidbody.position + new Vector2(m_IsFlinchingRight ? 1 : -1, 0) * m_FlinchSpeed * (m_FlinchTimer / m_FlinchLength));
		}
		else if (m_FallDirection != FallDirection.None)
		{

		}
		else if (m_IsDodging)
		{
			// Dodge
			m_Rigidbody.MovePosition(m_Rigidbody.position + new Vector2(m_IsDodgingRight ? 1 : -1, 0) * m_DodgeSpeed * (m_DodgeTimer / m_DodgeLength));
			if (!IsGrounded())
				hasDodgedInAir = true;
		}
		else if (isAttackBoosting)
		{
			// Attack boosting
			float mul = 1;//m_Attack == 3 ? 2 : 1;
			m_Rigidbody.MovePosition(m_Rigidbody.position + new Vector2(m_IsDodgingRight ? 1 : -1, 0) * m_AttackBoostSpeed * (m_AttackBoostTimer / m_AttackBoostLength) * mul);
		}
		else if (!isAttacking)
		{
			if (IsGrounded())
			{
				hasSaulted = false;
				hasDodgedInAir = false;
				hasDoubleJumped = false;
				// Ground movement
				var moveVec = new Vector2(inputVec.x, 0);
				// Get standing surface normal
				float traceLength = 0.5f;
				Vector2 rayOrigin = transform.position + new Vector3(0, traceLength * 0.5f, 0);
				Vector2 rayDirection = Vector2.down * traceLength;
				//Debug.DrawRay(rayOrigin, rayDirection);

				var hit = Physics2D.Raycast(rayOrigin, rayDirection, rayDirection.magnitude, m_GroundedMask);
				if (hit)
				{
					traceLength = 0.95f;

					// Get target surface normal
					rayOrigin += (Vector2)(Quaternion.Euler(0, 0, -90) * hit.normal) * inputVec.x * m_MovementSpeed * Time.fixedDeltaTime * 1.25f + hit.normal * traceLength * 0.5f;
					rayDirection = -hit.normal * traceLength;
					// Debug.DrawRay(rayOrigin, rayDirection);

					hit = Physics2D.Raycast(rayOrigin, rayDirection, rayDirection.magnitude, m_GroundedMask);
					if (hit)
					{
						// Align movement vector to target surface normal
						moveVec = (Quaternion.Euler(0, 0, -90) * hit.normal) * inputVec.x;
					}

					//Debug.DrawRay(rayOrigin, rayDirection, hit ? Color.green : Color.red);
				}

				m_Rigidbody.MovePosition(m_Rigidbody.position + moveVec * m_MovementSpeed * Time.fixedDeltaTime);
			}
			else
			{
				// Air movement
				Vector2 airVelocity = m_Rigidbody.velocity;
				airVelocity.x = Mathf.Lerp(airVelocity.x, inputVec.x * m_MovementSpeed, (Time.fixedDeltaTime / (1 - m_AirControl)) * m_AirControl);
				// airVelocity.y += Mathf.Min(m_InputVec.y, 0) * m_MovementSpeed * (Time.fixedDeltaTime / (1 - m_AirControl)) * m_AirControl;
				m_Rigidbody.velocity = airVelocity;
			}
		}

		if (isAttacking)
		{
			var vel = m_Rigidbody.velocity;
			vel.y *= 0;
			m_Rigidbody.velocity = vel;
		}

		// Update velocity
		Vector3 velocity = (transform.position - m_LastPos) / Time.fixedDeltaTime;
		m_CurrentVeclocity.x = velocity.x;
		m_CurrentVeclocity.y = velocity.y;

		m_WasGrounded = IsGrounded();
		m_LastPos = transform.position;
	}

	void UpdateFallDirection ()
	{
		// Fall direction
		if (!isFalling)
			m_FallDirection = FallDirection.None;
		else if (Mathf.Abs(m_Rigidbody.velocity.y) < 6f)
			m_FallDirection = FallDirection.Side;
		else if (m_Rigidbody.velocity.y > 0)
			m_FallDirection = FallDirection.Up;
		else
			m_FallDirection = FallDirection.Down;

		if (m_FallDirection != m_FallDirectionPrev)
		{
			if (m_FallDirection == FallDirection.None)
			{
				if (OnFall != null)
					OnFall(0);
			}
			else if (m_FallDirection == FallDirection.Side)
			{
				if (OnFall != null)
					OnFall(3);
			}
			else if (m_FallDirection == FallDirection.Up)
			{
				if (OnFall != null)
					OnFall(1);
			}
			else if (m_FallDirection == FallDirection.Down)
			{
				if (OnFall != null)
					OnFall(2);
			}
		}

		m_FallDirectionPrev = m_FallDirection;
	}

	public FallDirection GetFallDirection ()
	{
		return m_FallDirection;
	}
}
