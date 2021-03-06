﻿using System;
using System.ComponentModel.DataAnnotations;
using Tolk.BusinessLogic.Entities;

namespace Tolk.Web.Models
{
    public class InterpreterModel : IModel
    {
        public int? Id { get; set; }

        [Display(Name = "Namn")]
        public string FullName => $"{FirstName} {LastName}";

        [Required]
        [EmailAddress(ErrorMessage = "Felaktig e-postadress")]
        [RegularExpression(@"^[\w!#$%&'*+\-/=?\^_`{|}~]+(\.[\w!#$%&'*+\-/=?\^_`{|}~]+)*@((([\-\w]+\.)+[a-zA-Z]{2,4})|(([0-9]{1,3}\.){3}[0-9]{1,3}))$", ErrorMessage = "Felaktig e-postadress")]
        [Display(Name = "E-post")]
        [StringLength(255)]
        public string Email { get; set; }

        [Required]
        [Display(Name = "Förnamn")]
        [StringLength(255)]
        public string FirstName { get; set; }

        [Required]
        [Display(Name = "Efternamn")]
        [StringLength(255)]
        public string LastName { get; set; }

        [Required]
        [Display(Name = "Telefonnummer")]
        [StringLength(32)]
        public string PhoneNumber { get; set; }

        [Display(Name = "Kammarkollegiets tolknummer")]
        [StringLength(32)]
        public string OfficialInterpreterId { get; set; }

        [Display(Name = "Aktiv")]
        public bool IsActive { get; set; }

        internal static InterpreterModel GetModelFromInterpreter(InterpreterBroker interpreter)
        {
            return new InterpreterModel
            {
                Id = interpreter?.InterpreterBrokerId,
                Email = interpreter.Email,
                FirstName = interpreter.FirstName,
                LastName = interpreter.LastName,
                PhoneNumber = interpreter.PhoneNumber,
                OfficialInterpreterId = interpreter.OfficialInterpreterId,
                IsActive = interpreter.IsActive
            };
        }
        internal void UpdateAndChangeStatusInterpreter(InterpreterBroker interpreter, int? userId, int? impersonatorId, DateTimeOffset? inactivatedAt)
        {
            UpdateInterpreter(interpreter);
            interpreter.InactivatedAt = inactivatedAt;
            interpreter.InactivatedBy = userId;
            interpreter.ImpersonatingInactivatedBy = impersonatorId;
            interpreter.IsActive = IsActive;
        }

        internal void UpdateInterpreter(InterpreterBroker interpreter)
        {
            interpreter.Email = Email;
            interpreter.FirstName = FirstName;
            interpreter.LastName = LastName;
            interpreter.PhoneNumber = PhoneNumber;
            interpreter.OfficialInterpreterId = OfficialInterpreterId;
        }
    }
}
