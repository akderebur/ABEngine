using System;
using ABEngine.ABERuntime;
using System.Collections.Generic;
using System.Numerics;
using ABEngine.ABERuntime.Components;

namespace ABEngine.ABEditor.TilemapExtension
{
	public class AutoTile
	{
        public int defaultSpriteID;
		public List<TileRule> tileRules { get; set; }

		public AutoTile()
		{
			tileRules = new List<TileRule>();
		}

		public void UpdateSprite(Sprite sprite, Tilemap tilemap, bool existing = false)
        {
            Vector3 placePos = sprite.transform.worldPosition.RoundTo2Dec();
            TileRule rule = MatchRule(placePos, tilemap);

            int spriteId = rule != null ? rule.spriteID : defaultSpriteID;
            sprite.SetSpriteID(spriteId);

            if(!existing)
                tilemap.AddTile(sprite.transform, spriteId);	
        }

        private TileRule MatchRule(Vector3 pos, Tilemap tilemap)
        {
            TileRule selectedRule = null;
            foreach (TileRule tileRule in tileRules)
            {
                bool obeys = true;
                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        if (i == 1 && j == 1) // Self
                            continue;

                        // 1 - Empty, 2 - Not Empty, 0 - Don't Care
                        int state = tileRule.Grid[i][j];
                        Vector3 relativePos = new Vector3(j - 1, 1 - i, 0f);
                        relativePos *= tilemap.tileImage.spriteSize.PixelToWorld().ToVector3();
                        Vector3 finalPos = pos + relativePos;
                        finalPos = finalPos.RoundTo2Dec();

                        if (state == 1) // Should be free
                        {
                            if (tilemap.HasPlacement(finalPos))
                            {
                                obeys = false;
                                break;
                            }

                        }
                        else if (state == 2)
                        {
                            if (!tilemap.HasPlacement(finalPos))
                            {
                                obeys = false;
                                break;
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
		//public List<Vector3> emptyTiles { get; set; }
		//public List<Vector3> occupiedTiles { get; set; }
		public int spriteID;
		public bool selected;

		public int[][] Grid;
		public TileRule()
		{
            Grid = new int[3][];
            for (int i = 0; i < 3; i++)
            {
                Grid[i] = new int[3];
            }
        }
    }
}

