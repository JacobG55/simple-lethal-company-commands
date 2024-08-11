using GameNetcodeStuff;
using UnityEngine;

namespace SimpleCommands.Components
{
    internal class PlayerModification : MonoBehaviour
    {
        private PlayerControllerB player;

        public bool infinateSprint = false;
        public bool invulnerable = false;

        public void Start()
        {
            if (gameObject.TryGetComponent<PlayerControllerB>(out PlayerControllerB player))
            {
                this.player = player;
            }
        }

        public void Update()
        {
            if (player == null) return;

            ApplyUpdate();
        }

        private void ApplyUpdate()
        {
            if (infinateSprint)
            {
                player.sprintMeter = 1f;
            }
            if (invulnerable)
            {
                player.health++;
            }
        }
    }
}
