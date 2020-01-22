using System;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;

namespace UnityStandardAssets.Characters.ThirdPerson
{
    [RequireComponent(typeof (ThirdPersonCharacter))]
    public class ThirdPersonUserControl : MonoBehaviour
    {
        private ThirdPersonCharacter m_Character; // A reference to the ThirdPersonCharacter on the object
        private Transform m_Cam;                  // A reference to the main camera in the scenes transform
        private Vector3 m_CamForward;             // The current forward direction of the camera
        private Vector3 m_Move;
        private bool m_Jump;                      // the world-relative desired move direction, calculated from the camForward and user input.


        //my code
        private Transform m_ladderTrigger; // The current trigger we hit for the ladder
        private float m_ClimbSpeed; // How fast does the player climb the ladder
        private bool m_isClimbing = false; // Are we currently climbing?
        private TransitionState _climbingTransition = TransitionState.None;
        private Vector3 m_MoveDir = Vector3.zero;
        private CharacterController m_CharacterController;
        private CollisionFlags m_CollisionFlags;
        private Vector2 m_Input;
        private enum TransitionState
        {
            None = 0,
            ToLadder1 = 1,
            ToLadder2 = 2,
            ToLadder3 = 3
        }

        private void Start()
        {
            // get the transform of the main camera
            if (Camera.main != null)
            {
                m_Cam = Camera.main.transform;
            }
            else
            {
                Debug.LogWarning(
                    "Warning: no main camera found. Third person character needs a Camera tagged \"MainCamera\", for camera-relative controls.", gameObject);
                // we use self-relative controls in this case, which probably isn't what the user wants, but hey, we warned them!
            }

            // get the third person character ( this should never be null due to require component )
            m_Character = GetComponent<ThirdPersonCharacter>();
        }


        private void Update()
        {
            if (!m_Jump)
            {
                m_Jump = CrossPlatformInputManager.GetButtonDown("Jump");
            }
        }


        // Fixed update is called in sync with physics
        private void FixedUpdate()
        {

            // read inputs
            float h = CrossPlatformInputManager.GetAxis("Horizontal");
            float v = CrossPlatformInputManager.GetAxis("Vertical");
            bool crouch = Input.GetKey(KeyCode.C);

            // Ladder climbing logic
            Vector3 desiredMove = Vector3.zero;
            if (m_isClimbing)
            {
                Transform trLadder = m_ladderTrigger.parent;

                if (_climbingTransition != TransitionState.None)
                {
                    // Get the next point to which we have to move while we are climbing the ladder
                    transform.position = trLadder.Find(_climbingTransition.ToString()).position;
                    _climbingTransition = TransitionState.None;
                }
                else
                {

                    // Attach the player to the ladder with the rotation angle of the ladder transform
                    desiredMove = trLadder.rotation * Vector3.forward * m_Input.y;

                    m_MoveDir.y = desiredMove.y * m_ClimbSpeed;
                    m_MoveDir.x = desiredMove.x * m_ClimbSpeed;
                    m_MoveDir.z = desiredMove.z * m_ClimbSpeed;

                    m_CollisionFlags = m_CharacterController.Move(m_MoveDir * Time.fixedDeltaTime);
                }
            }

            // calculate move direction to pass to character
            if (m_Cam != null)
            {
                // calculate camera relative direction to move:
                m_CamForward = Vector3.Scale(m_Cam.forward, new Vector3(1, 0, 1)).normalized;
                m_Move = v*m_CamForward + h*m_Cam.right;
            }
            else
            {
                // we use world-relative directions in the case of no main camera
                m_Move = v*Vector3.forward + h*Vector3.right;
            }
#if !MOBILE_INPUT
			// walk speed multiplier
	        if (Input.GetKey(KeyCode.LeftShift)) m_Move *= 0.5f;
#endif

            // pass all parameters to the character control script
            m_Character.Move(m_Move, crouch, m_Jump);
            m_Jump = false;
        }


        void OnTriggerEnter(Collider other)
        {
            if (other.tag == "Ladder_Bottom")
            {
                m_ladderTrigger = other.transform;

                if (!m_isClimbing)
                {
                    _climbingTransition = TransitionState.ToLadder1;
                    ToggleClimbing();
                }
                else
                {
                    ToggleClimbing();
                    _climbingTransition = TransitionState.None;
                }
            }

            else if (other.tag == "Ladder_Top")
            {
                m_ladderTrigger = other.transform;

                // We hit the top trigger and come from the ladder
                if (m_isClimbing)
                {
                    // move to the upper point and exit the ladder
                    _climbingTransition = TransitionState.ToLadder3;
                }
                else
                {
                    // We seem to come from above, so let's move to tha ladder (point 2) again
                    _climbingTransition = TransitionState.ToLadder2;
                }

                ToggleClimbing();
            }
        }
        private void ToggleClimbing()
        {
            m_isClimbing = !m_isClimbing;
        }
    }
}
