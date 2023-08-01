using BepInEx;
using RWCustom;
using SlugBase.Features;
using System;
using System.Collections.Generic;
using UnityEngine;
using static SlugBase.Features.FeatureTypes;
using static TheEscort.Eshelp;

namespace TheEscort
{
    partial class Plugin : BaseUnityPlugin
    {
        // New Escapist tweak values
        // public static readonly PlayerFeature<> escapist = Player("theescort/newescapist/");
        // public static readonly PlayerFeature<float> escapist = PlayerFloat("theescort/newescapist/");
        // public static readonly PlayerFeature<float[]> escapist = PlayerFloats("theescort/newescapist/");


        public void Esclass_NE_Tick(Player self, ref Escort e)
        {
            if (e.NEsResetCooldown)
            {
                e.NEsClearVulnerable = 0;
                e.NEsCooldown = 0;
                e.NEsSetCooldown = 0;
                e.NEsAbility = 0;
                e.NEsShadowPlayer.GoAwayShadow();
                e.NEsResetCooldown = false;
            }

            if (e.NEsShadowPlayer is null)
            {
                e.NEsAbility = 0;
            }

            if (e.NEsCooldown > 0)
            {
                e.NEsCooldown--;
            }

            if (e.NEsLastInput.x > 0)
            {
                e.NEsLastInput.x--;
            }
            else if (e.NEsLastInput.x < 0)
            {
                e.NEsLastInput.x++;
            }
            if (e.NEsLastInput.y > 0)
            {
                e.NEsLastInput.y--;
            }
            if (e.NEsLastInput.y < 0)
            {
                e.NEsLastInput.y++;
            }
        
            if (e.NEsAbility > 0)
            {
                e.NEsAbility--;
            }
            else if (e.NEsSetCooldown > 0)
            {
                e.NEsCooldown = e.NEsSetCooldown;
                e.NEsSetCooldown = 0;
            }

            if (e.NEsClearVulnerable > 0)
            {
                e.NEsClearVulnerable--;
            }
            else if (e.NEsVulnerable.Count > 0)
            {
                e.NEsVulnerable.Clear();
            }

            if (e.NEsAbility > 0 || e.NEsCooldown > 0)
            {
                self.Blink(5);
            }
        }

        public void Esclass_NE_AbsoluteTick(Player self, ref Escort e)
        {
            throw new NotImplementedException();
        }

        private void Esclass_NE_Update(Player self, ref Escort e)
        {
            // Check if player has inputted the direction for ability activation
            if (e.NEsAbility == 0 && e.NEsCooldown == 0)  // Doubletap direction checker
            {
                if (e.CustomKeybindEnabled)
                {
                    if (Input.GetKey(e.CustomKeybind))
                    {
                        if (self.animation == Player.AnimationIndex.Flip && self.input[0].y != 0)
                        {
                            e.NEsAbility = Escort.NEsAbilityTime;
                            Esclass_NE_CreateShadow(self, ref e, true);
                        }
                        if (self.input[0].x != 0)
                        {
                            e.NEsAbility = Escort.NEsAbilityTime;
                            Esclass_NE_CreateShadow(self, ref e);
                        }
                    }
                }
                else
                {
                    // Left
                    if (self.input[0].x < 0 && self.input[1].x >= 0)
                    {
                        if (e.NEsLastInput.x < 0)
                        {
                            e.NEsLastInput.x = 0;
                            e.NEsAbility = Escort.NEsAbilityTime;
                            Esclass_NE_CreateShadow(self, ref e);
                        }
                        else
                        {
                            e.NEsLastInput.x = -15;
                        }
                    }

                    // Right
                    if (self.input[0].x > 0 && self.input[1].x <= 0)
                    {
                        if (e.NEsLastInput.x > 0)
                        {
                            e.NEsLastInput.x = 0;
                            e.NEsAbility = Escort.NEsAbilityTime;
                            Esclass_NE_CreateShadow(self, ref e);
                        }
                        else
                        {
                            e.NEsLastInput.x = 15;
                        }
                    }

                    // Up (May not end up implementing)
                    if (self.input[0].y > 0 && self.input[1].y <= 0)
                    {
                        if (e.NEsLastInput.y > 0 && self.animation == Player.AnimationIndex.Flip)
                        {
                            e.NEsLastInput.y = 0;
                            e.NEsAbility = Escort.NEsAbilityTime;
                            Esclass_NE_CreateShadow(self, ref e, true);
                        }
                        else
                        {
                            e.NEsLastInput.y = 15;
                        }
                    }

                    // Down (May not end up implementing)
                    if (self.input[0].y < 0 && self.input[1].y >= 0)
                    {
                        if (e.NEsLastInput.y < 0 && self.animation == Player.AnimationIndex.Flip)
                        {
                            e.NEsLastInput.y = 0;
                            e.NEsAbility = Escort.NEsAbilityTime;
                            Esclass_NE_CreateShadow(self, ref e, true);
                        }
                        else
                        {
                            e.NEsLastInput.y = -15;
                        }
                    }
                }
            }
        }



        private static void Esclass_NE_CreateShadow(Player self, ref Escort e, bool vertical = false)
        {
            if (self.room is null || e.NEsShelterCloseTime)  // Prevents a new one from being spawned in a shelter to not chance an accidental save
            {
                e.NEsAbility = 0;
                e.NEsSetCooldown = 0;
                return;
            }

            if (self.room.abstractRoom.gate)  // Prevents a new one from being spawned in a gate and causing a softcrash
            {
                e.NEsAbility = 0;
                e.NEsSetCooldown = 0;
                return;
            }

            // Realize shadow creature
            e.NEsAbstractShadowPlayer?.Destroy();
            e.NEsAbstractShadowPlayer = new AbstractCreature(self.abstractCreature.world, StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.Slugcat), null, self.coord, self.room.game.GetNewID())
            {
                saveCreature = false
            };
            e.NEsAbstractShadowPlayer.state = new PlayerState(e.NEsAbstractShadowPlayer, self.playerState.playerNumber, EscortMe, false)
            {
                meatLeft = 0
            };
            //e.NEsAbstractShadowPlayer.RealizeInRoom();
            e.NEsAbstractShadowPlayer.realizedCreature = e.NEsShadowPlayer = new ShadowPlayer(e.NEsAbstractShadowPlayer, self.abstractCreature.world, self);
            self.room.AddObject(e.NEsShadowPlayer);

            // Dash
            float dashDistance = 0;
            try
            {
                // Check for wall (max dash distance) so you don't go through it
                for (int i = 1; i < 15; i++)
                {
                    for (int j = 0; j < self.bodyChunks.Length; j++)
                    {
                        float xComparitor = self.bodyChunks[j].pos.x;
                        float yComparitor = self.bodyChunks[j].pos.y;
                        if (vertical)
                        {
                            yComparitor += 20 * i * self.input[0].y;
                        }
                        else
                        {
                            xComparitor += 20 * i * self.input[0].x;
                        }
                        Room.Tile rt = self.room.GetTile(self.room.GetTilePosition(new(xComparitor, yComparitor)));
                        if (rt.Solid) // hit something
                        {
                            Ebug(self, $"Wall detected at: {dashDistance}", 2, true);
                            goto breakAll;
                        }
                    }
                    dashDistance += 20;
                }
                breakAll:
                Ebug(self, "Wall check all done!", 2, true);

            }
            catch (Exception ex)
            {
                Ebug(ex, "Wallfinder didn't work!");
            }


            // Check for creature (if they're between your current position and your dash target position)
            bool creaturePass = false;
            bool emptyHanded = self.FreeHand() != -1;
            Vector2 a1, a2, b1, b2;
            float y1, y2;
            if (vertical)
            {
                if (self.bodyChunks[0].pos.x > self.bodyChunks[0].pos.x)
                {
                    y1 = self.bodyChunks[0].pos.x + 30;
                    y2 = self.bodyChunks[1].pos.x - 30;
                }
                else
                {
                    y1 = self.bodyChunks[1].pos.x + 30;
                    y2 = self.bodyChunks[0].pos.x - 30;
                }
                b1 = new(self.bodyChunks[0].pos.x, self.bodyChunks[0].pos.y + self.input[0].y * dashDistance);
                b2 = new(self.bodyChunks[1].pos.x, self.bodyChunks[1].pos.y + self.input[0].y * dashDistance);
            }
            else
            {
                if (self.bodyChunks[0].pos.y > self.bodyChunks[0].pos.y)
                {
                    y1 = self.bodyChunks[0].pos.y + 30;
                    y2 = self.bodyChunks[1].pos.y - 50;
                }
                else
                {
                    y1 = self.bodyChunks[1].pos.y + 30;
                    y2 = self.bodyChunks[0].pos.y - 50;
                }
                b1 = new(self.bodyChunks[0].pos.x + self.input[0].x * dashDistance, self.bodyChunks[0].pos.y);
                b2 = new(self.bodyChunks[1].pos.x + self.input[0].x * dashDistance, self.bodyChunks[1].pos.y);
            }
            a1 = self.bodyChunks[0].pos;
            a2 = self.bodyChunks[1].pos;
            try
            {
                Stack<Weapon> weaponList = new();
                foreach (UpdatableAndDeletable thing in self.room.updateList)
                {
                    if (
                        thing is Creature creature and not ShadowPlayer &&
                        creature.abstractCreature.creatureTemplate.type != MoreSlugcats.MoreSlugcatsEnums.CreatureTemplateType.SlugNPC && 
                        creature != self && 
                        !(
                            creature is Player && 
                            ModManager.CoopAvailable && 
                            !Custom.rainWorld.options.friendlyFire
                        ) && 
                        !creature.dead && 
                        Esclass_NE_BodyChecker(creature, a1, a2, b1, b2) &&
                        (
                            vertical? 
                                (creature.firstChunk.pos.x > y2 && creature.firstChunk.pos.x < y1) : 
                                (creature.firstChunk.pos.y > y2 && creature.firstChunk.pos.y < y1)
                        )
                    )
                    {
                        creaturePass = true;  // Reduce dash range to max 200 if you pass a creature or will pass creature
                        if (Custom.Dist(self.firstChunk.pos, creature.firstChunk.pos) < 200)
                        {
                            // Add creature to the vulnerable list if between current position and future position
                            e.NEsVulnerable.Add(creature);
                            e.NEsClearVulnerable = Escort.NEsAbilityTime;
                            creature.Stun((int)(80 * creature.Template.baseStunResistance));
                            Ebug("There's a creature between!", 2, true);
                        }
                    }
                    else if (
                        emptyHanded && 
                        thing is Weapon weapon && 
                        Custom.BetweenLines(weapon.firstChunk.pos, a1, a2, b1, b2) && 
                        (
                            vertical? 
                                (weapon.firstChunk.pos.x > y2 && weapon.firstChunk.pos.x < y1) : 
                                (weapon.firstChunk.pos.y > y2 && weapon.firstChunk.pos.y < y1)
                        )
                    )  // Pick up weapon if empty handed.
                    {
                        weaponList.Push(weapon);
                        Ebug("Yoink a weapon while you're at it", 2, true);
                    }
                }
                while (weaponList.Count > 0 && self.FreeHand() != -1)
                {
                    self.SlugcatGrab(weaponList.Pop(), self.FreeHand());
                }
            }
            catch (Exception err)
            {
                Ebug(err, "Creaturefinder didn't work!");
            }

            if (dashDistance > 200 && creaturePass)
            {
                dashDistance = 200;
            }
            self.bodyChunks[0].pos.x += self.input[0].x * dashDistance;
            self.bodyChunks[1].pos.x += self.input[0].x * dashDistance;
        }


        private static bool Esclass_NE_BodyChecker(Creature creature, Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2)
        {
            bool insideQuad = false;
            for (int i = 0; i < creature.bodyChunks.Length && !insideQuad; i++)
            {
                insideQuad = Custom.BetweenLines(creature.bodyChunks[i].pos, a1, a2, b1, b2);
            }
            return insideQuad;
        }


        private void Esclass_NE_CheckKiller(On.Creature.orig_SetKillTag orig, Creature self, AbstractCreature killer)
        {
            try
            {
                if (self is ShadowPlayer)
                {
                    Ebug("Ignoring shadowscort", 1, true);
                    return;
                }
            }
            catch (Exception err)
            {
                Ebug(err, "Couldn't bypass killtag setter");
            }
            orig(self, killer);
        }

        private bool Esclass_NE_HitShadowscort(On.Weapon.orig_HitThisObject orig, Weapon self, PhysicalObject obj)
        {
            if (obj is ShadowPlayer)
            {
                return true;
            }
            return orig(self, obj);
        }

    }
}
