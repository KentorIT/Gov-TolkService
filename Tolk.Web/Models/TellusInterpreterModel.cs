using System.Collections.Generic;

namespace Tolk.Web.Models
{
    public class TellusInterpreterModel
    {
        public string InterpreterId { get; set; }

        public string Name { get; set; }

        public string Email { get; set; }

        public string Cellphone { get; set; }

        public List<TellusCompetenceModel> Competences { get; set; }
    }
}
