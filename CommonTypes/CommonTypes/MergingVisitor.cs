using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Remoting;
using System.Runtime.Serialization;
public class MergingVisitor : MetricVisitor { 

	private CSF_metric original;

	public MergingVisitor(CSF_metric original) {
		this.original = original;
	}

	bool sameType(CSF_metric other) {
		return this.original.Metric.Equals(other.Metric);
	}

	public void visitCSF_metricIpInName 			(CSF_metricIpInName            metric) { throw new Exception("Merging does not do void visit"); }
	public void visitCSF_metricHighUpload 		    (CSF_metricHighUpload 		    metric) { throw new Exception("Merging does not do void visit"); }
	public void visitCSF_metricHighDownload 		(CSF_metricHighDownload 		metric) { throw new Exception("Merging does not do void visit"); }
	public void visitCSF_metricHighDataDiffPeers 	(CSF_metricHighDataDiffPeers 	metric) { throw new Exception("Merging does not do void visit"); }
	public void visitCSF_metricKnownTrackers 		(CSF_metricKnownTrackers 		metric) { throw new Exception("Merging does not do void visit"); }
	public void visitCSF_metricLocalPeerDiscovery  (CSF_metricLocalPeerDiscovery  metric) { throw new Exception("Merging does not do void visit"); }
	public void visitCSF_metricProtocolUPnP(CSF_metricProtocolUPnP        metric) { throw new Exception("Merging does not do void visit"); }
    public void visit(CSF_metric                    metric) { throw new Exception("Merging does not do the general type.."); }

    public bool visitWithBoolCSF_metricIpInName 			(CSF_metricIpInName 			metric) {	if(this.sameType(metric)) { this.mergeIpInName			 (metric); return true; } else { return false; } }
	public bool visitWithBoolCSF_metricHighUpload 		    (CSF_metricHighUpload 		    metric) {	if(this.sameType(metric)) { this.mergeHighUpload		 (metric); return true; } else { return false; } }
	public bool visitWithBoolCSF_metricHighDownload 		(CSF_metricHighDownload 		metric) {	if(this.sameType(metric)) { this.mergeHighDownload		 (metric); return true; } else { return false; } }
	public bool visitWithBoolCSF_metricHighDataDiffPeers 	(CSF_metricHighDataDiffPeers 	metric) {	if(this.sameType(metric)) { this.mergeHighDataDiffPeers	 (metric); return true; } else { return false; } }
	public bool visitWithBoolCSF_metricKnownTrackers 		(CSF_metricKnownTrackers 		metric) {	if(this.sameType(metric)) { this.mergeKnownTrackers		 (metric); return true; } else { return false; } }
	public bool visitWithBoolCSF_metricLocalPeerDiscovery  (CSF_metricLocalPeerDiscovery  metric) {	if(this.sameType(metric)) { this.mergeLocalPeerDiscovery (metric); return true; } else { return false; } }
	public bool visitWithBoolCSF_metricProtocolUPnP(CSF_metricProtocolUPnP        metric) {	if(this.sameType(metric)) { this.mergeProtocolUPnP		 (metric); return true; } else { return false; } }
    public bool visitWithBool(CSF_metric                    metric) {   throw new Exception("Merging does not do the general ABSTRACT type.."); }


    private void mergeIpInName(CSF_metric other) {
        Dictionary<string,Hashtable> originalList = this.original.RawValues;
		Dictionary<string,Hashtable>    otherList =         other.RawValues;

        if (otherList == null) {
            // nothing to be merged..
            if (originalList == null) {
                originalList = new Dictionary<string, Hashtable>();
            }
            return;
        } else if (originalList == null) {
            if (otherList != null) { //it is always true
                originalList = otherList;
            } else {
                originalList = new Dictionary<string, Hashtable>();
            }
            return;
        }

        //Go through every entry in the otherList
        foreach (KeyValuePair<string, Hashtable> othersEntry in otherList) {
			Hashtable existingCommunications;
			//Check if the original's list has this sinner already
			if (originalList.TryGetValue (othersEntry.Key, out existingCommunications)) {
				//Lets increase the sin then..
				foreach (DictionaryEntry connection in othersEntry.Value) {
					//check if it is the first time they talk
					if (existingCommunications.ContainsKey (connection.Key)) {
						//More talks to the same guy
						existingCommunications [connection.Key] = (int)existingCommunications [connection.Key] + (int)connection.Value;
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

	private void mergeHighUpload(CSF_metric other) {
        this.mergeIpInName(other);
    }

	private void mergeHighDownload(CSF_metric other)		{
		this.mergeIpInName(other);
	}

	private void mergeHighDataDiffPeers(CSF_metric other)	{
		this.mergeIpInName(other);
	}

	private void mergeKnownTrackers(CSF_metric other)		{
		this.mergeIpInName(other);
	}

	private void mergeLocalPeerDiscovery(CSF_metric other)	{
		Dictionary<string,int> originalList = original.Sinners;
		Dictionary<string,int>    otherList =    other.Sinners;

        if (otherList == null) {
            // nothing to be merged..
            if (originalList == null) {
                originalList = new Dictionary<string, int>();
            }
            return; 
        } else if (originalList == null) {
            if (otherList != null) { //it is always true
                originalList = otherList;
            } else {
                originalList = new Dictionary<string, int>();
            }
            return;
        }
        //else there is something to merge
		//Go through every entry in the otherList
		foreach(KeyValuePair<string, int> othersEntry in otherList) {

			if (originalList.ContainsKey (othersEntry.Key)) {
				originalList[othersEntry.Key] = originalList[othersEntry.Key] + othersEntry.Value;
			} else {
				originalList[othersEntry.Key] = othersEntry.Value;
			}
		}
	}

	private void mergeProtocolUPnP(CSF_metric other)		{
		this.mergeLocalPeerDiscovery(other);
	}
}
