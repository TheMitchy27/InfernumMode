using CalamityMod;
using CalamityMod.DataStructures;
using CalamityMod.Schematics;
using CalamityMod.World;
using InfernumMode.Achievements;
using InfernumMode.Achievements.InfernumAchievements;
using InfernumMode.BehaviorOverrides.BossAIs.GreatSandShark;
using Microsoft.Xna.Framework;
using SubworldLibrary;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.Events;
using Terraria.ID;
using Terraria.IO;
using Terraria.ModLoader;
using Terraria.Utilities;
using Terraria.WorldBuilding;

using static CalamityMod.Schematics.SchematicManager;

namespace InfernumMode.Subworlds
{
    public class LostColosseum : Subworld
    {
        internal static bool VassalWasBeaten = false;

        public static bool HasBereftVassalAppeared
        {
            get;
            set;
        } = false;

        public static bool HasBereftVassalBeenDefeated
        {
            get;
            set;
        } = false;

        public static float SunsetInterpolant
        {
            get;
            set;
        }

        public static Color SunlightColor =>
            Color.Lerp(Color.White, new(210, 85, 135), SunsetInterpolant * SunsetInterpolant * 0.67f);

        public const int SchematicWidth = 1199;

        public const int SchematicHeight = 251;

        public const int CaveWidth = 180;

        public static readonly Point PortalPosition = new(CaveWidth + 25, 190);

        public static readonly Point CampfirePosition = new(CaveWidth + 320, 165);

        public override int Width => SchematicWidth + CaveWidth + 36;

        public override int Height => SchematicHeight + 32;

        public override bool ShouldSave => true;

        public override List<GenPass> Tasks => new()
        {
            new LostColosseumGenPass()
        };

        public class LostColosseumGenPass : GenPass
        {
            public LostColosseumGenPass() : base("Terrain", 1f) { }

            protected override void ApplyPass(GenerationProgress progress, GameConfiguration configuration)
            {
                progress.Message = "Generating a Lost Colosseum";
                Main.worldSurface = Main.maxTilesY - 25;
                Main.rockLayer = Main.maxTilesY - 30;

                for (int i = 0; i < CaveWidth; i++)
                {
                    for (int j = 0; j < Main.maxTilesY; j++)
                    {
                        Main.tile[i, j].TileType = TileID.Sandstone;
                        Main.tile[i, j].Get<TileWallWireStateData>().HasTile = true;
                    }
                }

                GenerateCaveSystem(new(CaveWidth, 92), new(PortalPosition.X - 25, PortalPosition.Y));

                bool _ = false;
                Point bottomLeftOfWorld = new(Main.maxTilesX - 37, Main.maxTilesY - 30);
                PlaceSchematic<Action<Chest>>("LostColosseum", bottomLeftOfWorld, SchematicAnchor.BottomRight, ref _);

                // Set the default spawn position.
                Main.spawnTileX = PortalPosition.X;
                Main.spawnTileY = PortalPosition.Y;
            }

            public static void GenerateCaveSystem(Point start, Point end)
            {
                Point midpoint = new(CaveWidth / -4, (start.Y + end.Y) / 2 + WorldGen.genRand.Next(-25, 25));
                List<Vector2> baseCurvePoints = new BezierCurve(start.ToVector2(), midpoint.ToVector2(), end.ToVector2()).GetPoints(108);

                // Add a bit of direction variance based on perlin noise to the points.
                int caveSeed = WorldGen.genRand.Next();
                for (int i = 0; i < baseCurvePoints.Count; i++)
                {
                    Vector2 perpendicularDirection = Vector2.UnitY;
                    float noiseOffset = SulphurousSea.FractalBrownianMotion(baseCurvePoints[i].X / 120f, baseCurvePoints[i].Y / 120f, caveSeed, 3) * 28f;
                    if (i >= 1)
                        perpendicularDirection = (baseCurvePoints[i] - baseCurvePoints[i - 1]).SafeNormalize(Vector2.UnitY).RotatedBy(MathHelper.PiOver2);

                    baseCurvePoints[i] += perpendicularDirection * noiseOffset;
                    if (baseCurvePoints[i].X <= 25f)
                        baseCurvePoints[i] = new Vector2(25f, baseCurvePoints[i].Y);
                }

                // Carve out caves.
                foreach (Vector2 curvePoint in baseCurvePoints)
                {
                    WorldUtils.Gen(curvePoint.ToPoint(), new Shapes.Circle(7, 13), Actions.Chain(new GenAction[]
                    {
                        new Modifiers.Blotches(3, 0.27),
                        new Actions.ClearTile()
                    }));
                }
            }
        }

        public override bool GetLight(Tile tile, int x, int y, ref FastRandom rand, ref Vector3 color)
        {
            Vector3 lightMin = Vector3.Zero;
            bool notSolid = tile.Slope != SlopeType.Solid || tile.IsHalfBlock;
            if (!tile.HasTile || !Main.tileNoSunLight[tile.TileType] || (notSolid && Main.wallLight[tile.WallType] && tile.LiquidAmount < 200))
                lightMin = SunlightColor.ToVector3();

            color = Vector3.Max(color, lightMin);
            return false;
        }

        public override void OnExit()
        {
            // Reset the sunset interpolant.
            SunsetInterpolant = 0f;

            // Ensure that the vassal defeat achievement translates over when the player goes to a different subworld.
            List<Achievement> achievementList = new();
            foreach (var achievement in achievementList)
            {
                if (achievement.GetType() == typeof(BereftVassalAchievement))
                {
                    if (achievement.DoneCompletionEffects)
                        VassalWasBeaten = true;
                }
            }
        }

        public static void ManageSandstorm()
        {
            Main.windSpeedCurrent = 1.5f;
            bool useSandstorm = !HasBereftVassalBeenDefeated;
            int vassal = NPC.FindFirstNPC(ModContent.NPCType<BereftVassal>());
            if (useSandstorm && vassal >= 0 && !Main.LocalPlayer.dead)
                useSandstorm = Main.npc[vassal].ModNPC<BereftVassal>().CurrentAttack == BereftVassal.BereftVassalAttackType.IdleState;

            if (useSandstorm)
            {
                Sandstorm.Happening = true;
                Sandstorm.TimeLeft = 240;
                Sandstorm.IntendedSeverity = Sandstorm.Severity = 1.5f;
            }
            else
            {
                Sandstorm.StopSandstorm();
                Sandstorm.Severity *= 0.96f;
            }
        }

        public static void UpdateSunset()
        {
            // 12:00 PM.
            int noon = 27000;

            // 5:00 PM.
            int evening = noon + 18000;
            Main.time = (int)MathHelper.Lerp(noon, evening, SunsetInterpolant);

            if (HasBereftVassalBeenDefeated)
                SunsetInterpolant = 1f;
        }
    }
}
