using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Remoting;
using System.Runtime.Serialization;
using System.IO;
using System.Runtime.Serialization.Json;

public interface MetricVisitor
{
    bool visitWithBool(CSF_metric metric);
    void visit(CSF_metric metric);

    void visitCSF_metricIpInName(CSF_metricIpInName metric);
    void visitCSF_metricHighUpload 		     (CSF_metricHighUpload 		    metric);
    void visitCSF_metricHighDownload 		 (CSF_metricHighDownload 		metric);
    void visitCSF_metricHighDataDiffPeers 	 (CSF_metricHighDataDiffPeers 	metric);
    void visitCSF_metricKnownTrackers 		 (CSF_metricKnownTrackers 		metric);
    void visitCSF_metricLocalPeerDiscovery   (CSF_metricLocalPeerDiscovery  metric);
    void visitCSF_metricProtocolUPnP         (CSF_metricProtocolUPnP        metric);

    bool visitWithBoolCSF_metricIpInName(CSF_metricIpInName metric);
    bool visitWithBoolCSF_metricHighUpload(CSF_metricHighUpload metric);
    bool visitWithBoolCSF_metricHighDownload(CSF_metricHighDownload metric);
    bool visitWithBoolCSF_metricHighDataDiffPeers(CSF_metricHighDataDiffPeers metric);
    bool visitWithBoolCSF_metricKnownTrackers(CSF_metricKnownTrackers metric);
    bool visitWithBoolCSF_metricLocalPeerDiscovery(CSF_metricLocalPeerDiscovery metric);
    bool visitWithBoolCSF_metricProtocolUPnP(CSF_metricProtocolUPnP metric);

}

[DataContract]
public class CSF_metric {
    [DataMember]
    public  string metric;
    [DataMember]
    public Dictionary<string, int> sinners;
    //String is source ip; hastable key -> destIp , value -> size/number of communications
    [DataMember]
    public Dictionary<string, Hashtable> rawValues;

    public CSF_metric(string metric, Dictionary<string, int> sinners) {
        this.Metric = metric;
        this.Sinners = sinners;
    }

    public CSF_metric(string metric, Dictionary<string, Hashtable> rawValues)
    {
        this.Metric = metric;
        this.RawValues = rawValues;
    }

    public string Metric
    {
        get
        {
            return metric;
        }
        set
        {
            metric = value;
        }
    }

    public Dictionary<string, int> Sinners
    {
        get
        {
            return sinners;
        }
        set
        {
            sinners = value;
        }
    }

    public Dictionary<string, Hashtable> RawValues
    {
        get
        {
            return rawValues;
        }
        set
        {
            rawValues = value;
        }
    }

    //public CSF_metric() { }

    public virtual void acept(MetricVisitor visitor)
    {
        visitor.visit(this);
    }

    public virtual bool aceptWithBool(MetricVisitor visitor)
    {
        return visitor.visitWithBool(this);
    }
}

public class CSF_metricIpInName 			: CSF_metric {
    public CSF_metricIpInName            (string metric, Dictionary<string, int> sinners) : base(metric,sinners) {}
    public CSF_metricIpInName           (string metric, Dictionary<string, Hashtable> rawValues) : base(metric,rawValues) {}

    public override void acept(MetricVisitor visitor) { visitor.visitCSF_metricIpInName(this); }
    public override bool aceptWithBool(MetricVisitor visitor) { return visitor.visitWithBoolCSF_metricIpInName(this); }
}
public class CSF_metricHighUpload 		    : CSF_metric { public CSF_metricHighUpload 		    (string metric, Dictionary<string, int> sinners) : base(metric,sinners) {} public CSF_metricHighUpload 		   (string metric, Dictionary<string, Hashtable> rawValues) : base(metric,rawValues) {} public override void acept(MetricVisitor visitor) { visitor.visitCSF_metricHighUpload           (this); } public override bool aceptWithBool(MetricVisitor visitor) { return visitor.visitWithBoolCSF_metricHighUpload(this); } }
public class CSF_metricHighDownload 		: CSF_metric { public CSF_metricHighDownload 		(string metric, Dictionary<string, int> sinners) : base(metric,sinners) {} public CSF_metricHighDownload       (string metric, Dictionary<string, Hashtable> rawValues) : base(metric,rawValues) {} public override void acept(MetricVisitor visitor) { visitor.visitCSF_metricHighDownload 		(this); } public override bool aceptWithBool(MetricVisitor visitor) { return visitor.visitWithBoolCSF_metricHighDownload 		(this); } }
public class CSF_metricHighDataDiffPeers 	: CSF_metric { public CSF_metricHighDataDiffPeers 	(string metric, Dictionary<string, int> sinners) : base(metric,sinners) {} public CSF_metricHighDataDiffPeers  (string metric, Dictionary<string, Hashtable> rawValues) : base(metric,rawValues) {} public override void acept(MetricVisitor visitor) { visitor.visitCSF_metricHighDataDiffPeers 	(this); } public override bool aceptWithBool(MetricVisitor visitor) { return visitor.visitWithBoolCSF_metricHighDataDiffPeers 	(this); } }
public class CSF_metricKnownTrackers 		: CSF_metric { public CSF_metricKnownTrackers 		(string metric, Dictionary<string, int> sinners) : base(metric,sinners) {} public CSF_metricKnownTrackers 	   (string metric, Dictionary<string, Hashtable> rawValues) : base(metric,rawValues) {} public override void acept(MetricVisitor visitor) { visitor.visitCSF_metricKnownTrackers 		(this); } public override bool aceptWithBool(MetricVisitor visitor) { return visitor.visitWithBoolCSF_metricKnownTrackers 		(this); } }
public class CSF_metricLocalPeerDiscovery   : CSF_metric { public CSF_metricLocalPeerDiscovery  (string metric, Dictionary<string, int> sinners) : base(metric,sinners) {} public CSF_metricLocalPeerDiscovery (string metric, Dictionary<string, Hashtable> rawValues) : base(metric,rawValues) {} public override void acept(MetricVisitor visitor) { visitor.visitCSF_metricLocalPeerDiscovery   (this); } public override bool aceptWithBool(MetricVisitor visitor) { return visitor.visitWithBoolCSF_metricLocalPeerDiscovery   (this); } }
public class CSF_metricProtocolUPnP         : CSF_metric { public CSF_metricProtocolUPnP        (string metric, Dictionary<string, int> sinners) : base(metric,sinners) {} public CSF_metricProtocolUPnP       (string metric, Dictionary<string, Hashtable> rawValues) : base(metric,rawValues) {} public override void acept(MetricVisitor visitor) { visitor.visitCSF_metricProtocolUPnP         (this); } public override bool aceptWithBool(MetricVisitor visitor) { return visitor.visitWithBoolCSF_metricProtocolUPnP         (this); } }