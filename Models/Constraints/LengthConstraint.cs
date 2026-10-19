using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace _curved__polygon_editor.Models.Constraints
{
	public class LengthConstraint : Constraint
	{
		public double TargetLength { get; set; }

		public LengthConstraint(double targetLength)
		{
			TargetLength = targetLength;
		}
	}
}
