using UnityEngine;
using System.Collections;

//// <summary>
//// #DESCRIPTION OF CLASS#
//// </summary>
public class ThirdPersonCameraSlerp : MonoBehaviour
{
	#region Variables (private)
		[SerializeField]
		private float
				distanceAway; // fixed distance from back of follow target
		[SerializeField]
		private float
				distanceUp; // fixed distance up from follow target
		[SerializeField]
		private float
				smooth; // how long it takes for camera to move from current to target position
		[SerializeField]
		private Transform
				follow; // follow transform of player (which is updated every frame, not like character who will be updated every fixed update)
		private Vector3 targetPosition; // where we want to be

	#endregion


	#region Properties (public)
	 
	#endregion


	#region Unity event functions

		//// <summary>
		//// Use this for initialization
		//// </summary>
		void Start ()
		{
				follow = GameObject.FindGameObjectWithTag ("Player").transform;
		}

		//// <summary>
		//// Update is called once per frame
		//// </summary>
		void Update ()
		{
	
		}

		//// <summary>
		//// Debugging information should be put here
		//// </summary>
		void OnDrawGizmos ()
		{
	
		}

		// happens after all updates so we can be sure camera will be moving to right position
		void LateUpdate ()
		{
				// setting the target position to be the correct offset from the follow target
				targetPosition = follow.position + follow.up * distanceUp - follow.forward * distanceAway;
				Debug.DrawRay (follow.position, follow.up * distanceUp, Color.red);
				Debug.DrawRay (follow.position, -1f * follow.forward * distanceAway, Color.blue);
				Debug.DrawLine (follow.position, targetPosition, Color.magenta);

				// making a smooth transition between it's current position and the position it wants to be in
				transform.position = Vector3.Slerp (transform.position, targetPosition, Time.deltaTime * smooth);

				// make sure the camera is looking the right way!
				transform.LookAt (follow);
		}


	#endregion


	#region Methods

	#endregion
}
