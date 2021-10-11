﻿using CalamityMod;
using CalamityMod.NPCs.DevourerofGods;
using CalamityMod.World;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Reflection;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.DoG
{
	public class DoGPhase1BodyBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<DevourerofGodsBody>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCPreDraw;

        public static void DoGSegmentAI(NPC npc)
        {
            NPC aheadSegment = Main.npc[(int)npc.ai[1]];
            NPC head = Main.npc[(int)npc.ai[2]];
            npc.life = head.life;
            if (!head.active)
            {
                npc.life = 0;
                npc.HitEffect();
                npc.active = false;
                npc.netUpdate = true;
            }

            // Inherit various attributes from the head segment.
            // This code will go upstream across every segment, until it reaches the head.
            npc.scale = aheadSegment.scale;

            if (head.type == ModContent.NPCType<DevourerofGodsHeadS>() && head.Infernum().ExtraAI[1] == 0f && head.Infernum().ExtraAI[3] >= 1200f)
            {
                if (npc.Hitbox.Intersects(Main.projectile[(int)head.Infernum().ExtraAI[18]].Hitbox))
                {
                    npc.alpha += 70;
                    if (npc.alpha > 255)
                        npc.alpha = 255;
                }
            }

            else if (head.type == ModContent.NPCType<DevourerofGodsHeadS>() && head.Infernum().ExtraAI[13] > 381f)
            {
                if (npc.Hitbox.Intersects(Main.projectile[(int)head.Infernum().ExtraAI[14]].Hitbox))
                {
                    npc.alpha += 70;
                    if (npc.alpha > 255)
                        npc.alpha = 255;
                }
            }
            else if (head.type == ModContent.NPCType<DevourerofGodsHead>() && head.Infernum().ExtraAI[11] > 0f)
            {
                if (npc.Hitbox.Intersects(Main.projectile[(int)head.Infernum().ExtraAI[11]].Hitbox))
                {
                    npc.alpha += 140;
                    if (npc.alpha > 255)
                    {
                        npc.alpha = 255;

                        int headType = ModContent.NPCType<DevourerofGodsHead>();
                        int bodyType = ModContent.NPCType<DevourerofGodsBody>();
                        int tailType = ModContent.NPCType<DevourerofGodsTail>();
                        if (npc.type == tailType)
                        {
                            for (int i = 0; i < Main.maxNPCs; i++)
                            {
                                if (Main.npc[i].active && (Main.npc[i].type == headType || Main.npc[i].type == bodyType || Main.npc[i].type == tailType))
                                {
                                    Main.npc[i].active = false;
                                    Main.npc[i].netUpdate = true;
                                }
                            }
                        }

                        CalamityWorld.DoGSecondStageCountdown = 305;

                        if (Main.netMode == NetmodeID.Server)
                        {
                            var netMessage = InfernumMode.CalamityMod.GetPacket();
                            netMessage.Write((byte)CalamityModMessageType.DoGCountdownSync);
                            netMessage.Write(CalamityWorld.DoGSecondStageCountdown);
                            netMessage.Send();
                        }
                    }
                }
            }
            else
                npc.Opacity = aheadSegment.Opacity;

            if (npc.type == ModContent.NPCType<DevourerofGodsBodyS>())
                typeof(DevourerofGodsBodyS).GetField("invinceTime", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(npc.modNPC, 0);

            Vector2 size = npc.Size;
            if (npc.type == ModContent.NPCType<DevourerofGodsBody>())
                size = new Vector2(102f);
            if (npc.type == ModContent.NPCType<DevourerofGodsTail>())
                size = new Vector2(82f, 110f);

            if (npc.Size != size)
                npc.Size = size;

            npc.dontTakeDamage = head.dontTakeDamage;
            npc.damage = npc.dontTakeDamage ? 0 : npc.defDamage;
            if (head.Infernum().ExtraAI[20] > 0f)
                npc.life = npc.lifeMax;

            Vector2 directionToNextSegment = aheadSegment.Center - npc.Center;
            if (aheadSegment.rotation != npc.rotation)
                directionToNextSegment = directionToNextSegment.RotatedBy(MathHelper.WrapAngle(aheadSegment.rotation - npc.rotation) * 0.08f);

            npc.rotation = directionToNextSegment.ToRotation() + MathHelper.PiOver2;
            npc.Center = aheadSegment.Center - directionToNextSegment.SafeNormalize(Vector2.Zero) * npc.width * npc.scale;
            npc.spriteDirection = (directionToNextSegment.X > 0).ToDirectionInt();
        }

        public override bool PreAI(NPC npc)
        {
            DoGSegmentAI(npc);
            return false;
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D bodyTexture = ModContent.GetTexture("InfernumMode/BehaviorOverrides/BossAIs/DoG/DoGP1Body");
            Texture2D glowmaskTexture = ModContent.GetTexture("InfernumMode/BehaviorOverrides/BossAIs/DoG/DoGP1BodyGlowmask");
            Vector2 drawPosition = npc.Center - Main.screenPosition;
            Vector2 origin = bodyTexture.Size() * 0.5f;
            spriteBatch.Draw(bodyTexture, drawPosition, null, npc.GetAlpha(lightColor), npc.rotation, origin, npc.scale, SpriteEffects.None, 0f);
            spriteBatch.Draw(glowmaskTexture, drawPosition, null, npc.GetAlpha(Color.White), npc.rotation, origin, npc.scale, SpriteEffects.None, 0f);
            return false;
        }
    }

    public class DoGPhase1TailBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<DevourerofGodsTail>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCPreDraw;

        public override bool PreAI(NPC npc)
        {
            DoGPhase1BodyBehaviorOverride.DoGSegmentAI(npc);
            return false;
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D tailTexture = ModContent.GetTexture("InfernumMode/BehaviorOverrides/BossAIs/DoG/DoGP1Tail");
            Texture2D glowmaskTexture = ModContent.GetTexture("InfernumMode/BehaviorOverrides/BossAIs/DoG/DoGP1TailGlowmask");
            Vector2 drawPosition = npc.Center - Main.screenPosition;
            Vector2 origin = tailTexture.Size() * 0.5f;
            spriteBatch.Draw(tailTexture, drawPosition, null, npc.GetAlpha(lightColor), npc.rotation, origin, npc.scale, SpriteEffects.None, 0f);
            spriteBatch.Draw(glowmaskTexture, drawPosition, null, npc.GetAlpha(Color.White), npc.rotation, origin, npc.scale, SpriteEffects.None, 0f);
            return false;
        }
    }

    public class DoGPhase2BodyBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<DevourerofGodsBodyS>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

        public override bool PreAI(NPC npc)
        {
            DoGPhase1BodyBehaviorOverride.DoGSegmentAI(npc);
            return false;
        }
    }

    public class DoGPhase2TailBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<DevourerofGodsTailS>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

        public override bool PreAI(NPC npc)
        {
            DoGPhase1BodyBehaviorOverride.DoGSegmentAI(npc);
            return false;
        }
    }
}
