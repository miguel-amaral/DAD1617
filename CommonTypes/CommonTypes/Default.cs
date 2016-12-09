using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public class Default {
    int innerCounter = 0;
    int differentCounter = 0;
    List<string> seenTuples = new List<string>();

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

    private bool tuploUnico(IList<string> tuple, int index) {
        string newId = tuple[index];
        if (seenTuples.Contains(newId)) {
            return false;
        } else {
            seenTuples.Add(newId);
            return true;
        }
    }

    private IList<IList<string>> emptyTuple() {
        List<IList<string>> returning = new List<IList<string>>();
        IList<string> newTuple = new List<string>();
        returning.Add(newTuple);
        return returning;
    }

    public IList<IList<string>> Uniq(IList<string> tuple) {
        int index = Int32.Parse(tuple[0]);
        tuple.RemoveAt(0);
        if (tuploUnico(tuple, index)) {
            return Dup(tuple);
        } else {
            return emptyTuple();
        }
    }

    //	FILTER field number, condition, value
    public IList<IList<string>> Filter(IList<string> tuple) {
        int index = Int32.Parse(tuple[0]);
        tuple.RemoveAt(0);
        string condition = tuple[0];
        tuple.RemoveAt(0);
        string staticValue = tuple[0];
        tuple.RemoveAt(0);
        string dinamicValue = tuple[index];

        if (condition.Equals(">")) {
            if (String.Compare(dinamicValue, staticValue) > 0) {
                return Dup(tuple);
            }
        } else if (condition.Equals("<")) {
            if (String.Compare(dinamicValue, staticValue) < 0) {
                return Dup(tuple);
            }
        } else if (condition.Equals("=")) {
            if (String.Compare(dinamicValue, staticValue) == 0) {
                return Dup(tuple);
            }
        }
        return emptyTuple();
    }
    public IList<IList<string>> Output(IList<string> tuple) {

        string outputFile = @tuple[0];
        tuple.RemoveAt(0);

        using (System.IO.StreamWriter file = new System.IO.StreamWriter(outputFile, true)) {
//            foreach (string line in tuple) {
//                // If the line doesn't contain the word 'Second', write the line to the file.
//                //file.WriteLine(line);
//                file.WriteLine(line);
//            }
            file.WriteLine(String.Join(" ",tuple));
        }
        return Dup(tuple);
    }

    public IList<IList<string>> doubleReturn(IList<string> tuple) {
        List<IList<string>> newTuple = new List<IList<string>>();
        newTuple.Add(tuple);
        newTuple.Add(tuple);
        return newTuple;
    }

    public IList<IList<string>> addCounter(IList<string> tuple) {
        int ctr = differentCounter++;
        List<IList<string>> newTuple = new List<IList<string>>();
        tuple.Insert(0, ctr.ToString());
        newTuple.Add(tuple);
        return newTuple;
    }
}
