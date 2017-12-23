using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public struct PoolCategory
{
	[SerializeField] public string name;
	[SerializeField] public ObjectPooler[] m_Pools;
}

[System.Serializable]
public class PoolManager : MonoBehaviour
{
	public static PoolManager manager;

	[SerializeField] public PoolCategory[] m_Categories;

	void Awake()
	{
		// Singleton pattern
		if (manager == null)
			manager = this;
		else if (manager != this)
			Destroy(gameObject);

		// Initialize pools
		foreach (var cat in m_Categories)
		{
			foreach(var pool in cat.m_Pools)
			{
				// Instantiate pooled objects
				pool.pooledObjects = new List<PooledObject>();
				for (int i = 0; i < pool.poolSize; ++i)
				{
					PooledObject po = new PooledObject();
					po.go = Instantiate(pool.pooledObject);
					po.go.SetActive(false);
					pool.pooledObjects.Add(po);
				}
			}
		}
	}

	public static GameObject GetPooledObjectS(string category, string type, bool autoActivate = true)
	{
		return manager.GetPooledObject(category, type, autoActivate);
	}
	
	public GameObject GetPooledObject(string category, string type, bool autoActivate = true)
	{
		foreach (var cat in m_Categories)
		{
			if (cat.name == category)
			{
				// Attempt to find pool by name
				foreach (var pool in cat.m_Pools)
				{
					if (pool.name == type)
					{
						// Search for available pooled objects
						foreach (var po in pool.pooledObjects)
						{
							if (!po.go.activeInHierarchy)
							{
								// Available object found
								if (po.go && autoActivate)
									po.go.SetActive(true);
								po.setSpawnTime(Time.time);
								return po.go;
							}
						}

						// No available pooled object found, check if we are allowed to expand the pool
						if (pool.allowExpand)
						{
							PooledObject po = new PooledObject();
							po.go = Instantiate(pool.pooledObject);
							pool.pooledObjects.Add(po);
							if (po.go && autoActivate)
								po.go.SetActive(true);
							po.setSpawnTime(Time.time);
							return po.go;
						}

						// No available pooled object found and we were not allowed to expand the pool, check if we are allowed to steal active objects
						if (pool.allowSteal)
						{
							// Find the oldest active object
							PooledObject po = pool.pooledObjects[0];
							foreach (var p in pool.pooledObjects)
							{
								if (p.getSpawnTime() < po.getSpawnTime())
									po = p;
							}
							/*
							int index = 0;
							for (int i = 0; i < pool.pooledObjects.Count; i++)
							{
								if (pool.pooledObjects[i].getSpawnTime() < pool.pooledObjects[index].getSpawnTime())
									index = i;
							}
							PooledObject po = pool.pooledObjects[index];
							*/

							po.go.SetActive(false);
							if (po.go && autoActivate)
								po.go.SetActive(true);
							po.setSpawnTime(Time.time);
							return po.go;
						}

						// No available pooled object found and we were not allowed to expand the pool or steal active objects
						return null;
					}
				}
				// Pool not found
				return null;
			}
		}
		// Category not found
		return null;
	}

	public int GetActiveObjectCount(string category, string type)
	{
		foreach (var cat in m_Categories)
			if (cat.name == category)
				foreach(var pool in cat.m_Pools)
					if (pool.name == type)
						return pool.GetActiveObjectCount();

		// Pool not found
		return -1;
	}
}
