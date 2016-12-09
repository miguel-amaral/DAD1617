using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DADStormProcess {
    public class QueueRequests {
        public ConnectionPack author;
        public bool backup;
        public string tupleID;
        public IList<IList<string>> result;

        public QueueRequests(string iD) {
            this.tupleID = iD;
            this.backup = false;
        }

        public QueueRequests(string oldID, ConnectionPack author, IList<IList<string>> result) {
            this.tupleID = oldID;
            this.author = author;
            this.result = result;
            this.backup = true;
        }
    }
}
