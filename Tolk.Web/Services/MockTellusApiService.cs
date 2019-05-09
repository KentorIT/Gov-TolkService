using System;
using System.Collections.Generic;
using System.Linq;
using Tolk.BusinessLogic.Helpers;

namespace Tolk.Web.Services
{
    public class MockTellusApiService
    {
        private readonly List<TellusInterpreterModel> _interpreterModels = new List<TellusInterpreterModel>
        {
            new TellusInterpreterModel
            {
                InterpreterId = "90000",
                Givenname = "Tolk",
                Surname = "Tolksson",
                Email = "tolk.tolksson@hetbrev.se",
                Cellphone = "+4612345678910",
                Educations = Enumerable.Empty<TellusInterpreterEducationModel>(),
                Competences = new List<TellusInterpreterCompetenceModel>
                {
                    new TellusInterpreterCompetenceModel
                    {
                        Language = "sqi",
                        Competencelevel = new TellusCompetenceLevel{ Id= "1", Value = ""},
                        ValidFrom = null,
                        ValidTo = new DateTime(2020, 3, 15)
                    },
                    new TellusInterpreterCompetenceModel
                    {
                        Language = "sqi",
                        Competencelevel = new TellusCompetenceLevel{ Id= "2", Value = ""},
                        ValidFrom = null,
                        ValidTo = new DateTime(2020, 3, 15)
                    },
                    new TellusInterpreterCompetenceModel
                    {
                        Language = "sqi",
                        Competencelevel = new TellusCompetenceLevel{ Id= "3", Value = ""},
                        ValidFrom = null,
                        ValidTo = new DateTime(2020, 3, 15)
                    },
                    new TellusInterpreterCompetenceModel
                    {
                        Language = "bos,hrv,srp",
                        Competencelevel = new TellusCompetenceLevel{ Id= "1", Value = ""},
                        ValidFrom = null,
                        ValidTo = new DateTime(2020, 3, 15)
                    },
                    new TellusInterpreterCompetenceModel
                    {
                        Language = "mkd",
                        Competencelevel = new TellusCompetenceLevel{ Id= "1", Value = ""},
                        ValidFrom = null,
                        ValidTo = new DateTime(2020, 3, 15)
                    }
                }
            },
            new TellusInterpreterModel
            {
                InterpreterId = "90001",
                Givenname = "Test",
                Surname = "Testmeyer",
                Email = "test.testmeyer@hetbrev.se",
                Cellphone = "+4612345678910",
                Educations = Enumerable.Empty<TellusInterpreterEducationModel>(),
                Competences = new List<TellusInterpreterCompetenceModel>
                {
                    new TellusInterpreterCompetenceModel
                    {
                        Language = "nld",
                        Competencelevel = new TellusCompetenceLevel{ Id= "1", Value = ""},
                        ValidFrom  = new DateTime(2014, 3, 15),
                        ValidTo = new DateTime(2019, 3, 15)
                    },
                    new TellusInterpreterCompetenceModel
                    {
                        Language = "deu",
                        Competencelevel = new TellusCompetenceLevel{ Id= "1", Value = ""},
                        ValidFrom = new DateTime(2017, 3, 15),
                        ValidTo = new DateTime(2022, 3, 15)
                    },
                }
            },
            new TellusInterpreterModel
            {
                InterpreterId = "90002",
                Givenname = "John",
                Surname = "Doe",
                Email = "john.doe@hetbrev.se",
                Cellphone = "+4612345678910",
                Educations = Enumerable.Empty<TellusInterpreterEducationModel>(),
                Competences = new List<TellusInterpreterCompetenceModel>
                {
                    new TellusInterpreterCompetenceModel
                    {
                        Language = "gre",
                        Competencelevel = new TellusCompetenceLevel{ Id= "1", Value = ""},
                        ValidFrom = new DateTime(2014, 3, 15),
                        ValidTo = new DateTime(2019, 3, 15)
                    },
                    new TellusInterpreterCompetenceModel
                    {
                        Language = "gre",
                        Competencelevel = new TellusCompetenceLevel{ Id= "3", Value = ""},
                        ValidFrom = new DateTime(2017, 3, 15),
                        ValidTo = new DateTime(2022, 3, 15)
                    },
                }
            },
            new TellusInterpreterModel
            {
                InterpreterId = "90003",
                Givenname = "Carl",
                Surname = "Hamilton",
                Email = "carl.hamilton@hetbrev.se",
                Cellphone = "+4612345678910",
                Educations = Enumerable.Empty<TellusInterpreterEducationModel>(),
                Competences = new List<TellusInterpreterCompetenceModel>
                {
                    new TellusInterpreterCompetenceModel
                    {
                        Language = "rus",
                        Competencelevel = new TellusCompetenceLevel{ Id= "1", Value = ""},
                        ValidFrom = new DateTime(2014, 3, 15),
                        ValidTo = new DateTime(2020, 3, 15)
                    },
                    new TellusInterpreterCompetenceModel
                    {
                        Language = "rus",
                        Competencelevel = new TellusCompetenceLevel{ Id= "2", Value = ""},
                        ValidFrom = new DateTime(2014, 3, 15),
                        ValidTo = new DateTime(2022, 3, 15)
                    },
                }
            },
            new TellusInterpreterModel
            {
                InterpreterId = "90004",
                Givenname = "Edu",
                Surname = "Kation",
                Email = "edu.kation@tolk.se",
                Cellphone = "+46987654321",
                Competences = Enumerable.Empty<TellusInterpreterCompetenceModel>(),
                Educations = new List<TellusInterpreterEducationModel>
                {
                    new TellusInterpreterEducationModel
                    {
                        Language = "rus",
                        Educationlevel = "Detta är en jättefin kurs.",
                        ValidFrom = new DateTime(2014, 3, 15),
                        ValidTo = new DateTime(2020, 3, 15)
                    }
                }
            }
        };
        private readonly List<TellusLanguageModel> _languageModels = new List<TellusLanguageModel>
        {
            new TellusLanguageModel
            {
                Id = "sqi",
                Value ="albanska"
            },
             new TellusLanguageModel
            {
            Id = "ara",
            Value ="arabiska"
            },
             new TellusLanguageModel
            {
            Id = "bos,hrv,srp",
            Value ="bosniska, kroatiska, serbiska"
            },
             new TellusLanguageModel
            {
            Id = "bul",
            Value ="bulgariska"
            },
             new TellusLanguageModel
            {
            Id = "dan",
            Value ="danska"
            },
             new TellusLanguageModel
            {
            Id = "prs",
            Value ="dari"
            },
             new TellusLanguageModel
            {
            Id = "eng",
            Value ="engelska"
            },
             new TellusLanguageModel
            {
            Id = "est",
            Value ="estniska"
            },
             new TellusLanguageModel
            {
            Id = "fin",
            Value ="finska"
            },
             new TellusLanguageModel
            {
            Id = "fra",
            Value ="franska"
            },
             new TellusLanguageModel
            {
            Id = "gre",
            Value ="grekiska"
            },
             new TellusLanguageModel
            {
            Id = "hin",
            Value ="hindi"
            },
             new TellusLanguageModel
            {
            Id = "ita",
            Value ="italienska"
            },
             new TellusLanguageModel
            {
            Id = "jpn",
            Value ="japanska"
            },
             new TellusLanguageModel
            {
            Id = "yue",
            Value ="kantonesiska"
            },
             new TellusLanguageModel
            {
            Id = "lav",
            Value ="lettiska"
            },
             new TellusLanguageModel
            {
            Id = "lit",
            Value ="litauiska"
            },
             new TellusLanguageModel
            {
            Id = "mkd",
            Value ="makedonska"
            },
             new TellusLanguageModel
            {
            Id = "fit",
            Value ="meänkieli"
            },
             new TellusLanguageModel
            {
            Id = "nld",
            Value ="nederländska"
            },
             new TellusLanguageModel
            {
            Id = "kmr",
            Value ="kurdiska (kurmanji)"
            },
             new TellusLanguageModel
            {
            Id = "pes",
            Value ="persiska"
            },
             new TellusLanguageModel
            {
            Id = "pol",
            Value ="polska"
            },
             new TellusLanguageModel
            {
            Id = "por",
            Value ="portugisiska"
            },
             new TellusLanguageModel
            {
            Id = "cmn",
            Value ="rikskinesiska"
            },
             new TellusLanguageModel
            {
            Id = "ron",
            Value ="rumänska"
            },
             new TellusLanguageModel
            {
            Id = "rus",
            Value ="ryska"
            },
             new TellusLanguageModel
            {
            Id = "sme",
            Value ="samiska (nordsamiska)"
            },
             new TellusLanguageModel
            {
            Id = "som",
            Value ="somaliska"
            },
             new TellusLanguageModel
            {
            Id = "spa",
            Value ="spanska"
            },
             new TellusLanguageModel
            {
            Id = "ckb",
            Value ="kurdiska (sorani)"
            },
             new TellusLanguageModel
            {
            Id = "swa",
            Value ="swahili"
            },
             new TellusLanguageModel
            {
            Id = "swl",
            Value ="teckenspråk"
            },
             new TellusLanguageModel
            {
            Id = "tha",
            Value ="thai"
            },
             new TellusLanguageModel
            {
            Id = "tir",
            Value ="tigrinska"
            },
             new TellusLanguageModel
            {
            Id = "ces",
            Value ="tjeckiska"
            },
             new TellusLanguageModel
            {
            Id = "tur",
            Value ="turkiska"
            },
             new TellusLanguageModel
            {
            Id = "deu",
            Value ="tyska"
            },
             new TellusLanguageModel
            {
            Id = "hun",
            Value ="ungerska"
            },
             new TellusLanguageModel
            {
            Id = "vie",
            Value ="vietnamesiska"
            },
             new TellusLanguageModel
            {
            Id = "aze",
            Value ="azerbajdzjanska"
            },
             new TellusLanguageModel
            {
            Id = "zho",
            Value ="kinesiska"
            },
             new TellusLanguageModel
            {
            Id = "ukr",
            Value ="ukrainska"
            },
             new TellusLanguageModel
            {
            Id = "bel",
            Value ="vitryska"
            },
             new TellusLanguageModel
            {
            Id = "nor",
            Value ="norska"
            },
             new TellusLanguageModel
            {
            Id = "swe",
            Value ="svenska"
            },
             new TellusLanguageModel
            {
            Id = "mon",
            Value ="mongoliska"
            },
             new TellusLanguageModel
            {
            Id = "slk",
            Value ="slovakiska"
            },
             new TellusLanguageModel
            {
            Id = "isl",
            Value ="isländska"
            },
             new TellusLanguageModel
            {
            Id = "heb",
            Value ="hebreiska"
            },
             new TellusLanguageModel
            {
            Id = "ind",
            Value ="indonesiska"
            },
             new TellusLanguageModel
            {
            Id = "kor",
            Value ="koreanska"
            },
             new TellusLanguageModel
            {
            Id = "slv",
            Value ="slovenska"
            },
             new TellusLanguageModel
            {
            Id = "run",
            Value ="kirundi"
            },
             new TellusLanguageModel
            {
            Id = "nep",
            Value ="nepali"
            },
             new TellusLanguageModel
            {
            Id = "uzb",
            Value ="uzbekiska"
            },
             new TellusLanguageModel
            {
            Id = "rom",
            Value ="romska"
            },
             new TellusLanguageModel
            {
            Id = "hye",
            Value ="armeniska"
            },
             new TellusLanguageModel
            {
            Id = "pus",
            Value ="pashto"
            },
             new TellusLanguageModel
            {
            Id = "kat",
            Value ="georgiska"
            },
             new TellusLanguageModel
            {
            Id = "tgl",
            Value ="tagalog"
            },
             new TellusLanguageModel
            {
            Id = "urd",
            Value ="urdu"
            },
             new TellusLanguageModel
            {
            Id = "ben",
            Value ="bengali"
            },
             new TellusLanguageModel
            {
            Id = "fey",
            Value ="feyli"
            }
        };
        public TellusInterpreterResponse GetInterpreter(string id)
        {
            var result = _interpreterModels.Where(i => i.InterpreterId.ToString() == id);
            return new TellusInterpreterResponse
            {
                Result = result,
                Status = 200,
                TotalMatching = result.Count()
            };
        }
        public TellusLanguagesResponse GetLanguages()
        {
            return new TellusLanguagesResponse
            {
                Result = _languageModels,
                Status = 200,
            };
        }
    }
}
