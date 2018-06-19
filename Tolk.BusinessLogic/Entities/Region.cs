using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

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

        public static Region[] Regions { get; } = new[]
        {
                new Region { RegionId = 1, Name = "Stockholm" },
                new Region { RegionId = 2, Name = "Uppsala" },
                new Region { RegionId = 3, Name = "Södermanland" },
                new Region { RegionId = 4, Name = "Östergötland" },
                new Region { RegionId = 5, Name = "Jönköping" },
                new Region { RegionId = 6, Name = "Kronoberg" },
                new Region { RegionId = 7, Name = "Kalmar" },
                new Region { RegionId = 80, Name = "Gotland" },
                new Region { RegionId = 8, Name = "Blekinge " },
                new Region { RegionId = 25, Name = "Skåne" },
                new Region { RegionId = 11, Name = "Halland" },
                new Region { RegionId = 13, Name = "Västra Götaland" },
                new Region { RegionId = 15, Name = "Värmland" },
                new Region { RegionId = 16, Name = "Örebro" },
                new Region { RegionId = 17, Name = "Västmanland" },
                new Region { RegionId = 18, Name = "Dalarna" },
                new Region { RegionId = 19, Name = "Gävleborg" },
                new Region { RegionId = 20, Name = "Västernorrland" },
                new Region { RegionId = 21, Name = "Jämtland Härjedalen" },
                new Region { RegionId = 22, Name = "Västerbotten" },
                new Region { RegionId = 23, Name = "Norrbotten" }
        };
    }
}
