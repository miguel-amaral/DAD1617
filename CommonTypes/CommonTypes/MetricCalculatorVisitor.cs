using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class MetricCalculatorVisitor : MetricVisitor {

    //IpInName
    private int minimumIpInName = 0;
    private int maximumIpInName = 1;
    //Upload
    private int minimumHighUpload     = 0000000000 * 102 * 1024 * 1024; //100 MB
    private int maximumHighUpload     = 0000000001 * 102 * 1024 * 1024; //100 MB
    //Download
    private int minimumHighDownload   = 0000000000 * 102 * 1024 * 1024; //100 MB
    private int maximumHighDownload   = 0000000001 * 102 * 1024 * 1024; //100 MB
    //SeveralPeersHighData
    private int minimumDataConnection = 0000000000 * 100 * 1024 * 1024; //100MB
    private int minimumNumberConnectionsHighData = 0;
    private int maximumNumberConnectionsHighData = 1;
    //Trackers
    private int minimumTrackers = 0;
    private int maximumTrackers = 1;
    //LocalPeerDiscovery
    private int minimumLocalPeerDiscovery = 0;
    private int maximumLocalPeerDiscovery = 1;
    //UPnP
    private int minimumUPnP = 0;
    private int maximumUPnP = 1;

    private double treshold = 0;
    private Dictionary<string, List<double>> metricTable = new Dictionary<string, List<double>>();

    public MetricCalculatorVisitor() { }

    public void visit(CSF_metric metric)                    { throw new Exception("Merging does not do the general ABSTRACT type.."); }

    public bool visitWithBoolCSF_metricIpInName 			(CSF_metricIpInName metric)            { throw new Exception("Merging does not do bool visit"); }
    public bool visitWithBoolCSF_metricHighUpload 		    (CSF_metricHighUpload metric)          { throw new Exception("Merging does not do bool visit"); }
    public bool visitWithBoolCSF_metricHighDownload 		(CSF_metricHighDownload metric)        { throw new Exception("Merging does not do bool visit"); }
    public bool visitWithBoolCSF_metricHighDataDiffPeers 	(CSF_metricHighDataDiffPeers metric)   { throw new Exception("Merging does not do bool visit"); }
    public bool visitWithBoolCSF_metricKnownTrackers 		(CSF_metricKnownTrackers metric)       { throw new Exception("Merging does not do bool visit"); }
    public bool visitWithBoolCSF_metricLocalPeerDiscovery   (CSF_metricLocalPeerDiscovery metric)  { throw new Exception("Merging does not do bool visit"); }
    public bool visitWithBoolCSF_metricProtocolUPnP         (CSF_metricProtocolUPnP metric)        { throw new Exception("Merging does not do bool visit"); }
    public bool visitWithBool(CSF_metric metric)                    { throw new Exception("Merging does not do the general ABSTRACT type.."); }
    
    public void visitCSF_metricIpInName(CSF_metricIpInName metric) {
        Dictionary<string, Hashtable> sinnerList = metric.RawValues;
        if (DEBUG.METRIC) { 
            Console.ForegroundColor = ConsoleColor.DarkMagenta;
            System.Console.WriteLine(metric.Metric);
            Console.ResetColor();
        }
        //We do register if a connection happens only once or more often, but we ignore that..
        foreach (KeyValuePair<string, Hashtable> sourceEntry in sinnerList) {
            double metricValue = percentage(minimumIpInName, maximumIpInName, sourceEntry.Value.Count);
            if (metricValue > 0) {
                if (DEBUG.METRIC) {
                    System.Console.WriteLine(sourceEntry.Key + " talked to " + sourceEntry.Value.Count + " domestic IPs");
                }
                this.addEntry(sourceEntry.Key, metric.Metric, metricValue);
            }
        }
    }

    public void visitCSF_metricHighUpload(CSF_metricHighUpload metric) {
        if (DEBUG.METRIC) {
            Console.ForegroundColor = ConsoleColor.DarkMagenta;
            System.Console.WriteLine("\r\n" + metric.Metric);
            Console.ResetColor();
        }

        Dictionary<string, Hashtable> connections = metric.RawValues;

        foreach (KeyValuePair<string, Hashtable> sourceEntry in connections) {
            string ip = sourceEntry.Key;
            Hashtable table = sourceEntry.Value;
            int uploadSize = 0;
            foreach (DictionaryEntry pair in table) {
                uploadSize += (int)pair.Value;
            }
            double metricValue = percentage(minimumHighUpload,maximumHighUpload, uploadSize);
            if (metricValue > 0) {
                if (DEBUG.METRIC) {
                    System.Console.WriteLine(ip + " uploaded " + uploadSize + ", triggered when more than " + minimumHighUpload + " MB of data uploaded : value " + metricValue);
                }
                this.addEntry(ip, metric.Metric, metricValue);
            }
        }
    }

    public void visitCSF_metricHighDownload(CSF_metricHighDownload metric) {
        if (DEBUG.METRIC) {
            Console.ForegroundColor = ConsoleColor.DarkMagenta;
            System.Console.WriteLine("\r\n" + metric.Metric);
            Console.ResetColor();
        }

        Dictionary<string, Hashtable> connections = metric.RawValues;

        foreach (KeyValuePair<string, Hashtable> sourceEntry in connections) {
            string ip = sourceEntry.Key;
            Hashtable table = sourceEntry.Value;
            int downloadSize = 0;
            foreach (DictionaryEntry pair in table) {
                downloadSize += (int)pair.Value;
            }
            double metricValue = percentage(minimumHighDownload, maximumHighDownload, downloadSize);
            if (metricValue > 0) {
                if (DEBUG.METRIC) {
                    System.Console.WriteLine(ip + " download " + downloadSize + ", triggered when more than " + minimumHighDownload + " MB of data uploaded");
                }
                this.addEntry(ip, metric.Metric, metricValue);
            }
        }
    }

    public void visitCSF_metricHighDataDiffPeers(CSF_metricHighDataDiffPeers metric)  {
        if (DEBUG.METRIC) {
            Console.ForegroundColor = ConsoleColor.DarkMagenta;
            System.Console.WriteLine("\r\n" + metric.Metric);
            Console.ResetColor();
        }

        Dictionary<string, Hashtable> connections = metric.RawValues;

        foreach (KeyValuePair<string, Hashtable> sourceEntry in connections) {
            string ip = sourceEntry.Key;
            Hashtable table = sourceEntry.Value;
            int bigConnectionsCount = 0;
            foreach (DictionaryEntry pair in table) {
                if ((int)pair.Value > minimumDataConnection) {
                    bigConnectionsCount++;
                }
            }
            
            double metricValue = percentage(minimumNumberConnectionsHighData, maximumNumberConnectionsHighData, bigConnectionsCount);
            if (metricValue > 0) {
                if (DEBUG.METRIC) {
                    System.Console.WriteLine(ip + " talked to " + bigConnectionsCount + " IPs with more than " + minimumDataConnection + " of MB of data exchanged");
                }
                this.addEntry(ip, metric.Metric, metricValue);
            }
        }
    }

    public void visitCSF_metricKnownTrackers(CSF_metricKnownTrackers metric) {
        if (DEBUG.METRIC) {
            Console.ForegroundColor = ConsoleColor.DarkMagenta;
            System.Console.WriteLine("\r\n" + metric.Metric);
            Console.ResetColor();
        }

        Dictionary<string, Hashtable> sinnerList = metric.RawValues;
        if (sinnerList == null)
            System.Console.WriteLine("null");

        //We do register if a connection happens only once or more often, but we ignore that..
        foreach (KeyValuePair<string, Hashtable> sourceEntry in sinnerList) {
            double metricValue = percentage(minimumTrackers, maximumTrackers, sourceEntry.Value.Count);
            if (metricValue > 0) {
                if (DEBUG.METRIC) {
                    System.Console.WriteLine(sourceEntry.Key + " talked to " + sourceEntry.Value.Count + " trackers");
                }
                this.addEntry(sourceEntry.Key, metric.Metric, metricValue);
            }
        }
    }

    public void visitCSF_metricLocalPeerDiscovery(CSF_metricLocalPeerDiscovery metric) {
        if (DEBUG.METRIC) {
            Console.ForegroundColor = ConsoleColor.DarkMagenta;
            System.Console.WriteLine("\r\n" + metric.Metric);
            Console.ResetColor();
        }
        Dictionary<string, int> sinnerList = metric.Sinners;
        if(sinnerList == null)
            System.Console.WriteLine("null");

        //We do register if a connection happens only once or more often, but we ignore that..
        foreach (KeyValuePair<string, int> sourceEntry in sinnerList) {
            double metricValue = percentage(minimumLocalPeerDiscovery, maximumLocalPeerDiscovery, sourceEntry.Value);
            if (metricValue > 0) {
                if (DEBUG.METRIC) {
                    System.Console.WriteLine(sourceEntry.Key + " done local peer discovery " + sourceEntry.Value + " times");
                }
                this.addEntry(sourceEntry.Key, metric.Metric, metricValue);
            }
        }
    }

    public void visitCSF_metricProtocolUPnP(CSF_metricProtocolUPnP metric)       {
        if (DEBUG.METRIC) { 
            Console.ForegroundColor = ConsoleColor.DarkMagenta;
            System.Console.WriteLine("\r\n" + metric.Metric);
            Console.ResetColor();
        }
        Dictionary<string, int> sinnerList = metric.Sinners;

        //We do register if a connection happens only once or more often, but we ignore that..
        foreach (KeyValuePair<string, int> sourceEntry in sinnerList) {
            double metricValue = percentage(minimumUPnP, maximumUPnP, sourceEntry.Value);
            if (metricValue > 0) {
                if (DEBUG.METRIC) {
                    System.Console.WriteLine(sourceEntry.Key + " done " + sourceEntry.Value + " protocol messages ");
                }
                this.addEntry(sourceEntry.Key, metric.Metric, metricValue);
            }
        }
    }

    private void addEntry(string ip, string metricName, double value) {
        int columnIndex;
        switch (metricName) {
            case "CSF_IpInName"             : columnIndex = 0; break;
            case "CSF_HighUpload"           : columnIndex = 1; break;
            case "CSF_HighDownload"         : columnIndex = 2; break;
            case "CSF_HighDataDiffPeers"    : columnIndex = 3; break;
            case "CSF_KnownTrackers"        : columnIndex = 4; break;
            case "CSF_LocalPeerDiscovery"   : columnIndex = 5; break;
            case "CSF_ProtocolUPnP"         : columnIndex = 6; break;
            default: System.Console.WriteLine(metricName + " NOT FOUND "); return;
        }

        List<double> variousMetrics;
        if(!metricTable.TryGetValue(ip, out variousMetrics)) {
            //New guy here
            variousMetrics = new List<double>();
            for(int i = 0; i < 7; i++) {
                variousMetrics.Add(0);
            }
            metricTable.Add(ip,variousMetrics);
        }
        variousMetrics[columnIndex] = value;
    }

    public void printMetric() {
        String toPrint = String.Format(" ");
        int fields = 8; //Excluding ip
        for(int i = 0; i < (15 + 8*fields + fields*3 + 2); i++) {
            toPrint += "_";
        }
        toPrint += String.Format("\r\n| {0,15} | {1,8} | {2,8} | {3,8} | {4,8} | {5,8} | {6,8} | {7,8} | {8,8} |\r\n", "Sinner's IP", "Sinner %","IpNam", "Up(GB)", "Down(GB)", "Peers", "Track", "Local", "UPnP");
        foreach (KeyValuePair<string, List<double>> pair in metricTable) {
            string ip = pair.Key;
            List<double> metricsValues = pair.Value;
            double totalValue = calculateHeuristic(metricsValues);
            if(totalValue > treshold )
                toPrint += String.Format("| {0,15} | {1,8:P1} | {2,8:N6} | {3,8:N6} | {4,8:N6} | {5,8:N6} | {6,8:N6} | {7,8:N6} | {8,8:N6} |\r\n", ip, totalValue,metricsValues[0], metricsValues[1], metricsValues[2], metricsValues[3], metricsValues[4], metricsValues[5], metricsValues[6]);
        }
        toPrint += "|";
        for (int i = 0; i < (15 + 8 * fields + fields * 3 + 2); i++) {
                toPrint += "_";
        }
        toPrint += "|";
        System.Console.WriteLine(toPrint);
    }

    private double calculateHeuristic(List<double> metricas) {
        this.treshold = 0; //MAX 1
        List<double> weight = new List<double>(new double[] { 0.142 //CSF_IpInName"             
                                                            , 0.142 //CSF_HighUpload"           
                                                            , 0.142 //CSF_HighDownload"         
                                                            , 0.142 //CSF_HighDataDiffPeers"    
                                                            , 0.142 //CSF_KnownTrackers"        
                                                            , 0.142 //CSF_LocalPeerDiscovery"   
                                                            , 0.142 //CSF_ProtocolUPnP"         
        });

        double final = 0;
        for (int index = 0; index < weight.Count; index++) { 
            final += weight[index] * metricas[index];
        }
        return final;
    }

    private double percentage(int min, int max, int value) {
        if (value > min) {
            if (value > max) {
                if (DEBUG.METRIC) {
                    System.Console.WriteLine("metricValue: " + 1);
                }
                return 1;
            }
            int dividend = (value - min);
            double metric = (double)  dividend/ max;
            if (DEBUG.METRIC)             {
                System.Console.WriteLine("metricValue: " + metric);
            }

            return metric; 
        }
        if (DEBUG.METRIC) {
            System.Console.WriteLine("metricValue: " + 0);
        }
        return 0;
    }

}