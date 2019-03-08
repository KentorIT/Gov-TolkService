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
                InterpreterId = 90000,
                Name = "Tolk Tolksson",
                Email = "tolk.tolksson@hetbrev.se",
                Cellphone = "+4612345678910",
                Competences = new List<TellusInterpreterCompetenceModel>
                {
                    new TellusInterpreterCompetenceModel
                    {
                        Language = "albanska",
                        CompetenceLevel = "Authorized",
                        ValidFrom = null,
                        ValidTo = new DateTime(2020, 3, 15)
                    },
                    new TellusInterpreterCompetenceModel
                    {
                        Language = "albanska",
                        CompetenceLevel = "Legal",
                        ValidFrom = null,
                        ValidTo = new DateTime(2020, 3, 15)
                    },
                    new TellusInterpreterCompetenceModel
                    {
                        Language = "albanska",
                        CompetenceLevel = "Medical",
                        ValidFrom = null,
                        ValidTo = new DateTime(2020, 3, 15)
                    },
                    new TellusInterpreterCompetenceModel
                    {
                        Language = "bosniska, kroatiska, serbiska",
                        CompetenceLevel = "Authorized",
                        ValidFrom = null,
                        ValidTo = new DateTime(2020, 3, 15)
                    },
                    new TellusInterpreterCompetenceModel
                    {
                        Language = "makedonska",
                        CompetenceLevel = "Authorized",
                        ValidFrom = null,
                        ValidTo = new DateTime(2020, 3, 15)
                    }
                }
            },
            new TellusInterpreterModel
            {
                InterpreterId = 90001,
                Name = "Test Testmeyer",
                Email = "test.testmeyer@hetbrev.se",
                Cellphone = "+4612345678910",
                Competences = new List<TellusInterpreterCompetenceModel>
                {
                    new TellusInterpreterCompetenceModel
                    {
                        Language = "nederländska",
                        CompetenceLevel = "Authorized",
                        ValidFrom  = new DateTime(2014, 3, 15),
                        ValidTo = new DateTime(2019, 3, 15)
                    },
                    new TellusInterpreterCompetenceModel
                    {
                        Language = "tyska",
                        CompetenceLevel = "Authorized",
                        ValidFrom = new DateTime(2017, 3, 15),
                        ValidTo = new DateTime(2022, 3, 15)
                    },
                }
            },
            new TellusInterpreterModel
            {
                InterpreterId = 90002,
                Name = "John Doe",
                Email = "john.doe@hetbrev.se",
                Cellphone = "+4612345678910",
                Competences = new List<TellusInterpreterCompetenceModel>
                {
                    new TellusInterpreterCompetenceModel
                    {
                        Language = "grekiska",
                        CompetenceLevel = "Authorized",
                        ValidFrom = new DateTime(2014, 3, 15),
                        ValidTo = new DateTime(2019, 3, 15)
                    },
                    new TellusInterpreterCompetenceModel
                    {
                        Language = "grekiska",
                        CompetenceLevel = "Medical",
                        ValidFrom = new DateTime(2017, 3, 15),
                        ValidTo = new DateTime(2022, 3, 15)
                    },
                }
            },
            new TellusInterpreterModel
            {
                InterpreterId = 90003,
                Name = "Carl Hamilton",
                Email = "carl.hamilton@hetbrev.se",
                Cellphone = "+4612345678910",
                Competences = new List<TellusInterpreterCompetenceModel>
                {
                    new TellusInterpreterCompetenceModel
                    {
                        Language = "ryska",
                        CompetenceLevel = "Authorized",
                        ValidFrom = new DateTime(2014, 3, 15),
                        ValidTo = new DateTime(2020, 3, 15)
                    },
                    new TellusInterpreterCompetenceModel
                    {
                        Language = "ryska",
                        CompetenceLevel = "Legal",
                        ValidFrom = new DateTime(2014, 3, 15),
                        ValidTo = new DateTime(2022, 3, 15)
                    },
                }
            }
        };

        public TellusInterpreterModel GetInterpreter(string id)
        {
            return _interpreterModels.SingleOrDefault(i => i.InterpreterId.ToString() == id);
        }
    }
}
