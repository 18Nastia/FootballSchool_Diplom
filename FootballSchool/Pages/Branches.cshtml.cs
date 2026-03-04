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
using Microsoft.AspNetCore.Hosting;
using FootballSchool.Models;

namespace FootballSchool.Pages
{
    public class BranchesModel : PageModel
    {
        private readonly FootballSchoolContext _context;
        private readonly IWebHostEnvironment _environment;

        public BranchesModel(FootballSchoolContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
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
            public string PhotoPath { get; set; } = string.Empty;

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

            var query = _context.Branches
                .Include(b => b.Facilities)
                .AsQueryable();

            if (!string.IsNullOrEmpty(FilterCity))
                query = query.Where(b => b.CityBranch == FilterCity);

            if (!string.IsNullOrEmpty(FilterType))
                query = query.Where(b => b.Facilities.Any(f => f.TypeFacility == FilterType));

            if (!string.IsNullOrEmpty(FilterStatus))
                query = query.Where(b => b.Facilities.Any(f => f.StatusFacility == FilterStatus));

            var branchesData = await query.Select(b => new
            {
                b.BranchId,
                b.NameBranch,
                b.CityBranch,
                b.StreetBranch,
                b.HouseBranch,
                b.PhoneBranch,
                b.PhotoBranch,
                Facilities = b.Facilities.ToList()
            }).ToListAsync();

            var today = DateOnly.FromDateTime(DateTime.Today);

            foreach (var b in branchesData)
            {
                var trainings = await _context.Training
                    .Include(t => t.Coach)
                    .Include(t => t.Team)
                    .Where(t => t.Facility != null && t.Facility.BranchId == b.BranchId)
                    .ToListAsync();

                var teams = trainings.Where(t => t.Team != null)
                                     .Select(t => t.Team)
                                     .GroupBy(t => t.TeamId)
                                     .Select(g => g.First())
                                     .ToList();

                var upcoming = trainings.Where(t => t.DateTraining >= today)
                                        .OrderBy(t => t.DateTraining)
                                        .ThenBy(t => t.TimeTraining)
                                        .Take(10)
                                        .ToList();

                var mainCoach = trainings.Select(t => t.Coach).FirstOrDefault(c => c != null);
                string coachName = mainCoach != null ? $"{mainCoach.SurnameCoach} {mainCoach.NameCoach}" : "Не назначен";

                Branches.Add(new BranchDto
                {
                    BranchId = b.BranchId,
                    Name = b.NameBranch,
                    City = b.CityBranch,
                    Street = b.StreetBranch,
                    House = b.HouseBranch,
                    Phone = b.PhoneBranch,
                    PhotoPath = b.PhotoBranch ?? "",
                    GroupsCount = teams.Count,
                    CoachesCount = trainings.Select(t => t.CoachId).Distinct().Count(),
                    TrainingsCount = trainings.Count,
                    MainCoachName = coachName,
                    Facilities = b.Facilities,
                    UpcomingTrainings = upcoming,
                    BranchTeams = teams
                });
            }
        }

        public async Task<IActionResult> OnPostSaveBranchAsync(IFormFile? BranchPhoto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    TempData["ErrorMessage"] = "Ошибка заполнения формы.";
                    return RedirectToPage();
                }

                string? relativePhotoPath = null;

                if (BranchPhoto != null && BranchPhoto.Length > 0)
                {
                    string imgext = Path.GetExtension(BranchPhoto.FileName).ToLower();

                    if (imgext == ".jpg" || imgext == ".jpeg" || imgext == ".png" || imgext == ".gif")
                    {
                        string uniqueFileName = Guid.NewGuid().ToString() + imgext;
                        string uploadsFolder = Path.Combine(_environment.WebRootPath, "images", "branches");

                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await BranchPhoto.CopyToAsync(stream);
                        }
                        relativePhotoPath = "/images/branches/" + uniqueFileName;
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Разрешены только файлы .jpg, .png, .gif";
                        return RedirectToPage();
                    }
                }

                if (ModalBranch.BranchId == 0)
                {
                    if (relativePhotoPath != null)
                    {
                        ModalBranch.PhotoBranch = relativePhotoPath;
                    }
                    _context.Branches.Add(ModalBranch);
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

                        if (relativePhotoPath != null)
                        {
                            existing.PhotoBranch = relativePhotoPath;
                        }
                    }
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Филиал успешно сохранен!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Критическая ошибка базы данных: " + ex.InnerException?.Message ?? ex.Message;
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteBranchAsync(int id)
        {
            var branch = await _context.Branches.Include(b => b.Facilities).FirstOrDefaultAsync(b => b.BranchId == id);
            if (branch != null)
            {
                try
                {
                    var facilityIds = branch.Facilities.Select(f => f.FacilityId).ToList();
                    var trainings = await _context.Training.Where(t => facilityIds.Contains(t.FacilityId)).ToListAsync();
                    var trainingIds = trainings.Select(t => t.TrainingId).ToList();
                    var attendances = await _context.Attendances.Where(a => trainingIds.Contains(a.TrainingId)).ToListAsync();

                    if (attendances.Any())
                    {
                        _context.Attendances.RemoveRange(attendances);
                        await _context.SaveChangesAsync();
                    }
                    if (trainings.Any())
                    {
                        _context.Training.RemoveRange(trainings);
                        await _context.SaveChangesAsync();
                    }
                    if (branch.Facilities.Any())
                    {
                        _context.Facilities.RemoveRange(branch.Facilities);
                        await _context.SaveChangesAsync();
                    }

                    if (!string.IsNullOrEmpty(branch.PhotoBranch))
                    {
                        var filePath = Path.Combine(_environment.WebRootPath, branch.PhotoBranch.TrimStart('/'));
                        if (System.IO.File.Exists(filePath))
                        {
                            System.IO.File.Delete(filePath);
                        }
                    }

                    _context.Branches.Remove(branch);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Филиал успешно удален!";
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "Ошибка при удалении: " + ex.Message;
                }
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostSaveFacilityAsync()
        {
            ModelState.Clear();

            TryValidateModel(ModalFacility, nameof(ModalFacility));
            ModelState.Remove("ModalFacility.Branch");
            ModelState.Remove("ModalFacility.Training");

            if (!ModelState.IsValid)
            {
                var errs = ModelState.Where(x => x.Value?.Errors.Count > 0)
                    .Select(x => $"{x.Key}: {string.Join(", ", x.Value!.Errors.Select(e => e.ErrorMessage))}");
                TempData["ErrorMessage"] = "Ошибка заполнения площадки: " + string.Join(" | ", errs);
                return RedirectToPage();
            }

            try
            {
                if (ModalFacility.FacilityId == 0)
                {
                    var newFacility = new Facility
                    {
                        BranchId = ModalFacility.BranchId,
                        NameFacility = ModalFacility.NameFacility,
                        TypeFacility = ModalFacility.TypeFacility,
                        CapacityFacility = ModalFacility.CapacityFacility,
                        StatusFacility = ModalFacility.StatusFacility,
                        CostFacility = ModalFacility.CostFacility,
                        NumberFacility = ModalFacility.NumberFacility
                    };
                    _context.Facilities.Add(newFacility);
                    TempData["SuccessMessage"] = "Новая площадка добавлена!";
                }
                else
                {
                    var existing = await _context.Facilities.FindAsync(ModalFacility.FacilityId);
                    if (existing != null)
                    {
                        existing.NameFacility = ModalFacility.NameFacility;
                        existing.TypeFacility = ModalFacility.TypeFacility;
                        existing.CapacityFacility = ModalFacility.CapacityFacility;
                        existing.StatusFacility = ModalFacility.StatusFacility;
                        existing.CostFacility = ModalFacility.CostFacility;
                        existing.NumberFacility = ModalFacility.NumberFacility;
                        TempData["SuccessMessage"] = "Данные площадки обновлены!";
                    }
                }
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Ошибка при сохранении площадки: " + ex.Message;
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteFacilityAsync(int id)
        {
            var facility = await _context.Facilities.FindAsync(id);
            if (facility != null)
            {
                try
                {
                    var trainings = await _context.Training.Where(t => t.FacilityId == id).ToListAsync();
                    var trainingIds = trainings.Select(t => t.TrainingId).ToList();
                    var attendances = await _context.Attendances.Where(a => trainingIds.Contains(a.TrainingId)).ToListAsync();

                    if (attendances.Any())
                    {
                        _context.Attendances.RemoveRange(attendances);
                        await _context.SaveChangesAsync();
                    }
                    if (trainings.Any())
                    {
                        _context.Training.RemoveRange(trainings);
                        await _context.SaveChangesAsync();
                    }

                    _context.Facilities.Remove(facility);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Площадка успешно удалена!";
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "Ошибка при удалении площадки: " + ex.Message;
                }
            }
            return RedirectToPage();
        }
    }
}