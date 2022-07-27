using System;
using System.Collections;
using System.Collections.Generic;
using FishNet;
using FishNet.Object;
using FishNet.Object.Prediction;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerInputDriver : NetworkBehaviour
{
    [SerializeField] private float jumpSpeed = 6f;
    [SerializeField] private float speed = 8f;
    [SerializeField] private float gravity = 9.8f;
    
    private CharacterController _characterController;
    private Vector2 _moveInput;
    private Vector3 _moveDirection;
    private bool _jump;

    public struct MoveInputData
    {
        public Vector2 moveVector;
        public bool jump;
        public bool grounded;
    }

    public struct ReconcileData
    {
        public Vector3 Position;
        public Quaternion Rotation;

        public ReconcileData(Vector3 position, Quaternion rotation)
        {
            Position = position;
            Rotation = rotation;
        }
    }

    void Start()
    {
        _characterController = GetComponent<CharacterController>();
        InstanceFinder.TimeManager.OnTick += TimeManagerOnTick;
    }

    private void OnDestroy()
    {
        if (InstanceFinder.TimeManager != null)
            InstanceFinder.TimeManager.OnTick -= TimeManagerOnTick;
    }
    
    [Replicate]
    private void Move(MoveInputData md, bool asServer, bool replaying = false)
    {
        Vector3 move = new Vector3();
        if (md.grounded)
        {
            move.x = md.moveVector.x * speed;
            move.y = 0;
            move.z = md.moveVector.y * speed;
            if (md.jump)
                move.y = jumpSpeed;
        }
        else
        {
            move.x = md.moveVector.x;
            move.z = md.moveVector.y;
        }

        move.y -= gravity * (float)TimeManager.TickDelta;
        _characterController.Move(move * (float)TimeManager.TickDelta);
    }

    [Reconcile]
    private void Reconciliation(ReconcileData rd, bool asServer)
    {
        transform.position = rd.Position;
        transform.rotation = rd.Rotation;
    }

    private void GetInputData(out MoveInputData moveInputData)
    {
        moveInputData = new MoveInputData()
        {
            jump = _jump,
            grounded = _characterController.isGrounded,
            moveVector = _moveInput
        };
    }

    private void TimeManagerOnTick()
    {
        if (IsOwner)
        {
            Reconciliation(default, false);
            GetInputData(out MoveInputData md);
            Move(md, false);
        }

        if (IsServer)
        {
            Move(default, true);
            ReconcileData rd = new ReconcileData(transform.position, transform.rotation);
            Reconciliation(rd, true);
        }
    }

    public void OnMovement(InputAction.CallbackContext context)
    {
        if (!IsOwner)
            return;
        
        _moveInput = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (!IsOwner)
            return;

        if (context.started || context.performed)
            _jump = true;
        else if (context.canceled)
            _jump = false;
    }
}
