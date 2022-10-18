using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tolk.BusinessLogic.Entities
{
    public class RegionGroup
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int RegionGroupId { get; set; }

        [Required]
        [MaxLength(20)]
        public string Name { get; set; }

        #region Navigation properties

        public List<Region> Regions { get;  set; }

        public List<BrokerFeeByServiceTypePriceListRow> BrokerFeeByServiceTypePriceListRows { get; private set; }

        #endregion

        #region data

        public static RegionGroup[] RegionGroups { get; } = new[]
        {
                new RegionGroup { RegionGroupId = 1, Name = "Storstadsregioner" },
                new RegionGroup { RegionGroupId = 2, Name = "Norra mellansverige" },
                new RegionGroup { RegionGroupId = 3, Name = "Övriga" }
        };

        #endregion
    }
}
