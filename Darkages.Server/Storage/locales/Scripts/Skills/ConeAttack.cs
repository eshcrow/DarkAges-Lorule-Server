﻿using Darkages.Network.ServerFormats;
using Darkages.Types;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Darkages.Scripting.Scripts.Skills
{
    [Script("Cone Attack", "Dean")]
    public class ConeAttack : SkillScript
    {
        private readonly Random rand = new Random();
        public Skill _skill;

        public Sprite Target;

        public ConeAttack(Skill skill) : base(skill)
        {
            _skill = skill;
        }

        public override void OnFailed(Sprite sprite)
        {
            if (sprite is Aisling)
            {
                var client = (sprite as Aisling).Client;

                client.SendMessage(0x02,
                    string.IsNullOrEmpty(Skill.Template.FailMessage) ? Skill.Template.FailMessage : "failed.");
            }
        }

        public List<Sprite> GetInCone(Sprite sprite, int distance)
        {
            var result = new List<Sprite>();
            var objects = GetObjects(i => i.WithinRangeOf(sprite, distance), Get.Aislings | Get.Monsters | Get.Mundanes);
            foreach (var obj in objects)
            {
                if (sprite.Position.DistanceSquared(obj.Position) <= distance)
                {
                    if ((Direction)sprite.Direction == Direction.North)
                    {
                        if (obj.Y <= sprite.Y)
                            result.Add(obj);
                    }
                    else if ((Direction)sprite.Direction == Direction.South)
                    {
                        if (obj.Y >= sprite.Y)
                            result.Add(obj);
                    }
                    else if ((Direction)sprite.Direction == Direction.East)
                    {
                        if (obj.X >= sprite.X)
                            result.Add(obj);
                    }
                    else if ((Direction)sprite.Direction == Direction.West)
                    {
                        if (obj.X <= sprite.X)
                            result.Add(obj);
                    }

                }
            }

            return result;
        }

        public override void OnSuccess(Sprite sprite)
        {
            if (sprite is Aisling)
            {
                var client = (sprite as Aisling).Client;

                var action = new ServerFormat1A
                {
                    Serial = client.Aisling.Serial,
                    Number = 0x81,
                    Speed = 20
                };

                var enemy = GetInCone(sprite, 3);

                if (enemy != null)
                {
                    foreach (var i in enemy.OfType<Monster>())
                    {
                        if (i == null)
                            continue;



                        if (client.Aisling.Serial == i.Serial)
                            continue;


                        var dmg = client.Aisling.Invisible ? 2 : 1 * client.Aisling.Str * 10 * Skill.Level;
                        i.ApplyDamage(sprite, dmg, false, Skill.Template.Sound);
                        i.Target = client.Aisling;

                        client.Aisling.Show(Scope.NearbyAislings,
                            new ServerFormat29((uint)client.Aisling.Serial, (uint)i.Serial,
                                Skill.Template.TargetAnimation, 0, 100));
                    }

                    client.Aisling.Show(Scope.NearbyAislings, action);
                }
            }
        }

        public override void OnUse(Sprite sprite)
        {
            if (sprite is Aisling)
            {
                var client = (sprite as Aisling).Client;

                if (Skill.Ready)
                {
                    if (client.Aisling.Invisible && Skill.Template.PostQualifers.HasFlag(PostQualifer.BreakInvisible))
                    {
                        client.Aisling.Flags = AislingFlags.Normal;
                        client.Refresh();
                    }

                    client.TrainSkill(Skill);

                    var success = true;

                    if (success)
                        OnSuccess(sprite);
                    else
                        OnFailed(sprite);
                }
            }
            else
            {
                var target = sprite.Target;
                if (target == null)
                    return;

                if (target is Aisling)
                {
                    (target as Aisling).Client.Aisling.Show(Scope.NearbyAislings,
                        new ServerFormat29((uint)target.Serial, (uint)target.Serial,
                            Skill.Template.TargetAnimation, 0, 100));

                    var dmg = 1 * sprite.Str * 20 * Skill.Level;
                    target.ApplyDamage(sprite, dmg, true, Skill.Template.Sound);

                    var action = new ServerFormat1A
                    {
                        Serial = sprite.Serial,
                        Number = 0x82,
                        Speed = 20
                    };

                    if (sprite is Monster)
                    {
                        (target as Aisling).Client.SendStats(StatusFlags.All);
                        (target as Aisling).Client.Aisling.Show(Scope.NearbyAislings, action);
                    }
                }
            }
        }
    }
}