using LazyCache;
using Microsoft.AspNetCore.Http;
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
            var file = _cache.Get<ProcessedImage>(id.ToString());
            if (file is null)
            {
                return NotFound();
            }

            var cd = new ContentDisposition
            {
                FileName = file.FileName,
                Inline = true
            };
            Response.Headers.Append("Content-Disposition", cd.ToString());
            Response.Headers.Append("X-Content-Type-Options", "nosniff");
            return File(file.Contents, file.MimeType, file.FileName);
        }
    }
}