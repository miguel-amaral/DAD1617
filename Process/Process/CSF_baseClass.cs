using System;
using System.Net;
using System.Net.Sockets;
using System.Collections;
using System.Collections.Generic;

//High bandwidth to several different peers is abnominal behvaiour
namespace DADStormProcess {
	public class CSF_BaseClass: CSF_TupleStructure {
		public override void processTuple (IList<string> tuple) {

		}
		public override CSF_metric reportBack(){
            Dictionary<string, int> metricSinners = new Dictionary<string, int>();
            string metricName = this.GetType().Name;

            //--------------------------//
            //   calculate metrc value  //
            //--------------------------//

            CSF_metric metric = new CSF_metric(metricName, metricSinners);
            return metric;
        }
		public override void reset(){

		}
	}
}
