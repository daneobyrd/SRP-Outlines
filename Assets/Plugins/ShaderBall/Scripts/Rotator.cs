using UnityEngine;
using System.Collections;

public class Rotator : MonoBehaviour {

    private Vector3 pivot = Vector3.zero;
    public Vector3 speed = new(0, 20, 0);

	// Use this for initialization
	void Awake () {
        pivot = transform.position;
	}
	
	// Update is called once per frame
	void Update () {
        transform.RotateAround(pivot, Vector3.right, speed.x * Time.deltaTime);
        transform.RotateAround(pivot, Vector3.up, speed.y * Time.deltaTime);
        transform.RotateAround(pivot, Vector3.forward, speed.z * Time.deltaTime);
	}
}
