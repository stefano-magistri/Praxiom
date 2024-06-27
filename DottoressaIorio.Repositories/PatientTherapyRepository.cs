using DottoressaIorio.Data;
using DottoressaIorio.Models;
using Microsoft.EntityFrameworkCore;

namespace DottoressaIorio.Repositories;

public class PatientTherapyRepository : GenericRepository<Therapy>
{
    private readonly ApplicationDbContext context;

    public PatientTherapyRepository(ApplicationDbContext context) : base(context)
    {
        this.context = context;
    }

    public async Task<IList<Therapy>> GetAllsync(int patientId)
    {
        return await context.Therapies
            .Include(x => x.Patient)
            .Where(t => !t.Deleted && t.PatientId == patientId)
            .OrderBy(t => t.CreatedDate)
            .ToListAsync();
    }
}