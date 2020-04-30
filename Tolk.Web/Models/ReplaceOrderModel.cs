using AutoMapper;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Helpers;
using Tolk.Web.Services;

namespace Tolk.Web.Models
{
    [AutoMap(typeof(OrderModel))]
    public class ReplaceOrderModel : OrderBaseModel
    {
        /* 
        ContactPersonId
        Files
         */
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Used in razor view")]
        public List<FileModel> Files { get; set; }

        public Guid? FileGroupKey { get; set; }

        public long? CombinedMaxSizeAttachments { get; set; }

        [Display(Name = "Rätt att granska rekvisition", Description = "Välj vid behov en annan person som skall ges rätt att granska rekvisition, t ex person som deltar vid tolktillfället. Denna uppgift kan du även komplettera eller ändra senare.")]
        public int? ContactPersonId { get; set; }

        [Display(Name = "Uppdragstyp")]
        public AssignmentType AssignmentType { get; set; }

        [Display(Name = "Ersätter BokningsID")]
        public string ReplacingOrderNumber { get; set; }

        public int ReplacingOrderId { get; set; }

        [Display(Name = "Det ersatta uppdragets datum och tid")]
        public TimeRange ReplacedTimeRange { get; set; }

        public string CancelMessage { get; set; }

        [Display(Name = "Datum och tid för ersättning", Description = "Datum och tid för tolkuppdraget.")]
        [StayWithinOriginalRange(ErrorMessage = "Uppdraget måste ske inom tiden för det ersatta uppdraget", OtherRangeProperty = nameof(ReplacedTimeRange))]
        public TimeRange TimeRange { get; set; }

        internal static ReplaceOrderModel GetModelFromOrder(Order order, string cancelMessage, string brokerName, bool useAttachments)
        {
            var model = new ReplaceOrderModel
            {
                BrokerName = brokerName,
                AllowExceedingTravelCost = GetAllowExceedingTravelCost(order),
                Status = order.Status,
                AssignmentType = order.AssignmentType,
                ReplacingOrderId = order.OrderId,
                RegionName = order.Region.Name,
                CreatedBy = order.ContactInformation,
                ContactPersonId = order.ContactPersonId,
                CreatedAt = order.CreatedAt,
                InvoiceReference = order.InvoiceReference,
                LanguageName = order.OtherLanguage ?? order.Language?.Name,
                CompetenceLevelDesireType = new RadioButtonGroup
                {
                    SelectedItem = order.SpecificCompetenceLevelRequired
                    ? SelectListService.DesireTypes.Single(item => EnumHelper.Parse<DesireType>(item.Value) == DesireType.Requirement)
                    : SelectListService.DesireTypes.Single(item => EnumHelper.Parse<DesireType>(item.Value) == DesireType.Request)
                },
                CustomerUnitName = order.CustomerUnit?.Name,
                CustomerReferenceNumber = order.CustomerReferenceNumber,
                IsCreatorInterpreterUser = order.CreatorIsInterpreterUser,
                LanguageHasAuthorizedInterpreter = order.LanguageHasAuthorizedInterpreter,
                ReplacedTimeRange = new TimeRange
                {
                    StartDateTime = order.StartAt,
                    EndDateTime = order.EndAt
                },
                ReplacingOrderNumber = order.OrderNumber,
                CancelMessage = cancelMessage,
                TimeRange = new TimeRange
                {
                    StartDateTime = order.StartAt,
                    EndDateTime = order.EndAt
                },
                Description = order.Description,
                UnitName = order.UnitName,
                UseAttachments = useAttachments
            };
            return model;
        }
        internal void UpdateOrder(Order order, DateTimeOffset startAt, DateTimeOffset endAt, bool useAttachments)
        {
            order.ReplacingOrderId = ReplacingOrderId;
            order.CustomerReferenceNumber = CustomerReferenceNumber;
            order.StartAt = startAt;
            order.EndAt = endAt;
            order.Description = Description;
            order.UnitName = UnitName;
            order.ContactPersonId = ContactPersonId;
            order.Attachments = useAttachments ? Files?.Select(f => new OrderAttachment { AttachmentId = f.Id }).ToList() : null;
            order.InvoiceReference = InvoiceReference;
            //mealbreak is already set from original order but set it to false if replace occasion time is not more than four hours
            order.MealBreakIncluded = (order.MealBreakIncluded ?? false) && ((int)(endAt.DateTime - startAt.DateTime).TotalMinutes > 240);
            // need to be able to change the locations after getting the replaced order´s information copied...
            order.InterpreterLocations.Clear();
            order.InterpreterLocations.Add(RankedInterpreterLocationFirstAddressModel.GetInterpreterLocation(RankedInterpreterLocationFirst.Value, 1));
            if (RankedInterpreterLocationSecond.HasValue)
            {
                order.InterpreterLocations.Add(RankedInterpreterLocationSecondAddressModel.GetInterpreterLocation(RankedInterpreterLocationSecond.Value, 2));
                if (RankedInterpreterLocationThird.HasValue)
                {
                    order.InterpreterLocations.Add(RankedInterpreterLocationThirdAddressModel.GetInterpreterLocation(RankedInterpreterLocationThird.Value, 3));
                }
            }
            // OrderCompetenceRequirements
            //set OtherInterpreter as a requirement for languages that lacks authorized interpreters
            if (LanguageHasAuthorizedInterpreter.HasValue && !LanguageHasAuthorizedInterpreter.Value)
            {
                order.SpecificCompetenceLevelRequired = true;
                order.CompetenceRequirements.Add(new OrderCompetenceRequirement
                {
                    CompetenceLevel = CompetenceAndSpecialistLevel.OtherInterpreter
                });
            }
            else
            {
                if (RequestedCompetenceLevels.Any())
                {
                    // Counting rank for cases where e.g. first option is undefined, but second is defined
                    int rank = 0;
                    if (RequestedCompetenceLevelFirst.HasValue)
                    {
                        order.CompetenceRequirements.Add(new OrderCompetenceRequirement
                        {
                            CompetenceLevel = RequestedCompetenceLevelFirst.Value,
                            Rank = ++rank
                        });
                    }
                    if (RequestedCompetenceLevelSecond.HasValue)
                    {
                        order.CompetenceRequirements.Add(new OrderCompetenceRequirement
                        {
                            CompetenceLevel = RequestedCompetenceLevelSecond.Value,
                            Rank = ++rank
                        });
                    }
                }
            }
        }

        private static RadioButtonGroup GetAllowExceedingTravelCost(Order order)
            => new RadioButtonGroup { SelectedItem = SelectListService.AllowExceedingTravelCost.SingleOrDefault(e => e.Value == order.AllowExceedingTravelCost?.ToString()) };
    }
}
