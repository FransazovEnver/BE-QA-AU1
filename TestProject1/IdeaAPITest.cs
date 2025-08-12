using RestSharp;
using RestSharp.Authenticators;
using System.Net;
using System.Text.Json;
using TestProject1.Models;

namespace TestProject1
{
    public class IdeaAPITest
    {
        private RestClient client;
        private const string BaseUrl = "http://softuni-qa-loadbalancer-2137572849.eu-north-1.elb.amazonaws.com:84/api ";
        private const string Email = "testadidas@adidas.com";
        private const string PassWord = "123456";

        private static string lastIdeaId;
        [OneTimeSetUp]
        public void Setup()
        {
            string jwtToken = GetJwtToken(Email, PassWord);
            var options = new RestClientOptions(BaseUrl)
            {
                Authenticator = new JwtAuthenticator(jwtToken)
            };

            client = new RestClient(options);
        }

        private string GetJwtToken(string email, string password)
        {
            RestClient authClient = new RestClient(BaseUrl);
            var request = new RestRequest("/User/Authentication");
            request.AddJsonBody(new
            {
                email,
                password
            });

            var response = authClient.Execute(request, Method.Post);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = JsonSerializer.Deserialize<JsonElement>(response.Content);
                var token = content.GetProperty("accessToken").GetString();
                if (string.IsNullOrWhiteSpace(token))
                {
                    throw new InvalidOperationException("AccessToken is null or empty");
                }
                return token;
            }
            else
            {
                throw new InvalidOperationException($"Authentication failed: {response.StatusCode} - {response.Content}");
            }
        }

        [Test, Order(1)]
        public void CreateNewIdea_WithCorrectData_ShouldSucceed()
        {
            var requestData = new IdeaDTO()
            {
                Title = "Test Title",
                Description = "Testdescription.",
            };
            var request = new RestRequest("/Idea/Create");
            request.AddJsonBody(requestData);

            var response = client.Execute(request, Method.Post);
            var responseData = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Response status code is fail");
            Assert.That(responseData.Msg, Is.EqualTo("Successfully created!"), "Response Data is fail");
        }

        [Test, Order(2)]
        public void GetAllIdeas_ShouldReturnNonEmptyArray()
        {
            var request = new RestRequest("/Idea/All");

            var response = client.Execute(request, Method.Get);
            var responseDataArray = JsonSerializer.Deserialize<ApiResponseDTO[]>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Response code is fail");
            Assert.That(responseDataArray.Length, Is.GreaterThan(0), "Response Data is null");

            lastIdeaId = responseDataArray[responseDataArray.Length - 1].IdeaId;
        }

        [Test, Order(3)]
        public void PutIdea_EditLastIdea_ShouldSucceed()
        {
            var IdeaRequest = new IdeaDTO()
            {
                Title = "EditedTestTitle",
                Description = "EditedTestDescription",
            };

            var request = new RestRequest("/Idea/Edit");
            request.AddQueryParameter("ideaId", lastIdeaId);
            request.AddJsonBody(IdeaRequest);
            
            var response = client.Execute(request, Method.Put);
            var responseData = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Response status code is fail");
            Assert.That(responseData.Msg, Is.EqualTo("Edited successfully"), "Response Msg is fail");
        }


        [Test, Order(4)]
        public void DeleteIdea_DeleteLastIdeaYouEdited()
        {
            var requestData = new RestRequest("/Idea/Delete");
            requestData.AddQueryParameter("ideaId", lastIdeaId);

            var response = client.Execute(requestData, Method.Delete);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Response status code is fail");
            Assert.That(response.Content, Does.Contain("The idea is deleted!"),"Response is not content");



        }


        [Test, Order(5)]
        public void Create_IdeaWithMissingRequiredFields()
        {
            var requestData = new IdeaDTO()
            {
                Title = "Test Title",
            };
            var request = new RestRequest("/Idea/Create");
            request.AddJsonBody(requestData);

            var response = client.Execute(request, Method.Post);
            var responseData = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }


        [Test, Order(6)]
        public void Edit_ANon_ExistingIdea()
        {
            var IdeaRequest = new IdeaDTO()
            {
                Title = "EditedTestTitle",
                Description = "EditedTestDescription",
            };

            var request = new RestRequest("/Idea/Edit");
            request.AddQueryParameter("ideaId", "112233");
            request.AddJsonBody(IdeaRequest);

            var response = client.Execute(request, Method.Put);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(response.Content, Does.Contain("There is no such idea!"));
        }


        [Test, Order(7)]
        public void Delete_ANon_ExistingIdea()
        {
            var requestData = new RestRequest("/Idea/Delete");
            requestData.AddQueryParameter("ideaId", "1122334444");

            var response = client.Execute(requestData, Method.Delete);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(response.Content, Does.Contain("There is no such idea!"));
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            client?.Dispose();
        }
    }
}