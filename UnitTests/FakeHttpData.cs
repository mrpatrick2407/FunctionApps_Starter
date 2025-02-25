using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using Azure.Core.Serialization;
using Moq;
using Microsoft.Extensions.DependencyInjection;
using Twilio.Converters;
using System.Text.Json;

namespace UnitTests
{
    public class FakeHttpRequestData : HttpRequestData
    {
        private readonly string _jsonContent;
        private readonly FunctionContext _functionContext;
        public FakeHttpRequestData(FunctionContext functionContext, Uri url, Stream body = null, string jsonContent = "")
            : base(functionContext)
        {
            Url = url;
            Body = body ?? new MemoryStream();
            _jsonContent = jsonContent;
            _functionContext = functionContext;
        }

        public override Stream Body { get; } = new MemoryStream();
        public override HttpHeadersCollection Headers { get; } = new HttpHeadersCollection();
        public override IReadOnlyCollection<IHttpCookie> Cookies { get; }
        public override Uri Url { get; }
        public override IEnumerable<ClaimsIdentity> Identities { get; }
        public override string Method { get; }

        // Simulate ReadAsStringAsync by returning predefined JSON content
        public async Task<string> ReadAsStringAsync(Encoding? encoding = null)
        {
            return await Task.FromResult(_jsonContent); // Return the predefined JSON content
        }

        public override HttpResponseData CreateResponse()
        { 
            return new FakeHttpResponseData(_functionContext);
        }
    }

    public class FakeHttpResponseData : HttpResponseData
    {
        private ObjectSerializer GetObjectSerializer()
        {
            // Try to get an ObjectSerializer from DI
            var serializer = _context.InstanceServices.GetService<ObjectSerializer>();
            if (serializer == null)
            {
                throw new InvalidOperationException("ObjectSerializer is not registered in DI. Ensure it is added in tests.");
            }
            // Fallback to SystemTextJsonObjectSerializer if not registered
            return serializer ?? new JsonObjectSerializer();
        }
        private readonly ObjectSerializer _serializer;
        private readonly FunctionContext _context;
        public FakeHttpResponseData(FunctionContext context) : base(context)
        {
            _context = context;
            if (_context.InstanceServices == null)
            {
                throw new InvalidOperationException("InstanceServices is null. Ensure FunctionContext is properly configured in tests.");
            }
            _serializer = GetObjectSerializer();
        }
        public override HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;
        public override HttpHeadersCollection Headers { get; set; } = new HttpHeadersCollection();
        public override Stream Body { get; set; } = new MemoryStream();
        public override HttpCookies Cookies { get; }

        //public async Task WriteAsJsonAsync<T>(T instance, CancellationToken cancellationToken = default)
        //{
        //    var serializer = new JsonObjectSerializer(); // Use real serializer
        //    await serializer.SerializeAsync(Body, typeof(T), cancellationToken);
        //    Body.Position = 0;
        //}
        public async Task WriteAsJsonAsync<T>(T content, CancellationToken cancellationToken = default)
        {
            if (_serializer == null)
            {
                throw new InvalidOperationException("ObjectSerializer is not available.");
            }

            // 🔥 Call WriteAsJsonAsync as an extension method
            await HttpResponseDataExtensions.WriteAsJsonAsync(this, content, _serializer, "application/json", cancellationToken);
        }
    }



}
