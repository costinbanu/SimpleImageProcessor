using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Net.Mime;
using System.Threading.Tasks;

namespace SimpleImageProcessor.Pages
{
    public class FileModel : PageModel
    {
        private readonly IDistributedCache _cache;

        public FileModel(IDistributedCache cache)
        {
            _cache = cache;
        }

        public async Task<IActionResult> OnGet(int id, Guid sessionId)
        {
            var content = await _cache.GetAsync($"{sessionId}_file_{id}");
            var contentType = await _cache.GetStringAsync($"{sessionId}_contentType_{id}");
            var fileName = await _cache.GetStringAsync($"{sessionId}_fileName_{id}");
            var cd = new ContentDisposition
            {
                FileName = fileName,
                Inline = true
            };
            Response.Headers.Add("Content-Disposition", cd.ToString());
            Response.Headers.Add("X-Content-Type-Options", "nosniff");
            return File(content, contentType, fileName);
        }
    }
}