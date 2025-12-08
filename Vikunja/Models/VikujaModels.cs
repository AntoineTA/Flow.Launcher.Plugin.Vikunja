using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Flow.Launcher.Plugin.Vikunja.Models
{
    public class VikujaTask
    {
        [JsonProperty("title")]
        public string Title { get; set; } = "";

        [JsonProperty("description")]
        public string? Description { get; set; }

        [JsonProperty("due_date")]
        public DateTime? DueDate { get; set; }

        [JsonProperty("priority")]
        public int Priority { get; set; } = 0;

        [JsonProperty("project_id")]
        public int ProjectId { get; set; }

        // Note: Labels are handled via a separate API call after task creation
        // [JsonProperty("labels")]
        // public List<VikujaLabel> Labels { get; set; } = new();
    }

    public class VikujaLabel
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; } = "";

        [JsonProperty("hex_color")]
        public string HexColor { get; set; } = "#1973ff";
    }

    public class VikujaProject
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; } = "";

        [JsonProperty("description")]
        public string? Description { get; set; }
    }

    public class VikujaTaskResponse
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; } = "";

        [JsonProperty("project_id")]
        public int ProjectId { get; set; }

        [JsonProperty("created")]
        public DateTime Created { get; set; }
    }

    public class VikujaLabelTask
    {
        [JsonProperty("label_id")]
        public int LabelId { get; set; }
    }

    public class VikujaError
    {
        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; } = "";
    }
}