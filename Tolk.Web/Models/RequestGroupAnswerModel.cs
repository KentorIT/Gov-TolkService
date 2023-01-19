using System;
using Tolk.BusinessLogic.Enums;

namespace Tolk.Web.Models
{
    public class RequestGroupAnswerModel : RequestGroupAcceptModel, IModel
    {
        public InterpreterLocation? InterpreterLocation { get; set; }

        public RadioButtonGroup SetLatestAnswerTimeForCustomer { get; set; }

        public DateTimeOffset? LatestAnswerTimeForCustomer { get; set; }

        public InterpreterAnswerModel InterpreterAnswerModel { get; set; }

        public InterpreterAnswerModel ExtraInterpreterAnswerModel { get; set; }
    }
}
