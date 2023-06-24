using System;
using System.Collections.Generic;
using System.Numerics;
using ABEngine.ABERuntime;
using ABEngine.ABERuntime.Components;
using Arch.Core.Extensions;

namespace ABEngine.ABEditor.TilemapExtension
{
	public static class TilemapExtensions
	{
		public static void PlaceAutoTile(this Tilemap tilemap, Sprite sprite, AutoTile autoTile)
		{
			autoTile.UpdateSprite(sprite, tilemap);

            // Update neighbors
            Vector3 pos3d = sprite.transform.worldPosition;
            Vector2 pos = pos3d.ToVector2().RoundTo2Dec();

            List<Vector2> neighbors = null;
            if (autoTile.extended)
                neighbors = pos.GetAdjacentDiagonalExtended(tilemap.tileImage.spriteSize.PixelToWorld());
            else
                neighbors = pos.GetAdjacentDiagonal(tilemap.tileImage.spriteSize.PixelToWorld());
            foreach (var neighbor in neighbors)
            {
                Transform spriteTrans = tilemap.GetSpriteTransFromPos(neighbor.ToVector3().RoundTo2Dec());
                if (spriteTrans != null)
                    autoTile.UpdateSprite(spriteTrans.entity.Get<Sprite>(), tilemap, true);
            }

            //autoTile.UpdateExisting(tilemap); - Whole tilemap

            //CheckNeighbors(pos, tilemap, autoTile, new List<Vector2>() { pos }); - Recurse neighbor
        }

        // Recursive - doesn't seem needed
        static void CheckNeighbors(Vector2 pos, Tilemap tilemap, AutoTile autoTile, List<Vector2> visited)
        {
            List<Vector2> neighbors = null;
            if (autoTile.extended)
                neighbors = pos.GetAdjacentDiagonalExtended(tilemap.tileImage.spriteSize.PixelToWorld());
            else
                neighbors = pos.GetAdjacentDiagonal(tilemap.tileImage.spriteSize.PixelToWorld());

            List<Vector2> updated = new List<Vector2>();
            foreach (var neighbor in neighbors)
            {
                if (visited.Contains(neighbor))
                    continue;

                visited.Add(neighbor);
                Transform spriteTrans = tilemap.GetSpriteTransFromPos(neighbor.ToVector3().RoundTo2Dec());
                if (spriteTrans != null)
                {
                    autoTile.UpdateSprite(spriteTrans.entity.Get<Sprite>(), tilemap, true);
                    updated.Add(neighbor);
                }
            }

            foreach (var neighbor in updated)
            {
                CheckNeighbors(neighbor, tilemap, autoTile, visited);
            }
        }
    }
}

