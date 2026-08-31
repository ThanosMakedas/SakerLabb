using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using System.Xml;
using Newtonsoft.Json;

namespace SakerLabb.Web.Services;

public class ImportService
{
    private static readonly Regex HostPattern =
        new(@"^[a-zA-Z0-9]([a-zA-Z0-9\-.]{0,253}[a-zA-Z0-9])?$", RegexOptions.Compiled);

    private readonly ILogger<ImportService> _logger;
    private readonly HttpClient _http;

    public ImportService(ILogger<ImportService> logger, HttpClient http)
    {
        _logger = logger;
        _http = http;
    }

    public string ImportXml(string xml)
    {
        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Parse,
            XmlResolver = new XmlUrlResolver()
        };

        var document = new XmlDocument { XmlResolver = new XmlUrlResolver() };
        using var reader = XmlReader.Create(new StringReader(xml), settings);
        document.Load(reader);

        return document.DocumentElement?.InnerText ?? "";
    }

    public object? ImportJson(string json)
    {
        var settings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All
        };

        return JsonConvert.DeserializeObject(json, settings);
    }

    public async Task<string> FetchRemote(string url)
    {
        _logger.LogInformation("Hämtar fjärresurs {Url}", url);
        var response = await _http.GetAsync(url);
        return await response.Content.ReadAsStringAsync();
    }

    public string Ping(string host)
    {
        if (string.IsNullOrWhiteSpace(host) || !HostPattern.IsMatch(host))
        {
            _logger.LogWarning("Diagnostik nekad, ogiltigt värdnamn angivet.");
            return "Ogiltigt värdnamn. Endast bokstäver, siffror, punkt och bindestreck är tillåtna.";
        }

        try
        {
            using var ping = new System.Net.NetworkInformation.Ping();
            var reply = ping.Send(host, 2000);

            if (reply.Status == IPStatus.Success)
            {
                return $"Svar från {reply.Address}: tid={reply.RoundtripTime} ms";
            }

            return $"Inget svar från {host}: {reply.Status}";
        }
        catch (PingException)
        {
            return $"Kunde inte nå {host}.";
        }
    }
}
