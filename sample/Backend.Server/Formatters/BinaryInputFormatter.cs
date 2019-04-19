using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;

namespace Backend.Server.Formatters
{
    public class BinaryInputFormatter : InputFormatter
    {
        public BinaryInputFormatter() =>
            SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/octet-stream"));

        protected override bool CanReadType(Type type) => type == typeof(byte[]);

        public override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context)
        {
            using (var stream = new MemoryStream())
            {
                await context.HttpContext.Request.Body.CopyToAsync(stream);
                return await InputFormatterResult.SuccessAsync(stream.ToArray());
            }
        }
    }
}