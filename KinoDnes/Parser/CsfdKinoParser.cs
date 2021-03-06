﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using KinoDnes.Cache;
using KinoDnes.Models;

namespace KinoDnes.Parser
{
    public class CsfdKinoParser
    {
        private const string AllCinemasCacheKey = "allCinemas";

        public List<Cinema> GetAllCinemas()
        {
            List<Cinema> allCinemaList = (List<Cinema>) ResponseCache.Get(AllCinemasCacheKey);
            if (allCinemaList != null)
            {
                return allCinemaList;
            }

            allCinemaList = new List<Cinema>();

            // CZ
            allCinemaList.AddRange(GetCinemaListing("http://www.csfd.cz/kino/?district-filter=0"));
            // SK
            allCinemaList.AddRange(GetCinemaListing("http://www.csfd.cz/kino/filtr-2/?district-filter=55"));

            ResponseCache.Set(AllCinemasCacheKey, allCinemaList);

            return allCinemaList;
        }

        private List<Cinema> GetCinemaListing(string url)
        {
            var document = GetDocumentByUrl(url);

            var cinemaElements = GetCinemaElements(document);

            var cinemaListing = new List<Cinema>();

            foreach (var cinema in cinemaElements)
            {
                var titleNode = cinema.SelectSingleNode("div/h2");
                var title = titleNode.InnerText;

                var movieNodes = GetMovieElements(cinema);
                var movieList = movieNodes.Select(GetMovie).ToList();

                cinemaListing.Add(new Cinema
                {
                    CinemaName = title,
                    Movies = movieList
                });
            }

            return cinemaListing;
        }

        private HtmlNodeCollection GetCinemaElements(HtmlDocument doc)
        {
            var result = doc.DocumentNode.SelectNodes("//*[contains(@class,'cinema')]");
            return result;
        }

        private HtmlNodeCollection GetMovieElements(HtmlNode cinemaNode)
        {
            var result = cinemaNode.SelectNodes("div/table/tr");
            return result;
        }

        private Movie GetMovie(HtmlNode movieNode)
        {
            var titleNode = movieNode.SelectSingleNode("th/a");

            var title = titleNode.InnerText;
            var url = $"http://csfd.cz{titleNode.GetAttributeValue("href", "")}";
            var yearNode = movieNode.SelectSingleNode("th/span");
            var year = yearNode.InnerText;

            var timeNodes = movieNode.SelectNodes("td[not(@class)]");

            var timeList = (from timeNode in timeNodes where !string.IsNullOrEmpty(timeNode.InnerText) select timeNode.InnerText).ToList();

            var flags = GetFlags(movieNode);
            var rating = GetMovieRating(url);

            return new Movie
            {
                MovieName = $"{title} {year}",
                Times = timeList,
                Url = url,
                Rating = rating,
                Flags = flags
            };
        }

        private List<string> GetFlags(HtmlNode movieNode)
        {
            var flagNodes = movieNode.SelectNodes("td[@class='flags']/span");
            if (flagNodes != null)
            {
                var flagList = (from flagNode in flagNodes where !string.IsNullOrEmpty(flagNode.InnerText) select flagNode.InnerText).ToList();
                return flagList;
            }
        
            return new List<string>();
        }

        private int GetMovieRating(string url)
        {
            var rating = ResponseCache.Get(url);
            if (rating != null)
            {
                return (int) rating;
            }
      
            HtmlDocument document = GetDocumentByUrl(url);

            var node = document.DocumentNode.SelectSingleNode("//h2[@class='average']");

            try
            {
                rating = int.Parse(Regex.Match(node.InnerText, @"\d*").Value);
            }
            catch
            {
                rating = -1;
            }

            ResponseCache.Set(url, rating);
            return (int) rating;
        }

        /// <summary>
        ///     Get HTMLDocument by URL
        /// </summary>
        /// <param name="url">URL to load HTML from</param>
        /// <returns>HtmlDocument instance</returns>
        private HtmlDocument GetDocumentByUrl(string url)
        {
            var request = (HttpWebRequest) WebRequest.Create(url);
            request.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip,deflate");
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            var response = request.GetResponse();
            using (var stream = response.GetResponseStream())
            {
                if (stream != null)
                {
                    var reader = new StreamReader(stream, Encoding.UTF8);
                    var responseString = reader.ReadToEnd();
                    var htmlDocument = new HtmlDocument();
                    htmlDocument.LoadHtml(responseString);
                    return htmlDocument;
                }
            }
            return null;
        }
    }
}