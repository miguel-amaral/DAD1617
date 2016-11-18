public class MergingVisitor : MetricVisitor{

	private CSF_metric original;

	public MergingVisitor(CSF_metric original){
		this.original = original;
	}

	bool sameType(CSF_metric other) {
		return this.original.Metric.Equals(other.Metric);
	}

	void visit(CSF_IpInName 		  metric) { throw Exception("Merging does not do void visit"); }
	void visit(CSF_HighUpload 		  metric) { throw Exception("Merging does not do void visit"); }
	void visit(CSF_HighDownload 	  metric) { throw Exception("Merging does not do void visit"); }
	void visit(CSF_HighDataDiffPeers  metric) { throw Exception("Merging does not do void visit"); }
	void visit(CSF_KnownTrackers 	  metric) { throw Exception("Merging does not do void visit"); }
	void visit(CSF_LocalPeerDiscovery metric) { throw Exception("Merging does not do void visit"); }
	void visit(CSF_ProtocolUPnP       metric) { throw Exception("Merging does not do void visit"); }

	bool visitWithBool(CSF_IpInName 		  metric) {	if(this.sameType(metric) {	mergeIpInName			(metric); return true; } else { return false; } }
	bool visitWithBool(CSF_HighUpload 		  metric) {	if(this.sameType(metric) {	mergeHighUpload			(metric); return true; } else { return false; } }
	bool visitWithBool(CSF_HighDownload 	  metric) {	if(this.sameType(metric) {	mergeHighDownload		(metric); return true; } else { return false; } }
	bool visitWithBool(CSF_HighDataDiffPeers  metric) {	if(this.sameType(metric) {	mergeHighDataDiffPeers	(metric); return true; } else { return false; } }
	bool visitWithBool(CSF_KnownTrackers 	  metric) {	if(this.sameType(metric) {	mergeKnownTrackers		(metric); return true; } else { return false; } }
	bool visitWithBool(CSF_LocalPeerDiscovery metric) {	if(this.sameType(metric) {	mergeLocalPeerDiscovery	(metric); return true; } else { return false; } }
	bool visitWithBool(CSF_ProtocolUPnP       metric) {	if(this.sameType(metric) {	mergeProtocolUPnP		(metric); return true; } else { return false; } }

	private void mergeIpInName(CSF_metric other)			{
		private Dictionary<string,Hashtable> originalList = original.RawValues();
		private Dictionary<string,Hashtable>    otherList =    other.RawValues();

		//Go through every entry in the otherList
		foreach(KeyValuePair<string, Hashtable> othersEntry in otherList) {
			Hashtable existingCommunications;
			//Check if the original's list has this sinner already
			if (originalList.TryGetValue (othersEntry.key, out existingCommunications)) {
				//Lets increase the sin then..
				foreach (DictionaryEntry connection in othersEntry.Value) {
					//check if it is the first time they talk
					if (existingCommunications.ContainsKey (connection.Key)) {
						//More talks to the same guy
						existingCommunications [connection.Key] = (int)existingCommunications [connection.Key] + connection.Value;
					} else {
						//Oh new guy added
						existingCommunications [connection.Key] = connection.Value;
					}
				}
			} else { //it does not have, simple we will add this sinner to the list
				originalList [othersEntry.Key] = othersEntry.Value;
			}
		}

		//Update the table
		original.RawValues = originalList;
	}

	private void mergeHighUpload(CSF_metric other)			{

	}

	private void mergeHighDownload(CSF_metric other)		{}
	private void mergeHighDataDiffPeers(CSF_metric other)	{}
	private void mergeKnownTrackers(CSF_metric other)		{}
	private void mergeLocalPeerDiscovery(CSF_metric other)	{}
	private void mergeProtocolUPnP(CSF_metric other)		{}
}
