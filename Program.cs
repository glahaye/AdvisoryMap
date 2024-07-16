/* TODO:
 * - Report regions for which no ISO code is found
 * - Report failures to get level
 */

using System.Text;
using System.Text.Json;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;

namespace AdvisoryMap
{
    internal class Program
    {
        async static Task Main(string[] args)
        {
            string[] RegionsToSkip = { "Azores", "Canary Islands", "Saint-Pierre-et-Miquelon" };

            var entries = new SortedDictionary<string, AdvisoryEntry>();
            var isoMapper = new IsoMapper();

            HttpClient client = new();
            HtmlParser parser = new();

            var responseBody = await client.GetStringAsync("https://travel.gc.ca/");
            var document = await parser.ParseDocumentAsync(responseBody);

            IElement? countriesDropDown = document.QuerySelector("#CountryDropDown1_ddlCountries");
            if (countriesDropDown is null)
            {
                Console.WriteLine("Can't find list of countries");

                return;
            }

            foreach (var country in countriesDropDown.Children)
            {
                IHtmlOptionElement? countryOption = country as IHtmlOptionElement;
                if (countryOption is not null && !string.IsNullOrWhiteSpace(countryOption.Value))
                {
                    string countryName = country.Text().Split(',')[0];
                    string countryDirectoryName = countryOption.Value;

                    if (RegionsToSkip.Contains(countryName))
                    {
                        continue;
                    }

                    string isoCode = isoMapper.NameToCode(countryName);
                    if (string.IsNullOrEmpty(isoCode))
                    {
                        Console.WriteLine($"Can't find ISO code for {countryName}");
                    }
                    else
                    {
                        AdvisoryLevel level = await GetAdvisoryLevel(countryDirectoryName, client, parser);
                        if (level == AdvisoryLevel.Invalid)
                        {
                            Console.WriteLine($"Can't find advisory level for {countryName}");
                        }
                        else
                        {
                            entries[isoCode] = new AdvisoryEntry(countryName, countryDirectoryName, isoCode, level, DateTimeOffset.UtcNow);
                        }
                    }
                }
            }

            // Add an entry for Canada
            entries["CA"] = new AdvisoryEntry("Canada", "canada", "CA", AdvisoryLevel.Normal, DateTimeOffset.UtcNow);
            // Add an entry for Svalbard and Jan Mayen (Norwegian islands)
            entries["SJ"] = new AdvisoryEntry("Svalbard and Jan Mayen", "norway", "SJ", AdvisoryLevel.Normal, DateTimeOffset.UtcNow);
            // Add an entry for Western Sahara
            entries["SJ"] = new AdvisoryEntry("Western Sahara", "morocco", "EH", AdvisoryLevel.AvoidNonEssentialTravel, DateTimeOffset.UtcNow);

            string jsonString = JsonSerializer.Serialize(entries);
            await File.WriteAllTextAsync(@"C:\Users\gillahaye\Desktop\entries.json", jsonString);

            string colorCodedMap = GenerateColorCodedMap(entries);
            await File.WriteAllTextAsync(@"C:\Users\gillahaye\Desktop\map.html", colorCodedMap);
        }

        async static Task<AdvisoryLevel> GetAdvisoryLevel(string countryDirectory, HttpClient client, HtmlParser parser)
        {
            string responseBody = await client.GetStringAsync($"https://travel.gc.ca/destinations/{countryDirectory}");
            var document = await parser.ParseDocumentAsync(responseBody);
            IElement? riskLevelBanner = document.QuerySelector("#riskLevelBanner");
            string? riskText = riskLevelBanner?.QuerySelector("div")
                                              ?.QuerySelector("div")
                                              ?.QuerySelector("a")
                                              ?.QuerySelector("div")
                                              ?.TextContent;

            if (riskText?.StartsWith("Avoid all travel") == true)
            {
                return AdvisoryLevel.AvoidAllTravel;
            }

            if (riskText?.StartsWith("Avoid non-essential travel") == true)
            {
                return AdvisoryLevel.AvoidNonEssentialTravel;
            }

            if (riskText?.StartsWith("Exercise a high degree of caution") == true)
            {
                return AdvisoryLevel.Caution;
            }

            if (riskText?.StartsWith("Take normal security precautions") == true)
            {
                return AdvisoryLevel.Normal;
            }

            return AdvisoryLevel.Invalid;
        }

        static string GenerateColorCodedMap(SortedDictionary<string, AdvisoryEntry> entries)
        {
            string header = "<html>\r\n  <head>\r\n    <script type=\"text/javascript\" src=\"https://www.gstatic.com/charts/loader.js\"></script>\r\n    <script type=\"text/javascript\">\r\n      google.charts.load('upcoming', {'packages':['geochart']});\r\n      google.charts.setOnLoadCallback(drawRegionsMap);\r\n\r\n      function drawRegionsMap() {\r\n\r\n        var data = google.visualization.arrayToDataTable([\r\n          ['Country', 'Level'],";
            var sb = new StringBuilder(header);

            foreach (var entry in entries)
            {
                sb.AppendLine($"\t\t  ['{entry.Key}', {(int)entry.Value.Level}],");
            }

            string footer = "        ]);\r\n\r\n        var options = { backgroundColor : '#A3CCFF',\r\n                        datalessRegionColor : \"#CCCCCC\",\r\n                        domain : 'IN',\r\n                        height : '640',\r\n                        keepAspectRatio : \"true\",\r\n                        legend : 'none' };\r\n\r\n        var chart = new google.visualization.GeoChart(document.getElementById('regions_div'));\r\n\r\n        chart.draw(data, options);\r\n      }\r\n    </script>\r\n  </head>\r\n  <body>\r\n    <div id=\"regions_div\"></div>\r\n  </body>\r\n</html>";
            sb.AppendLine(footer);

            return sb.ToString();
        }
    }
}