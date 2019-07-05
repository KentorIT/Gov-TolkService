using System.Collections.Generic;

namespace Tolk.BusinessLogic.Helpers
{
    public class TellusLanguagesCompetenceInfoResponse
    {
        public int Status { get; set; }

        public IEnumerable<TellusLanguagesInfoModel> Result { get; set; }
    }
}
