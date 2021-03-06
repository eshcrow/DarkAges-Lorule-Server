﻿using Darkages.Network.ServerFormats;
using Darkages.Scripting;
using Darkages.Storage.locales.debuffs;
using Darkages.Types;

namespace Darkages.Assets.locales.Scripts.Traps
{
    [Script("Poison Trap")]
    public class PoisonTrap : SpellScript
    {
        public PoisonTrap(Spell spell) : base(spell)
        {

        }

        public override void OnFailed(Sprite sprite, Sprite target)
        {

        }

        public override void OnSuccess(Sprite sprite, Sprite target)
        {

        }

        public override void OnUse(Sprite sprite, Sprite target)
        {
            Trap.Set(sprite, 300, 0, OnTriggeredBy);

            if (sprite is Aisling)
            {
                (sprite as Aisling).Client.SendMessage(0x02, string.Format("You Cast {0}", Spell.Template.Name));
            }
        }


        public override void OnActivated(Sprite sprite)
        {

        }

        public override void OnSelectionToggle(Sprite sprite)
        {

        }

        public override void OnTriggeredBy(Sprite sprite, Sprite target)
        {
            var debuff = new debuff_poison("Poison Trap", 5000, 35, 25, 0.35, true);

            if (!target.HasBuff(debuff.Name))
            {
                debuff.OnApplied(target, debuff);
                {
                    if (target is Monster || target is Mundane || target is Aisling)
                        target.Show(Scope.NearbyAislings,
                            new ServerFormat29((uint)target.Serial, (uint)target.Serial,
                                Spell.Template.TargetAnimation, 0, 100));

                };
            }
        }
    }
}