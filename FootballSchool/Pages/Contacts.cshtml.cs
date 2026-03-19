using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FootballSchool.Pages
{
    public class ContactsModel : PageModel
    {
        private readonly IWebHostEnvironment _env;

        public ContactsModel(IWebHostEnvironment env)
        {
            _env = env;
        }

        [BindProperty]
        public string PageContent { get; set; } = string.Empty;

        private string FilePath => Path.Combine(_env.ContentRootPath, "TextContent", "Contacts.txt");

        public async Task OnGetAsync()
        {
            if (System.IO.File.Exists(FilePath))
            {
                PageContent = await System.IO.File.ReadAllTextAsync(FilePath);
            }
            else
            {
                PageContent = "Наши контакты:\nТелефон: +7 (999) 000-00-00\nEmail: info@footballschool.ru";
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
            TempData["SuccessMessage"] = "Текст страницы «Контакты» успешно сохранен!";
            return RedirectToPage();
        }
    }
}