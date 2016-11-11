using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomOperators {
    public class Default {
        int innerCounter = 0;
        public IList<IList<string>> Dup(IList<string> tuple) {
            List<IList<string>> newTuple = new List<IList<string>>();
            newTuple.Add(tuple);
            return newTuple;
        }

        public IList<IList<string>> Count(IList<string> tuple) {
            string toReturn = (innerCounter++).ToString();
            tuple = new List<string>();
            tuple.Add(toReturn);
            List<IList<string>> newTuple = new List<IList<string>>();
            newTuple.Add(tuple);
            return newTuple;
        }
    }
}
