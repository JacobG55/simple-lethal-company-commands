using GameNetcodeStuff;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SimpleCommands.Components
{
    public class PlayerModification : MonoBehaviour
    {
        public PlayerControllerB player;

        public bool infinateSprint = false;
        public bool invulnerable = false;

        public bool enableFlying = false;
        private bool flying = false;
        private float lastJumpPress = 0;
        public Vector2 flightSpeed = new Vector2(2.6f, 8f);
        private float oldMovementSpeed = 0;

        private InputAction jumpAction;
        private InputAction crouchAction;

        public float defaultSpeed;

        public void Start()
        {
            player = gameObject.GetComponent<PlayerControllerB>();

            InputActionAsset actions = IngamePlayerSettings.Instance.playerInput.actions;
            jumpAction = actions.FindAction("Jump");
            crouchAction = actions.FindAction("Crouch");
            defaultSpeed = player.movementSpeed;
        }


        public void Update()
        {
            if (infinateSprint)
            {
                player.sprintMeter = 1f;
            }

            bool canInput = !player.quickMenuManager.isMenuOpen && player.IsOwner && player.isPlayerControlled && !player.inSpecialInteractAnimation && !player.isTypingChat;

            if (enableFlying)
            {
                if (canInput && jumpAction.WasPressedThisFrame())
                {
                    if (Time.realtimeSinceStartup - lastJumpPress < 0.4f) SetFlying(!flying);
                    lastJumpPress = Time.realtimeSinceStartup;
                }
                if (flying)
                {
                    float y = 0f;
                    if (canInput)
                    {
                        if (jumpAction.IsPressed()) y = flightSpeed.y;
                        else if (crouchAction.IsPressed()) y = flightSpeed.y * -1.4f;
                    }
                    player.fallValueUncapped = player.fallValue = y * (player.isSprinting ? 1.6f : 1f);
                    if (player.thisController.isGrounded) SetFlying(false);
                    player.takingFallDamage = false;
                }
            }
        }

        public void SetFlying(bool value)
        {
            if (value && player.thisController.isGrounded) return;
            flying = value;
            if (value)
            {
                oldMovementSpeed = player.movementSpeed;
                player.movementSpeed *= flightSpeed.x;
            }
            else
            {
                player.movementSpeed = oldMovementSpeed;
            }
        }
    }
}
