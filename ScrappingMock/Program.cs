using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System.Text;
using System.Xml;

namespace ScrappingMock
{
    class ItemAcervo
    {
        public string Titulo { get; set; }
        public string Historia { get; set; }
        public string Modalidade { get; set; }
        public string Especificacoes { get; set; }
        public string Data { get; set; }
        public List<string> CarrosselItem { get; set; }
        public string Genero { get; set; } = "";
        public string Categoria { get; set; } = "";
        public string Inscricao { get; set; } = "";
    }
    public class Program
    {
        static void Main()
        {
            var options = new ChromeOptions();
            options.AddArgument("--headless=new");
            using var driver = new ChromeDriver(options);
            driver.Navigate().GoToUrl("https://www.museuflamengo.com/acervo/tipo/trofeus-medalhas-e-insignias");
            Thread.Sleep(3000);

            var baseUrl = "https://www.museuflamengo.com";
            var links = driver.FindElements(By.CssSelector("div.itens a"))
                              .Select(e => e.GetAttribute("href")?.Trim())
                              .Where(href => !string.IsNullOrWhiteSpace(href))
                              .Distinct()
                              .ToList();

            Console.WriteLine($"🔍 Total de itens encontrados: {links.Count}");
            var dados = new List<ItemAcervo>();

            for (int i = 0; i < links.Count; i++)
            {
                try
                {
                    var url = links[i].StartsWith("http") ? links[i] : baseUrl + links[i];
                    driver.Navigate().GoToUrl(url);
                    Thread.Sleep(1500);

                    var titulo = ObterTexto(driver, "h1");
                    var data = ObterTexto(driver, ".item p.data");

                    var div = driver.FindElement(By.CssSelector(".lado_lado.acervoBox.single .texto > div"));
                    var paragrafos = div.FindElements(By.CssSelector("p"));

                    string modalidade = "", categoria = "", genero = "", especificacoes = "", historia = "", inscricao = "";

                    foreach (var p in paragrafos)
                    {
                        var texto = p.Text;

                        if (texto.Contains("Modalidade:")) modalidade = ObterValor(texto, "Modalidade:");
                        if (texto.Contains("Categoria:")) categoria = ObterValor(texto, "Categoria:");
                        if (texto.Contains("Gênero:")) genero = ObterValor(texto, "Gênero:");

                        if (texto.Contains("Altura, Largura, Profundidade e Peso:"))
                            especificacoes = texto.Replace("Altura, Largura, Profundidade e Peso:", "").Trim();

                        if (texto.StartsWith("História:"))
                            historia = texto.Replace("História:", "").Trim();

                        if (texto.Contains("Inscrição no Troféu:"))
                            inscricao = texto.Replace("Inscrição no Troféu:", "").Trim();
                    }

                    var imagem = driver.FindElements(By.CssSelector("div.item img"))
                                       .Select(img => img.GetAttribute("src"))
                                       .Where(src => !string.IsNullOrWhiteSpace(src))
                                       .Distinct()
                                       .ToList();

                    dados.Add(new ItemAcervo
                    {
                        Titulo = titulo,
                        Modalidade = modalidade,
                        Categoria = categoria,
                        Genero = genero,
                        Especificacoes = especificacoes,
                        Historia = historia,
                        Inscricao = inscricao,
                        Data = data,
                        CarrosselItem = imagem
                    });

                    Console.WriteLine($"✔️ [{i + 1}/{links.Count}] {titulo}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Erro ao processar item {i + 1}: {ex.Message}");
                }
            }

            // Gerar um txt com os objetos ImagemAcervoViewModel prontos pra colar no código
            var sb = new StringBuilder();
            foreach (var item in dados)
            {
                sb.AppendLine($@"
                    new ImagemAcervoViewModel
                    {{
                        Id = 0,
                        Titulo = @""{item.Titulo}"",
                        Modalidade = ""{item.Modalidade}"",
                        Categoria = ""{item.Categoria}"",
                        Genero = ""{item.Genero}"",
                        Especificacoes = ""{item.Especificacoes}"",
                        CarrosselItem = new List<string>
                        {{
                            {string.Join(",\n        ", item.CarrosselItem.Select(img => $"\"{img}\""))}
                        }},
                        Historia = @""{item.Historia}"",
                        Inscricao = @""{item.Inscricao}"",
                        Data = ""{item.Data}""
                    }},");
            }

            File.WriteAllText("mock_trofeus_output.txt", sb.ToString());
            Console.WriteLine("\n✅ Código mock gerado em: mock_trofeus_output.txt");
        }

        static string ObterValor(string texto, string label)
        {
            try
            {
                int start = texto.IndexOf(label) + label.Length;
                int end = texto.IndexOf('\n', start);
                if (end == -1) end = texto.Length;
                return texto.Substring(start, end - start).Trim();
            }
            catch
            {
                return "";
            }
        }

        static string ObterTexto(IWebDriver driver, string seletor)
        {
            try
            {
                return driver.FindElement(By.CssSelector(seletor)).Text.Trim();
            }
            catch
            {
                return "";
            }
        }
    }
}
