using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PositionJitter : MonoBehaviour
{
	public float m_Amplitude = 0.1f;
	public float m_Frequency = 0.1f;
	public float m_Drag = 1.0f;
	public bool m_UseLocalPosition = true;
	public bool m_LockXAxis;
	public bool m_LockYAxis;
	public bool m_LockZAxis;

	private Vector3 m_OriginalPosition;
	private Vector3 m_TargetPosition;
	private float m_Timer = 0;

	// Use this for initialization
	void Start ()
	{
		m_OriginalPosition = m_UseLocalPosition ? transform.localPosition : transform.position;
	}

	void OnValidate ()
	{
		m_Frequency = Mathf.Max(m_Frequency, 0.01f);
	}
	
	// Update is called once per frame
	void Update ()
	{
		m_Timer += Time.deltaTime;
		if (m_Timer > m_Frequency)
		{
			while (m_Timer > m_Frequency)
				m_Timer -= m_Frequency;
			// Generate new position
			var offset = Random.insideUnitSphere * m_Amplitude;
			if (m_LockXAxis) offset.x = 0;
			if (m_LockYAxis) offset.y = 0;
			if (m_LockZAxis) offset.z = 0;
			m_TargetPosition = m_OriginalPosition + offset;
		}

		if (m_UseLocalPosition)
			transform.localPosition = Vector3.Lerp(transform.localPosition, m_TargetPosition, Time.deltaTime / m_Drag);
		else
			transform.position = Vector3.Lerp(transform.position, m_TargetPosition, Time.deltaTime / m_Drag);
	}
}
