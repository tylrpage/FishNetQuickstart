using System.Collections;
using System.Collections.Generic;
using FishNet.Object;
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

    void Start()
    {
        _characterController = GetComponent<CharacterController>();
    }
    
    void Update()
    {
        if (!IsOwner)
            return;

        if (_characterController.isGrounded)
        {
            _moveDirection = new Vector3(_moveInput.x, 0, _moveInput.y) * speed;

            if (_jump)
            {
                _moveDirection.y = jumpSpeed;
                _jump = false;
            }
        }

        _moveDirection.y -= gravity * Time.deltaTime;
        _characterController.Move(_moveDirection * Time.deltaTime);
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
