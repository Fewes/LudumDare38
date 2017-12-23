using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Breakable : Damageable
{
	[Header("Breakable")]
	[SerializeField] private GameObject		m_BreakPrefab;
	[SerializeField] private Vector3		m_BreakPrefabOffset;
	[SerializeField] private AudioClip		m_HitSound;
	[SerializeField] private AudioClip		m_BreakSound;
	[SerializeField] private Transform		m_ShakeTransform;
	[SerializeField] private float			m_ShakeAmp = 0.5f;
	[SerializeField] private float			m_ShakeDur = 0.5f;

	private Vector3							m_ShakeTransformOrigPos;
	private float							m_ShakeTimer;

	new void Start ()
	{
		base.Start();
		m_ShakeTransformOrigPos = m_ShakeTransform.localPosition;
	}

	void Update ()
	{
		m_ShakeTimer -= Time.deltaTime;
		if (m_ShakeTransform)
		{
			if (m_ShakeTimer > 0)
			{
				m_ShakeTransform.localPosition = new Vector2(m_ShakeTransformOrigPos.x, m_ShakeTransformOrigPos.y) + Random.insideUnitCircle * m_ShakeAmp * (m_ShakeTimer / m_ShakeDur);
			}
			else
				m_ShakeTransform.localPosition = m_ShakeTransformOrigPos;
		}
	}

	public void Launch (Vector3 dir)
	{
		var vel = GetComponent<Rigidbody2D>().velocity;
		vel.x = dir.x;
		vel.y = dir.y;
		GetComponent<Rigidbody2D>().velocity = vel;
	}

	public override void OnDamage()
	{
		m_ShakeTimer = m_ShakeDur;
		if (GetHealth() > 0)
			AudioSource.PlayClipAtPoint(m_HitSound, transform.position);
	}

	public override void OnDie()
	{
		if (m_BreakPrefab)
		{
			var breakPrefab = Instantiate(m_BreakPrefab);
			breakPrefab.transform.position = transform.position + transform.rotation * m_BreakPrefabOffset;
		}
		AudioSource.PlayClipAtPoint(m_BreakSound, transform.position);

		Destroy(gameObject);
	}
}
