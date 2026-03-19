using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FootballSchool.Pages
{
    public class AboutModel : PageModel
    {
        private readonly IWebHostEnvironment _env;

        public AboutModel(IWebHostEnvironment env)
        {
            _env = env;
        }

        [BindProperty]
        public string PageContent { get; set; } = string.Empty;

        // Файл будет храниться в папке TextContent в корне проекта
        private string FilePath => Path.Combine(_env.ContentRootPath, "TextContent", "About.txt");

        public async Task OnGetAsync()
        {
            if (System.IO.File.Exists(FilePath))
            {
                PageContent = await System.IO.File.ReadAllTextAsync(FilePath);
            }
            else
            {
                PageContent = "Добро пожаловать в нашу футбольную школу! Здесь будет размещена информация о нас.";
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!User.IsInRole("Admin")) return Forbid();

            var dir = Path.GetDirectoryName(FilePath);
            if (dir != null && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            await System.IO.File.WriteAllTextAsync(FilePath, PageContent ?? "");
            TempData["SuccessMessage"] = "Текст страницы «О нас» успешно сохранен!";
            return RedirectToPage();
        }
    }
}