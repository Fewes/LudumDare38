using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
	public Player					connectedPlayer;

	[SerializeField] RectTransform	healthBar;
	[SerializeField] RectTransform	healthBarLag;
	[SerializeField] Text			healthText;
	[SerializeField] RectTransform	xpBar;
	[SerializeField] Text			xpText;
	[SerializeField] Text			lvlText;

	private int						prevHealth;
	private float					healthBarLagTimer;

	// Use this for initialization
	void Start ()
	{
		
	}

	void SetHealth (int health, int maxHealth)
	{
		// Loss of health
		if (health < prevHealth)
		{
			var scale = healthBar.localScale;
			healthBarLag.localScale = scale;
			scale.x = (float)health / (float)maxHealth;
			healthBar.localScale = scale;
			healthBarLagTimer = 0.5f;

			var shake = healthBar.parent.GetComponent<LocalPositionShake>();
			if (shake)
			{
				shake.Shake();
			}
		}
		else
		{
			var scale = healthBar.localScale;
			scale.x = (float)health / (float)maxHealth;
			healthBar.localScale = scale;
			healthBarLag.localScale = scale;
		}

		healthText.text = health.ToString();
	}
	
	// Update is called once per frame
	void Update ()
	{
		healthBarLagTimer -= Time.deltaTime;
		if (healthBarLagTimer <= 0)
		{
			healthBarLag.localScale = Vector3.Lerp(healthBarLag.localScale, healthBar.localScale, Time.deltaTime * 5);
		}

		int health = connectedPlayer.GetHealth();
		if (health != prevHealth)
		{
			SetHealth(health, connectedPlayer.GetMaxHealth());
		}
		prevHealth = health;
	}
}
