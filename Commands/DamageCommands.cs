using GameNetcodeStuff;
using System;
using UnityEngine;

namespace SimpleCommands.Commands
{
    public abstract class BaseDamageCommand : SimpleCommand
    {
        public BaseDamageCommand(string name, string description) : base(name, description) 
        {
            instructions.Add("[/cmd]");
            instructions.Add("[/cmd] [value]");
            instructions.Add("[/cmd] [value] [damageType]");
            instructions.Add("[/cmd] [target]");
            instructions.Add("[/cmd] [target] [value]");
            instructions.Add("[/cmd] [target] [value] [damageType]");
        }

        public static CauseOfDeath GetDamageType(string name)
        {
            foreach (CauseOfDeath cause in Enum.GetValues(typeof(CauseOfDeath)))
            {
                if (cause.ToString().ToLower() == name.ToLower())
                {
                    return cause;
                }
            }
            return CauseOfDeath.Unknown;
        }

        public override string Execute(PlayerControllerB sender, CommandParameters parameters, out bool success)
        {
            PlayerControllerB effected = sender;
            int amount = DefaultValue();
            CauseOfDeath cause = CauseOfDeath.Unknown;

            if (!parameters.IsEmpty())
            {
                string name = parameters.GetStringAt(0);
                PlayerControllerB? player = GetPlayer(name);

                bool startedWithPlayer = false;

                if (player != null)
                {
                    effected = player;
                    startedWithPlayer = true;
                }
                else
                {
                    int value = parameters.GetNumberAt(0, out bool isNumber);

                    if (isNumber)
                    {
                        amount = value;
                    }
                    else
                    {
                        success = false;
                        return UnknownNumberException();
                    }
                }

                if (parameters.Count() > 1)
                {
                    if (startedWithPlayer)
                    {
                        int num = parameters.GetNumberAt(1, out bool isNumber);
                        if (isNumber)
                        {
                            amount = num;
                        }
                        else
                        {
                            success = false;
                            return UnknownNumberException();
                        }
                    }
                    else
                    {
                        cause = GetDamageType(parameters.GetStringAt(1));
                    }
                }

                if (startedWithPlayer && parameters.Count() > 2)
                {
                    cause = GetDamageType(parameters.GetStringAt(2));
                }
            }

            success = true;
            return ApplyDamage(amount, effected, cause);
        }

        public abstract int DefaultValue();

        public abstract string ApplyDamage(int amount, PlayerControllerB effected, CauseOfDeath causeOfDeath);
    }

    public class HealCommand : BaseDamageCommand
    {
        public HealCommand() : base("heal", "heals players") { }

        public override string ApplyDamage(int amount, PlayerControllerB effected, CauseOfDeath causeOfDeath)
        {
            effected.DamagePlayer(-amount, causeOfDeath: causeOfDeath);
            effected.health = Mathf.Clamp(amount, 0, 100);

            return $"Healed {effected.playerUsername} to {(effected.health == 100 ? "max" : $"effected.health")} HP.";
        }

        public override int DefaultValue()
        {
            return 100;
        }
    }

    public class DamageCommand : BaseDamageCommand
    {
        public DamageCommand() : base("damage", "hurts players") { }

        public override string ApplyDamage(int amount, PlayerControllerB effected, CauseOfDeath causeOfDeath)
        {
            effected.DamagePlayer(amount, causeOfDeath: causeOfDeath);

            return $"Delt {amount} damage to {effected.playerUsername}.";
        }

        public override int DefaultValue()
        {
            return 10;
        }
    }

    public class ExplodeCommand : BaseDamageCommand
    {
        public ExplodeCommand() : base("explode", "explodes players") { }

        public override string ApplyDamage(int amount, PlayerControllerB effected, CauseOfDeath causeOfDeath)
        {
            Landmine.SpawnExplosion(effected.transform.position, true, 0, 8, amount, amount * 0.75f);

            return $"Exploded {effected.playerUsername}. {amount} dmg.";
        }

        public override int DefaultValue()
        {
            return 69;
        }
    }
}
