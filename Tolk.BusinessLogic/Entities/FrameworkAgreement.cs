﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Tolk.BusinessLogic.Enums;

namespace Tolk.BusinessLogic.Entities
{
    public class FrameworkAgreement
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int FrameworkAgreementId { get; set; }

        [Required]
        [MaxLength(50)]
        public string AgreementNumber { get; set; }

        [MaxLength(255)]
        public string Description { get; set; }

        [Column(TypeName = "date")]
        public DateTime FirstValidDate { get; set; }

        [Column(TypeName = "date")]
        public DateTime LastValidDate { get; set; }
        [Column(TypeName = "date")]
        public DateTime OriginalLastValidDate { get; set; }
        public int PossibleAgreementExtensionsInMonths { get; set; }

        public BrokerFeeCalculationType BrokerFeeCalculationType { get; set; }

        public FrameworkAgreementResponseRuleset FrameworkAgreementResponseRuleset { get; set; }

        #region navigation properites

        public List<Ranking> Rankings { get; private set; }

        //Probably Orders as well
    }

    #endregion
}
