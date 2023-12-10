using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Mvc;
using Newtonsoft.Json;
using webleitour.Container.Models;
using NLog;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.Text;

namespace webleitour.Container.Controllers
{
    public class BookController : Controller
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        private readonly HttpClient httpClient;

        public BookController()
        {
            httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri("https://localhost:5226/");
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        }
        public async Task<ActionResult> BookPage(string ISBN)
        {
            if (!string.IsNullOrEmpty(ISBN))
            {
                logger.Info($"ISBN received: {ISBN}");

                HttpResponseMessage response = await httpClient.GetAsync($"api/SearchBy/isbn/{ISBN}");

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    Book book = JsonConvert.DeserializeObject<Book>(json);

                    HttpResponseMessage authorResponse = await httpClient.GetAsync($"api/SearchBy/author/{book.Authors}");

                    if (authorResponse.IsSuccessStatusCode)
                    {
                        string authorJson = await authorResponse.Content.ReadAsStringAsync();
                        List<Book> authorBooks = JsonConvert.DeserializeObject<List<Book>>(authorJson);

                        ViewBag.AuthorBooks = authorBooks;
                    }
                    else
                    {
                        logger.Error($"Failed to fetch data for author's books. Status code: {authorResponse.StatusCode}");
                    }

                    return View(book);
                }
                else
                {
                    logger.Error($"Failed to fetch data from the API. Status code: {response.StatusCode}");
                    return RedirectToAction("Error");
                }
            }
            else
            {
                logger.Error("ISBN parameter is missing or invalid.");
                return RedirectToAction("Error");
            }
        }

        public async Task<ActionResult> SavedBooks()
        {
            string baseUrl = "https://localhost:5226";

            if (Request.Cookies["AuthToken"] != null)
            {
                string token = Request.Cookies["AuthToken"].Value;
                httpClient.DefaultRequestHeaders.Add("token", token);
            }
            else
            {
                return RedirectToAction("HandleMissingToken");
            }

            HttpResponseMessage savedBooksResponse = await httpClient.GetAsync($"{baseUrl}/api/SavedBooks");
            logger.Info($"SAVED BOOK RESPONDE {savedBooksResponse}");

            if (savedBooksResponse.IsSuccessStatusCode)
            {
                string savedBooksJson = await savedBooksResponse.Content.ReadAsStringAsync();
                var savedBooks = JArray.Parse(savedBooksJson);

                List<Book> booksDetails = new List<Book>();

                foreach (var book in savedBooks)
                {
                    var bookKey = (string)book["bookKey"];

                    HttpResponseMessage bookDetailsResponse = await httpClient.GetAsync($"{baseUrl}/api/SearchBy/Key/{bookKey}");
                    logger.Info($"BOOK DETAILS RESPONDE {bookDetailsResponse}");

                    if (bookDetailsResponse.IsSuccessStatusCode)
                    {
                        string bookDetailsJson = await bookDetailsResponse.Content.ReadAsStringAsync();
                        var bookDetails = JsonConvert.DeserializeObject<Book>(bookDetailsJson);

                        booksDetails.Add(bookDetails);
                    }
                    else
                    {
                        logger.Error($"Failed to fetch details for book with key {bookKey}. Status code: {bookDetailsResponse.StatusCode}");
                    }

                }

                return View(booksDetails);
            }
            else
            {
                logger.Error($"Failed to fetch saved books. Status code: {savedBooksResponse.StatusCode}");
                return RedirectToAction("Error");
            }
        }




        [HttpPost]
        public async Task<ActionResult> CreateAnnotation(string annotationText, int bookSavedId, string bookIsbn10, string bookIsbn13)
        {
            if (Request.Cookies["AuthToken"] != null)
            {
                string token = Request.Cookies["AuthToken"].Value;

                var annotationData = new
                {
                    createdDate = DateTime.UtcNow,
                    annotationId = 0,
                    savedBookId = bookSavedId,
                    annotationText = annotationText,
                    alteratedDate = DateTime.UtcNow
                };

                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri("https://localhost:5226/");
                    client.DefaultRequestHeaders.Clear();
                    client.DefaultRequestHeaders.Add("token", token);

                    var content = new StringContent(
                        Newtonsoft.Json.JsonConvert.SerializeObject(annotationData),
                        Encoding.UTF8,
                        "application/json"
                    );
                    logger.Info($"RESPONSE CONTENT {content}");


                    HttpResponseMessage response = await client.PostAsync("api/annotations/savedBook/" + bookSavedId, content);
                    logger.Info($"RESPONSE BOOKPAGE {response}");

                    if (response.IsSuccessStatusCode)
                    {
                        return RedirectToAction("BookPage", new { ISBN = bookIsbn10 });
                    }
                    else
                    {
                        return RedirectToAction("BookPage", new { ISBN = bookIsbn10 });
                    }
                }
            }
            else
            {
                return RedirectToAction("BookPage", new { ISBN = bookIsbn10 });
            }
        }
    }
}

