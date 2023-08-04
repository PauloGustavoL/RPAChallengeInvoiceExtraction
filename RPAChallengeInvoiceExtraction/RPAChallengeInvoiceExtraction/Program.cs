using OpenQA.Selenium.Chrome;
using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using RPAChallengeInvoiceExtraction.Models;

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

        Console.ReadLine();

    }

    private static void ReadingTable()
    {

        List<CSVModels> csvDataList = new List<CSVModels>();
        

        // Localizar a tabela pelo ID
        IWebElement tabela = _driver.FindElement(By.Id("tableSandbox"));
       

        while (true)
        {
            // Encontrar todas as linhas (tr) da tabela
            IList<IWebElement> linhas = tabela.FindElements(By.TagName("tr"));

            // Loop pelas linhas da tabela e obter os dados
            foreach (IWebElement linha in linhas)
            {
                // Encontrar todas as células (td) da linha
                IList<IWebElement> celulas = linha.FindElements(By.TagName("td"));                
                


                // Loop pelas células da linha e obter o texto de cada célula
                foreach (IWebElement celula in celulas)
                {


                    string dataString = celulas[2].Text;
                    DateTime dataReferenciaString = DateTime.Today;
                    string formatoData = "dd-MM-yyyy";
                    // Converter as strings para objetos DateTime
                    DateTime data = DateTime.ParseExact(dataString, formatoData, null);

                    
                    if(data <= dataReferenciaString)
                    {
                        CSVModels csvData = new CSVModels();
                        // Atribuir os valores das células às propriedades do objeto
                        csvData.numberID = celulas[0].Text;
                        csvData.ID = celulas[1].Text;
                        // Adicionar o objeto à lista
                        csvDataList.Add(csvData);
                        Console.WriteLine(csvData.ID);
                    }


                }

                
            }

            Thread.Sleep(3500);

            IWebElement proximaPagina = _driver.FindElement(By.Id("tableSandbox_next"));
            IWebElement? proximaPaginaDesabilitada = null;

            Thread.Sleep(3500);
          

            try
            {
                proximaPaginaDesabilitada = _driver.FindElement(By.XPath("//*[contains(@class, 'paginate_button next disabled')]"));
            }
            catch (NoSuchElementException)
            {
                // Se não encontrar o elemento, significa que o botão "Next" está habilitado, então podemos clicar nele
                proximaPagina.Click();
            }

            // Se o botão "Next" estiver desabilitado, significa que não há mais páginas para navegar, então saímos do loop
            if (proximaPaginaDesabilitada != null)
            {
                break;
            }

        }


    }
}