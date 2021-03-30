using NSwag;
using NSwag.Generation.Processors;
using NSwag.Generation.Processors.Contexts;
using System.Threading.Tasks;

namespace Tolk.Web.Api.Authorization
{
    public class AddRequiredHeaderParameter : IOperationProcessor
    {
        readonly string _name;
        public AddRequiredHeaderParameter(string name)
        {
            _name = name;
        }
        public bool Process(OperationProcessorContext context)
        {
            context?.OperationDescription.Operation.Parameters.Add(
            new OpenApiParameter
            {
                Name = _name,//"X-Kammarkollegiet-InterpreterService-UserName",
                Kind = OpenApiParameterKind.Header,
                Type = NJsonSchema.JsonObjectType.String,
                IsRequired = true,
                Description = "Tvingande fält",
            });
            return true;
        }

        public Task<bool> ProcessAsync(OperationProcessorContext context)
        {
            return Task.FromResult(Process(context));
        }
    }
}

