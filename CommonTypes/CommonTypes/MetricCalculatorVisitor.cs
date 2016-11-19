using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class MetricCalculatorVisitor : MetricVisitor {

    private int minimumHighUpload     = 0000000000 * 102 * 1024 * 1024; //100 MB
    private int minimumHighDownload   = 0000000000 * 102 * 1024 * 1024; //100 MB
    private int minimumDataConnection = 0000000000 * 100 * 1024 * 1024; //100MB


    public MetricCalculatorVisitor() { }

    public void visit(CSF_metric metric)                    { throw new Exception("Merging does not do the general ABSTRACT type.."); }

    public bool visitWithBoolCSF_metricIpInName 			(CSF_metricIpInName metric)            { throw new Exception("Merging does not do bool visit"); }
    public bool visitWithBoolCSF_metricHighUpload 		    (CSF_metricHighUpload metric)          { throw new Exception("Merging does not do bool visit"); }
    public bool visitWithBoolCSF_metricHighDownload 		(CSF_metricHighDownload metric)        { throw new Exception("Merging does not do bool visit"); }
    public bool visitWithBoolCSF_metricHighDataDiffPeers 	(CSF_metricHighDataDiffPeers metric)   { throw new Exception("Merging does not do bool visit"); }
    public bool visitWithBoolCSF_metricKnownTrackers 		(CSF_metricKnownTrackers metric)       { throw new Exception("Merging does not do bool visit"); }
    public bool visitWithBoolCSF_metricLocalPeerDiscovery  (CSF_metricLocalPeerDiscovery metric)  { throw new Exception("Merging does not do bool visit"); }
    public bool visitWithBoolCSF_metricProtocolUPnP(CSF_metricProtocolUPnP metric)        { throw new Exception("Merging does not do bool visit"); }
    public bool visitWithBool(CSF_metric metric)                    { throw new Exception("Merging does not do the general ABSTRACT type.."); }

    private void doIndividual(CSF_metric metric) {
        string strMetric = metric.Metric;
        CSF_metric toReturn = metric;
        switch (strMetric) {
            case "CSF_HighDataDiffPeers":
                toReturn = new CSF_metricHighDataDiffPeers(strMetric, metric.RawValues);
                break;
            case "CSF_ProtocolUPnP":
                toReturn = new CSF_metricProtocolUPnP(strMetric, metric.Sinners);
                break;
            case "CSF_LocalPeerDiscovery":
                toReturn = new CSF_metricLocalPeerDiscovery(strMetric, metric.Sinners);
                break;
            case "CSF_KnownTrackers":
                toReturn = new CSF_metricKnownTrackers(strMetric, metric.RawValues);
                break;
            case "CSF_IpInName":
                toReturn = new CSF_metricIpInName(strMetric, metric.RawValues);
                break;
            case "CSF_HighUpload":
                toReturn = new CSF_metricHighUpload(strMetric, metric.RawValues);
                break;
            case "CSF_HighDownload":
                toReturn = new CSF_metricHighDownload(strMetric, metric.RawValues);
                break;
            default:
                System.Console.WriteLine("\n\nNOT FOUND!!!\n\n" + strMetric + "\n\n");
                break;
        }
        this.visit(toReturn);
    }

    public void visitCSF_metricIpInName(CSF_metricIpInName metric) {
        Dictionary<string, Hashtable> sinnerList = metric.RawValues;

        Console.ForegroundColor = ConsoleColor.DarkMagenta;
        System.Console.WriteLine(metric.Metric);
        Console.ResetColor();

        //We do register if a connection happens only once or more often, but we ignore that..
        foreach (KeyValuePair<string, Hashtable> sourceEntry in sinnerList)
        {
            System.Console.WriteLine(sourceEntry.Key + " talked to " + sourceEntry.Value.Count + " domestic IPs");
            //metricSinners.Add(sourceEntry.Key, sourceEntry.Value.Count);
        }
    }

    public void visitCSF_metricHighUpload(CSF_metricHighUpload metric) {
        Console.ForegroundColor = ConsoleColor.DarkMagenta;
        System.Console.WriteLine(metric.Metric);
        Console.ResetColor();

        Dictionary<string, Hashtable> connections = metric.RawValues;

        foreach (KeyValuePair<string, Hashtable> sourceEntry in connections) {
            string ip = sourceEntry.Key;
            Hashtable table = sourceEntry.Value;
            int uploadSize = 0;
            foreach (DictionaryEntry pair in table) {
                uploadSize += (int)pair.Value;
            }

            if (uploadSize > minimumHighUpload) {
                System.Console.WriteLine(ip + " uploaded " + uploadSize + ", triggered when more than " + minimumHighUpload + " MB of data uploaded");
                //metricSinners.Add(ip, uploadSize);
            }
        }
    }

    public void visitCSF_metricHighDownload(CSF_metricHighDownload metric) {
        Console.ForegroundColor = ConsoleColor.DarkMagenta;
        System.Console.WriteLine(metric.Metric);
        Console.ResetColor();

        Dictionary<string, Hashtable> connections = metric.RawValues;

        foreach (KeyValuePair<string, Hashtable> sourceEntry in connections) {
            string ip = sourceEntry.Key;
            Hashtable table = sourceEntry.Value;
            int downloadSize = 0;
            foreach (DictionaryEntry pair in table) {
                downloadSize += (int)pair.Value;
            }
            if (downloadSize > minimumHighDownload) {
                System.Console.WriteLine(ip + " download " + downloadSize + ", triggered when more than " + minimumHighDownload + " MB of data uploaded");
                //metricSinners.Add(ip, downloadSize);
            }
        }
    }

    public void visitCSF_metricHighDataDiffPeers(CSF_metricHighDataDiffPeers metric)  {
        Console.ForegroundColor = ConsoleColor.DarkMagenta;
        System.Console.WriteLine(metric.Metric);
        Console.ResetColor();

        Dictionary<string, Hashtable> connections = metric.RawValues;

        foreach (KeyValuePair<string, Hashtable> sourceEntry in connections) {
            string ip = sourceEntry.Key;
            Hashtable table = sourceEntry.Value;
            int bigConnectionsCount = 0;
            foreach (DictionaryEntry pair in table)
            {
                if ((int)pair.Value > minimumDataConnection)
                {
                    bigConnectionsCount++;
                }
            }
            if (bigConnectionsCount > 0) {
                System.Console.WriteLine(ip + " talked to " + bigConnectionsCount + " IPs with more than " + minimumDataConnection + " of MB of data exchanged");
                //sinners.Add(ip, bigConnectionsCount);
            }
        }
    }

    public void visitCSF_metricKnownTrackers(CSF_metricKnownTrackers metric)      {
        Console.ForegroundColor = ConsoleColor.DarkMagenta;
        System.Console.WriteLine(metric.Metric);
        Console.ResetColor();

        Dictionary<string, Hashtable> sinnerList = metric.RawValues;

        //We do register if a connection happens only once or more often, but we ignore that..
        foreach (KeyValuePair<string, Hashtable> sourceEntry in sinnerList) {
            System.Console.WriteLine(sourceEntry.Key + " talked to " + sourceEntry.Value.Count + " trackers");
            //metricSinners.Add(sourceEntry.Key, sourceEntry.Value.Count);
        }
    }

    public void visitCSF_metricLocalPeerDiscovery(CSF_metricLocalPeerDiscovery metric) {
        Console.ForegroundColor = ConsoleColor.DarkMagenta;
        System.Console.WriteLine(metric.Metric);
        Console.ResetColor();

        Dictionary<string, int> sinnerList = metric.Sinners;

        //We do register if a connection happens only once or more often, but we ignore that..
        foreach (KeyValuePair<string, int> sourceEntry in sinnerList) {
            System.Console.WriteLine(sourceEntry.Key + " has " + sourceEntry.Value);
            //metricSinners.Add(sourceEntry.Key, sourceEntry.Value.Count);
        }
    }

    public void visitCSF_metricProtocolUPnP(CSF_metricProtocolUPnP metric)       {
        Console.ForegroundColor = ConsoleColor.DarkMagenta;
        System.Console.WriteLine(metric.Metric);
        Console.ResetColor();

        Dictionary<string, int> sinnerList = metric.Sinners;

        //We do register if a connection happens only once or more often, but we ignore that..
        foreach (KeyValuePair<string, int> sourceEntry in sinnerList)
        {
            System.Console.WriteLine(sourceEntry.Key + " done " + sourceEntry.Value + " protocol messages ");
            //metricSinners.Add(sourceEntry.Key, sourceEntry.Value.Count);
        }
    }
}