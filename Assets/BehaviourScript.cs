using UnityEngine;
using System.Collections;
using System.Linq;
using Fuzzy;

public class BehaviourScript : MonoBehaviour {

	// Use this for initialization
	Fis fis;
	Rigidbody phy;

	public GameObject[] motors;
	public float droneForce;
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
			yield return new WaitForSeconds(0);
			E_0 = E;
			E = 0 - transform.position.y;
			ED = E - E_0;

			if(E > 1) E = 1;
			if(E < -1) E = -1;
			if(ED > 1) ED = 1;
			if(ED < -1) ED = -1;

			float[] r = fis.Eval(new float[2] {E, ED});

			float[] aa = new float[4] { r[0], r[0], r[0]*0.9f, r[0]*0.9f};

			Vector3 up;
			for(int i = 0; i < motors.Length; i++) {
				up = motors[i].transform.up;
				motors[i].GetComponent<Rigidbody>().velocity = up * aa[i] * droneForce;
			}
		}
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
