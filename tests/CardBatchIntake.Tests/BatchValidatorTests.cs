using CardBatchIntake.Api.Models;
using CardBatchIntake.Api.Services;
using Xunit;

namespace CardBatchIntake.Tests;

public class BatchValidatorTests
{
    private readonly BatchValidator _validator = new();

    private static BatchRequest ValidRequest() => new()
    {
        ClientCode = "KAYA",
        FileName = $"KAYA_{DateTime.UtcNow:yyyyMMdd}_001.txt",
        ExpectedRecordCount = 1,
        Records = new() { new CardRecord
        {
            RecordId = "R0001", CardholderName = "T MOKOENA",
            MaskedPan = "************4821", ProductCode = "DEBIT_GOLD",
            Expiry = DateTime.UtcNow.AddYears(2).ToString("MM/yy")
        }}
    };

    [Fact] public void ValidRequest_HasNoErrors() => Assert.Empty(_validator.Validate(ValidRequest()));

    [Theory]
    [InlineData("ka", false)]
    [InlineData("KAYA", true)]
    [InlineData("TOOLONG", false)]
    public void ClientCode_IsValidated(string code, bool valid)
    {
        var request = ValidRequest(); request.ClientCode = code;
        var errors = _validator.Validate(request);
        Assert.Equal(valid, !errors.Any(e => e.Field == "clientCode"));
    }

    [Fact] public void FileNameMustMatchClientAndFormat()
    {
        var request = ValidRequest(); request.FileName = "BANK_20261340_001.txt";
        Assert.Contains(_validator.Validate(request), e => e.Field == "fileName");
    }

    [Fact] public void ExpectedCountMustMatchRecords()
    {
        var request = ValidRequest(); request.ExpectedRecordCount = 2;
        Assert.Contains(_validator.Validate(request), e => e.Field == "expectedRecordCount");
    }

    [Fact] public void DuplicateRecordIdsAreRejected()
    {
        var request = ValidRequest(); request.Records.Add(new CardRecord { RecordId = "R0001", CardholderName = "J VAN WYK", MaskedPan = "************1937", ProductCode = "DEBIT_CLASSIC", Expiry = DateTime.UtcNow.AddYears(2).ToString("MM/yy") });
        request.ExpectedRecordCount = 2;
        Assert.Contains(_validator.Validate(request), e => e.Field == "records[1].recordId");
    }

    [Fact] public void NullRecordIsRejectedWithoutThrowing()
    {
        var request = ValidRequest(); request.Records[0] = null!;
        Assert.Contains(_validator.Validate(request), e => e.Field == "records[0]");
    }

    [Fact] public void FullPanProducesSpecificSecurityError()
    {
        var request = ValidRequest(); request.Records[0].MaskedPan = "1234567890123456";
        Assert.Contains(_validator.Validate(request), e => e.Code == "SECURITY_UNMASKED_PAN");
    }

    [Fact] public void IncorrectMaskedPanIsRejected()
    {
        var request = ValidRequest(); request.Records[0].MaskedPan = "****4821";
        Assert.Contains(_validator.Validate(request), e => e.Field == "records[0].maskedPan");
    }

    [Fact] public void PastExpiryIsRejected()
    {
        var request = ValidRequest(); request.Records[0].Expiry = "01/20";
        Assert.Contains(_validator.Validate(request), e => e.Field == "records[0].expiry");
    }

    [Fact] public void AllowedProductPassesAndUnknownProductFails()
    {
        var request = ValidRequest();
        Assert.DoesNotContain(_validator.Validate(request), e => e.Field == "records[0].productCode");
        request.Records[0].ProductCode = "PLATINUM";
        Assert.Contains(_validator.Validate(request), e => e.Field == "records[0].productCode");
    }
}
