using System;
using System.Collections.Generic;
using System.Linq;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Helpers;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.Web.Services
{
    public class MockTellusApiService
    {
        private readonly TolkDbContext _dbContext;
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
        private readonly List<MockTellusLanguageModel> _languageModels = new List<MockTellusLanguageModel>
        {
            new MockTellusLanguageModel
            {
                Id = "swe",
                Value ="svenska",
                AllwaysAdd = true
            },
            new MockTellusLanguageModel
            {
                Id = "swl",
                Value ="teckenspråk",
                AllwaysAdd = true
            },
            new MockTellusLanguageModel
            {
                Id = "abk",
                Value ="oromo",
                AddOnTest = true
            },
            new MockTellusLanguageModel
            {
                Id = "xxx",
                Value ="nytt språk",
                AddOnTest = true
            },
            new MockTellusLanguageModel
            {
                Id = "deu",
                Value ="tyska",
                RemoveOnTest = true
            }


        };
        public MockTellusApiService(TolkDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public TellusInterpreterResponse GetInterpreter(string id)
        {
            var result = _interpreterModels.Where(i => i.InterpreterId == id);
            return new TellusInterpreterResponse
            {
                Result = result,
                Status = 200,
                TotalMatching = result.Count()
            };
        }

        public TellusLanguagesResponse GetLanguages()
        {
            var tellusLanguages = _dbContext.Languages.Where(l => !string.IsNullOrEmpty(l.TellusName))
                .Select(l => new TellusLanguageModel
                {
                    Id = l.TellusName,
                    Value = l.Name.ToLower()
                }).ToList().Where(l => _languageModels.Any(t => t.RemoveOnTest && t.Id == l.Id));
            return new TellusLanguagesResponse
            {
                Result = tellusLanguages
                    .Concat(_languageModels.Where(l => l.AllwaysAdd || l.AddOnTest)
                    .Select(l => new TellusLanguageModel
                    {
                        Id = l.Id,
                        Value = l.Value
                    })),
                Status = 200,
            };
        }

        public TellusLanguagesCompetenceInfoResponse GetLanguagesInfo()
        {
            List<TellusLanguagesInfoModel> tellusLanguagesInfo = new List<TellusLanguagesInfoModel> {
                new TellusLanguagesInfoModel
                {
                    Id = "bul",
                    HasAuthorized = true,
                    HasEducated = false,
                    HasHealthcare = false,
                    HasLegal = false
                } };
            tellusLanguagesInfo.Add(new TellusLanguagesInfoModel
            {
                Id = "deu",
                HasAuthorized = true,
                HasEducated = true,
                HasHealthcare = true,
                HasLegal = true
            });
            tellusLanguagesInfo.Add(new TellusLanguagesInfoModel
            {
                Id = "est",
                HasAuthorized = true,
                HasEducated = true,
                HasHealthcare = false,
                HasLegal = true
            });
            tellusLanguagesInfo.Add(new TellusLanguagesInfoModel
            {
                Id = "ind",
                HasAuthorized = false,
                HasEducated = false,
                HasHealthcare = false,
                HasLegal = false
            });
            return new TellusLanguagesCompetenceInfoResponse
            {
                Result = tellusLanguagesInfo,
                Status = 200,
            };
        }
    }
}
