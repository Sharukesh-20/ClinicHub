
using ClinicHub.DTOs.Doctor;
using ClinicHub.DTOs.Doctor.DoctorProfile;
using ClinicHub.Filters;
using ClinicHub.Models;
using ClinicHub.Models.Clinic;
using ClinicHub.Models.Enums;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ClinicHub.Controllers
{
    public class DoctorsController : Controller
    {
        private readonly ClinicDbContext db;
        private readonly IWebHostEnvironment _environment;

        public DoctorsController(
            ClinicDbContext db,
            IWebHostEnvironment environment)
        {
            this.db = db;
            _environment = environment;
        }

        // Get the actual wwwroot folder path
        private string GetWebRootPath()
        {
            return _environment.WebRootPath
                ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
        }

        // Get doctors image folder
        private string GetDoctorsImageFolder()
        {
            var folderPath = Path.Combine(
                GetWebRootPath(),
                "images",
                "doctors"
            );

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            return folderPath;
        }

        // Save uploaded doctor image
        private async Task<string> SaveDoctorImageAsync(IFormFile imageFile)
        {
            var folderPath = GetDoctorsImageFolder();

            var extension = Path.GetExtension(imageFile.FileName);
            var fileName = Guid.NewGuid().ToString() + extension;

            var filePath = Path.Combine(folderPath, fileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(fileStream);
            }

            return $"/images/doctors/{fileName}";
        }

        // Delete old doctor image safely
        private void DeleteDoctorImage(string? imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath))
                return;

            var fileName = Path.GetFileName(imagePath);

            if (string.IsNullOrWhiteSpace(fileName))
                return;

            var imageFolder = GetDoctorsImageFolder();
            var oldImagePath = Path.Combine(imageFolder, fileName);

            if (System.IO.File.Exists(oldImagePath))
            {
                System.IO.File.Delete(oldImagePath);
            }
        }

        // ======================
        // Index
        // ======================

        [AuthFilter(Roles = new[] { "Admin", "Super_admin" })]
        public async Task<IActionResult> Index(
            string? search,
            int? specialtyId,
            AppointmentStatus? status)
        {
            var doctors = db.Doctors
                .Include(d => d.User)
                .Include(d => d.Specialty)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                doctors = doctors.Where(d =>
                    d.User.FullName.Contains(search) ||
                    d.User.Email.Contains(search));
            }

            if (specialtyId.HasValue)
            {
                doctors = doctors.Where(
                    d => d.SpecialtyId == specialtyId.Value);
            }

            ViewBag.Specialties = await db.Specialties
                .Where(s => s.IsActive)
                .OrderBy(s => s.Name)
                .ToListAsync();

            ViewBag.Search = search;
            ViewBag.SpecialtyId = specialtyId;
            ViewBag.Status = status;

            var doctorList = await doctors.ToListAsync();

            return View(doctorList);
        }

        // ======================
        // Details
        // ======================

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var doctor = await db.Doctors
                .AsNoTracking()
                .Where(d => d.Id == id)
                .Select(d => new DoctorDetailsDto
                {
                    Id = d.Id,
                    FullName = d.User.FullName,
                    Email = d.User.Email,
                    Phone = d.User.Phone,
                    SpecialtyName = d.Specialty != null
                        ? d.Specialty.Name
                        : "General Practice",
                    YearsOfExperience = d.YearsOfExperience,
                    Biography = d.Biography,
                    ImagePath = d.ImagePath,
                    CreatedAt = d.User.CreatedAt,
                    IsActive = d.IsActive,
                    AppointmentCount = d.Appointments.Count()
                })
                .FirstOrDefaultAsync();

            if (doctor == null)
                return NotFound();

            return View(doctor);
        }

        // ======================
        // Create
        // ======================

        [AuthFilter(Roles = new[] { "Admin", "Super_admin" })]
        public async Task<IActionResult> Create()
        {
            await LoadSpecialties();
            return View();
        }

        [AuthFilter(Roles = new[] { "Admin", "Super_admin" })]
        [HttpPost]
        public async Task<IActionResult> Create(DoctorCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                await LoadSpecialties(dto.SpecialtyId);
                return View(dto);
            }

            // Check duplicate email
            var emailExists = await db.Users
                .AnyAsync(u => u.Email == dto.Email);

            if (emailExists)
            {
                ModelState.AddModelError(
                    "Email",
                    "Email already exists.");

                await LoadSpecialties(dto.SpecialtyId);
                return View(dto);
            }

            var doctorRole = await db.Roles
                .FirstOrDefaultAsync(r => r.Name == "Doctor");

            if (doctorRole == null)
                return NotFound("Doctor role not found.");

            // Create user
            var user = new User
            {
                FullName = dto.FullName,
                Email = dto.Email,
                Phone = dto.Phone,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                RoleId = doctorRole.Id,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            // Upload image
            string? imagePath = null;

            if (dto.ImageFile != null && dto.ImageFile.Length > 0)
            {
                imagePath = await SaveDoctorImageAsync(dto.ImageFile);
            }

            db.Users.Add(user);
            await db.SaveChangesAsync();

            // Create doctor record
            var doctor = new Doctor
            {
                UserId = user.Id,
                SpecialtyId = dto.SpecialtyId,
                YearsOfExperience = dto.YearsOfExperience,
                Biography = dto.Biography,
                ImagePath = imagePath,
                IsActive = true
            };

            db.Doctors.Add(doctor);
            await db.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        // ======================
        // Edit
        // ======================

        [AuthFilter(Roles = new[] { "Admin", "Super_admin" })]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var doctor = await db.Doctors
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (doctor == null)
                return NotFound();

            var dto = new DoctorEditDto
            {
                Id = doctor.Id,
                FullName = doctor.User.FullName,
                Email = doctor.User.Email,
                Phone = doctor.User.Phone,
                SpecialtyId = doctor.SpecialtyId,
                IconOrImagePath = doctor.ImagePath,
                YearsOfExperience = doctor.YearsOfExperience,
                Biography = doctor.Biography
            };

            await LoadSpecialties(dto.SpecialtyId);

            return View(dto);
        }

        [AuthFilter(Roles = new[] { "Admin", "Super_admin" })]
        [HttpPost]
        public async Task<IActionResult> Edit(int id, DoctorEditDto dto)
        {
            if (id != dto.Id)
                return NotFound();

            if (!ModelState.IsValid)
            {
                await LoadSpecialties(dto.SpecialtyId);
                return View(dto);
            }

            var doctor = await db.Doctors
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (doctor == null)
                return NotFound();

            // Update user information
            doctor.User.FullName = dto.FullName;
            doctor.User.Email = dto.Email;
            doctor.User.Phone = dto.Phone;

            // Update doctor information
            doctor.SpecialtyId = dto.SpecialtyId;
            doctor.YearsOfExperience = dto.YearsOfExperience;
            doctor.Biography = dto.Biography;

            // Upload new image
            if (dto.ImageFile != null && dto.ImageFile.Length > 0)
            {
                var newImagePath =
                    await SaveDoctorImageAsync(dto.ImageFile);

                var oldImagePath = doctor.ImagePath;

                doctor.ImagePath = newImagePath;

                await db.SaveChangesAsync();

                // Delete old image only after database save succeeds
                DeleteDoctorImage(oldImagePath);

                return RedirectToAction("Index");
            }

            await db.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        // ======================
        // Toggle Status
        // ======================

        [AuthFilter(Roles = new[] { "Admin", "Super_admin" })]
        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var doctor = await db.Doctors
                .FirstOrDefaultAsync(d => d.Id == id);

            if (doctor == null)
                return NotFound();

            doctor.IsActive = !doctor.IsActive;

            await db.SaveChangesAsync();

            if (doctor.IsActive)
            {
                TempData["Success"] =
                    "Doctor activated successfully.";

                TempData["SuccessType"] = "success";
            }
            else
            {
                TempData["Success"] =
                    "Doctor deactivated successfully.";

                TempData["SuccessType"] = "danger";
            }

            return RedirectToAction("Index");
        }

        // ======================
        // Delete
        // ======================

        [AuthFilter(Roles = new[] { "Admin", "Super_admin" })]
        [HttpPost]
        public async Task<IActionResult> Delete(int? id)
        {
            var doctor = await db.Doctors
                .Include(d => d.User)
                .Include(d => d.Appointments)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (doctor == null)
                return NotFound();

            // Cannot delete while appointments are linked
            if (doctor.Appointments.Any())
            {
                TempData["Error"] =
                    $"This doctor cannot be deleted because " +
                    $"{doctor.Appointments.Count} appointment(s) " +
                    "are linked to this doctor.";

                return RedirectToAction("Index");
            }

            try
            {
                var user = doctor.User;

                db.Doctors.Remove(doctor);

                if (user != null)
                {
                    db.Users.Remove(user);
                }

                await db.SaveChangesAsync();

                TempData["Success"] =
                    "Doctor deleted successfully.";
            }
            catch (DbUpdateException)
            {
                TempData["Error"] =
                    "This doctor cannot be deleted because they have " +
                    "available time slots (schedule) set up. " +
                    "Please remove the doctor's available slots first, " +
                    "then try again.";
            }

            return RedirectToAction("Index");
        }

        // ======================
        // Load Specialties
        // ======================

        private async Task LoadSpecialties(int? selectedId = null)
        {
            ViewBag.Specialties = new SelectList(
                await db.Specialties
                    .Where(s => s.IsActive)
                    .ToListAsync(),
                "Id",
                "Name",
                selectedId
            );
        }

        // ======================
        // Doctor Profile
        // ======================

        [AuthFilter(Roles = new[] { "Doctor" })]
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
                return RedirectToAction("Login", "Account");

            var doctor = await db.Doctors
                .Include(d => d.User)
                .Include(d => d.Specialty)
                .Include(d => d.DoctorSchedules)
                .Include(d => d.Appointments)
                    .ThenInclude(a => a.Patient)
                        .ThenInclude(p => p.User)
                .FirstOrDefaultAsync(d => d.UserId == userId);

            if (doctor == null)
                return NotFound();

            var dto = new DoctorProfileDto
            {
                Id = doctor.Id,
                FullName = doctor.User.FullName,
                Email = doctor.User.Email,
                Phone = doctor.User.Phone,
                Biography = doctor.Biography,
                YearsOfExperience = doctor.YearsOfExperience,
                ImagePath = doctor.ImagePath,
                SpecialtyName = doctor.Specialty.Name,

                Schedules = doctor.DoctorSchedules
                    .Where(s => s.IsAvailable)
                    .Select(s => new DoctorScheduleDto
                    {
                        Id = s.Id,
                        AvailableDate = s.AvailableDate,
                        StartTime = s.StartTime,
                        EndTime = s.EndTime,
                        SlotMinutes = s.SlotMinutes,
                        IsAvailable = s.IsAvailable
                    })
                    .ToList(),

                Appointments = doctor.Appointments
                    .Select(a => new DoctorAppointmentDto
                    {
                        Id = a.Id,
                        PatientName = a.Patient.User.FullName,
                        AppointmentDate = a.AppointmentDate,
                        AppointmentTime = a.AppointmentTime,
                        Notes = a.Notes,
                        Status = (AppointmentStatus)a.Status
                    })
                    .ToList()
            };

            return View(dto);
        }

        // ======================
        // Edit Profile - GET
        // ======================

        [AuthFilter(Roles = new[] { "Doctor" })]
        public async Task<IActionResult> EditProfile()
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
                return RedirectToAction("Login", "Account");

            var doctor = await db.Doctors
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.UserId == userId);

            if (doctor == null)
                return NotFound();

            var dto = new DoctorProfileEditDto
            {
                FullName = doctor.User.FullName,
                Email = doctor.User.Email,
                Phone = doctor.User.Phone,
                Biography = doctor.Biography,
                YearsOfExperience = doctor.YearsOfExperience,
                ImagePath = doctor.ImagePath
            };

            return View(dto);
        }

        // ======================
        // Edit Profile - POST
        // ======================

        [AuthFilter(Roles = new[] { "Doctor" })]
        [HttpPost]
        public async Task<IActionResult> EditProfile(
            DoctorProfileEditDto dto)
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
                return RedirectToAction("Login", "Account");

            var doctor = await db.Doctors
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.UserId == userId);

            if (doctor == null)
                return NotFound();

            if (!ModelState.IsValid)
            {
                dto.ImagePath = doctor.ImagePath;
                return View(dto);
            }

            // Check duplicate email
            var emailExists = await db.Users
                .AnyAsync(u =>
                    u.Email == dto.Email &&
                    u.Id != doctor.UserId);

            if (emailExists)
            {
                ModelState.AddModelError(
                    "Email",
                    "Email already exists.");

                dto.ImagePath = doctor.ImagePath;

                return View(dto);
            }

            // Update user information
            doctor.User.FullName = dto.FullName;
            doctor.User.Email = dto.Email;
            doctor.User.Phone = dto.Phone;

            // Update doctor information
            doctor.Biography = dto.Biography;
            doctor.YearsOfExperience = dto.YearsOfExperience;

            string? oldImagePath = null;

            // Upload profile image
            if (dto.ImageFile != null && dto.ImageFile.Length > 0)
            {
                var newImagePath =
                    await SaveDoctorImageAsync(dto.ImageFile);

                oldImagePath = doctor.ImagePath;
                doctor.ImagePath = newImagePath;
            }

            await db.SaveChangesAsync();

            // Delete old image after successful database save
            if (!string.IsNullOrEmpty(oldImagePath))
            {
                DeleteDoctorImage(oldImagePath);
            }

            TempData["Success"] =
                "Profile updated successfully.";

            TempData["SuccessType"] = "success";

            return RedirectToAction("Profile");
        }
    }
}
