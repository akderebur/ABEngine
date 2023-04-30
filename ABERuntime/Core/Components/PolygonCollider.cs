using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace ABEngine.ABERuntime.Components
{
	public class PolygonCollider : ABComponent
	{
        List<Vector2> points;

        public PolygonCollider()
		{
			points = new List<Vector2>();
		}

        public PolygonCollider(List<Vector2> points)
        {
            SetPoints(points);
        }

        public void SetPoints(List<Vector2> points)
		{
			this.points = points.ToList();
		}

        public List<Vector2> GetPoints()
        {
			return points.ToList();
        }
    }
}

