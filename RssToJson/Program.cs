using LibGit2Sharp;
using Newtonsoft.Json;
using System.ServiceModel.Syndication;
using System.Xml;

namespace RssToJson
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Ler o arquivo JSON com as URLs
            var rssUrls = JsonConvert.DeserializeObject<RssUrlCollection>(File.ReadAllText("rssUrls.json"));

            // Lista para armazenar todos os itens de feed
            var allFeedItems = new List<dynamic>();

            // Iterar sobre cada URL do feed RSS
            foreach (var rssUrl in rssUrls.RssUrls)
            {
                var feedItems = await GetRssFeedItemsAsync(rssUrl);

                var rssData = feedItems.Select(item => new
                {
                    Title = item.Title.Text,
                    Content = item.Content == null ? null : item.Summary.Text, // Usar o conteúdo completo se disponível, caso contrário, o resumo,
                    Link = item.Links.FirstOrDefault()?.Uri.ToString(),
                    Date = item.PublishDate.DateTime,
                });

                allFeedItems.AddRange(rssData);
            }

            // Converter todos os itens de feed para JSON
            string json = JsonConvert.SerializeObject(allFeedItems, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText("allRssFeeds.json", json);

            Console.WriteLine("All RSS feed data has been saved to allRssFeeds.json");

            // Etapa 2: Ler o JSON das notícias e fazer commits vazios
            var feedItemsFromJson = JsonConvert.DeserializeObject<List<FeedItem>>(File.ReadAllText("allRssFeeds.json"));
            string repoPath = @"path\to\your\repo"; // Substitua pelo caminho do seu repositório

            foreach (var feedItem in feedItemsFromJson)
            {
                CommitNewsToRepo(feedItem, repoPath);
            }
        }

        static async Task<List<SyndicationItem>> GetRssFeedItemsAsync(string rssUrl)
        {
            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetStringAsync(rssUrl);
                using (var stringReader = new StringReader(response))
                using (var xmlReader = XmlReader.Create(stringReader))
                {
                    var feed = SyndicationFeed.Load(xmlReader);
                    return feed.Items.ToList();
                }
            }
        }

        static void CommitNewsToRepo(FeedItem feedItem, string repoPath)
        {
            using (var repo = new Repository(repoPath))
            {
                // Criar uma mensagem de commit composta por múltiplas partes
                var messageParts = new List<string>
                {
                    $"Title: {feedItem.Title}",
                    $"Content: {feedItem.Content}",
                    $"Link: {feedItem.Link}",
                    $"Date: {feedItem.Date}"
                };

                var commitMessage = string.Join(Environment.NewLine, messageParts);

                // Criar um commit vazio com a mensagem da notícia
                var signature = new Signature("Your Name", "your.email@example.com", DateTimeOffset.Now);
                var committer = signature;

                repo.Commit(commitMessage, signature, committer, new CommitOptions { AllowEmptyCommit = true });
                Console.WriteLine($"Commit created for news: {feedItem.Title}");
            }
        }

        public class RssUrlCollection
        {
            [JsonProperty("rssUrls")]
            public List<string> RssUrls { get; set; }
        }

        public class FeedItem
        {
            public string Title { get; set; }
            public string Content { get; set; }
            public string Link { get; set; }
            public DateTime Date { get; set; }
            public List<string> Tags { get; set; }
        }
    }
}
