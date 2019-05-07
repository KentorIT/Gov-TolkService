using System.Collections.Generic;

namespace Tolk.BusinessLogic.Helpers
{
    public class TellusInterpreterModel
    {
        public string InterpreterId { get; set; }
        public string Givenname { get; set; }
        public string Surname { get; set; }
        public string Email { get; set; }
        public string Cellphone { get; set; }
        public string Homephone { get; set; }
        public string Otherphone { get; set; }
        public string County { get; set; }
        public IEnumerable<TellusInterpreterCompetenceModel> Competences { get; set; }
        public IEnumerable<TellusInterpreterEducationModel> Educations { get; set; }
    }
}
