using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace playerapi.updates
{
    public static class updates
    {
        [FunctionName("updates")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            string building = req.Query["building"];
            if (string.IsNullOrWhiteSpace(building))
            {
                building = "Overmoed";
            }
            log.LogInformation($"Get request for updates in {building}");

            try
            {
                dynamic returnValue = new JObject();
                var url = $"{BlobPropertiesUrlPrefix}{building}";
                var xml = await new HttpClient().GetStringAsync(url);
                var xdoc = XDocument.Parse(xml);
                log.LogInformation($"XML: {xdoc}");
                var blobElements = xdoc.Element("EnumerationResults").Element("Blobs").Elements("Blob");
                foreach (var blobElement in blobElements)
                {
                    var blobName = blobElement.Element("Name").Value;
                    var version = blobElement.Element("Metadata").Element("Version").Value;
                    var date = blobElement.Element("Metadata").Element("Date").Value;
                    dynamic versionObject = new JObject();
                    versionObject["version"] = version;
                    versionObject["date"] = date;
                    var parts = blobName.Split("/")[1].Split(".");
                    if (returnValue[parts[1]] != null)
                    {
                        returnValue[parts[1]][parts[0]] = versionObject;
                    }
                    else
                    {
                        returnValue[parts[1]] = new JObject { [parts[0]] = versionObject };
                    }
                }

                return new OkObjectResult(returnValue);
            }
            catch (Exception ex)
            {
                var errorMessage = $"Exception during GetUpdates: {ex}";
                log.LogError(errorMessage);

                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
