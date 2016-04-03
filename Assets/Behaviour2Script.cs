using UnityEngine;
using System.Collections;
using System.Linq;
using Fuzzy;

public class Behaviour2Script : MonoBehaviour {

	// Use this for initialization
	Fis fis;
	Rigidbody phy;
	public Rigidbody body;


	public float force;
	void Start () {
		fis = new Fis("Assets/b7matlab.txt");
		phy = GetComponent<Rigidbody>();

		StartCoroutine("Runtime");
	}

	IEnumerator Runtime(){
		float E = 0;
		float E_0  = 0;
		float ED = 0;
		while(true) {
			yield return new WaitForSeconds(0.1f);
			E_0 = E;
			E = 0 - transform.position.x;
			ED = E - E_0;

			if(E > 1) E = 1;
			if(E < -1) E = -1;
			if(ED > 1) ED = 1;
			if(ED < -1) ED = -1;

			float[] r = fis.Eval(new float[2] {E, ED});

			body.AddForce(body.transform.right*r[0]*force, ForceMode.Impulse);

		}
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
