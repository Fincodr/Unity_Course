using UnityEngine;
using System.Collections;

public class Explosion : MonoBehaviour 
{
	public float disappearanceSpeed = 2.0f;
	public float scaleSpeed = 100.0f;
	// Update is called once per frame
	void Update () 
	{
		// scale up
		float uniformScale = Time.deltaTime *scaleSpeed;
		Vector3 newScale = this.transform.localScale + new Vector3(uniformScale,uniformScale,uniformScale); 
		this.transform.localScale = newScale;
		
		float alpha = this.renderer.material.GetFloat("_Alpha");
		alpha -= disappearanceSpeed*Time.deltaTime;
		// When we are shown no more, it is time to die.
		if (alpha < 0.0f ) 
		{
			alpha = 0.0f;
			GameObject.Destroy(this.gameObject);
		}
		this.renderer.material.SetFloat("_Alpha", alpha);
		
	}
}
