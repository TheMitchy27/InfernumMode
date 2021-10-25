﻿using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Buffs.StatDebuffs;
using CalamityMod.Items.Placeables.Banners;
using CalamityMod.NPCs;
using CalamityMod.NPCs.HiveMind;
using CalamityMod.Projectiles.Boss;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

using GreatSandSharkNPC = CalamityMod.NPCs.GreatSandShark.GreatSandShark;

namespace InfernumMode.BehaviorOverrides.BossAIs.GreatSandShark
{
	public class GreatSandSharkBehaviorOverride : NPCBehaviorOverride
    {
        public enum GreatSandSharkAttackState
        {
            SwimSandRush
        }

        public override int NPCOverrideType => ModContent.NPCType<GreatSandSharkNPC>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCSetDefaults | NPCOverrideContext.NPCAI | NPCOverrideContext.NPCFindFrame | NPCOverrideContext.NPCPreDraw;

        public override void SetDefaults(NPC npc)
        {
            NPCID.Sets.TrailCacheLength[npc.type] = 8;
            NPCID.Sets.TrailingMode[npc.type] = 1;

            npc.boss = true;
            npc.noGravity = true;
            npc.noTileCollide = true;
            npc.npcSlots = 15f;
            npc.damage = 135;
            npc.width = 300;
            npc.height = 120;
            npc.defense = 20;
            npc.DR_NERD(0.25f);
            npc.LifeMaxNERB(84720, 84720);
            npc.aiStyle = -1;
            npc.modNPC.aiType = -1;
            npc.knockBackResist = 0f;
            npc.value = Item.buyPrice(0, 40, 0, 0);
            for (int k = 0; k < npc.buffImmune.Length; k++)
                npc.buffImmune[k] = true;

            npc.buffImmune[BuffID.Ichor] = false;
            npc.buffImmune[ModContent.BuffType<MarkedforDeath>()] = false;
            npc.buffImmune[BuffID.Frostburn] = false;
            npc.buffImmune[BuffID.CursedInferno] = false;
            npc.buffImmune[BuffID.Daybreak] = false;
            npc.buffImmune[BuffID.StardustMinionBleed] = false;
            npc.buffImmune[BuffID.DryadsWardDebuff] = false;
            npc.buffImmune[BuffID.Oiled] = false;
            npc.buffImmune[BuffID.BetsysCurse] = false;
            npc.buffImmune[ModContent.BuffType<AstralInfectionDebuff>()] = false;
            npc.buffImmune[ModContent.BuffType<GodSlayerInferno>()] = false;
            npc.buffImmune[ModContent.BuffType<AbyssalFlames>()] = false;
            npc.buffImmune[ModContent.BuffType<ArmorCrunch>()] = false;
            npc.buffImmune[ModContent.BuffType<DemonFlames>()] = false;
            npc.buffImmune[ModContent.BuffType<HolyFlames>()] = false;
            npc.buffImmune[ModContent.BuffType<Nightwither>()] = false;
            npc.buffImmune[ModContent.BuffType<Plague>()] = false;
            npc.buffImmune[ModContent.BuffType<Shred>()] = false;
            npc.buffImmune[ModContent.BuffType<WarCleave>()] = false;
            npc.buffImmune[ModContent.BuffType<WhisperingDeath>()] = false;
            npc.buffImmune[ModContent.BuffType<SilvaStun>()] = false;
            npc.behindTiles = true;
            npc.netAlways = true;
            npc.HitSound = SoundID.NPCHit1;
            npc.DeathSound = SoundID.NPCDeath1;
            npc.timeLeft = NPC.activeTime * 30;
            npc.modNPC.banner = npc.type;
            npc.modNPC.bannerItem = ModContent.ItemType<GreatSandSharkBanner>();
        }

        public override bool PreAI(NPC npc)
        {
            ref float attackState = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];

            npc.TargetClosest();

            Player target = Main.player[npc.target];
            bool pissedOff = !(Main.sandTiles > 1000 && target.Center.Y > (Main.worldSurface - 180f) * 16D);

            if ((!target.active || target.dead || !npc.WithinRange(target.Center, pissedOff ? 2250f : 4200f)) && npc.target != 255)
            {
                npc.velocity = Vector2.Lerp(npc.velocity, Vector2.UnitY * 18f, 0.05f);
                npc.rotation = npc.velocity.Y * npc.direction * 0.02f;
                if (!npc.WithinRange(target.Center, 1600f))
                {
                    npc.life = 0;
                    npc.active = false;
                    npc.netUpdate = true;
                }
                return false;
            }

            // Reset things.
            npc.defense = pissedOff ? 250 : npc.defDefense;
            npc.damage = npc.defDamage;
            npc.timeLeft = 3600;

            switch ((GreatSandSharkAttackState)(int)attackState)
            {
                case GreatSandSharkAttackState.SwimSandRush:
                    DoAttack_SwimSandRush(npc, target);
                    break;
            }

            return false;
        }

        public static void DoAttack_SwimSandRush(NPC npc, Player target)
        {
            bool inTiles = Collision.SolidCollision(npc.Center - Vector2.One * 36f, 72, 72);
            bool canCharge = inTiles && npc.WithinRange(target.Center, 750f);
            float swimAcceleration = 0.9f;
            float chargeSpeed = npc.Distance(target.Center) * 0.02f + 23f;

            ref float chargingFlag = ref npc.Infernum().ExtraAI[0];
            ref float chargeCountdown = ref npc.Infernum().ExtraAI[1];
            ref float chargeInterpolantTimer = ref npc.Infernum().ExtraAI[2];

            if (!canCharge && chargeCountdown <= 0f)
            {
                npc.direction = (target.Center.X > npc.Center.X).ToDirectionInt();
                npc.directionY = (target.Center.Y > npc.Center.Y).ToDirectionInt();

                // Swim towards the target quickly.
                if (inTiles)
                {
                    npc.velocity.X = MathHelper.Clamp(npc.velocity.X + npc.direction * swimAcceleration, -14f, 14f);
                    npc.velocity.Y = MathHelper.Clamp(npc.velocity.Y + npc.directionY * swimAcceleration, -10f, 10f);
                }
                else if (npc.velocity.Y < 15f)
                    npc.velocity.Y += npc.WithinRange(target.Center, 600f) || npc.Center.Y < target.Center.Y ? 0.5f : 0.3f;

                chargingFlag = 0f;
                chargeInterpolantTimer = 0f;
            }
            else
            {
                Vector2 chargeDirection = npc.SafeDirectionTo(target.Center, -Vector2.UnitY);

                // Charge at the target and release a bunch of sand on the first frame the shark leaves solid tiles.
                if (chargingFlag == 0f && npc.velocity.AngleBetween(chargeDirection) < MathHelper.Pi * 0.45f)
                {
                    chargeCountdown = 35f;
                    chargeInterpolantTimer = 1f;

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = 0; i < 36; i++)
                        {
                            Vector2 sandVelocity = (MathHelper.TwoPi * i / 36f).ToRotationVector2() * 8f;
                            int sand = Utilities.NewProjectileBetter(npc.Center + sandVelocity * 3f, sandVelocity, ModContent.ProjectileType<SandBlast>(), 155, 0f);
                            if (Main.projectile.IndexInRange(sand))
                                Main.projectile[sand].tileCollide = false;
                        }
                    }

                    npc.netUpdate = true;
                    chargingFlag = 1f;
                }
                else if (npc.velocity.Y < 15f)
                    npc.velocity.Y += 0.3f;

                if (chargeInterpolantTimer > 0f && chargeInterpolantTimer < 10f)
                {
                    npc.velocity = Vector2.Lerp(npc.velocity, chargeDirection * chargeSpeed, 0.2f);
                    chargeInterpolantTimer++;
                }

                chargeCountdown--;
            }

            // Define rotation and direction.
            npc.spriteDirection = (npc.velocity.X < 0f).ToDirectionInt();
            npc.rotation = MathHelper.Clamp(npc.velocity.Y * npc.spriteDirection * 0.1f, -0.15f, 0.15f);
        }

        public override void FindFrame(NPC npc, int frameHeight)
        {
            npc.frameCounter += 0.15f;
            npc.frameCounter %= Main.npcFrameCount[npc.type];
            npc.frame.Y = (int)npc.frameCounter * frameHeight;
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            SpriteEffects spriteEffects = SpriteEffects.None;
            if (npc.spriteDirection == 1)
                spriteEffects = SpriteEffects.FlipHorizontally;

            Texture2D texture = Main.npcTexture[npc.type];
            Color topLeftLight = Lighting.GetColor((int)npc.TopLeft.X / 16, (int)npc.TopLeft.Y / 16);
            Color topRightLight = Lighting.GetColor((int)npc.TopRight.X / 16, (int)npc.TopRight.Y / 16);
            Color bottomLeftLight = Lighting.GetColor((int)npc.BottomLeft.X / 16, (int)npc.BottomLeft.Y / 16);
            Color bottomRightLight = Lighting.GetColor((int)npc.BottomRight.X / 16, (int)npc.BottomRight.Y / 16);
            Vector4 averageLight = (topLeftLight.ToVector4() + topRightLight.ToVector4() + bottomLeftLight.ToVector4() + bottomRightLight.ToVector4()) * 0.5f;
            Color averageColor = new Color(averageLight);
            Vector2 origin = npc.frame.Size() * 0.5f;

            if (CalamityConfig.Instance.Afterimages)
            {
                for (int i = 1; i < 8; i += 2)
                {
                    Color afterimageColor = npc.GetAlpha(averageColor);
                    float afterimageFade = 8f - i;
                    afterimageColor *= afterimageFade / (NPCID.Sets.TrailCacheLength[npc.type] * 1.5f);
                    Vector2 afterimageDrawPosition = npc.oldPos[i] + npc.Size * 0.5f - Main.screenPosition + Vector2.UnitY * npc.gfxOffY;
                    spriteBatch.Draw(texture, afterimageDrawPosition, npc.frame, afterimageColor, npc.rotation, origin, npc.scale, spriteEffects, 0f);
                }
            }

            Vector2 drawPosition = npc.Center - Main.screenPosition + Vector2.UnitY * npc.gfxOffY;
            spriteBatch.Draw(texture, drawPosition, npc.frame, npc.GetAlpha(averageColor), npc.rotation, origin, npc.scale, spriteEffects, 0);
            return false;
        }
    }
}
