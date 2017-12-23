using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationListener : MonoBehaviour
{
	public enum AnimationListenerType
	{
		Unknown,
		Player,
		Enemy
	}

	Player	m_Player;
	Enemy	m_Enemy;

	AnimationListenerType type = AnimationListenerType.Unknown;

	void Start ()
	{
		m_Player = GetComponentInParent<Player>();
		m_Enemy = GetComponentInParent<Enemy>();

		if (m_Player)
			type = AnimationListenerType.Player;
		else if (m_Enemy)
			type = AnimationListenerType.Enemy;
	}

	public void MecanimEvent (string value)
	{
		if (type == AnimationListenerType.Player)
		{
			if (value == "Footstep")
				m_Player.Footstep();
			//else if (value == "AttackHurt")
				//m_Player.AttackHurt();
			else if (value == "AttackFinished")
				m_Player.AttackFinished();
			else if (value == "SaultFinished")
				m_Player.SaultFinished();
		}
		else if (type == AnimationListenerType.Enemy)
		{
			if (value == "Footstep")
				m_Enemy.Footstep();
			else if (value == "AttackHurt")
				m_Enemy.AttackHurt();
			else if (value == "AttackFinished")
				m_Enemy.AttackFinished();
			else
				Debug.LogWarning("Unhandled MecanimEvent: " + value);
		}
	}
}
