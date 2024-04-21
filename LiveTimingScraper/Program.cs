using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using LiveTimingScraper;


internal class Program
{
    static async Task Main(string[] args)
    {
        var url = "http://livetiming.pl/contests/regional/2";
        bool continueProcessing = true;

        while (continueProcessing)
        {
            Console.WriteLine("wszedłem");
            var aNodes = Loader(url)
                .DocumentNode
                .SelectNodes("//div[@class='contests list']//a");

            var wholeHTML = aNodes.Select(node =>
            {
                var h3Node = node.SelectSingleNode(".//h3");
                var text = h3Node?.InnerText;
                return new
                {
                    Text = text,
                    Href = "http://livetiming.pl" + node.Attributes["href"].Value
                };
            })
            .ToList();

            var data = Loader(url)
                .DocumentNode
                .SelectNodes("//span[@class='red']")
                .Where((node, index) => index % 2 == 1)
                .Select(node => node.InnerText)
                .ToList();

            Console.WriteLine(aNodes.Count);
            Console.WriteLine(wholeHTML.Count);
            Console.WriteLine(data.Count);

            var filteredCompetitionDates = wholeHTML.Zip(data, (competition, dateOfCompetition) => (competition.Text, dateOfCompetition, competition.Href))
                .Where(x =>
                {
                    if (DateTime.TryParse(x.dateOfCompetition, out DateTime competitionDate))
                    {
                        // Sprawdzamy, czy data zawodów jest mniejsza lub równa dzisiejszej dacie pomniejszonej o jeden dzień
                        return competitionDate.Date <= DateTime.Today.AddDays(-1) && competitionDate.Year == DateTime.Now.Year;
                    }
                    return false;
                })
                .ToList();
            Console.WriteLine("liczba zmiennych:" + filteredCompetitionDates.Count);
            foreach (var (competition, dateofCompetition, href) in filteredCompetitionDates)
            {
                Console.WriteLine(competition);
                Console.WriteLine(competition);
                Console.WriteLine(competition);
                Console.ReadKey();
            }
            

            foreach (var (competition, dateofCompetition, href) in filteredCompetitionDates)
            {
                Console.WriteLine($"{competition} {dateofCompetition} {href}");

                // Przejdź do wyników live
                var resultsPageUrl = GetResultsPageUrl(href);
                if (!string.IsNullOrEmpty(resultsPageUrl))
                {
                    Console.WriteLine($"Przekierowanie do wyników live: {resultsPageUrl}");

                    // Pobierz link do pliku PDF "Progresja zawodników (szczegóły)" z wyników live
                    var pdfFileUrl = GetPdfFileUrl(resultsPageUrl);
                    if (!string.IsNullOrEmpty(pdfFileUrl))
                    {
                        Console.WriteLine($"Link do pliku PDF: {pdfFileUrl}");

                        // Pobierz plik PDF i zapisz go na dysku
                        await DownloadPdfFile(pdfFileUrl, "C:\\users\\mzkwcim\\Downloads\\ProgressionDetails.pdf");

                        // Poczekaj na pobranie pliku PDF i przeczytaj jego zawartość
                        ProcessPdfFile("C:\\users\\mzkwcim\\Downloads\\ProgressionDetails.pdf");
                    }
                }
                Console.ReadKey();
            }

            // Sprawdzamy, czy wszystkie daty zawodów są z bieżącego roku
            bool allCompetitionDatesCurrentYear = data.All(date =>
            {
                if (DateTime.TryParse(date, out DateTime competitionDate))
                {
                    return competitionDate.Year == DateTime.Now.Year;
                }
                return false;
            });

            if (allCompetitionDatesCurrentYear)
            {
                // Aktualizujemy URL, dodając 1 do liczby po ostatnim "/"
                int lastSlashIndex = url.LastIndexOf('/');
                if (lastSlashIndex != -1)
                {
                    string pageNumberString = url.Substring(lastSlashIndex + 1);
                    Console.WriteLine(pageNumberString);
                    if (int.TryParse(pageNumberString, out int pageNumber))
                    {
                        Console.WriteLine(pageNumber);
                        pageNumber++;
                        Console.WriteLine(pageNumber);
                        url = url.Substring(0, lastSlashIndex + 1) + pageNumber.ToString();
                    }
                }
            }
            else
            {
                // Jeśli przynajmniej jedna data zawodów nie jest z bieżącego roku, kończymy proces
                continueProcessing = false;
            }
        }

    }

    static List<string> GetTextFromPdfAsync(string pdfFilePath)
    {
        try
        {
            using (PdfDocument pdfDocument = new PdfDocument(new PdfReader(pdfFilePath)))
            {
                List<string> chunks = new List<string>();
                for (int i = 1; i <= pdfDocument.GetNumberOfPages(); i++)
                {
                    ITextExtractionStrategy strategy = new SimpleTextExtractionStrategy();
                    string pageText = PdfTextExtractor.GetTextFromPage(pdfDocument.GetPage(i), strategy);
                    chunks.Add(pageText);
                }
                return chunks;
            }
        }
        catch (IOException ex)
        {
            Console.WriteLine($"Błąd podczas odczytywania pliku PDF: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Wystąpił nieoczekiwany błąd: {ex.Message}");
        }
        return new List<string>();
    }


    static void ProcessPdfFile(string pdfFilePath)
    {
        if (!File.Exists(pdfFilePath))
        {
            Console.WriteLine($"Plik PDF '{pdfFilePath}' nie istnieje.");
            return;
        }

        List<string> pdfTextChunks = GetTextFromPdfAsync(pdfFilePath);
        if (pdfTextChunks.Any(chunk => chunk.Contains("KS Posnania Poznań")))
        {
            // Znaleziono frazę "KS Posnania Poznań", przetwarzaj tekst
            List<string> importantStrings = SelectImportantString(pdfTextChunks.ToList());
            foreach (string str in importantStrings)
            {
                Console.WriteLine(str);
            }
        }
        else
        {
            // Nie znaleziono frazy "KS Posnania Poznań", usuń plik PDF
            File.Delete(pdfFilePath);
            Console.WriteLine($"Plik PDF '{pdfFilePath}' nie zawiera frazy 'KS Posnania Poznań' i został usunięty.");
        }
    }

    public static List<string> SelectImportantString(List<string> chunksOfText)
    {
        int adder = 0;
        List<string> competition = new List<string>();
        foreach (string chunk in chunksOfText)
        {
            string name = "";
            foreach (string c in chunk.Split("\n"))
            {
                string swimmingEvent = "";
                if (Regex.IsMatch(c, @"\w+,\s+\d+") || Regex.IsMatch(c, @"\d+\w\s"))
                {
                    string[] distance = Regex.Replace(c, @"%.+", "").Split(" ");
                    if (c.Contains('%'))
                    {
                        swimmingEvent = $"{distance[0]} {distance[1]}";
                        competition.Add($"{name} {swimmingEvent} {StringOperator.ArabicToRomanianNumbers(Convert.ToInt32(distance[5].Replace(".", "")))} miejsce {distance[^5]} {StringOperator.IsPersonalBest(distance[^1])}");
                    }
                    else if (adder == 0 && !c.Contains('%'))
                    {
                        name = StringOperator.ToTitleString(c[..c.IndexOf(',')]);
                    }
                    else if (distance.Length > 5 && distance[5] != "-")
                    {
                        swimmingEvent = $"{distance[0]} {distance[1]}";
                        competition.Add($"{name} {swimmingEvent} {StringOperator.ArabicToRomanianNumbers(Convert.ToInt32(distance[5].Replace(".", "")))} miejsce {distance[6]} r.ż.");
                    }
                }
                adder++;
            }
            adder = 0;
        }
        return competition;
    }

    static async Task DownloadPdfFile(string pdfUrl, string savePath)
    {
        try
        {
            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync(pdfUrl);
                response.EnsureSuccessStatusCode(); // Upewniamy się, że żądanie zakończyło się sukcesem
                using (Stream fileStream = await response.Content.ReadAsStreamAsync())
                {
                    using (FileStream outputFileStream = new FileStream(savePath, FileMode.Create))
                    {
                        await fileStream.CopyToAsync(outputFileStream);
                    }
                }
            }
            Console.WriteLine($"Plik PDF '{savePath}' został pomyślnie pobrany i zapisany.");
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Błąd podczas pobierania pliku PDF: {ex.Message}");
        }
    }

    static string GetResultsPageUrl(string competitionHref)
    {
        var competitionHtml = Loader(competitionHref).DocumentNode;
        var resultsPageNode = competitionHtml.SelectSingleNode("//a[text()='Wyniki live' and @target='_blank']");

        return resultsPageNode?.Attributes["href"].Value;
    }

    static string GetPdfFileUrl(string resultsPageUrl)
    {
        var resultsHtml = Loader(resultsPageUrl).DocumentNode;
        var pdfFileNode = resultsHtml.SelectNodes("//a");
        if (pdfFileNode != null)
        {
            foreach (var node in pdfFileNode)
            {
                if (node.InnerText.Trim() == "Progresja zawodników (szczegóły)" || node.InnerText.Trim() == "Progression of Athletes (Details)")
                {
                    Console.WriteLine(Regex.Replace(resultsPageUrl, @"/[^/]+$", "") + "/" + node.Attributes["href"].Value);
                    return Regex.Replace(resultsPageUrl, @"/[^/]+$", "") + "/" + node.Attributes["href"].Value;
                }
            }
        }
        return null;
    }

    public static HtmlAgilityPack.HtmlDocument Loader(string url)
    {
        var httpClient = new HttpClient();
        var html = httpClient.GetStringAsync(url).Result;
        var htmlDocument = new HtmlDocument();
        htmlDocument.LoadHtml(html);
        return htmlDocument;
    }
}
