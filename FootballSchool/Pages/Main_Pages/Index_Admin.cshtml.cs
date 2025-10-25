using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FootballSchool.Pages.Main_Pages
{
    public class Index_AdminModel : PageModel
    {
        private readonly ILogger<Index_AdminModel> _logger;

        public Index_AdminModel(ILogger<Index_AdminModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {

        }
    }
}