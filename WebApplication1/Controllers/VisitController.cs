using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;
using System.Threading.Tasks;
using System.Linq;

namespace WebApplication1.Controllers
{
    [Route("/api/[controller]")]
    [ApiController]
    public class VisitController : ControllerBase
    {
        private readonly WebApplication1.Zadanie6Context.Zadanie6Context _context;

        public VisitController(WebApplication1.Zadanie6Context.Zadanie6Context context)
        {
            _context = context;
        }

        // Końcówkę pozwalającą na wystawienie nowej recepty.
        // Końcówka powinna przyjmować jako element żądania informacje
        // o pacjencie, recepcie i informacje o lekarz wystawionych na
        // danej recepcie.
        [HttpPost("AddPrescription")]
        public async Task<IActionResult> AddPrescription([FromBody] PrescriptionRequest request)
        {
            //Recepta może obejmować maksymalnie 10 leków. W innym
            //wypadku zwracamy błąd
            if (request == null || request.Medicaments.Count > 10)
            {
                return BadRequest("Invalid request or more than 10 medications provided.");
            }
            //Jeśli lek podany na recepcie nie istnieje, zwracamy błąd.
            foreach (var med in request.Medicaments)
            {
                var medicament = await _context.Medicaments.FindAsync(med.IdMedicament);
                if (medicament == null)
                {
                    return BadRequest($"Medicament with ID {med.IdMedicament} does not exist.");
                }
            }

            //Jeśli pacjent przekazany w żądaniu nie istnieje, wstawiamy
            //nowego pacjenta do tabeli Pacjent.

            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.IdPatient == request.IdPatient);
            if (patient == null)
            {
                patient = new Patient
                {
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    BirthDate = request.BirthDate
                };
                _context.Patients.Add(patient);
                await _context.SaveChangesAsync();
                request.IdPatient = patient.IdPatient; // Ustawienie IdPatient na nowo dodanego pacjenta
            }

            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.IdDoctor == request.DoctorId);
            if (doctor == null)
            {
                return BadRequest("Doctor does not exist.");
            }
            
            //Musimy sprawdzić czy DueData>=Date
            if (request.DueDate < request.Date)
            {
                return BadRequest("DueDate must be greater than or equal to Date.");
            }
            

            var prescription = new Prescription
            {
                Date = request.Date,
                DueDate = request.DueDate,
                IdPatient = request.IdPatient,
                IdDoctor = doctor.IdDoctor,
                PrescriptionMedicaments = request.Medicaments.Select(m => new PrescriptionMedicament
                {
                    IdMedicament = m.IdMedicament,
                    Dose = m.Dose,
                    Details = m.Description
                }).ToList()
            };

            _context.Prescriptions.Add(prescription);
            await _context.SaveChangesAsync();

            return Ok("Prescription added successfully.");
        }


        [HttpGet("GetPatientData/{id}")]
        public async Task<IActionResult> GetPatientDataAsync(int id)
        {
            var patient = await _context.Patients
                .Include(p => p.Prescriptions)
                .ThenInclude(pr => pr.PrescriptionMedicaments)
                .ThenInclude(pm => pm.IdMedicamentNavigation)
                .Include(p => p.Prescriptions)
                .ThenInclude(pr => pr.IdDoctorNavigation)
                .FirstOrDefaultAsync(p => p.IdPatient == id);

            if (patient == null)
            {
                return NotFound("Patient not found.");
            }

            var result = new PatientDataResponse
            {
                IdPatient = patient.IdPatient,
                FirstName = patient.FirstName,
                LastName = patient.LastName,
                BirthDate = patient.BirthDate,
                Prescriptions = patient.Prescriptions.Select(pr => new PrescriptionResponse
                {
                    IdPrescription = pr.IdPrescription,
                    Date = pr.Date,
                    DueDate = pr.DueDate,
                    Medicaments = pr.PrescriptionMedicaments.Select(pm => new MedicamentResponse
                    {
                        IdMedicament = pm.IdMedicament,
                        Name = pm.IdMedicamentNavigation.Name,
                        Dose = pm.Dose,
                        Details = pm.Details
                    }).ToList(),
                    Doctor = new DoctorResponse
                    {
                        IdDoctor = pr.IdDoctorNavigation.IdDoctor,
                        FirstName = pr.IdDoctorNavigation.FirstName,
                        LastName = pr.IdDoctorNavigation.LastName,
                        Email = pr.IdDoctorNavigation.Email
                    }
                }).OrderBy(pr => pr.DueDate).ToList()
            };

            return Ok(result);
        }
    }


    public class PrescriptionRequest
        {
            public int IdPatient { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public DateTime BirthDate { get; set; }
            public int DoctorId { get; set; }
            public List<MedicamentRequest> Medicaments { get; set; }
            public DateTime Date { get; set; }
            public DateTime DueDate { get; set; }
        }
    

        public class MedicamentRequest
        {
            public int IdMedicament { get; set; }
            public int Dose { get; set; }
            public string Description { get; set; }
        }

        public class PatientDataResponse
        {
            public int IdPatient { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public DateTime BirthDate { get; set; }
            public List<PrescriptionResponse> Prescriptions { get; set; }
        }

        public class PrescriptionResponse
        {
            public int IdPrescription { get; set; }
            public DateTime Date { get; set; }
            public DateTime DueDate { get; set; }
            public List<MedicamentResponse> Medicaments { get; set; }
            public DoctorResponse Doctor { get; set; }
        }

        public class MedicamentResponse
        {
            public int IdMedicament { get; set; }
            public string Name { get; set; }
            public int? Dose { get; set; }
            public string Details { get; set; }
        }

        public class DoctorResponse
        {
            public int IdDoctor { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string Email { get; set; }
        }


       
    }