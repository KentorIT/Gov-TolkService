using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Helpers;
using Tolk.Web.Helpers;

namespace Tolk.Web.Models
{
    public class InterpreterAcceptModel
    {
        [ClientRequired]
        [Display(Name = "Tolkens kompetensnivå")]
        public CompetenceAndSpecialistLevel? InterpreterCompetenceLevel { get; set; }

        public List<RequestRequirementAnswerModel> RequiredRequirementAnswers { get; set; }

        public List<RequestRequirementAnswerModel> DesiredRequirementAnswers { get; set; }
        public InterpreterAcceptDto AcceptDto
        {
            get
            {
                var requirementAnswers = RequiredRequirementAnswers?.Select(ra => new OrderRequirementRequestAnswer
                {
                    OrderRequirementId = ra.OrderRequirementId,
                    Answer = ra.Answer,
                    CanSatisfyRequirement = ra.CanMeetRequirement
                }).ToList() ?? new List<OrderRequirementRequestAnswer>();

                if (DesiredRequirementAnswers != null)
                {
                    requirementAnswers.AddRange(DesiredRequirementAnswers.Select(ra => new OrderRequirementRequestAnswer
                    {
                        OrderRequirementId = ra.OrderRequirementId,
                        Answer = ra.Answer,
                        CanSatisfyRequirement = ra.CanMeetRequirement
                    }).ToList());
                }

                return new InterpreterAcceptDto
                {
                    CompetenceLevel = InterpreterCompetenceLevel,
                    RequirementAnswers = requirementAnswers
                };
            }
        }
    }
}
