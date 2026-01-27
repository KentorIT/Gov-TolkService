using System;
using System.ComponentModel.DataAnnotations;
using Tolk.Web.Helpers;

namespace Tolk.Web.Models.AccountViewModels
{
    public class TwoFactorModel
    {
        [Required(ErrorMessage = "Valideringskod måste anges")]
        [NoAutoComplete]
        [Display(Name = "Valideringskod", Description = "Koden skickas till din registrerade e‑postadress.\nOm du inte ser den inom ett par minuter, kontrollera gärna din skräppostmapp. Om koden fortfarande inte har kommit inom fem minuter, testa att skicka efter en ny kod.\nOm det fortfarande inte fungerar, kontakta vår support för hjälp. Se kontaktuppgifter på botten av sidan.")]
        public string Code { get; set; }
        public Uri ReturnUrl { get; set; }
        public string Message { get; set; }
    }
}
