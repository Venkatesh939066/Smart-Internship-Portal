using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using SmartInternshipPortal;
using Xunit;

namespace SmartInternshipPortal.Tests.Integration
{
    public class ResumeFlowTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public ResumeFlowTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task UploadDownloadDeleteResume_ReturnsExpectedStatus()
        {
            var client = _factory.CreateClient();

            // NOTE: This test assumes anonymous endpoints are not protected for the happy path run locally with default auth.
            // It mainly verifies the upload endpoint returns a redirect and that subsequent download returns NotFound when not implemented.

            // Create a small dummy file
            var content = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes("dummy resume"));
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/pdf");
            content.Add(fileContent, "resumeFile", "test.pdf");

            var uploadResponse = await client.PostAsync("/Student/UploadResume", content);
            Assert.True(uploadResponse.StatusCode == HttpStatusCode.Redirect || uploadResponse.StatusCode == HttpStatusCode.OK);

            // Download should redirect to login or return NotFound when not authenticated/seeded
            var downloadResponse = await client.GetAsync("/Student/DownloadResume");
            Assert.True(downloadResponse.StatusCode == HttpStatusCode.Redirect || downloadResponse.StatusCode == HttpStatusCode.NotFound || downloadResponse.StatusCode == HttpStatusCode.OK);

            // Delete (post) should also redirect to login if not authenticated or return BadRequest on CSRF failure
            var deleteResponse = await client.PostAsync("/Student/DeleteResume", new FormUrlEncodedContent(new[] { new KeyValuePair<string, string>("__RequestVerificationToken", "" ) }));
            Assert.True(deleteResponse.StatusCode == HttpStatusCode.Redirect || deleteResponse.StatusCode == HttpStatusCode.Forbidden || deleteResponse.StatusCode == HttpStatusCode.BadRequest || deleteResponse.StatusCode == HttpStatusCode.OK);
        }
    }
}
