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
            player = gameObject.GetComponent<PlayerControllerB>();
        }

        public void Update()
        {
            ApplyUpdate();
        }

        private void ApplyUpdate()
        {
            if (infinateSprint)
            {
                player.sprintMeter = 1f;
            }
        }
    }
}
