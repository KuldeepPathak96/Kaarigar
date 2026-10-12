using Kaarigar.Models;
using Microsoft.Extensions.Configuration;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Kaarigar.Services;

public interface IInvoicePdfService
{
    Task<string> GenerateKaarigarInvoicePdfAsync(KaarigarInvoice invoice, EmployerProfile? employerProfile, UserAccount? employer, UserAccount? worker, JobPost jobPost);

    Task<string> GenerateWebsiteInvoicePdfAsync(WebsiteInvoice invoice, EmployerProfile? employerProfile, UserAccount? employer, UserAccount? worker, JobPost jobPost);
}

public class InvoicePdfService : IInvoicePdfService
{
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;

    public InvoicePdfService(IConfiguration config, IWebHostEnvironment env)
    {
        _config = config;
        _env = env;

        // QuestPDF Community license — free for small businesses, required
        // to be set once before any document is generated.
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public Task<string> GenerateKaarigarInvoicePdfAsync(KaarigarInvoice invoice, EmployerProfile? employerProfile, UserAccount? employer, UserAccount? worker, JobPost jobPost)
    {
        // "Website Invoice" business term — see BillingModels.cs header.
        // Service Fee is the only payable line item; GST is shown as computed
        // on (wage + service fee), but the wage itself isn't billed here —
        // it's paid directly to the worker (see the Employee Invoice below).
        var wageBaseAmt = invoice.TaxableAmt - invoice.ServiceChargeAmt;
        var lineItems = new List<(string Name, string Note, decimal Taxable, decimal Gst)>
        {
            ("Kaarigar Platform Service Fee",
             $"Job: {jobPost.JobTitle} — Worker: {FullName(worker)}. GST calculated on wage (₹{wageBaseAmt:N2}) + service fee (₹{invoice.ServiceChargeAmt:N2}) = ₹{invoice.TaxableAmt:N2}.",
             invoice.ServiceChargeAmt, invoice.CgstAmt + invoice.SgstAmt + invoice.IgstAmt)
        };

        var path = BuildOutputPath("kaarigar-invoices", invoice.InvoiceNbr);
        RenderInvoicePdf(
            path,
            invoice.InvoiceNbr, invoice.InvoiceDt, invoice.DueDt, invoice.PlaceOfSupplyTxt,
            employerProfile, employer,
            lineItems,
            invoice.ServiceChargeAmt, invoice.CgstAmt, invoice.SgstAmt, invoice.IgstAmt, invoice.TotalAmt,
            invoice.GstTypeCd);

        return Task.FromResult(path);
    }

    public Task<string> GenerateWebsiteInvoicePdfAsync(WebsiteInvoice invoice, EmployerProfile? employerProfile, UserAccount? employer, UserAccount? worker, JobPost jobPost)
    {
        // "Employee Invoice" business term — see BillingModels.cs header.
        // Hours x Rate ONLY. No PF/ESIC/GST line items — paid directly by
        // the Employer to the worker, outside the platform.
        var jobNote = $"Job: {jobPost.JobTitle} — Worker: {FullName(worker)} — {invoice.HoursWorkedNbr:0.##} hrs @ Rs.{invoice.HourlyRateAmt:0.##}/hr. No GST — paid directly to the worker.";

        var lineItems = new List<(string Name, string Note, decimal Taxable, decimal Gst)>
        {
            ("Wages (Hours × Rate)", jobNote, invoice.ManpowerAmt, 0),
        };

        var path = BuildOutputPath("website-invoices", invoice.InvoiceNbr);
        RenderInvoicePdf(
            path,
            invoice.InvoiceNbr, invoice.InvoiceDt, invoice.DueDt, invoice.PlaceOfSupplyTxt,
            employerProfile, employer,
            lineItems,
            invoice.ManpowerAmt, 0m, 0m, 0m, invoice.TotalAmt,
            invoice.GstTypeCd);

        return Task.FromResult(path);
    }

    // ── shared rendering ─────────────────────────────────────────────────────

    private void RenderInvoicePdf(
        string outputPath,
        string invoiceNbr, DateTime invoiceDt, DateTime dueDt, string? placeOfSupply,
        EmployerProfile? employerProfile, UserAccount? employer,
        List<(string Name, string Note, decimal Taxable, decimal Gst)> lineItems,
        decimal taxableAmt, decimal cgstAmt, decimal sgstAmt, decimal igstAmt, decimal totalAmt,
        string gstTypeCd)
    {
        var company = _config.GetSection("CompanyProfile");
        var companyName = company["CompanyName"] ?? "KAARIGAR TECHNOLOGIES";
        var companyAddress = company["AddressLine"] ?? "";
        var companyPhone = company["Phone"] ?? "";
        var companyEmail = company["Email"] ?? "";
        var companyGstin = company["Gstin"] ?? "";
        var companyState = company["State"] ?? "24-Gujarat";

        var billToName = !string.IsNullOrWhiteSpace(employerProfile?.InvoiceNameTxt)
            ? employerProfile!.InvoiceNameTxt
            : (employerProfile?.CompanyName ?? FullName(employer));

        var billToAddress = !string.IsNullOrWhiteSpace(employerProfile?.BillingAddressTxt)
            ? employerProfile!.BillingAddressTxt
            : string.Join(", ", new[] { employerProfile?.AreaAddressTxt, employerProfile?.CityName }
                .Where(s => !string.IsNullOrWhiteSpace(s)));

        var billToContact = employer?.ContactNbr ?? "";
        var billToGstin = employerProfile?.GstinTxt ?? "—";
        var billToFinanceContact = employerProfile?.FinanceContactTxt;
        var billToAccountsEmail = employerProfile?.AccountsEmailTxt;

        var absolutePath = Path.Combine(_env.ContentRootPath, "wwwroot", outputPath);
        Directory.CreateDirectory(Path.GetDirectoryName(absolutePath)!);

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(24);
                page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));

                page.Content().Column(col =>
                {
                    col.Item().Border(1).Padding(4).Text("Tax Invoice").Bold().FontSize(12).AlignCenter();

                    col.Item().PaddingTop(4).Row(row =>
                    {
                        row.RelativeItem(2).Border(1).Padding(6).Column(c =>
                        {
                            c.Item().Text(companyName).Bold().FontSize(11);
                            c.Item().Text(companyAddress);
                            c.Item().Text($"Phone: {companyPhone}    Email: {companyEmail}");
                            c.Item().Text($"GSTIN: {companyGstin}    State: {companyState}");
                        });

                        row.RelativeItem(1).Border(1).Padding(6).Column(c =>
                        {
                            c.Item().Text($"Invoice No.: {invoiceNbr}").Bold();
                            c.Item().Text($"Date: {invoiceDt:dd/MM/yyyy}");
                            c.Item().Text($"Due Date: {dueDt:dd/MM/yyyy}");
                            c.Item().Text($"Place Of Supply: {placeOfSupply}");
                        });
                    });

                    col.Item().PaddingTop(4).Border(1).Padding(6).Column(c =>
                    {
                        c.Item().Text("Bill To:").Bold();
                        c.Item().Text(billToName).Bold();
                        c.Item().Text(billToAddress);
                        c.Item().Text($"Contact No: {billToContact}    GSTIN: {billToGstin}");
                        if (!string.IsNullOrWhiteSpace(billToFinanceContact) || !string.IsNullOrWhiteSpace(billToAccountsEmail))
                        {
                            var financeLine = string.Join("    ", new[]
                            {
                                !string.IsNullOrWhiteSpace(billToFinanceContact) ? $"Finance Contact: {billToFinanceContact}" : null,
                                !string.IsNullOrWhiteSpace(billToAccountsEmail) ? $"Accounts Email: {billToAccountsEmail}" : null,
                            }.Where(s => s != null));
                            c.Item().Text(financeLine);
                        }
                    });

                    col.Item().PaddingTop(4).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(20);
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Border(1).Padding(4).Text("#").Bold();
                            header.Cell().Border(1).Padding(4).Text("Item name").Bold();
                            header.Cell().Border(1).Padding(4).Text("Taxable Amount (Rs.)").Bold();
                            header.Cell().Border(1).Padding(4).Text("Amount (Rs.)").Bold();
                        });

                        var i = 1;
                        foreach (var item in lineItems)
                        {
                            table.Cell().Border(1).Padding(4).Text(i.ToString());
                            table.Cell().Border(1).Padding(4).Column(c =>
                            {
                                c.Item().Text(item.Name).Bold();
                                c.Item().Text(item.Note).FontSize(7).FontColor(Colors.Grey.Darken1);
                            });
                            table.Cell().Border(1).Padding(4).AlignRight().Text($"{item.Taxable:N2}");
                            table.Cell().Border(1).Padding(4).AlignRight().Text($"{item.Taxable + item.Gst:N2}");
                            i++;
                        }
                    });

                    col.Item().PaddingTop(4).Row(row =>
                    {
                        row.RelativeItem(3).Border(1).Padding(6).Column(c =>
                        {
                            c.Item().Text("GST Breakup").Bold();
                            c.Item().Text($"Taxable Amount: Rs. {taxableAmt:N2}");
                            if (gstTypeCd == "INTRA")
                            {
                                c.Item().Text($"CGST @ 9%: Rs. {cgstAmt:N2}");
                                c.Item().Text($"SGST @ 9%: Rs. {sgstAmt:N2}");
                            }
                            else
                            {
                                c.Item().Text($"IGST @ 18%: Rs. {igstAmt:N2}");
                            }
                        });

                        row.RelativeItem(2).Border(1).Padding(6).Column(c =>
                        {
                            c.Item().Row(r => { r.RelativeItem().Text("Sub Total:"); r.RelativeItem().AlignRight().Text($"Rs. {totalAmt:N2}"); });
                            c.Item().Row(r => { r.RelativeItem().Text("Total:").Bold(); r.RelativeItem().AlignRight().Text($"Rs. {totalAmt:N2}").Bold(); });
                        });
                    });

                    col.Item().PaddingTop(4).Border(1).Padding(6).Column(c =>
                    {
                        c.Item().Text("Terms & Conditions:").Bold();
                        c.Item().Text("All disputes are subject to Vadodara jurisdiction only.");
                        c.Item().Text("This is a system-generated invoice.");
                    });

                    col.Item().PaddingTop(12).AlignRight().Text($"For {companyName}: Authorized Signatory").Italic();
                });
            });
        }).GeneratePdf(absolutePath);
    }

    private static string BuildOutputPath(string subFolder, string invoiceNbr)
    {
        var safeName = invoiceNbr.Replace("/", "-") + ".pdf";
        // Web-servable relative path (under wwwroot), stored on the invoice row.
        return Path.Combine("invoices", subFolder, safeName).Replace('\\', '/');
    }

    private static string FullName(UserAccount? user) =>
        user == null ? "" : $"{user.FirstName} {user.LastName}".Trim();
}
