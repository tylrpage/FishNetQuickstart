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
    [SerializeField] private float gravityRate = 9.8f;
    
    private CharacterController _characterController;
    private Vector2 _moveInput;
    private bool _jump;

    private float _downVel;

    public struct MoveInputData
    {
        public Vector2 InputVector;
        public bool Jump;
    }

    public struct ReconcileData
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public float DownVel;

        public ReconcileData(Vector3 position, Quaternion rotation, float downVel)
        {
            Position = position;
            Rotation = rotation;
            DownVel = downVel;
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
        Vector3 move = new Vector3(md.InputVector.x, 0, md.InputVector.y) * speed;
        move.y = _downVel;
        if (_characterController.isGrounded)
        {
            if (md.Jump)
                _downVel = jumpSpeed;
        }

        _downVel -= gravityRate * (float)TimeManager.TickDelta;
        
        _characterController.Move(move * (float)TimeManager.TickDelta);
        
        Debug.Log($"Move, replaying: {replaying}");
    }

    [Reconcile]
    private void Reconciliation(ReconcileData rd, bool asServer)
    {
        transform.position = rd.Position;
        transform.rotation = rd.Rotation;
        _downVel = rd.DownVel;
    }

    private void GetInputData(out MoveInputData moveInputData)
    {
        moveInputData = new MoveInputData()
        {
            Jump = _jump,
            InputVector = _moveInput
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
            ReconcileData rd = new ReconcileData(transform.position, transform.rotation, _downVel);
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
