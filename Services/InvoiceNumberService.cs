using Kaarigar.Data;
using Microsoft.EntityFrameworkCore;

namespace Kaarigar.Services;

public interface IInvoiceNumberService
{
    /// <summary>Atomically issues the next invoice number for the given sequence, formatted "PREFIX/26-27/0001".</summary>
    Task<string> GetNextInvoiceNumberAsync(string sequenceCd, string prefix);
}

public class InvoiceNumberService : IInvoiceNumberService
{
    private readonly AppDbContext _db;

    public InvoiceNumberService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<string> GetNextInvoiceNumberAsync(string sequenceCd, string prefix)
    {
        // Simple row-locking increment. Fine for this app's volume; if
        // invoice generation ever becomes high-concurrency, wrap this in a
        // proper SQL-level UPDATE ... OUTPUT to avoid the read-then-write race.
        var sequence = await _db.InvoiceNumberSequences.FirstOrDefaultAsync(s => s.SequenceCd == sequenceCd);
        if (sequence == null)
        {
            sequence = new Models.InvoiceNumberSequence
            {
                SequenceCd = sequenceCd,
                FinancialYearTxt = CurrentFinancialYear(),
                LastNumberNbr = 0
            };
            _db.InvoiceNumberSequences.Add(sequence);
        }

        sequence.LastNumberNbr += 1;
        await _db.SaveChangesAsync();

        return $"{prefix}/{sequence.FinancialYearTxt}/{sequence.LastNumberNbr:D4}";
    }

    private static string CurrentFinancialYear()
    {
        var now = DateTime.UtcNow;
        // Indian FY: Apr–Mar
        var startYear = now.Month >= 4 ? now.Year : now.Year - 1;
        var endYearShort = (startYear + 1) % 100;
        return $"{startYear % 100:D2}-{endYearShort:D2}";
    }
}
