using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.Web.Models;

namespace Tolk.Web.Services
{
    public class MockInterpreterModel
    {
        public class MockInterpreterCompetenceModel
        {
            public string language { get; set; }
            public string competenceLevel { get; set; }
            public string validFrom { get; set; }
            public string validTo { get; set; }
        }

        public int interpreterId { get; set; }
        public string name { get; set; }
        public string email { get; set; }
        public string cellphone { get; set; }
        public List<MockInterpreterCompetenceModel> competences { get; set; }
    }

    public class MockTellusApiService
    {
        private readonly List<MockInterpreterModel> _interpreterModels = new List<MockInterpreterModel>
        {
            new MockInterpreterModel
            {
                interpreterId = 90000,
                name = "Tolk Tolksson",
                email = "tolk.tolksson@hetbrev.se",
                cellphone = "+4612345678910",
                competences = new List<MockInterpreterModel.MockInterpreterCompetenceModel>
                {
                    new MockInterpreterModel.MockInterpreterCompetenceModel
                    {
                        language = "albanska",
                        competenceLevel = "Authorized",
                        validFrom = null,
                        validTo = "2020-03-15"
                    },
                    new MockInterpreterModel.MockInterpreterCompetenceModel
                    {
                        language = "albanska",
                        competenceLevel = "Legal",
                        validFrom = null,
                        validTo = "2020-03-15"
                    },
                    new MockInterpreterModel.MockInterpreterCompetenceModel
                    {
                        language = "albanska",
                        competenceLevel = "Medical",
                        validFrom = null,
                        validTo = "2020-03-15"
                    },
                    new MockInterpreterModel.MockInterpreterCompetenceModel
                    {
                        language = "bosniska, kroatiska, serbiska",
                        competenceLevel = "Authorized",
                        validFrom = null,
                        validTo = "2020-03-15"
                    },
                    new MockInterpreterModel.MockInterpreterCompetenceModel
                    {
                        language = "makedonska",
                        competenceLevel = "Authorized",
                        validFrom = null,
                        validTo = "2020-03-15"
                    }
                }
            },
            new MockInterpreterModel
            {
                interpreterId = 90001,
                name = "Test Testmeyer",
                email = "test.testmeyer@hetbrev.se",
                cellphone = "+4612345678910",
                competences = new List<MockInterpreterModel.MockInterpreterCompetenceModel>
                {
                    new MockInterpreterModel.MockInterpreterCompetenceModel
                    {
                        language = "nederländska",
                        competenceLevel = "Authorized",
                        validFrom = "2014-03-15",
                        validTo = "2019-03-15"
                    },
                    new MockInterpreterModel.MockInterpreterCompetenceModel
                    {
                        language = "tyska",
                        competenceLevel = "Authorized",
                        validFrom = "2017-03-15",
                        validTo = "2022-03-15"
                    },
                }
            },
            new MockInterpreterModel
            {
                interpreterId = 90002,
                name = "John Doe",
                email = "john.doe@hetbrev.se",
                cellphone = "+4612345678910", 
                competences = new List<MockInterpreterModel.MockInterpreterCompetenceModel>
                {
                    new MockInterpreterModel.MockInterpreterCompetenceModel
                    {
                        language = "grekiska",
                        competenceLevel = "Authorized",
                        validFrom = "2014-03-15",
                        validTo = "2019-03-15"
                    },
                    new MockInterpreterModel.MockInterpreterCompetenceModel
                    {
                        language = "grekiska",
                        competenceLevel = "Medical",
                        validFrom = "2017-03-15",
                        validTo = "2022-03-15"
                    },
                }
            },
            new MockInterpreterModel
            {
                interpreterId = 90003,
                name = "Carl Hamilton",
                email = "carl.hamilton@hetbrev.se",
                cellphone = "+4612345678910",
                competences = new List<MockInterpreterModel.MockInterpreterCompetenceModel>
                {
                    new MockInterpreterModel.MockInterpreterCompetenceModel
                    {
                        language = "ryska",
                        competenceLevel = "Authorized",
                        validFrom = "2014-03-15",
                        validTo = "2019-03-15"
                    },
                    new MockInterpreterModel.MockInterpreterCompetenceModel
                    {
                        language = "ryska",
                        competenceLevel = "Legal",
                        validFrom = "2017-03-15",
                        validTo = "2022-03-15"
                    },
                }
            }
        };

        public MockInterpreterModel GetInterpreter(int id)
        {
            return _interpreterModels.SingleOrDefault(i => i.interpreterId == id);
        }
    }
}
