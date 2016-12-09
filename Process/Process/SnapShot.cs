using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DADStormProcess {
    [Serializable]
    public class SnapShot {


        public brotherIDsResponsibleDictionaryList brotherIDsResponsible;
        public idTranslationDictionaryList idTranslation;
        public responsabilityDictionaryList responsability;
        public responsabilityLinksDictionaryList responsabilityLinks;
        public List<string> processedIDs;

        public SnapShot(Dictionary<string, IList<IList<string>>> responsability, Dictionary<string, string> idTranslation, Dictionary<string, List<string>> responsabilityLinks, Dictionary<string, ConnectionPack> brotherIDsResponsible, List<string> processedIDs) {
            this.brotherIDsResponsible = new brotherIDsResponsibleDictionaryList(brotherIDsResponsible);
            this.idTranslation          = new idTranslationDictionaryList (idTranslation);
            this.responsability = new responsabilityDictionaryList(responsability);
            this.responsabilityLinks = new responsabilityLinksDictionaryList(responsabilityLinks);
            this.processedIDs = new List<string>( processedIDs);
        }
    }
}
