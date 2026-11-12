using Amazon.Lambda.Core;
using System.Text;
using Newtonsoft.Json;
// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace EsepWebhook;

public class Function
{
    
    /// <summary>
    /// Receives a GitHub issue webhook and posts the issue URL to Slack.
    /// </summary>
    /// <param name="input">The raw JSON event (a GitHub payload, or a Function URL / API Gateway request wrapping it).</param>
    /// <param name="context">The ILambdaContext that provides methods for logging and describing the Lambda environment.</param>
    /// <returns>Slack's response body.</returns>
    public string FunctionHandler(object input, ILambdaContext context)
    {
        string raw = input.ToString() ?? "";
        context.Logger.LogLine($"Received: {raw}");

        dynamic json = JsonConvert.DeserializeObject<dynamic>(raw)!;

        // Function URLs and API Gateway wrap the HTTP request; the GitHub payload is in "body".
        if (json.body != null)
        {
            string body = (string)json.body;
            if (json.isBase64Encoded == true)
                body = Encoding.UTF8.GetString(Convert.FromBase64String(body));
            json = JsonConvert.DeserializeObject<dynamic>(body)!;
        }

        string issueUrl = (string)json.issue.html_url;
        string payload = JsonConvert.SerializeObject(new { text = $"Issue Created: {issueUrl}" });
        
        var client = new HttpClient();
        var webRequest = new HttpRequestMessage(HttpMethod.Post, Environment.GetEnvironmentVariable("SLACK_URL")) 
        { 
            Content = new StringContent(payload, Encoding.UTF8, "application/json") 
        };
        var response = client.Send(webRequest);
        using var reader = new StreamReader(response.Content.ReadAsStream());
        return reader.ReadToEnd();
    }
}
