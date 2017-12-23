using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Damageable : MonoBehaviour
{
	public delegate void DeathAction(Damageable damageable);	public event DeathAction OnDeath;
	public delegate void DamageAction(Damageable damageable);	public event DamageAction OnHurt;

	[Header("Damageable")]
	[SerializeField] private AudioClip[]	hitSounds;
	[SerializeField] private AudioClip[]	deathSounds;
	
	[SerializeField] private int			health = 100;
	[SerializeField] private Vector3		centerOffset = Vector3.zero;
	[SerializeField] bool					dramatic = false;

	private int								currentHealth;

	public void Start ()
	{
		currentHealth = health;
	}

	public int GetHealth ()
	{
		return currentHealth;
	}

	public int GetMaxHealth ()
	{
		return health;
	}

	public bool IsAlive ()
	{
		return currentHealth > 0;
	}

	public Vector3 GetCenter()
	{
		return transform.position + transform.TransformVector(centerOffset);
	}

	public void Damage (int value)
	{
		if (!IsAlive())
			return;

		TextManager.AddWorldText("-" + Mathf.Min(value, currentHealth).ToString(), transform.position + Vector3.up, 1);

		int prevHealth = currentHealth;
		currentHealth = Mathf.Max(currentHealth - value, 0);

		if (!IsAlive() && prevHealth > 1 && dramatic)
			currentHealth = 1;

		OnDamage();
		if (OnHurt != null)
			OnHurt(this);
		if (currentHealth <= 0)
		{
			if (OnDeath != null)
				OnDeath(this);
			OnDie();

			if (deathSounds.Length > 0)
				AudioSource.PlayClipAtPoint(deathSounds[Random.Range(0, deathSounds.Length)], transform.position);
		}
		else
		{
			if (hitSounds.Length > 0)
				AudioSource.PlayClipAtPoint(hitSounds[Random.Range(0, hitSounds.Length)], transform.position);
		}
	}
	
	public abstract void OnDamage ();
	public abstract void OnDie ();
}
