 using UnityEditor;
 using UnityEngine;

 [InitializeOnLoad]
 class EditorUpdater
 {
     static EditorUpdater ()
     {
         EditorApplication.update += Update;
     }
 
     static void Update ()
     {
		var lightManager = GameObject.FindObjectOfType<LightManager>();
		if (lightManager)
			lightManager.EditorUpdate();
     }
 }