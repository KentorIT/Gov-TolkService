using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tolk.BusinessLogic.Entities
{
    public class Region
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int RegionId { get; set; }

        [Required]
        [MaxLength(20)]
        public string Name { get; set; }

        public List<Ranking> Rankings { get; private set; }

        #region foreign keys

        public int RegionGroupId { get; set; }

        [ForeignKey(nameof(RegionGroupId))]
        public RegionGroup RegionGroup { get; set; }

        #endregion

        #region data

        public static Region[] Regions { get; } = new[]
        {
                new Region { RegionId = 1, Name = "Stockholm", RegionGroupId = 1 },
                new Region { RegionId = 2, Name = "Uppsala", RegionGroupId = 1 },
                new Region { RegionId = 3, Name = "Södermanland", RegionGroupId = 1 },
                new Region { RegionId = 4, Name = "Östergötland", RegionGroupId = 3 },
                new Region { RegionId = 5, Name = "Jönköping" , RegionGroupId = 3 },
                new Region { RegionId = 6, Name = "Kronoberg" , RegionGroupId = 3 },
                new Region { RegionId = 7, Name = "Kalmar" , RegionGroupId = 3 },
                new Region { RegionId = 80, Name = "Gotland", RegionGroupId = 3 },
                new Region { RegionId = 8, Name = "Blekinge ", RegionGroupId = 3 },
                new Region { RegionId = 25, Name = "Skåne", RegionGroupId = 1 },
                new Region { RegionId = 11, Name = "Halland", RegionGroupId = 3 },
                new Region { RegionId = 13, Name = "Västra Götaland", RegionGroupId = 1 },
                new Region { RegionId = 15, Name = "Värmland", RegionGroupId = 3 },
                new Region { RegionId = 16, Name = "Örebro", RegionGroupId = 3 },
                new Region { RegionId = 17, Name = "Västmanland", RegionGroupId = 3 },
                new Region { RegionId = 18, Name = "Dalarna", RegionGroupId = 2 },
                new Region { RegionId = 19, Name = "Gävleborg", RegionGroupId = 3 },
                new Region { RegionId = 20, Name = "Västernorrland", RegionGroupId = 2 },
                new Region { RegionId = 21, Name = "Jämtland", RegionGroupId = 2 },
                new Region { RegionId = 22, Name = "Västerbotten", RegionGroupId = 2 },
                new Region { RegionId = 23, Name = "Norrbotten", RegionGroupId = 3 }
        };

        #endregion
    }
}
