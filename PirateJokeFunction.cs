using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Net.Http.Headers;
using PirateFunction.Models;
using System.Text.Json;
using System.Dynamic;
using System.Text;
using System.Collections.Generic;

namespace PirateFunction
{
    public static class PirateJokeFunction
    {
        private static HttpClient _httpClient = new HttpClient();

        [FunctionName("GetJoke")]
        public static async Task<IActionResult> GetJoke(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            
            _httpClient.DefaultRequestHeaders
                .Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));           
            
            var response = await _httpClient.GetAsync("https://icanhazdadjoke.com/");
            var jsonString = await response.Content.ReadAsStringAsync();


            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            };

            JokeResponse jokeResponse = JsonSerializer.Deserialize<JokeResponse>(jsonString, options);

            var translatedJoke = await TranslateToPirate(jokeResponse.Joke);
            if(translatedJoke.translated == "error")
            {
                translatedJoke.translated = jokeResponse.Joke;
            }
            string name = req.Query["name"];


            string responseMessage = string.IsNullOrEmpty(name)
                ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
                : $"Hello, {name}. Here is a joke: {translatedJoke.translated}";

            return new OkObjectResult(responseMessage);
        }



        private async static Task<PirateTranslation> TranslateToPirate(string textToTranslate)
        {

            var pirateText = new PirateText { text = textToTranslate };           
            var response = await _httpClient
                .PostAsJsonAsync("https://api.funtranslations.com/translate/pirate.json", pirateText);

            var jsonString = await response.Content.ReadAsStringAsync();
            
            dynamic jsonResult = JsonSerializer.Deserialize<ExpandoObject>(jsonString);
            PirateTranslation translation = new PirateTranslation();
            foreach (KeyValuePair<string, object> kvp in jsonResult)
            {
                var key = kvp.Key;
                var value = kvp.Value.ToString();
                if (key == "contents")
                {
                    translation = JsonSerializer.Deserialize<PirateTranslation>(value);
                }
                if(key == "error")
                {
                    translation.translated = "error";
                    translation.text = textToTranslate;
                   return translation;
                }
            }

            return translation;
        }

           }

    public struct PirateText
    {
        public string text { get; set; }
    }
    public struct PirateTranslation
    {
        public string translated { get; set; }
        public string text { get; set; }
        public string translation { get; set; }
    }
}
