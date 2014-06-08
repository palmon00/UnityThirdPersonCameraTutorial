using UnityEngine;
using System.Collections;

// <summary>
// #DESCRIPTION OF CLASS#
// </summary>
public class CharacterControllerLogic : MonoBehaviour
{
	#region Variables (private)

		// Inspector serialized
		[SerializeField]
		private Animator
				animator;	// reference to character controller
		[SerializeField]
		private float
				rotationDegreePerSecond = 120f;
		[SerializeField]
		private float
				directionDampTime = 0.25f; // used to set delay on time for setting direction.  will ensure we use previous frame's value(?)
		[SerializeField]
		private ThirdPersonCameraLerp
				gameCam;
		[SerializeField]
		private float
				directionSpeed = 3f; // acceleration factor on camera angle

		// Private global only
		private float speed = 0.0f;
		private float direction = 0.0f;
		private float horizontal = 0.0f;
		private float vertical = 0.0f;
		private AnimatorStateInfo stateInfo;

		// Hashes
		private int m_LocomotionId = 0;

	#endregion


	#region Properties (public)
		public Animator Animator {
				get{ return this.animator; }
		}

	#endregion


	#region Unity event functions

		// <summary>
		// Use this for initialization
		// </summary>
		void Start ()
		{
				animator = GetComponent<Animator> ();

				if (animator.layerCount >= 2) {
						animator.SetLayerWeight (1, 1);
				}

				// Hash all animation names for performance
				m_LocomotionId = Animator.StringToHash ("Base Layer.Locomotion");

		}
 
		// <summary>
		// Update is called once per frame
		// </summary>
		void Update ()
		{

				stateInfo = animator.GetCurrentAnimatorStateInfo (0);

				switch (gameCam.CamState) {
				case ThirdPersonCameraLerp.CamStates.Behind:
						Debug.Log ("In behind state");
						break;
				case ThirdPersonCameraLerp.CamStates.FirstPerson:
						Debug.Log ("In FPV state");
						break;

				case ThirdPersonCameraLerp.CamStates.Target:
						Debug.Log ("In target state");
						break;

				}

				if (animator && gameCam.CamState != ThirdPersonCameraLerp.CamStates.FirstPerson) {
						// Pull values from controller/keyboard
						horizontal = Input.GetAxis ("Horizontal");
						vertical = Input.GetAxis ("Vertical");

						// naive setting of speed and direction
//						speed = new Vector2 (horizontal, vertical).sqrMagnitude;
//						animator.SetFloat ("Speed", speed);
//						animator.SetFloat ("Direction", horizontal, directionDampTime, Time.deltaTime); // use damp time to prevent rapid directional changes

						// Translate control stick coordinates into world/cam/character space
						StickToWorldSpace (this.transform, gameCam.transform, ref direction, ref speed);

						// changing speed and direction based on direction player is facing
						animator.SetFloat ("Speed", speed);
						animator.SetFloat ("Direction", direction, directionDampTime, Time.deltaTime);

				}
		}

		// <summary>
		// Any code that moves the character needs to be checked against physics
		// </summary>
		void FixedUpdate ()
		{
				// Rotate character model if stick is tilted right or left, but only if character is moving in that direction
				if (IsInLocomotion () && ((direction >= 0 && horizontal >= 0) || (direction < 0 && horizontal < 0))) {
						Vector3 rotationAmount = Vector3.Lerp (Vector3.zero, new Vector3 (0f, rotationDegreePerSecond * (horizontal < 0f ? -1f : 1f), 0f), Mathf.Abs (horizontal));
						Quaternion deltaRotation = Quaternion.Euler (rotationAmount * Time.deltaTime);
						this.transform.rotation = (this.transform.rotation * deltaRotation);
				}
		}

		// <summary>
		// Debugging information should be put here
		// </summary>
		void OnDrawGizmos ()
		{
	
		}

	#endregion


	#region Methods
	
		// convert "z axis" stick to world coordinates
		public void StickToWorldSpace (Transform root, Transform camera, ref float directionOut, ref float speedOut)
		{
				Vector3 rootDirection = root.forward; // this is our reference vector
		
				Vector3 stickDirection = new Vector3 (horizontal, 0, vertical); // where the player is moving the controller stick

				speedOut = stickDirection.sqrMagnitude; // update speed out

				// Get camera rotation
				Vector3 cameraDirection = camera.forward;
				cameraDirection.y = 0.0f; // flatten y
				Quaternion referentialShift = Quaternion.FromToRotation (Vector3.forward, cameraDirection);

				// Convert joystick input into Worldspace coordinates
				Vector3 moveDirection = referentialShift * stickDirection;
				Vector3 axisSign = Vector3.Cross (moveDirection, rootDirection);

				Debug.DrawRay (new Vector3 (root.position.x, root.position.y + 2f, root.position.z), moveDirection, Color.green);
				Debug.DrawRay (new Vector3 (root.position.x, root.position.y + 2f, root.position.z), axisSign, Color.red);
				Debug.DrawRay (new Vector3 (root.position.x, root.position.y + 2f, root.position.z), rootDirection, Color.magenta);
				Debug.DrawRay (new Vector3 (root.position.x, root.position.y + 2f, root.position.z), stickDirection, Color.blue);

				float angleRootToMove = Vector3.Angle (rootDirection, moveDirection) * (axisSign.y >= 0 ? -1f : 1f);

				angleRootToMove /= 180f;

				directionOut = angleRootToMove * directionSpeed;
		}

		// see if we're in locomotion
		public bool IsInLocomotion ()
		{
				return stateInfo.nameHash == m_LocomotionId;
		}
	#endregion
}
