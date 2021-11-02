using LazyCache;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SimpleImageProcessor.Contracts;
using System;
using System.Net.Mime;

namespace SimpleImageProcessor.Pages
{
    public class FileModel : PageModel
    {
        private readonly IAppCache _cache;

        public FileModel(IAppCache cache)
        {
            _cache = cache;
        }

        public IActionResult OnGet(Guid id)
        {
            var file = _cache.Get<ProcessingImage>(id.ToString());
            var cd = new ContentDisposition
            {
                FileName = file.FileName,
                Inline = true
            };
            Response.Headers.Add("Content-Disposition", cd.ToString());
            Response.Headers.Add("X-Content-Type-Options", "nosniff");
            return File(file.Contents, file.MimeType, file.FileName);
        }
    }
}