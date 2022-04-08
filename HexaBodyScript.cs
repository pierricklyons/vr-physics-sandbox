using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEditor.XR.LegacyInputHelpers;
using UnityEngine.XR;
using UnityEngine.InputSystem;

public class HexaBodyScript : MonoBehaviour
{
    [Header("XR Toolkit Parts")]
    public XRRig XRRig;
    public GameObject XRCamera;

    [Header("Actionbased Controller")]
    public ActionBasedController CameraController;
    public ActionBasedController RightHandController;
    public ActionBasedController LeftHandController;

    public InputActionReference RightTrackPadPress;
    public InputActionReference RightTrackPadTouch;

    public InputActionReference LeftTrackPadPress;
    public InputActionReference LeftTrackPadTouch;

    [Header("Hexabody Parts")]
    public GameObject Body;
    public GameObject Head;
    public GameObject Chest;
    public GameObject Fender;
    public GameObject Monoball;

    public ConfigurableJoint RightHandJoint;
    public ConfigurableJoint LeftHandJoint;
    public ConfigurableJoint Spine;

    [Header("Hexabody Movespeed")]
    public float TurnSpeed;
    public float moveForceCrouch;
    public float moveForceWalk;
    public float moveForceSprint;

    [Header("Hexabody Drag")]
    public float angularDragOnMove;
    public float angularBreakDrag;

    [Header("Hexabody Croch & Jump")]
    bool jumping = false;

    public float crouchSpeed;
    public float lowestCrouch;
    public float highestCrouch;
    private float additionalHeight;

    public Vector3 CrouchTarget;

    //---------Input Values---------------------------------------------------------------------------------------------------------------//

    private Quaternion headYaw;
    private Vector3 moveDirection;
    private Vector3 monoballTorque;

    private Vector3 CameraControllerPos;

    private Vector3 RightHandControllerPos;
    private Vector3 LeftHandControllerPos;

    private Quaternion RightHandControllerRotation;
    private Quaternion LeftHandControllerRotation;

    private Vector2 RightTrackpad;
    private Vector2 LeftTrackpad;

    private float RightTrackpadPressed;
    private float LeftTrackpadPressed;

    private float RightTrackpadTouched;
    private float LeftTrackpadTouched;

    void Start() {
        additionalHeight = (0.5f * Monoball.transform.lossyScale.y) + (0.5f * Fender.transform.lossyScale.y) + (Head.transform.position.y - Chest.transform.position.y);
    }

    
    void Update() {
        getContollerInputValues();
        CameraToPlayer();
        XRRigToPlayer();
    }

    private void FixedUpdate()  {
        movePlayerViaController();
        jump();
        ajust();

        if (!jumping) {
            spineContractionOnRealWorldCrouch();
        }

        rotatePlayer();
        moveAndRotateHand();
    }

    private void getContollerInputValues() {
        //Right Controller
        //Position & Rotation
        RightHandControllerPos = RightHandController.positionAction.action.ReadValue<Vector3>();
        RightHandControllerRotation = RightHandController.rotationAction.action.ReadValue<Quaternion>();

        //Trackpad
        RightTrackpad = RightHandController.translateAnchorAction.action.ReadValue<Vector2>();
        RightTrackpadPressed = RightTrackPadPress.action.ReadValue<float>();
        RightTrackpadTouched = RightTrackPadTouch.action.ReadValue<float>();

        //Left Contoller
        //Position & Rotation
        LeftHandControllerPos = LeftHandController.positionAction.action.ReadValue<Vector3>();
        LeftHandControllerRotation = LeftHandController.rotationAction.action.ReadValue<Quaternion>();

        //Trackpad
        LeftTrackpad = LeftHandController.translateAnchorAction.action.ReadValue<Vector2>();
        LeftTrackpadPressed = LeftTrackPadPress.action.ReadValue<float>();
        LeftTrackpadTouched = LeftTrackPadTouch.action.ReadValue<float>();

        //Camera Inputs
        CameraControllerPos = CameraController.positionAction.action.ReadValue<Vector3>();

        headYaw = Quaternion.Euler(0, XRRig.cameraGameObject.transform.eulerAngles.y, 0);
        moveDirection = headYaw * new Vector3(LeftTrackpad.x, 0, LeftTrackpad.y);
        monoballTorque = new Vector3(moveDirection.z, 0, -moveDirection.x);
    }

    //------Transforms---------------------------------------------------------------------------------------
    private void CameraToPlayer() {
        XRCamera.transform.position = Head.transform.position;
    }

    private void XRRigToPlayer() {
        XRRig.transform.position = new Vector3(Fender.transform.position.x, Fender.transform.position.y - (0.5f * Fender.transform.localScale.y + 0.5f * Monoball.transform.localScale.y), Fender.transform.position.z);
    }

    private void rotatePlayer() {
        if (RightTrackpadPressed == 1) return;
        Head.transform.Rotate(0, RightTrackpad.x * TurnSpeed, 0, Space.Self);
        Chest.transform.rotation = headYaw;
    }

    //-----HexaBody Movement---------------------------------------------------------------------------------
    private void movePlayerViaController() {
        if (!jumping) {
            if (LeftTrackpadTouched == 0) {
                stopMonoball();
            }

            if (LeftTrackpadPressed == 0 && LeftTrackpadTouched == 1) {
                moveMonoball(moveForceWalk);
            }

            if (LeftTrackpadPressed == 1 && LeftTrackpadTouched == 1) {
                moveMonoball(moveForceSprint);
            }
        }

        else if (jumping) {
            // if (LeftTrackpadTouched == 0) {
            //     stopMonoball();
            // }

            if (LeftTrackpadTouched == 1) {
                moveMonoball(moveForceCrouch);
            }
        }
    }

    private void moveMonoball(float force) {
        Monoball.GetComponent<Rigidbody>().freezeRotation = false;
        Monoball.GetComponent<Rigidbody>().angularDrag = angularDragOnMove;
        Monoball.GetComponent<Rigidbody>().AddTorque(monoballTorque.normalized * (force * 2), ForceMode.Force);
    }

    private void stopMonoball() {
        Monoball.GetComponent<Rigidbody>().angularDrag = angularBreakDrag;

        if (Monoball.GetComponent<Rigidbody>().velocity == Vector3.zero) {
            Monoball.GetComponent<Rigidbody>().freezeRotation = true;
        }
    }

    //------Jumping------------------------------------------------------------------------------------------
    private void jump() {
        if (RightTrackpadPressed == 1 && RightTrackpad.y < 0) {
            jumping = true;
            jumpSitDown();
        }

        if ((RightTrackpadPressed == 0) && jumping == true) {
            jumping = false;
            jumpSitUp();
        }

    }

    private void ajust() {
        if (RightTrackpadPressed == 1 && RightTrackpad.y > 0) {
            CrouchTarget.y += crouchSpeed * Time.fixedDeltaTime;
            Spine.targetPosition = new Vector3(0, CrouchTarget.y, 0);
        }
    }

    private void jumpSitDown() {
        if (CrouchTarget.y >= lowestCrouch) {
            CrouchTarget.y -= crouchSpeed * Time.fixedDeltaTime;
            Spine.targetPosition = new Vector3(0, CrouchTarget.y, 0);
        }
    }

    private void jumpSitUp() {
        CrouchTarget = new Vector3(0, highestCrouch - additionalHeight, 0);
        Spine.targetPosition = CrouchTarget;
    }

    //------Joint Control-----------------------------------------------------------------------------------
    private void spineContractionOnRealWorldCrouch() {
        CrouchTarget.y = Mathf.Clamp(CameraControllerPos.y - additionalHeight, lowestCrouch, highestCrouch - additionalHeight);
        Spine.targetPosition = new Vector3(0, CrouchTarget.y, 0);

    }

    private void moveAndRotateHand() {
        // RightHandJoint.targetPosition = RightHandControllerPos - CameraControllerPos + new Vector3(0, offset, 0);
        // LeftHandJoint.targetPosition = LeftHandControllerPos - CameraControllerPos + new Vector3(0, offset, 0);

        RightHandJoint.targetPosition = RightHandControllerPos - CameraControllerPos;
        LeftHandJoint.targetPosition = LeftHandControllerPos - CameraControllerPos;

        RightHandJoint.targetRotation = RightHandControllerRotation;
        LeftHandJoint.targetRotation = LeftHandControllerRotation;
    }
}
