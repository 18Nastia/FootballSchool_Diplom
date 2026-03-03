using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using FootballSchool.Models;

namespace FootballSchool.Pages
{
    public class BranchesModel : PageModel
    {
        private readonly FootballSchoolContext _context;

        public BranchesModel(FootballSchoolContext context)
        {
            _context = context;
        }

        public class BranchDto
        {
            public int BranchId { get; set; }
            public string Name { get; set; } = string.Empty;
            public string City { get; set; } = string.Empty;
            public string Street { get; set; } = string.Empty;
            public string House { get; set; } = string.Empty;
            public string Phone { get; set; } = string.Empty;
            public string Address => $"{Street}, {House}";

            public string PhotoMimeType { get; set; } = "image/jpeg";
            // Ñâîéñòâî äëÿ îòîáðàæåíèÿ êàðòèíêè èç áàéòîâ ÁÄ
            public string? PhotoBase64 { get; set; }

            public int GroupsCount { get; set; }
            public int CoachesCount { get; set; }
            public int TrainingsCount { get; set; }
            public string MainCoachName { get; set; } = string.Empty;

            public List<Facility> Facilities { get; set; } = new List<Facility>();
            public List<Training> UpcomingTrainings { get; set; } = new List<Training>();
            public List<Team> BranchTeams { get; set; } = new List<Team>();
        }

        public List<BranchDto> Branches { get; set; } = new List<BranchDto>();

        [BindProperty]
        public Branch ModalBranch { get; set; } = new Branch();

        [BindProperty]
        public Facility ModalFacility { get; set; } = new Facility();

        [BindProperty]
        public IFormFile? BranchPhoto { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? FilterCity { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? FilterType { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? FilterStatus { get; set; }

        public SelectList CityList { get; set; } = default!;
        public SelectList TypeList { get; set; } = default!;
        public SelectList StatusList { get; set; } = default!;

        public async Task OnGetAsync()
        {
            var cities = await _context.Branches.Select(b => b.CityBranch).Distinct().ToListAsync();
            CityList = new SelectList(cities);
            var types = await _context.Facilities.Select(f => f.TypeFacility).Distinct().ToListAsync();
            TypeList = new SelectList(types);
            var statuses = await _context.Facilities.Select(f => f.StatusFacility).Distinct().ToListAsync();
            StatusList = new SelectList(statuses);

            var query = _context.Branches.Include(b => b.Facilities).AsQueryable();
            if (!string.IsNullOrEmpty(FilterCity)) query = query.Where(b => b.CityBranch == FilterCity);
            if (!string.IsNullOrEmpty(FilterType)) query = query.Where(b => b.Facilities.Any(f => f.TypeFacility == FilterType));
            if (!string.IsNullOrEmpty(FilterStatus)) query = query.Where(b => b.Facilities.Any(f => f.StatusFacility == FilterStatus));

            var branchesFromDb = await query.ToListAsync();
            var today = DateOnly.FromDateTime(DateTime.Today);

            foreach (var b in branchesFromDb)
            {
                var trainings = await _context.Training.Include(t => t.Coach).Include(t => t.Team)
                    .Where(t => t.Facility != null && t.Facility.BranchId == b.BranchId).ToListAsync();
                var teams = trainings.Where(t => t.Team != null).Select(t => t.Team).GroupBy(t => t.TeamId).Select(g => g.First()).ToList();
                var upcoming = trainings.Where(t => t.DateTraining >= today).OrderBy(t => t.DateTraining).ThenBy(t => t.TimeTraining).Take(10).ToList();
                var mainCoach = trainings.Select(t => t.Coach).FirstOrDefault(c => c != null);

                Branches.Add(new BranchDto
                {
                    BranchId = b.BranchId,
                    Name = b.NameBranch,
                    City = b.CityBranch,
                    Street = b.StreetBranch,
                    House = b.HouseBranch,
                    Phone = b.PhoneBranch,
                    PhotoMimeType = b.PhotoBranch != null ? GetImageMimeType(b.PhotoBranch) ?? "image/jpeg" : "image/jpeg",
                    const long maxImageSize = 5 * 1024 * 1024; // 5 MB
                    if (BranchPhoto.Length > maxImageSize)
                    {
                        TempData["ErrorMessage"] = "     5 .";
                        return RedirectToPage();
                    }


                    if (GetImageMimeType(imageData) == null)
                    {
                        TempData["ErrorMessage"] = "   JPG, PNG, GIF  WebP.";
                        return RedirectToPage();
                    }
                    // Êîíâåðòèðóåì áàéòû èç ÁÄ â ñòðîêó äëÿ HTML
                    PhotoBase64 = b.PhotoBranch != null ? Convert.ToBase64String(b.PhotoBranch) : null,
                    GroupsCount = teams.Count,
                    CoachesCount = trainings.Select(t => t.CoachId).Distinct().Count(),
                    TrainingsCount = trainings.Count,
                    MainCoachName = mainCoach != null ? $"{mainCoach.SurnameCoach} {mainCoach.NameCoach}" : "Íå íàçíà÷åí",
                    Facilities = b.Facilities.ToList(),
                    UpcomingTrainings = upcoming,
                    BranchTeams = teams
                });
            }
        }

        public async Task<IActionResult> OnPostSaveBranchAsync()
        {
            try
            {
                byte[]? imageData = null;
                if (BranchPhoto != null && BranchPhoto.Length > 0)
                {
                    using (var ms = new MemoryStream())
                    {
                        await BranchPhoto.CopyToAsync(ms);
                        imageData = ms.ToArray();
                    }
                }

                if (ModalBranch.BranchId == 0)
                {
                    var newBranch = new Branch
                    {
                        NameBranch = ModalBranch.NameBranch,
                        CityBranch = ModalBranch.CityBranch,
                        StreetBranch = ModalBranch.StreetBranch,
                        HouseBranch = ModalBranch.HouseBranch,
                        PhoneBranch = ModalBranch.PhoneBranch,
                        PhotoBranch = imageData // Ñîõðàíÿåì áàéòû â ÁÄ
                    };
                    _context.Branches.Add(newBranch);
                    TempData["SuccessMessage"] = "Íîâûé ôèëèàë äîáàâëåí â áàçó!";
                }
                else
                {
                    var existing = await _context.Branches.FindAsync(ModalBranch.BranchId);
                    if (existing != null)
                    {
                        existing.NameBranch = ModalBranch.NameBranch;
                        existing.CityBranch = ModalBranch.CityBranch;
                        existing.StreetBranch = ModalBranch.StreetBranch;
                        existing.HouseBranch = ModalBranch.HouseBranch;
                        existing.PhoneBranch = ModalBranch.PhoneBranch;

                        // Îáíîâëÿåì ôîòî òîëüêî åñëè çàãðóæåíî íîâîå
                        if (imageData != null) existing.PhotoBranch = imageData;

                        TempData["SuccessMessage"] = "Äàííûå â ÁÄ îáíîâëåíû!";
                    }
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Îøèáêà ÁÄ: " + ex.Message;
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteBranchAsync(int id)
        {
            var branch = await _context.Branches.Include(b => b.Facilities).FirstOrDefaultAsync(b => b.BranchId == id);
            if (branch != null)
            {
                var facilityIds = branch.Facilities.Select(f => f.FacilityId).ToList();
                var trainings = await _context.Training.Where(t => facilityIds.Contains(t.FacilityId)).ToListAsync();
                var attendances = await _context.Attendances.Where(a => trainings.Select(t => t.TrainingId).Contains(a.TrainingId)).ToListAsync();

                _context.Attendances.RemoveRange(attendances);
                _context.Training.RemoveRange(trainings);
                _context.Facilities.RemoveRange(branch.Facilities);
                _context.Branches.Remove(branch);

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Ôèëèàë óäàëåí èç áàçû äàííûõ!";
            }
            return RedirectToPage();
        }

        // Ëîãèêà çàëîâ îñòàåòñÿ ïðåæíåé
        public async Task<IActionResult> OnPostSaveFacilityAsync()
        {
            if (ModalFacility.FacilityId == 0) _context.Facilities.Add(ModalFacility);
            else
            {
                var ex = await _context.Facilities.FindAsync(ModalFacility.FacilityId);
                if (ex != null)
                {
                    ex.NameFacility = ModalFacility.NameFacility; ex.TypeFacility = ModalFacility.TypeFacility;
                    ex.CapacityFacility = ModalFacility.CapacityFacility; ex.StatusFacility = ModalFacility.StatusFacility;
                    ex.CostFacility = ModalFacility.CostFacility; ex.NumberFacility = ModalFacility.NumberFacility;
                }
            }
            await _context.SaveChangesAsync();
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteFacilityAsync(int id)
        {
            var facility = await _context.Facilities.FindAsync(id);
            if (facility != null)
            {
                var trainings = await _context.Training.Where(t => t.FacilityId == id).ToListAsync();
                var attendances = await _context.Attendances.Where(a => trainings.Select(t => t.TrainingId).Contains(a.TrainingId)).ToListAsync();
                _context.Attendances.RemoveRange(attendances);
                _context.Training.RemoveRange(trainings);
                _context.Facilities.Remove(facility);
                await _context.SaveChangesAsync();
            }
            return RedirectToPage();
        }
   
        private static string? GetImageMimeType(byte[] imageData)
        {
            if (imageData.Length >= 3 && imageData[0] == 0xFF && imageData[1] == 0xD8 && imageData[2] == 0xFF)
                return "image/jpeg";

            if (imageData.Length >= 8 && imageData[0] == 0x89 && imageData[1] == 0x50 && imageData[2] == 0x4E && imageData[3] == 0x47
                && imageData[4] == 0x0D && imageData[5] == 0x0A && imageData[6] == 0x1A && imageData[7] == 0x0A)
                return "image/png";

            if (imageData.Length >= 6)
            {
                var gifHeader = System.Text.Encoding.ASCII.GetString(imageData, 0, 6);
                if (gifHeader == "GIF87a" || gifHeader == "GIF89a")
                    return "image/gif";
            }

            if (imageData.Length >= 12
                && imageData[0] == 0x52 && imageData[1] == 0x49 && imageData[2] == 0x46 && imageData[3] == 0x46
                && imageData[8] == 0x57 && imageData[9] == 0x45 && imageData[10] == 0x42 && imageData[11] == 0x50)
                return "image/webp";

            return null;
        }
    }
}