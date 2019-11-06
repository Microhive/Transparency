using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Globalization;

namespace TingFetch
{
    public static class FetchVotes
    {
        [FunctionName("FetchVotes")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            HttpClient client = new HttpClient();

            string name = req.Query["votePage"];

            var response = await client.GetAsync($"{name}");
            var pageContents = await response.Content.ReadAsStringAsync();
            
            log.LogInformation("Fetched Content");


            var decodedContent = System.Web.HttpUtility.HtmlDecode(pageContents);
            var removedHTML = StripHTML(decodedContent);
            
            log.LogInformation("Stripped Markup");


            string[] lines = removedHTML.Split(
                new[] { "\r\n", "\r", "\n" },
                StringSplitOptions.None
            );

            var finalLines = new List<string>();

            foreach (var line in lines)
            {
                string trimmedLine = line.TrimStart().TrimEnd();

                if (string.IsNullOrEmpty(trimmedLine))
                {
                    continue;
                }

                finalLines.Add(trimmedLine);
            }

            var voteResult = new VoteResult();

            string[] split = finalLines[0].Split(new String[] { "Mál:", "Viðgerð:" }, StringSplitOptions.RemoveEmptyEntries);

            voteResult.Term = int.Parse(Regex.Match(split[0], @"\d+").Value).ToString();
            voteResult.Topic = int.Parse(Regex.Match(split[1], @"\d+").Value).ToString();
            voteResult.Reading = int.Parse(Regex.Match(split[2], @"\d+").Value).ToString();


            DateTime.TryParseExact(finalLines[1], "dd-MM-yyyy hh:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime resultDate);
            voteResult.VoteDate = resultDate;

            voteResult.Present = int.Parse(Regex.Match(finalLines.FirstOrDefault(x => x.Contains("Present")), @"\d+").Value);
            voteResult.TotalYes = int.Parse(Regex.Match(finalLines.FirstOrDefault(x => x.Contains("Total JA")), @"\d+").Value);
            voteResult.TotalNo = int.Parse(Regex.Match(finalLines.FirstOrDefault(x => x.Contains("Total NEI")), @"\d+").Value);
            voteResult.TotalBlank = int.Parse(Regex.Match(finalLines.FirstOrDefault(x => x.Contains("Total BLANK")), @"\d+").Value);
            voteResult.TotalAbsent = 33 - voteResult.Present;

            voteResult.YesVotes = new List<string>();
            voteResult.NoVotes = new List<string>();
            voteResult.BlankVotes = new List<string>();
            voteResult.AbsentVotes = new List<string>();

            int counter = finalLines.FindIndex(x => x.Contains("JA:")) + 1;

            for (int i = counter; i < (counter + voteResult.TotalYes); i++)
            {
                voteResult.YesVotes.Add(finalLines[i].Substring(finalLines[i].IndexOf(" ") + 1));
            }

            counter = finalLines.FindIndex(x => x.Contains("NEI:")) + 1;

            for (int i = counter; i < (counter + voteResult.TotalNo); i++)
            {
                voteResult.NoVotes.Add(finalLines[i].Substring(finalLines[i].IndexOf(" ") + 1));
            }

            counter = finalLines.FindIndex(x => x.Contains("BLANK:")) + 1;

            for (int i = counter; i < (counter + voteResult.TotalBlank); i++)
            {
                voteResult.BlankVotes.Add(finalLines[i].Substring(finalLines[i].IndexOf(" ") + 1));
            }

            log.LogInformation("Parsed Votes");

            string json = JsonConvert.SerializeObject(voteResult);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        }

        public static string StripHTML(string input)
        {
            return Regex.Replace(input, "<.*?>", String.Empty);
        }

        public class VoteResult
        {
            public string Term { get; set; }
            public string Topic { get; set; }
            public string Reading { get; set; }
            public DateTime VoteDate { get; set; }
            public int Present { get; set; }
            public int TotalYes { get; set; }
            public int TotalNo { get; set; }
            public int TotalBlank { get; set; }
            public int TotalAbsent { get; set; }
            public List<string> YesVotes { get; set; }
            public List<string> NoVotes { get; set; }
            public List<string> BlankVotes { get; set; }
            public List<string> AbsentVotes { get; set; }
        }
    }
}
