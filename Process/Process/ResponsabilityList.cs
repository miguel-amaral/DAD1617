using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DADStormProcess {
    [Serializable]
    public class DictionaryList<T> {

        private List<DictionaryElement> differentDictionary = new List<DictionaryElement>();

        public DictionaryList(Dictionary<string, T> dictionary) {
            foreach (KeyValuePair<string, T> entry in dictionary) {
                differentDictionary.Add(new DictionaryElement(entry.Key, entry.Value));
            }
        }

        public Dictionary<string, T> getDictionary() {
            Dictionary<string, T> toReturn = new Dictionary<string, T>();
            foreach (DictionaryElement element in differentDictionary) {
                toReturn[element.getKey()] = element.getElement();
            }
            return toReturn;
        }

        [Serializable]
        class DictionaryElement {
            private string key;
            private T element;
            public DictionaryElement(string key, T element) {
                this.key = key;
                this.element = element;

            }
            public string getKey() { return key; }
            public T getElement() { return element; }
        }
    }

    [Serializable]
    public class brotherIDsResponsibleDictionaryList : DictionaryList<ConnectionPack> {
        public brotherIDsResponsibleDictionaryList (Dictionary<string, ConnectionPack> dictionary) : base(dictionary) { }
    }
    [Serializable]
    public class idTranslationDictionaryList : DictionaryList<string> {
        public idTranslationDictionaryList(Dictionary<string, string> dictionary) : base(dictionary) { }
    }
    [Serializable]
    public class responsabilityDictionaryList : DictionaryList<IList<IList<string>>> {
        public responsabilityDictionaryList(Dictionary<string, IList<IList<string>>> dictionary) : base(dictionary) { }
    }
    [Serializable]
    public class responsabilityLinksDictionaryList : DictionaryList<List<string>> {
        public responsabilityLinksDictionaryList(Dictionary<string, List<string>> dictionary) : base(dictionary) { }
    }





   
        
        
}
