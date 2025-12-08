using System;
using System.Collections.Generic;

namespace Flow.Launcher.Plugin.Vikunja
{
    public class ParsedTask
    {
        public string Title { get; set; } = "";
        public string? Project { get; set; }
        public DateTime? DueDate { get; set; }
        public int Priority { get; set; } = 0;
        public List<string> Labels { get; set; } = new();
        public string? Description { get; set; }
    }
}