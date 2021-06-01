using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ArtistYoutubeListGenerator.VocaDbService;
using HtmlAgilityPack;

namespace ArtistYoutubeListGenerator {

	class Program {

		private static readonly Regex nicoRegex = new Regex(@"nicovideo\.jp/mylist/(\d+)");

		private static bool IsFullLink(string str) {

			return (str.StartsWith("http://", StringComparison.InvariantCultureIgnoreCase)
				|| str.StartsWith("https://", StringComparison.InvariantCultureIgnoreCase)
				|| str.StartsWith("mailto:", StringComparison.InvariantCultureIgnoreCase));

		}

		public static string MakeLink(string partialLink) {

			if (string.IsNullOrEmpty(partialLink))
				return partialLink;

			if (IsFullLink(partialLink))
				return partialLink;

			return string.Format("http://{0}", partialLink);

		}

		static void Main(string[] args) {

			Console.WriteLine("Getting artists from VocaDB.");

			var client = new QueryServiceClient();

			var artists = client.GetArtistsWithYoutubeChannels(ContentLanguagePreference.Romaji).OrderBy(a => a.Name).ToArray();

			Console.WriteLine("Generating document.");

			var template = ResourceHelper.ReadTextFile("Template.html");
			var doc = new HtmlDocument();
			doc.LoadHtml(template);

			var count = doc.GetElementbyId("artistCount");
			count.InnerHtml = artists.Length + " artists";

			var generatedAt = doc.GetElementbyId("generatedAt");
			generatedAt.InnerHtml = DateTime.Now.ToString();

			var table = doc.GetElementbyId("artistsTable");

			foreach (var artist in artists) {

				var youtube = artist.WebLinks.FirstOrDefault(a => a.Url.Contains("youtube.com/user") || a.Url.Contains("youtube.com/channel"));

				if (youtube == null) {
					Console.WriteLine("Found no Youtube link for '{0}' - skipping.", artist.Name);
					continue;
				}

				var tr = table.AppendChild(doc.CreateElement("tr"));
				var titleTd = tr.AppendChild(doc.CreateElement("td"));
				var titleLink = titleTd.AppendChild(doc.CreateElement("a"));
				titleLink.InnerHtml = artist.Name;
				titleLink.SetAttributeValue("href", MakeLink(youtube.Url));
				titleLink.SetAttributeValue("title", artist.AdditionalNames);
				titleLink.SetAttributeValue("class", "extLink youtubeLink");

				var nicoTd = tr.AppendChild(doc.CreateElement("td"));
				var nicoWebLink = artist.WebLinks.FirstOrDefault(w => w.Url.Contains("www.nicovideo.jp/mylist/"));

				if (nicoWebLink != null) {

					var match = nicoRegex.Match(nicoWebLink.Url);

					if (match.Success) {

						var nicoLink = nicoTd.AppendChild(doc.CreateElement("a"));
						nicoLink.InnerHtml = "mylist/" + match.Groups[1].Value;
						nicoLink.SetAttributeValue("href", MakeLink(nicoWebLink.Url));
						nicoLink.SetAttributeValue("class", "extLink nicoLink");

					}

				}

				var vocaDbTd = tr.AppendChild(doc.CreateElement("td"));
				var vocaDbLink = vocaDbTd.AppendChild(doc.CreateElement("a"));
				vocaDbLink.InnerHtml = "VocaDB page";
				vocaDbLink.SetAttributeValue("href", string.Format("http://vocadb.net/Ar/{0}", artist.Id));
				vocaDbLink.SetAttributeValue("class", "extLink vocaDbLink");

			}

			Console.WriteLine("Saving document.");

			var path = (args.Any() ? args.First() : "ArtistYoutubes.html");

			doc.Save(path, Encoding.UTF8);

		}
	}
}
