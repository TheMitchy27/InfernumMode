using CalamityMod;
using CalamityMod.NPCs.AdultEidolonWyrm;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.SummonItems
{
    public class EvokingSearune : ModItem
    {
        public override void SetStaticDefaults()
        {
            SacrificeTotal = 1;
            DisplayName.SetDefault("Evoking Searune");
            Tooltip.SetDefault("Summons the Adult Eidolon Wyrm in the deepest layer of the Abyss\n" +
                "A primordial artifact said to have been crafted by the Creator, it is capable of summoning a guarding apparition\n" +
                "Not consumable");
        }

        public override void SetDefaults()
        {
            Item.width = 34;
            Item.height = 30;
            Item.rare = ItemRarityID.Blue;
            Item.useAnimation = 45;
            Item.useTime = 45;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.consumable = false;
        }

        public override bool CanUseItem(Player player) => !NPC.AnyNPCs(ModContent.NPCType<AdultEidolonWyrmHead>()) && player.Calamity().ZoneAbyssLayer4;

        public override bool? UseItem(Player player)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 spawnPosition = player.Center - Vector2.UnitY * 600f;
                NPC.NewNPC(player.GetSource_ItemUse(Item), (int)spawnPosition.X, (int)spawnPosition.Y, ModContent.NPCType<AdultEidolonWyrmHead>());
            }
            return true;
        }
    }
}