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
                GivenName = "Tolk",
                Surname = "Tolksson",
                Email = "tolk.tolksson@hetbrev.se",
                Cellphone = "+4612345678910",
                County = "Stockholm",
                Competences = new List<TellusInterpreterCompetenceModel>
                {
                    new TellusInterpreterCompetenceModel
                    {
                        Language = "sqi",
                        CompetenceLevel = new TellusInterpreterCompetencePairModel ("1", "auktoriserad tolk"),
                        ValidFrom = null,
                        ValidTo = new DateTime(2020, 3, 15)
                    },
                    new TellusInterpreterCompetenceModel
                    {
                        Language = "sqi",
                        CompetenceLevel = new TellusInterpreterCompetencePairModel ("2", "rättstolk"),
                        ValidFrom = null,
                        ValidTo = new DateTime(2020, 3, 15)
                    },
                    new TellusInterpreterCompetenceModel
                    {
                        Language = "sqi",
                        CompetenceLevel = new TellusInterpreterCompetencePairModel ("3", "sjukvårdstolk"),
                        ValidFrom = null,
                        ValidTo = new DateTime(2020, 3, 15)
                    },
                    new TellusInterpreterCompetenceModel
                    {
                        Language = "bos,hrv,srp",
                        CompetenceLevel =  new TellusInterpreterCompetencePairModel ("1", "auktoriserad tolk"),
                        ValidFrom = null,
                        ValidTo = new DateTime(2020, 3, 15)
                    },
                    new TellusInterpreterCompetenceModel
                    {
                        Language = "mkd",
                        CompetenceLevel =  new TellusInterpreterCompetencePairModel ("1", "auktoriserad tolk"),
                        ValidFrom = null,
                        ValidTo = new DateTime(2020, 3, 15)
                    }
                },
                Educations = new List<TellusInterpreterEducationModel> { }
            },
            new TellusInterpreterModel
            {
                InterpreterId = "90001",
                GivenName = "Test",
                Surname = "Testmeyer",
                Email = "test.testmeyer@hetbrev.se",
                Cellphone = "+4612345678910",
                County = "Malmö",
                Competences = new List<TellusInterpreterCompetenceModel>
                {
                    new TellusInterpreterCompetenceModel
                    {
                        Language = "nld",
                        CompetenceLevel = new TellusInterpreterCompetencePairModel ("1", "auktoriserad tolk"),
                        ValidFrom  = new DateTime(2014, 3, 15),
                        ValidTo = new DateTime(2019, 3, 15)
                    },
                    new TellusInterpreterCompetenceModel
                    {
                        Language = "deu",
                        CompetenceLevel = new TellusInterpreterCompetencePairModel ("1", "auktoriserad tolk"),
                        ValidFrom = new DateTime(2017, 3, 15),
                        ValidTo = new DateTime(2022, 3, 15)
                    },
                },
                Educations = new List<TellusInterpreterEducationModel>
                {
                    new TellusInterpreterEducationModel
                    {
                        Language = "nld",
                        FromLanguage = "nld",
                        ToLanguage = "swe",
                        EducationLevel = "konferenstolk (universitet)",
                        ValidFrom = new DateTime(2016, 2, 26),
                        ValidTo = new DateTime(2021, 6, 29)
                    }
                }
            },
            new TellusInterpreterModel
            {
                InterpreterId = "90002",
                GivenName = "John",
                Surname = "Doe",
                Email = "john.doe@hetbrev.se",
                Cellphone = "+4612345678910",
                County = "Stockholm",
                Competences = new List<TellusInterpreterCompetenceModel>
                {
                    new TellusInterpreterCompetenceModel
                    {
                        Language = "gre",
                        CompetenceLevel = new TellusInterpreterCompetencePairModel ("1", "auktoriserad tolk"),
                        ValidFrom = new DateTime(2014, 3, 15),
                        ValidTo = new DateTime(2019, 3, 15)
                    },
                    new TellusInterpreterCompetenceModel
                    {
                        Language = "gre",
                        CompetenceLevel = new TellusInterpreterCompetencePairModel ("3", "sjukvårdstolk"),
                        ValidFrom = new DateTime(2017, 3, 15),
                        ValidTo = new DateTime(2022, 3, 15)
                    },
                },
                Educations = new List<TellusInterpreterEducationModel> { }
            },
            new TellusInterpreterModel
            {
                InterpreterId = "90003",
                GivenName = "Carl",
                Surname = "Hamilton",
                Email = "carl.hamilton@hetbrev.se",
                Cellphone = "+4612345678910",
                County = "Stockholm",
                Competences = new List<TellusInterpreterCompetenceModel>
                {
                    new TellusInterpreterCompetenceModel
                    {
                        Language = "rus",
                        CompetenceLevel = new TellusInterpreterCompetencePairModel ("1", "auktoriserad tolk"),
                        ValidFrom = new DateTime(2014, 3, 15),
                        ValidTo = new DateTime(2020, 3, 15)
                    },
                    new TellusInterpreterCompetenceModel
                    {
                        Language = "rus",
                        CompetenceLevel = new TellusInterpreterCompetencePairModel ("2", "rättstolk"),
                        ValidFrom = new DateTime(2014, 3, 15),
                        ValidTo = new DateTime(2022, 3, 15)
                    },
                },
                Educations = new List<TellusInterpreterEducationModel>
                {
                    new TellusInterpreterEducationModel
                    {
                        Language = "rus",
                        EducationLevel = "tolk, talade språk (folkbildning)",
                        ValidFrom = new DateTime(2012, 3, 15),
                        ValidTo = new DateTime(2019, 3, 15),
                    }
                }
            }
        };

        public ITellusResultModel GetInterpreter(string id)
        {
            return _interpreterModels.SingleOrDefault(i => i.InterpreterId.ToString() == id);
        }
    }
}
