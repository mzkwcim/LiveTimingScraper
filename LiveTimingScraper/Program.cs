using HtmlAgilityPack;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium;


internal class Program
{
    public static void Main(string[] args)
    {
        List<string> athletes = ["Bartoszewska Marta", "Berg Maria", "Dopierala Piotr", "Drost Stanislaw", "Heymann Patryk",
            "Jaglowski Dawid", "Krzesniak Jakub", "Lechowicz Mikolaj", "Madelska Natalia", "Makowska Blanka", "Moros Bruno",
            "Olejniczak Mateusz", "Polody Estera", "Siepka Zofia", "Smykaj Antonina", "Szmidchen Alan", "Zakens Gabriela",
            "Horowska Zuzanna", "Kubiak Zuzanna", "Kurek Antoni", "Malicka Maja", "Mroczek Dominik", "Nogalska Iga",
            "Sewilo Martyna", "Sumisławska Aleksandra"];
        for (int k = 19; k < athletes.Count; k++)
        {
            IWebDriver driver = new ChromeDriver();

            // Otwarcie strony internetowej
            driver.Navigate().GoToUrl("https://www.swimrankings.net/index.php?page=athleteSelect&nationId=0&selectPage=SEARCH");
            IWebElement lastnNameBox = driver.FindElement(By.CssSelector("input#athlete_lastname.inputMedium"));
            lastnNameBox.SendKeys(athletes[k].Split(" ")[0]);

            IWebElement firstNameBox = driver.FindElement(By.CssSelector("input#athlete_firstname.inputMedium"));
            firstNameBox.SendKeys(athletes[k].Split(" ")[1]);
            Thread.Sleep(3000);
            IWebElement athleteButton = driver.FindElement(By.XPath("//*[@id=\"searchResult\"]/table/tbody/tr[2]/td[2]/a"));
            athleteButton.Click();
            
            Thread.Sleep(2000);
            IWebElement club = driver.FindElement(By.Id("nationclub"));
            int adder = 2;
            while (!club.Text.Contains("KS POSNANIA Poznan"))
            {
                Console.WriteLine("zawieram?");
                Thread.Sleep(1000);
                adder++;
                athleteButton = driver.FindElement(By.XPath($"//*[@id=\"searchResult\"]/table/tbody/tr[{adder}]/td[2]/a"));
                athleteButton.Click();
                club = driver.FindElement(By.Id("nationclub")); 
            }
            string athleteUrl = driver.Url + $"&result={DateTime.Now.Year}";
            Console.WriteLine("Adres URL aktualnej strony: " + athleteUrl);
            driver.Navigate().GoToUrl(athleteUrl);

            var distances = Loader(athleteUrl).DocumentNode.SelectNodes("//td[@class='city']//a[@href]");
            var dates = Loader(athleteUrl).DocumentNode.SelectNodes("//td[@class='date']");
            string fullname = $"{athletes[k].Split(" ")[0].ToUpper()}, {athletes[k].Split(" ")[1].ToLower()}";
            for (int i = 0; i < dates.Count; i += 2)
            {
                if (Convert.ToInt32(dates[i].InnerText.Substring(dates[i].InnerText.Length - 4).Trim()) == 2024)
                {
                    var htmlDocument = Loader($"https://www.swimrankings.net/index.php{distances[i / 2].GetAttributeValue("href", "").Replace("amp;", "")}");
                    driver.Navigate().GoToUrl($"https://www.swimrankings.net/index.php{distances[i / 2].GetAttributeValue("href", "").Replace("amp;", "")}");
                    var nameNodes = htmlDocument.DocumentNode.SelectNodes("//tr[contains(@class,'meetResult0') or contains(@class,'meetResult1')]/td[@class='name']/a");
                    var placeNodes = htmlDocument.DocumentNode.SelectNodes("//td[@class='meetPlace']");
                    var title = htmlDocument.DocumentNode.SelectSingleNode("//td[@class='titleLeft']").InnerText;
                    IWebElement element = driver.FindElement(By.XPath("//*[@id=\"content\"]/table/tbody/tr/td/form/table[3]/tbody/tr[1]/th[1]"));

                    // Pobierz tekst spod elementu
                    string[] text = element.Text.Replace(",", "").Split(" ")[0..3];
                    string distance = text[1] + TranslateStroke(text[2]) + TranslateGender(text[0]);



                    if (nameNodes != null)
                    {
                        for (int j = 0; j < nameNodes.Count; j += 2)
                        {
                            if (nameNodes[j].InnerText.Trim().ToLower() == fullname.ToLower())
                            {
                                Console.WriteLine($"{nameNodes[j].InnerText.Trim().Replace(",", "")} zajeła {placeNodes[j].InnerText} miejsce na dystansie {distance} na zawodach {title}");
                                break;
                            }
                        }
                    }
                }
            }
            driver.Quit();
        }
    }
    public static string TranslateGender(string key)
    {
        switch (key)
        {
            case "Men": return " Mężczyzn";
            case "Women": return " Kobiet";
            default: return "";
        }
    }
    public static string TranslateStroke(string key)
    {

        switch (key)
        {
            case "Freestyle": return " Dowolnym";
            case "Backstroke": return " Grzbietowym";
            case "Breaststroke": return " Klasycznym";
            case "Butterfly": return " Motylkowym";
            case "Medley": return " Zmiennym";
            default: return "";
        }
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