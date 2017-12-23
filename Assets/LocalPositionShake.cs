using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LocalPositionShake : MonoBehaviour
{
	public float		shakeAmp = 0.25f;
	public float		shakeDur = 0.5f;

	private Vector3		m_ShakeTransformOrigPos;
	private float		m_ShakeTimer;

	void Start ()
	{
		m_ShakeTransformOrigPos = transform.localPosition;
	}
	
	void Update ()
	{
		m_ShakeTimer -= Time.deltaTime;
		if (transform)
		{
			if (m_ShakeTimer > 0)
			{
				transform.localPosition = new Vector2(m_ShakeTransformOrigPos.x, m_ShakeTransformOrigPos.y) + Random.insideUnitCircle * shakeAmp * (m_ShakeTimer / shakeDur);
			}
			else
				transform.localPosition = m_ShakeTransformOrigPos;
		}
	}

	public void Shake ()
	{
		m_ShakeTimer = shakeDur;
	}
}
