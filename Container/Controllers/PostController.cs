using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using webleitour.Container.Models;
using System.Text;
using NLog;
using static System.Net.Mime.MediaTypeNames;

namespace webleitour.Controllers
{
    public class PostController : Controller
    {
        private readonly string apiUrl = "https://localhost:5226/api/posts";
        private static Logger logger = LogManager.GetCurrentClassLogger();


        [HttpPost]
        public async Task<ActionResult> CreatePost(string feedPost)
        {
            int userId;
            if (Request.Cookies["UserID"] != null && int.TryParse(Request.Cookies["UserID"].Value, out userId))
            {
                var newPost = new Post
                {
                    Id = 0,
                    UserId = userId,
                    MessagePost = feedPost,
                    PostDate = DateTime.UtcNow,
                    AlteratedDate = DateTime.UtcNow
                };

                using (HttpClient client = new HttpClient())
                {
                    if (Request.Cookies["AuthToken"] != null)
                    {
                        string token = Request.Cookies["AuthToken"].Value;
                        client.DefaultRequestHeaders.Remove("token");
                        client.DefaultRequestHeaders.Add("token", token);
                    }

                    var postContent = new StringContent(JsonConvert.SerializeObject(newPost), Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await client.PostAsync(apiUrl, postContent);

                    if (response.IsSuccessStatusCode)
                    {
                        return RedirectToAction("Post");
                    }
                    else
                    {
                        ViewBag.ErrorMessage = "Erro ao criar o post. Tente novamente.";
                        return RedirectToAction("Post");
                    }
                }
            }
            else
            {
                ViewBag.ErrorMessage = "O ID do usuário não está presente no cookie.";
                return RedirectToAction("Index", "User");
            }
        }

        public async Task<ActionResult> Post()
        {
            int userId;
            if (Request.Cookies["UserID"] != null && int.TryParse(Request.Cookies["UserID"].Value, out userId))
            {
                var apiUrlUser = $"https://localhost:5226/api/User/{userId}";
                var apiUrlPosts = "https://localhost:5226/api/Posts";

                UserModel user = new UserModel();
                List<Post> publicacoes = new List<Post>();

                using (HttpClient client = new HttpClient())
                {
                    if (Request.Cookies["AuthToken"] != null)
                    {
                        string token = Request.Cookies["AuthToken"].Value;
                        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("token", token);
                    }

                    HttpResponseMessage responseUser = await client.GetAsync(apiUrlUser);
                    HttpResponseMessage responsePosts = await client.GetAsync(apiUrlPosts);

                    if (responseUser.IsSuccessStatusCode && responsePosts.IsSuccessStatusCode)
                    {
                        string contentUser = await responseUser.Content.ReadAsStringAsync();
                        string contentPosts = await responsePosts.Content.ReadAsStringAsync();

                        if (!string.IsNullOrEmpty(contentUser) && !string.IsNullOrEmpty(contentPosts))
                        {
                            user = JsonConvert.DeserializeObject<UserModel>(contentUser);
                            publicacoes = JsonConvert.DeserializeObject<List<Post>>(contentPosts).OrderByDescending(post => post.PostDate).ToList();
                            return View(Tuple.Create(publicacoes, user));
                        }
                        else
                        {
                            ViewBag.ErrorMessage = "A resposta da API está vazia.";
                            return RedirectToAction("Index", "User");
                        }
                    }
                    else
                    {
                        return View("Error");
                    }
                }
            }
            else
            {
                ViewBag.ErrorMessage = "O ID do usuário não está presente no cookie.";
                return RedirectToAction("Index", "User");
            }
        }

        public async Task<ActionResult> Comentario(int id)
        {
            var httpClient = new HttpClient();

            var postResponse = await httpClient.GetAsync($"https://localhost:5226/api/Posts/{id}");
            if (!postResponse.IsSuccessStatusCode)
            {
                return View();
            }

            var postContent = await postResponse.Content.ReadAsStringAsync();
            var post = JsonConvert.DeserializeObject<Post>(postContent);

            var commentsResponse = await httpClient.GetAsync($"https://localhost:5226/api/Posts/Comment/{id}");
            if (!commentsResponse.IsSuccessStatusCode)
            {
                return View(post);
            }

            var commentsContent = await commentsResponse.Content.ReadAsStringAsync();
            var comments = JsonConvert.DeserializeObject<List<Comment>>(commentsContent);

            var viewModel = new Tuple<Post, List<Comment>>(post, comments);
            return View(viewModel);
        }

        [HttpPost]
        public async Task<ActionResult> CreateComment(string Commenttext, int postId)
        {
            int userId;
            if (Request.Cookies["UserID"] != null && int.TryParse(Request.Cookies["UserID"].Value, out userId))
            {
                var newComment = new Comment
                {
                    CreatedDate = DateTime.UtcNow,
                    CommentId = 0,
                    UserId = userId,
                    UserName = "string", // Substitua pela lógica correta para obter o nome do usuário.
                    Email = "string", // Substitua pela lógica correta para obter o email do usuário.
                    UserPhoto = "string", // Substitua pela lógica correta para obter a foto do usuário.
                    PostId = postId,
                    MessagePost = Commenttext,
                    AlteratedDate = DateTime.UtcNow
                };

                using (HttpClient client = new HttpClient())
                {
                    if (Request.Cookies["AuthToken"] != null)
                    {
                        string token = Request.Cookies["AuthToken"].Value;
                        client.DefaultRequestHeaders.Remove("token");
                        client.DefaultRequestHeaders.Add("token", token);
                    }

                    var commentContent = new StringContent(JsonConvert.SerializeObject(newComment), Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await client.PostAsync("https://localhost:5226/api/Comment", commentContent);

                    if (response.IsSuccessStatusCode)
                    {
                        return RedirectToAction("Comentario", new { id = postId });
                    }
                    else
                    {
                        ViewBag.ErrorMessage = "Erro ao criar o comentário. Tente novamente.";
                        return RedirectToAction("Comentario", new { id = postId });
                    }
                }
            }
            else
            {
                ViewBag.ErrorMessage = "O ID do usuário não está presente no cookie.";
                return RedirectToAction("Index", "User");
            }
        }

        public async Task<ActionResult> SearchResult(string query)
        {
            int userId;
            if (Request.Cookies["UserID"] != null && int.TryParse(Request.Cookies["UserID"].Value, out userId))
            {
                var apiUrlSearch = $"https://localhost:5226/api/SearchBy/search/{query}";

                UserModel user = new UserModel();
                List<Post> searchResults = new List<Post>();

                using (HttpClient client = new HttpClient())
                {
                    if (Request.Cookies["AuthToken"] != null)
                    {
                        string token = Request.Cookies["AuthToken"].Value;
                        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                    }

                    HttpResponseMessage responseSearch = await client.GetAsync(apiUrlSearch);

                    if (responseSearch.IsSuccessStatusCode)
                    {
                        string contentSearch = await responseSearch.Content.ReadAsStringAsync();

                        if (!string.IsNullOrEmpty(contentSearch))
                        {
                            searchResults = JsonConvert.DeserializeObject<List<Post>>(contentSearch).OrderByDescending(post => post.PostDate).ToList();
                            return View(searchResults);
                        }
                        else
                        {
                            ViewBag.ErrorMessage = "A resposta da API de busca está vazia.";
                            return View("Error");
                        }
                    }
                    else
                    {
                        ViewBag.ErrorMessage = "Erro ao buscar na API.";
                        return View("Error");
                    }
                }
            }
            else
            {
                ViewBag.ErrorMessage = "O ID do usuário não está presente no cookie.";
                return RedirectToAction("Index", "User");
            }
        }

    }
}
