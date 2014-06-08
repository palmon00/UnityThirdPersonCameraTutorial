using UnityEngine;
using System.Collections;

//// <summary>
/// Struct to hold data for aligning camera
/// </summary>
struct CameraPosition
{
		// Position to align camera to, probably somewhere behind the character
		// or position to point camera at, probably somehwere along character's axis
		private Vector3 position;
		// Transform used for any rotation
		private Transform xForm;

		public Vector3 Position { get { return position; } set { position = value; } }
		public Transform XForm { get { return xForm; } set { xForm = value; } }

		public void Init (string camName, Vector3 pos, Transform transform, Transform parent)
		{
				position = pos;
				xForm = transform;
				xForm.name = camName;
				xForm.parent = parent;
				xForm.localPosition = position;
		}
}

//// <summary>
//// #DESCRIPTION OF CLASS#
//// </summary>
[RequireComponent (typeof(BarsEffect))]
public class ThirdPersonCameraLerp : MonoBehaviour
{
	#region Variables (private)
		private CharacterControllerLogic follow; // reference to our camera controller
		[SerializeField]
		private float
				distanceAway; // fixed distance away from follow target
		[SerializeField]
		private float
				distanceUp; // fixed distance up from follow target
		[SerializeField]
		private float
				smooth; // how long it takes for camera to move from current to target position
		[SerializeField]
		private Transform
				followXform; // follow transform of player (which is updated every frame, not like character who will be updated every fixed update)

		// Switching to FP mode
//		[SerializeField]
//		private float
//				widescreen = 0.2f; // time it takes 
//		[SerializeField]
//		private float
//				targetingTime = 0.5f; // time it takes for letterbox to appear/disappear

		// First Person
		[SerializeField]
		private float
				firstPersonCamUp = 1.7f; // how high first person camera is off ground
		[SerializeField]
		private float
				firstPersonCamForward = 0.2f; // how far forward first person cam is from target
		private CameraPosition firstPersonCamPos;
		[SerializeField]
		private float
				firstPersonThreshold = 0.5f; // how far player must push rightY up to trigger first person
		private float xAxisRot = 0.0f; // how far player has rotated their head up/down
		private float yAxisRot = 0.0f; // how far the player has rotated their head left/right
		private float lookWeight; // how much weighting we have on head movement when looking around
		[SerializeField]
		private const float
				targetingThreshold = 0.1f; // how strongly we must press to get into targeting mode
		[SerializeField]
		private float
				firstPersonLookSpeed = 3f; // how fast we look in FP mode
		[SerializeField]
		private Vector2
				firstPersonXAxisClamp = new Vector2 (-70.0f, 90.0f); // min and max angles we can look down and up
		[SerializeField]
		private Vector2
				firstPersonYAxisClamp = new Vector2 (-90.0f, 90.0f); // min and max angles we can look left and right
		[SerializeField]
		private float
				camFirstPersonNearClippingPlane = 0.01f;

		// Smoothing and collisions
		[SerializeField]
		private float
				camSmoothDampTime = 0.1f;
		[SerializeField]
		private float
				camCollisionOffset = 0.1f; // how much to move camera towards target after pointing towards target.  this moves us off any collisions
		[SerializeField]
		private float
				camCollisionNearClippingPlane = 0.01f; // size of near clipping plane when a collision has occurred
		private Vector3 velocityCamSmooth = Vector3.zero;
		private float camDefaultNearClippingPlane; // default near clipping plane when no collision has occurred

	
		// Motion of target camera
		private Vector3 lookDir; // direction camera is facing
		private Vector3 targetPosition; // where we want to be
	
		// Reference to image effect
//		private BarsEffect barEffect;

		// Start off in behind state
		private CamStates camState = CamStates.Behind;

	#endregion


	#region Properties (public)
		public CamStates CamState {
				get { return this.camState; }
		}

		public enum CamStates
		{
				Behind,
				FirstPerson,
				Target,
				Free
		}

	#endregion


	#region Unity event functions

		//// <summary>
		//// Use this for initialization
		//// </summary>
		void Start ()
		{
				// Get character controller
				follow = GameObject.FindWithTag ("Player").GetComponent<CharacterControllerLogic> ();
				if (follow == null) {
						Debug.LogError ("Could not find CharacterControllerLogic.", this);

				}
		
				// setup transform for player
				followXform = GameObject.FindWithTag ("FollowXform").transform;
				lookDir = followXform.forward;

				// setup bar effect for target state
//				barEffect = GetComponent<BarsEffect> ();
//				if (barEffect == null) {
//						Debug.LogError ("Attach a widescreen BarsEffect script to the camera.", this);
//				}

				// Position and parent a GameObject where first person view should be
				firstPersonCamPos = new CameraPosition ();
				firstPersonCamPos.Init
				(
					"First Person Camera",
					new Vector3 (0.0f, firstPersonCamUp, firstPersonCamForward),
					new GameObject ().transform,
					followXform
				);

				// store the default clipping plane
				camDefaultNearClippingPlane = this.camera.nearClipPlane;
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
				// Pull values from controller/keyboard
				float rightX = Input.GetAxis ("RightStickX");
				float rightY = Input.GetAxis ("RightStickY");

				// Setup character offset
				Vector3 characterOffset = followXform.position + new Vector3 (0f, distanceUp, 0f);

				// Set default lookAt value to point at character
				Vector3 lookAt = characterOffset;

				// Determine camera state
				if (Input.GetAxis ("Target") > targetingThreshold) {
//						barEffect.coverage = Mathf.SmoothStep (barEffect.coverage, widescreen, targetingTime);

						// Reset the head's position to face body's forward
						follow.Animator.SetLookAtPosition (follow.transform.position + follow.transform.forward);
						lookWeight = 0f;

						camState = CamStates.Target;
				} else {
//						barEffect.coverage = Mathf.SmoothStep (barEffect.coverage, 0f, targetingTime);

						// Enter FP mode if not moving and player pushes rightY up or down
						if ((Mathf.Abs (rightY) + Mathf.Abs (rightX)) > firstPersonThreshold && camState != CamStates.FirstPerson && !follow.IsInLocomotion ()) {
								// Set the clipping plane when entering first person mode
								this.camera.nearClipPlane = camFirstPersonNearClippingPlane;
								camState = CamStates.FirstPerson;
						}

						// Exit FP mode if player hits exit button and target mode if player is no longer requesting targeting
						if ((camState == CamStates.FirstPerson && Input.GetButton ("ExitFPV")) || (camState == CamStates.Target && (Input.GetAxis ("Target") <= targetingThreshold))) {

								// Set the clipping plane when entering first person mode
								this.camera.nearClipPlane = camDefaultNearClippingPlane;

								// Reset the head's position to face body's forward
								follow.Animator.SetLookAtPosition (follow.transform.position + follow.transform.forward);
								lookWeight = 0f;
					
								camState = CamStates.Behind;

						}
				}
		
				// Set the Look Weight - amount to use look at IK vs using the head's animation
				follow.Animator.SetLookAtWeight (lookWeight);

				// Execute camera state
				switch (camState) {
				case CamStates.Behind:
						{
								// Calculate the direction from camera to player, kill Y, and normalize to give a valid direction with unit magnitude
								lookDir = characterOffset - this.transform.position;
								lookDir.y = 0;
								lookDir.Normalize ();
								//				Debug.DrawRay (this.transform.position, lookDir, Color.yellow);
			
								// setting the target position to be the correct offset from the follow target
								//				targetPosition = followTransform.position + followTransform.up * distanceUp - followTransform.forward * distanceAway;
			
								// setting the target position to be the correct distance from the follow target
								targetPosition = characterOffset + followXform.up * distanceUp - lookDir * distanceAway;
								//				Debug.DrawRay (this.transform.position, targetPosition, Color.green);
			
								//				Debug.DrawRay (followTransform.position, followTransform.up * distanceUp, Color.red);
								//				Debug.DrawRay (followTransform.position, -1f * followTransform.forward * distanceAway, Color.blue);
								//				Debug.DrawLine (followTransform.position, targetPosition, Color.magenta);
								break;
						}
				case CamStates.Target:
						{
								// position camera behind player by turning camera to face player's forward
								lookDir = followXform.forward;

								targetPosition = characterOffset + followXform.up * distanceUp - lookDir * distanceAway;

								break;
						}
				case CamStates.FirstPerson:
						// Looking up and down
						// Calculate the amount of rotation and apply to the firstPersonCamPos GameObject
						xAxisRot += (rightY * firstPersonLookSpeed);
						xAxisRot = Mathf.Clamp (xAxisRot, firstPersonXAxisClamp.x, firstPersonXAxisClamp.y);
						yAxisRot += (rightX * firstPersonLookSpeed);
						yAxisRot = Mathf.Clamp (yAxisRot, firstPersonYAxisClamp.x, firstPersonYAxisClamp.y);
						firstPersonCamPos.XForm.localRotation = Quaternion.Euler (xAxisRot, yAxisRot, 0);

						// Superimpose firstPersonCamPos GameObject's rotation on camera
						Quaternion rotationShift = Quaternion.FromToRotation (this.transform.forward, firstPersonCamPos.XForm.forward);
						this.transform.rotation = rotationShift * this.transform.rotation;

						// Move the character model's head
						follow.Animator.SetLookAtPosition (firstPersonCamPos.XForm.position + firstPersonCamPos.XForm.forward);
						lookWeight = Mathf.Lerp (lookWeight, 1.0f, Time.deltaTime * firstPersonLookSpeed);

						// Move camera to firstPersonCamPos
						targetPosition = firstPersonCamPos.XForm.position;

						// Choose lookAt target based on distance
						lookAt = (Vector3.Lerp (this.transform.position + this.transform.forward, lookAt, Vector3.Distance (this.transform.position, firstPersonCamPos.XForm.position)));

						break;

				}

				compensateForWalls (characterOffset, ref targetPosition);
				smoothPosition (transform.position, targetPosition);

				// make sure the camera is looking the right way!
				transform.LookAt (lookAt);
		}


	#endregion


	#region Methods

		private void smoothPosition (Vector3 fromPos, Vector3 toPos)
		{
				// Making a smooth transition between camera's current position and the position it wants to be in
				this.transform.position = Vector3.SmoothDamp (fromPos, toPos, ref velocityCamSmooth, camSmoothDampTime);
		}

		private void compensateForWalls (Vector3 fromObject, ref Vector3 toTarget)
		{
				Debug.DrawLine (fromObject, toTarget, Color.cyan);
				// Compensate for walls between camera and target
				
				RaycastHit wallHit = new RaycastHit ();
				if (Physics.Linecast (fromObject, toTarget, out wallHit)) {
						Debug.DrawRay (wallHit.point, Vector3.right, Color.red);
						Debug.DrawRay (wallHit.point, Vector3.forward, Color.blue);
						Debug.DrawRay (wallHit.point, Vector3.up, Color.green);

						// set our new target location to the collision point
						toTarget = new Vector3 (wallHit.point.x, toTarget.y, wallHit.point.z);

						// move away from collision point
						toTarget = Vector3.MoveTowards (toTarget, fromObject, Vector3.Distance (toTarget, fromObject) * camCollisionOffset);

						// set the camera's near clipping plane
						this.camera.nearClipPlane = camCollisionNearClippingPlane;
				} else {
						// ensure the near clipping plane is restored
						this.camera.nearClipPlane = camDefaultNearClippingPlane;
				}
		}
	#endregion
}
