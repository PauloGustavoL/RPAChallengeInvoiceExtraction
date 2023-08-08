using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using RPAChallengeInvoiceExtraction.Models;
using System.Globalization;
using System.Text.RegularExpressions;
using Tesseract;

public static class Program
{

    public static ChromeDriver? _driver;
    public static IWebElement? _element;

    public static void Main()
    {
        _driver = new ChromeDriver();
        _driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(30);
        _driver.Navigate().GoToUrl("https://rpachallengeocr.azurewebsites.net/");

        Thread.Sleep(3000);

        var begin = _driver.FindElement(By.TagName("button"));
        begin.Click();

        Thread.Sleep(2000);

        ReadingTable();

        Thread.Sleep(2000);

        sendCSV();

        Console.ReadLine();

    }

    private static void ReadingTable()
    {

        List<CSVModels> csvDataList = new List<CSVModels>();       

        IWebElement tabela = _driver.FindElement(By.Id("tableSandbox"));
    
        while (true)
        {
            
            IList<IWebElement> linhas = tabela.FindElements(By.TagName("tr"));
           
            foreach (IWebElement linha in linhas)
            {
                
                IList<IWebElement> celulas = linha.FindElements(By.TagName("td"));

                if (celulas.Count >= 3)
                {
                    string dataString = celulas[2].Text;
                    DateTime dataReferenciaString = DateTime.Today;
                    string formatoData = "dd-MM-yyyy";

                    // Converter as strings para objetos DateTime
                    DateTime data;
                    if (DateTime.TryParseExact(dataString, formatoData, null, System.Globalization.DateTimeStyles.None, out data))
                    {
                        if (data <= dataReferenciaString)
                        {
                            CSVModels csvData = new CSVModels();
                            
                            csvData.numberID = celulas[0].Text;
                            csvData.ID = celulas[1].Text;
                            csvData.DueData = dataString;

                            csvDataList.Add(csvData);
                        
                            IWebElement linkImagem = linha.FindElement(By.XPath(".//a[contains(@href, '.jpg')]"));                         
                            string imageURL = linkImagem.GetAttribute("href");

                            // download da imagem
                            using (HttpClient httpClient = new HttpClient())
                            {
                                HttpResponseMessage response = httpClient.GetAsync(imageURL).Result;
                                if (response.IsSuccessStatusCode)
                                {
                                    // Obter o conteúdo do arquivo JPEG como um array de bytes
                                    byte[] imagemBytes = response.Content.ReadAsByteArrayAsync().Result;
                                   
                                    string nomeArquivoLocal = Path.GetFileName(imageURL);
                                    string caminhoLocal = Path.Combine(Directory.GetCurrentDirectory(), nomeArquivoLocal);
                                    File.WriteAllBytes(caminhoLocal, imagemBytes);                                   

                                    ReadingImg(caminhoLocal, csvData.ID, csvData.DueData);
                                }
                                else
                                {
                                   Console.WriteLine("Falha ao fazer o download da imagem.");
                                }
                            }

                        }
                    }
                }
            }

            Thread.Sleep(2000);
           
            IWebElement? proximaPaginaDesabilitada = null;
            IWebElement proximaPagina = _driver.FindElement(By.Id("tableSandbox_next"));

            Thread.Sleep(2000);

            try
            {
                proximaPaginaDesabilitada = _driver.FindElement(By.XPath("//*[contains(@class, 'paginate_button next disabled')]"));
            }
            catch (NoSuchElementException)
            {
                // Se não encontrar o elemento, significa que o botão "Next" está habilitado, então podemos clicar nele usando Actions
                try
                {
                    Actions actions = new Actions(_driver);
                    actions.MoveToElement(proximaPagina).Click().Perform();
                }
                catch (ElementClickInterceptedException)
                {
                    // Se ocorrer a exceção de "element click intercepted", ignore-a e continue para a próxima iteração do loop
                }
            }

            // Se o botão "Next" estiver desabilitado, significa que não há mais páginas para navegar, então saímos do loop
            if (proximaPaginaDesabilitada != null)
            {
                break;
            }
        
    }

    }


    private static void ReadingImg(string imgTest, string fullID, string webData)
    {
        
        var tessdata = @"C:\Users\fiska\Desktop\projetos\RPAChallengeInvoiceExtraction\RPAChallengeInvoiceExtraction\RPAChallengeInvoiceExtraction\obj\Debug\net7.0\tessdata";

        try
        {
            using (var engine = new TesseractEngine(tessdata, "eng"))
            {
                using (var img = Pix.LoadFromFile(imgTest))
                {
                    using (var page = engine.Process(img))
                    {
                        var linhas = page.GetText().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                        string padraoRegex = @"#\d+";                       
                        string padraoRegexTotal = @"Total\s+(\d+(\.\d{1,2})?)";
                        string padraoRegexData = @"\d{4}-\d{2}-\d{2}";
                        string regexCompany = @"(Aenean LLC|Sit Amet Corp)";

                        string invoiceDate = "";
                        string company = "";
                        string totalValue = "";
                        string invoiceNumber = "";

                        
                        

                        foreach (string linha in linhas)
                        {
                            Match matchData = Regex.Match(linha, padraoRegexData);                           
                            MatchCollection totalMatches = Regex.Matches(linha, padraoRegexTotal);
                            MatchCollection matches = Regex.Matches(linha, padraoRegex);
                            Match matchCompany = Regex.Match(linha, regexCompany);

                            if (matchCompany.Success)
                            {
                               
                                company = matchCompany.Groups[0].Value;                         

                            }

                            if (matchData.Success)
                            {
                                invoiceDate = matchData.Value;
                            }

                            foreach (Match match in totalMatches)
                            {
                                totalValue = match.Groups[1].Value;
                            }

                            foreach (Match match in matches)
                            {
                                 invoiceNumber = match.Value.Substring(1);
                            }
                        }

                        if (DateTime.TryParseExact(invoiceDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate))
                        {
                            invoiceDate = parsedDate.ToString("dd-MM-yyyy");
                        }

                        string caminhoArquivoCSV = @"C:\Users\fiska\Desktop\projetos\RPAChallengeInvoiceExtraction\RPAChallengeInvoiceExtraction\RPAChallengeInvoiceExtraction\obj\Debug\net7.0\data.csv";


                        string csvData = $"{fullID}, {webData}, {invoiceNumber}, {invoiceDate}, {company}, {totalValue}\n";

                        // Se o arquivo CSV já existir, adiciona apenas os novos dados
                        if (File.Exists(caminhoArquivoCSV))
                        {
                            File.AppendAllText(caminhoArquivoCSV, csvData);
                        }
                        else
                        {
                            // Se o arquivo ainda não existir, adiciona o cabeçalho e os novos dados
                            string cabecalho = $"ID, DueDate, InvoiceNo, InvoiceDate, CompanyName, TotalDue\n";
                            File.WriteAllText(caminhoArquivoCSV, cabecalho + csvData);
                        }

                        Console.WriteLine("Dados salvos em dados.csv");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
          
        }
    }

    private static void sendCSV()
    {
                
        IWebElement inputFile = _driver.FindElement(By.Name("csv"));        
        string arquivoCSV = @"C:\Users\fiska\Desktop\projetos\RPAChallengeInvoiceExtraction\RPAChallengeInvoiceExtraction\RPAChallengeInvoiceExtraction\obj\Debug\net7.0\data.csv";
        inputFile.SendKeys(arquivoCSV);


    }
}


    


