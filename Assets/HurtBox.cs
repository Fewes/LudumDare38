using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HurtBox : MonoBehaviour
{
	public delegate void HurtAction(Damageable damageable);
	public event HurtAction OnHurt;

	[SerializeField] private bool			m_EnemyOwned;

	private List<Damageable>				m_OverlappedDamageables;

	// Use this for initialization
	void Start ()
	{
		m_OverlappedDamageables = new List<Damageable>();
	}

	void OnTriggerEnter2D(Collider2D other)
	{
		var damageable = other.GetComponent<Damageable>();
		if (damageable)
		{
			m_OverlappedDamageables.Add(damageable);
			damageable.OnDeath += Unregister;
		}
    }

	private void Unregister(Damageable damageable)
	{
		m_OverlappedDamageables.Remove(damageable);
	}

	void OnTriggerExit2D(Collider2D other)
	{
        var damageable = other.GetComponent<Damageable>();
		if (damageable)
			m_OverlappedDamageables.Remove(damageable);
    }

	public void Hurt (int value, float delay = 0)
	{
		StartCoroutine(HurtDelayed(value, delay));
	}

	public void HurtLaunch (int value, Vector3 dir, float delay = 0)
	{
		StartCoroutine(HurtLaunchDelayed(value, dir, delay));
	}

	IEnumerator HurtDelayed (int value, float delay)
	{
		yield return new WaitForSeconds(delay);

		foreach (var damageable in m_OverlappedDamageables.ToArray())
		{
			var wasAlive = damageable.IsAlive();
			damageable.Damage(value);
			if (OnHurt != null && wasAlive)
				OnHurt(damageable);
		}
	}

	IEnumerator HurtLaunchDelayed (int value, Vector3 dir, float delay)
	{
		yield return new WaitForSeconds(delay);

		foreach (var damageable in m_OverlappedDamageables.ToArray())
		{
			var wasAlive = damageable.IsAlive();
			damageable.Damage(value);
			if (OnHurt != null && wasAlive)
				OnHurt(damageable);
			var controller = damageable.GetComponent<CharController>();
			if (controller && wasAlive)
			{
				controller.Launch(dir, true);
			}
			var breakable = damageable.GetComponent<Breakable>();
			if (breakable && wasAlive)
			{
				breakable.Launch(dir);
			}
		}
	}
}
