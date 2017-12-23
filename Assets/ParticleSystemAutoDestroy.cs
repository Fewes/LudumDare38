using UnityEngine;
using System.Collections;
 
public class ParticleSystemAutoDestroy : MonoBehaviour 
{
	private ParticleSystem ps;
 
	public void Start () 
	{
		ps = GetComponent<ParticleSystem>();
	}

	void OnDisable ()
	{
		if (ps)
			ps.Stop();
	}
 
	public void Update () 
	{
		if(!ps.IsAlive())
		{
			Destroy(gameObject);
		}
	}
}