﻿using Darkages.Network.ServerFormats;
using Darkages.Types;
using System;
using System.Linq;

namespace Darkages.Scripting.Scripts.Skills
{
    [Script("Wallop", "Dean")]
    public class Wallop : SkillScript
    {
        public Skill _skill;
        public Sprite Target;

        public Wallop(Skill skill) : base(skill)
        {
            _skill = skill;
        }

        public override void OnFailed(Sprite sprite)
        {
            if (Target != null)
                if (sprite is Aisling)
                {
                    var client = (sprite as Aisling).Client;
                    client.Aisling.Show(Scope.NearbyAislings,
                        new ServerFormat29(Skill.Template.MissAnimation, (ushort)Target.X, (ushort)Target.Y));
                }
        }

        public override void OnSuccess(Sprite sprite)
        {
            if (sprite is Aisling)
            {
                var client = (sprite as Aisling).Client;

                var action = new ServerFormat1A
                {
                    Serial = client.Aisling.Serial,
                    Number = (byte)(client.Aisling.UsingTwoHanded ? 0x81 : 0x01),
                    Speed = 25
                };


                var enemy = client.Aisling.GetInfront();

                if (enemy != null)
                {
                    client.Aisling.Show(Scope.VeryNearbyAislings, new ServerFormat13(0, 0, Skill.Template.Sound));

                    foreach (var i in enemy)
                    {
                        if (i == null)
                            continue;

                        if (client.Aisling.Serial == i.Serial)
                            continue;
                        if (i is Money)
                            continue;

                        if (!i.Attackable)
                            continue;

                        Target = i;


                        var imp = (Skill.Level * 5 / 100);
                        var dmg = (client.Aisling.Str + client.Aisling.Dex * (imp * 2));

                        i.ApplyDamage(sprite, dmg, false, Skill.Template.Sound);

                        if (i is Aisling)
                        {
                            (i as Aisling).Client.Aisling.Show(Scope.NearbyAislings,
                                new ServerFormat29((uint)client.Aisling.Serial, (uint)i.Serial, byte.MinValue,
                                    Skill.Template.TargetAnimation, 100));
                            (i as Aisling).Client.Send(new ServerFormat08(i as Aisling, StatusFlags.All));
                        }

                        if (i is Monster || i is Mundane || i is Aisling)
                            client.Aisling.Show(Scope.NearbyAislings,
                                new ServerFormat29((uint)client.Aisling.Serial, (uint)i.Serial,
                                    Skill.Template.TargetAnimation, 0, 100));
                    }
                }

                client.Aisling.Show(Scope.NearbyAislings, action);
            }
        }

        public override void OnUse(Sprite sprite)
        {
            if (sprite is Aisling)
            {
                var client = (sprite as Aisling).Client;

                if (Skill.Ready)
                {
                    if (client.Aisling.Invisible)
                    {
                        client.Aisling.Flags = AislingFlags.Normal;
                        client.Refresh();
                    }
                    client.TrainSkill(Skill);
                    OnSuccess(sprite);
                }
            }
        }
    }
}