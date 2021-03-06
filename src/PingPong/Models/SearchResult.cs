using System;
using System.Json;
using Caliburn.Micro;

namespace PingPong.Models
{
    public class SearchResult : PropertyChangedBase, ITweetItem
    {
        public SearchResult(JsonObject json)
        {
            Text = json["text"]; // explicit conversion will unescape json
            Text = Text.UnescapeXml(); // unescape again for & escapes
            Id = json["id_str"];
            Source = json["source"];
            CreatedAt = json.GetSearchDateTime("created_at");
            User = new User
            {
                Id = json["from_user_id_str"],
                ScreenName = json["from_user"],
                ProfileImageUrl = json["profile_image_url"]
            };
        }

        public string Id { get; set; }
        public User User { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Text { get; set; }
        public string Source { get; set; }
    }
}