using System.Globalization;
using System.Text.RegularExpressions;
using CardBatchIntake.Api.Models;

namespace CardBatchIntake.Api.Services;

// Checks submitted batches and returns every issue that needs correction.
public interface IBatchValidator
{
    // Applies the client, file, record, card-mask, expiry, and product rules.
    List<ValidationError> Validate(BatchRequest request);
}

// Implements the intake rules for card production batches.
public sealed class BatchValidator : IBatchValidator
{
    private static readonly Regex ClientCodePattern = new("^[A-Z]{3,6}$", RegexOptions.Compiled);
    private static readonly Regex FileNamePattern = new("^(?<client>[A-Z]{3,6})_(?<date>\\d{8})_(?<seq>\\d{3})\\.txt$", RegexOptions.Compiled);
    private static readonly Regex MaskedPanPattern = new("^\\*{12}\\d{4}$", RegexOptions.Compiled);
    private static readonly Regex FullPanPattern = new("^\\d{13,19}$", RegexOptions.Compiled);
    private static readonly HashSet<string> ProductCodes = new(StringComparer.Ordinal)
        { "DEBIT_CLASSIC", "DEBIT_GOLD", "CREDIT_PLAT" };

    public List<ValidationError> Validate(BatchRequest request)
    {
        var errors = new List<ValidationError>();
        if (request is null)
        {
            errors.Add(new("request", "Request body is required."));
            return errors;
        }

        if (!ClientCodePattern.IsMatch(request.ClientCode ?? ""))
            errors.Add(new("clientCode", "Must contain 3 to 6 uppercase letters."));

        var fileMatch = FileNamePattern.Match(request.FileName ?? "");
        if (!fileMatch.Success)
            errors.Add(new("fileName", "Must follow {clientCode}_{yyyyMMdd}_{seq}.txt, with a three-digit sequence."));
        else
        {
            if (!string.Equals(fileMatch.Groups["client"].Value, request.ClientCode, StringComparison.Ordinal))
                errors.Add(new("fileName", "The client code in fileName must match clientCode."));
            if (!DateTime.TryParseExact(fileMatch.Groups["date"].Value, "yyyyMMdd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out _))
                errors.Add(new("fileName", "The date in fileName must be a valid yyyyMMdd date."));
        }

        request.Records ??= new List<CardRecord>();
        if (request.ExpectedRecordCount != request.Records.Count)
            errors.Add(new("expectedRecordCount", $"Expected {request.ExpectedRecordCount} records but received {request.Records.Count}."));

        var seenIds = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < request.Records.Count; i++)
        {
            var record = request.Records[i];
            var prefix = $"records[{i}]";
            if (record is null)
            {
                errors.Add(new(prefix, "Record is required."));
                continue;
            }

            if (!seenIds.Add(record.RecordId ?? ""))
                errors.Add(new($"{prefix}.recordId", "recordId must be unique within the batch."));

            var pan = record.MaskedPan ?? "";
            if (FullPanPattern.IsMatch(pan))
                errors.Add(new($"{prefix}.maskedPan", "Security error: unmasked card numbers are not accepted. Do not submit a full card number.", "SECURITY_UNMASKED_PAN"));
            else if (!MaskedPanPattern.IsMatch(pan))
                errors.Add(new($"{prefix}.maskedPan", "Must be exactly 12 asterisks followed by 4 digits."));

            if (!DateTime.TryParseExact(record.Expiry, "MM/yy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var expiry))
                errors.Add(new($"{prefix}.expiry", "Must use MM/YY format."));
            else
            {
                var expiryEnd = new DateTime(expiry.Year, expiry.Month, DateTime.DaysInMonth(expiry.Year, expiry.Month), 23, 59, 59, DateTimeKind.Utc);
                if (expiryEnd < DateTime.UtcNow)
                    errors.Add(new($"{prefix}.expiry", "Card expiry must not be in the past."));
            }

            if (!ProductCodes.Contains(record.ProductCode ?? ""))
                errors.Add(new($"{prefix}.productCode", "Must be DEBIT_CLASSIC, DEBIT_GOLD or CREDIT_PLAT."));
        }
        return errors;
    }
}