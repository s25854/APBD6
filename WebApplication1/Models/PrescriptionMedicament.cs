namespace WebApplication1.Models;

public class PrescriptionMedicament
{
    public int IdMedicament { get; set; }    
    public virtual Medicament IdMedicamentNavigation { get; set; }
    public int IdPrescription { get; set; }
    public virtual Prescription IdPrescriptionNavigation { get; set; }
    public int? Dose { get; set; }
    public string Details { get; set; }
}