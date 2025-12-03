using Microsoft.AspNetCore.Mvc;

[ApiController]
public class PwaController : ControllerBase
{
    [HttpGet("manifest.json")]
    public IActionResult Manifest()
    {
        var origin = $"{Request.Scheme}://{Request.Host}";
        var manifest = new
        {
            name = "InsCanli",
            short_name = "Gelir-Gider",
            start_url = origin + "/login.html",
            scope = origin + "/",
            display = "standalone",
            background_color = "#ffffff",
            theme_color = "#ffffff",
            icons = new[] {
                new { src = origin + "/icons/icon-192.png", sizes = "192x192", type = "image/png" },
                new { src = origin + "/icons/icon-512.png", sizes = "512x512", type = "image/png" }
            }
        };
        return new JsonResult(manifest);
    }
}
