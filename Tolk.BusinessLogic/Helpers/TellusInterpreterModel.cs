using System;
using System.Collections.Generic;
using System.Text;

namespace Tolk.BusinessLogic.Helpers
{
    public  class TellusInterpreterModel : ITellusResultModel
    {
        public string InterpreterId { get; set; }
        public string GivenName { get; set; }
        public string Surname { get; set; }
        public string Email { get; set; }
        public string Cellphone { get; set; }
        public string County { get; set; }
        public List<TellusInterpreterCompetenceModel> Competences { get; set; }
        public List<TellusInterpreterEducationModel> Educations { get; set; }
    }
}
