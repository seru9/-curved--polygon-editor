using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace _curved__polygon_editor.Models.Constraints
{
	public class PerpendicularConstraint : Constraint
	{
		public Edge TargetEdge { get; set; }
		public int PairId { get; set; }

		public PerpendicularConstraint(Edge targetEdge, int pairId = 1)
		{
			TargetEdge = targetEdge;
			PairId = pairId;
		}
	}
}
