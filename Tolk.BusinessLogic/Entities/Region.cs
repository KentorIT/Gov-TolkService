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
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string Name { get; set; }

        public static Region[] Regions { get; } = new[]
        {
                new Region { Id = 1, Name = "Stockholm" },
                new Region { Id = 2, Name = "Uppsala" },
                new Region { Id = 3, Name = "Södermanland" },
                new Region { Id = 4, Name = "Östergötland" },
                new Region { Id = 5, Name = "Jönköping" },
                new Region { Id = 6, Name = "Kronoberg" },
                new Region { Id = 7, Name = "Kalmar" },
                new Region { Id = 80, Name = "Gotland" },
                new Region { Id = 8, Name = "Blekinge " },
                new Region { Id = 25, Name = "Skåne" },
                new Region { Id = 11, Name = "Halland" },
                new Region { Id = 13, Name = "Västra Götaland" },
                new Region { Id = 15, Name = "Värmland" },
                new Region { Id = 16, Name = "Örebro" },
                new Region { Id = 17, Name = "Västmanland" },
                new Region { Id = 18, Name = "Dalarna" },
                new Region { Id = 19, Name = "Gävleborg" },
                new Region { Id = 20, Name = "Västernorrland" },
                new Region { Id = 21, Name = "Jämtland Härjedalen" },
                new Region { Id = 22, Name = "Västerbotten" },
                new Region { Id = 23, Name = "Norrbotten" }
        };
    }
}
