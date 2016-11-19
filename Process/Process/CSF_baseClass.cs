using System;
using System.Net;
using System.Net.Sockets;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;

//explanation
namespace DADStormProcess {
	public class CSF_BaseClass: CSF_TupleStructure {
		public override void processTuple (IList<string> tuple) {

		}
		public override MemoryStream reportBack(){
            Dictionary<string, int> metricSinners = new Dictionary<string, int>();
            string metricName = this.GetType().Name;

            //--------------------------//
            //   calculate metrc value  //
            //--------------------------//

            CSF_metric metric = new CSF_metric(metricName, metricSinners);

            DataContractJsonSerializer js = new DataContractJsonSerializer(typeof(CSF_metric));
            MemoryStream stream = new MemoryStream();
            js.WriteObject(stream, metric);

            return stream;
        }
		public override void reset(){

		}
	}
}
