using System;
using System.Collections.Generic;

namespace DADStormProcess {

	public abstract class RoutingTechinic {
		protected List<ConnectionPack> possibleDestinations;

		public abstract ConnectionPack nextDestination(List<ConnectionPack> possibleDestinations, string[] tuple);
		public abstract string methodName();
	}

	public class Primmary : RoutingTechinic {
		public override ConnectionPack nextDestination(List<ConnectionPack> possibleDestinations, string[] tuple) {
			return possibleDestinations [0];
		}
		public override string methodName() {
			return "Primmary";
		}

	}

	public class RandomRouting : RoutingTechinic {
		private static readonly Random rnd = new Random();
		public override ConnectionPack nextDestination(List<ConnectionPack> possibleDestinations, string[] tuple) {
			int index = rnd.Next (0, possibleDestinations.Count);
			return possibleDestinations [index];
		}
		public override string methodName() {
			return "Random";
		}
	}
	public class Hashing : RoutingTechinic {
		private int tuplePosition;
		public Hashing(int tuplePosition) {
			this.tuplePosition = tuplePosition;
		}

		public override ConnectionPack nextDestination(List<ConnectionPack> possibleDestinations, string[] tuple) {
			string tupleValue = tuple [tuplePosition];
			int maxIndex = possibleDestinations.Count;
			int index = tupleValue.GetHashCode () % maxIndex;
			return possibleDestinations [index];
		}
		public override string methodName() {
			return "Hashing " + tuplePosition;
		}
	}
}


