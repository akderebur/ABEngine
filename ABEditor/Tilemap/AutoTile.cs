using System;
using ABEngine.ABERuntime;
using System.Collections.Generic;
using System.Numerics;
using ABEngine.ABERuntime.Components;
using System.Linq;
using Arch.Core.Extensions;

namespace ABEngine.ABEditor.TilemapExtension
{
	public class AutoTile
	{
        public int defaultSpriteID;
        public bool extended;

		public List<TileRule> tileRules { get; set; }

		public AutoTile()
		{
			tileRules = new List<TileRule>();
		}

        bool IsSpriteInSet(Sprite sprite)
        {
            int spriteId = sprite.GetSpriteID();
            if (spriteId == defaultSpriteID)
                return true;

            var rule = tileRules.FirstOrDefault(r => r.spriteID == spriteId);
            if (rule != null)
                return true;

            return false;
        }

		public void UpdateSprite(Sprite sprite, Tilemap tilemap, bool existing = false)
        {
            if (existing && !IsSpriteInSet(sprite))
                return;

            Vector3 placePos = sprite.transform.worldPosition.RoundTo2Dec();
            TileRule rule = MatchRule(placePos, tilemap);

            int spriteId = rule != null ? rule.spriteID : defaultSpriteID;
            sprite.SetSpriteID(spriteId);

            if(!existing)
                tilemap.AddTile(sprite.transform, spriteId);	
        }

        public void UpdateExisting(Tilemap tilemap)
        {
            foreach (var sprTrans in tilemap.GetAllSprites())
            {
                Sprite sprite = sprTrans.entity.Get<Sprite>();
                if (!IsSpriteInSet(sprite))
                    continue;

                Vector3 placePos = sprite.transform.worldPosition.RoundTo2Dec();
                TileRule rule = MatchRule(placePos, tilemap);

                int spriteId = rule != null ? rule.spriteID : defaultSpriteID;
                sprite.SetSpriteID(spriteId);
            }
        }

        private TileRule MatchRule(Vector3 pos, Tilemap tilemap)
        {
            int offset = extended ? 2 : 1;
            int count = extended ? 5 : 3;

            TileRule selectedRule = null;
            foreach (TileRule tileRule in tileRules)
            {
                bool obeys = true;
                for (int i = 0; i < count; i++)
                {
                    for (int j = 0; j < count; j++)
                    {
                        if (i == offset && j == offset) // Self
                            continue;

                        // 1 - Same set, 2 - Different set or empty, 0 - Don't Care
                        int state = tileRule.Grid[i][j];
                        Vector3 relativePos = new Vector3(j - offset, offset - i, 0f);
                        relativePos *= tilemap.tileImage.spriteSize.PixelToWorld().ToVector3();
                        Vector3 finalPos = pos + relativePos;
                        finalPos = finalPos.RoundTo2Dec();

                        if (state == 1) // Should be same set
                        {
                            if (!tilemap.HasPlacement(finalPos))
                            {
                                obeys = false;
                                break;
                            }

                            // Check if same set
                            var exSprite = tilemap.GetSpriteTransFromPos(finalPos).entity.Get<Sprite>();
                            if(!IsSpriteInSet(exSprite))
                            {
                                obeys = false;
                                break;
                            }

                        }
                        else if (state == 2) // Should be other set or empty
                        {
                            if (tilemap.HasPlacement(finalPos))
                            {
                                // Check if other set
                                var exSprite = tilemap.GetSpriteTransFromPos(finalPos).entity.Get<Sprite>();
                                if (IsSpriteInSet(exSprite))
                                {
                                    obeys = false;
                                    break;
                                }

                            }
                        }
                    }

                    if (!obeys)
                        break;
                }

                if (obeys)
                {
                    selectedRule = tileRule;
                    break;
                }
            }

            return selectedRule;
        }
	}

	public class TileRule
	{
		public int spriteID;

		public int[][] Grid;
		public TileRule(bool extended = false)
		{
            int count = extended ? 5 : 3;
            Grid = new int[count][];
            for (int i = 0; i < count; i++)
            {
                Grid[i] = new int[count];
            }
        }

        
    }
}

