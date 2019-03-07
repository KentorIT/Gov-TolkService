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
                interpreterId = 90000,
                name = "Tolk Tolksson",
                email = "tolk.tolksson@hetbrev.se",
                cellphone = "+4612345678910",
                competences = new List<TellusInterpreterCompetenceModel>
                {
                    new TellusInterpreterCompetenceModel
                    {
                        language = "albanska",
                        competenceLevel = "Authorized",
                        validFrom = null,
                        validTo = new DateTime(2020, 3, 15)
                    },
                    new TellusInterpreterCompetenceModel
                    {
                        language = "albanska",
                        competenceLevel = "Legal",
                        validFrom = null,
                        validTo = new DateTime(2020, 3, 15)
                    },
                    new TellusInterpreterCompetenceModel
                    {
                        language = "albanska",
                        competenceLevel = "Medical",
                        validFrom = null,
                        validTo = new DateTime(2020, 3, 15)
                    },
                    new TellusInterpreterCompetenceModel
                    {
                        language = "bosniska, kroatiska, serbiska",
                        competenceLevel = "Authorized",
                        validFrom = null,
                        validTo = new DateTime(2020, 3, 15)
                    },
                    new TellusInterpreterCompetenceModel
                    {
                        language = "makedonska",
                        competenceLevel = "Authorized",
                        validFrom = null,
                        validTo = new DateTime(2020, 3, 15)
                    }
                }
            },
            new TellusInterpreterModel
            {
                interpreterId = 90001,
                name = "Test Testmeyer",
                email = "test.testmeyer@hetbrev.se",
                cellphone = "+4612345678910",
                competences = new List<TellusInterpreterCompetenceModel>
                {
                    new TellusInterpreterCompetenceModel
                    {
                        language = "nederländska",
                        competenceLevel = "Authorized",
                        validFrom  = new DateTime(2014, 3, 15),
                        validTo = new DateTime(2019, 3, 15)
                    },
                    new TellusInterpreterCompetenceModel
                    {
                        language = "tyska",
                        competenceLevel = "Authorized",
                        validFrom = new DateTime(2017, 3, 15),
                        validTo = new DateTime(2022, 3, 15)
                    },
                }
            },
            new TellusInterpreterModel
            {
                interpreterId = 90002,
                name = "John Doe",
                email = "john.doe@hetbrev.se",
                cellphone = "+4612345678910",
                competences = new List<TellusInterpreterCompetenceModel>
                {
                    new TellusInterpreterCompetenceModel
                    {
                        language = "grekiska",
                        competenceLevel = "Authorized",
                        validFrom = new DateTime(2014, 3, 15),
                        validTo = new DateTime(2019, 3, 15)
                    },
                    new TellusInterpreterCompetenceModel
                    {
                        language = "grekiska",
                        competenceLevel = "Medical",
                        validFrom = new DateTime(2017, 3, 15),
                        validTo = new DateTime(2022, 3, 15)
                    },
                }
            },
            new TellusInterpreterModel
            {
                interpreterId = 90003,
                name = "Carl Hamilton",
                email = "carl.hamilton@hetbrev.se",
                cellphone = "+4612345678910",
                competences = new List<TellusInterpreterCompetenceModel>
                {
                    new TellusInterpreterCompetenceModel
                    {
                        language = "ryska",
                        competenceLevel = "Authorized",
                        validFrom = new DateTime(2014, 3, 15),
                        validTo = new DateTime(2020, 3, 15)
                    },
                    new TellusInterpreterCompetenceModel
                    {
                        language = "ryska",
                        competenceLevel = "Legal",
                        validFrom = new DateTime(2014, 3, 15),
                        validTo = new DateTime(2022, 3, 15)
                    },
                }
            }
        };

        public TellusInterpreterModel GetInterpreter(int id)
        {
            return _interpreterModels.SingleOrDefault(i => i.interpreterId == id);
        }
    }
}
