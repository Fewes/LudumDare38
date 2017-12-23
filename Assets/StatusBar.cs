using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StatusBar : MonoBehaviour
{
	[SerializeField] private float barLagTime = 0.25f;

	private TextMesh	lvlText;
	private TextMesh	healthText;
	private Transform	bar;
	private Transform	barLag;
	private Damageable	damageable;

	private float		lagTimer;

	// Use this for initialization
	void Start ()
	{
		lvlText = transform.Find("LevelText").GetComponent<TextMesh>();
		healthText = transform.Find("HealthText").GetComponent<TextMesh>();
		bar = transform.Find("Bar");
		barLag = transform.Find("Bar_Lag");
		damageable = GetComponentInParent<Damageable>();

		damageable.OnHurt += OnHurt;
		damageable.OnDeath += OnDeath;
	}

	private void OnDeath(Damageable d)
	{
		// gameObject.SetActive(false);
	}

	private void OnHurt(Damageable d)
	{
		var health = d.GetHealth();

		healthText.text = health.ToString();

		barLag.transform.localScale = bar.transform.localScale;

		var scale = bar.transform.localScale;
		scale.x = Mathf.Max((float)health / 100f, 0.00001f);
		bar.transform.localScale = scale;

		lagTimer = barLagTime;
	}

	void Update ()
	{
		lagTimer -= Time.deltaTime;

		if (lagTimer <= 0)
		{
			barLag.transform.localScale = bar.transform.localScale;
		}
	}
}
