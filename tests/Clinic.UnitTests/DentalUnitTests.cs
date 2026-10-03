using System.Text.Json;
using Clinic.Domain.Entities;
using Clinic.Domain.Enums;
using Xunit;

namespace Clinic.UnitTests;

public class DentalUnitTests
{
    [Fact]
    public void DentalLog_DefaultConsumedMaterials_ShouldBeEmptyJsonArray()
    {
        // Arrange & Act
        var log = new DentalLog();

        // Assert
        Assert.Equal("[]", log.ConsumedMaterials);
    }

    [Theory]
    [InlineData("16", false)] // Permanent molar
    [InlineData("21", false)] // Permanent central incisor
    [InlineData("A", true)]   // Deciduous primary tooth
    [InlineData("E", true)]   // Deciduous primary tooth
    public void DentalLog_ToothNotation_ShouldHandleAdultAndPediatricIdentifiers(string toothNumber, bool isPediatric)
    {
        // Arrange
        var log = new DentalLog
        {
            Id = "den-001",
            PatientId = "pat-001",
            ToothNumber = toothNumber,
            DoctorName = "Dr. Samy"
        };

        // Assert
        Assert.Equal(toothNumber, log.ToothNumber);
        if (isPediatric)
        {
            Assert.True(char.IsLetter(toothNumber[0]));
        }
        else
        {
            Assert.True(int.TryParse(toothNumber, out int num) && num >= 1 && num <= 32);
        }
    }

    [Fact]
    public void DentalLog_StatusJsonSerialization_ShouldSupportMultipleToothStatuses()
    {
        // Arrange
        var statuses = new List<string>
        {
            nameof(ToothStatus.Caries),
            nameof(ToothStatus.Filled)
        };
        var serialized = JsonSerializer.Serialize(statuses);

        var log = new DentalLog
        {
            Id = "den-002",
            Status = serialized,
            PainLevel = 5,
            Treatment = "Composite Restoration"
        };

        // Act
        var deserialized = JsonSerializer.Deserialize<List<string>>(log.Status);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Contains(nameof(ToothStatus.Caries), deserialized);
        Assert.Contains(nameof(ToothStatus.Filled), deserialized);
        Assert.Equal(5, log.PainLevel);
    }

    [Fact]
    public void DentalLog_PlanningToggle_ShouldDifferentiatePlannedVsCompleted()
    {
        // Arrange
        var plannedLog = new DentalLog { IsPlanned = true, Treatment = "Root Canal Therapy" };
        var completedLog = new DentalLog { IsPlanned = false, Treatment = "Root Canal Therapy (Obturation)" };

        // Assert
        Assert.True(plannedLog.IsPlanned);
        Assert.False(completedLog.IsPlanned);
    }

    [Fact]
    public void DentalLog_DefaultStage_ShouldBeProposed()
    {
        // Arrange & Act
        var log = new DentalLog();

        // Assert
        Assert.Equal("proposed", log.Stage);
        Assert.Equal(0m, log.Cost);
        Assert.Null(log.InvoiceId);
    }

    [Theory]
    [InlineData("proposed", "accepted", true)]
    [InlineData("accepted", "in_progress", true)]
    [InlineData("in_progress", "completed", true)]
    [InlineData("proposed", "in_progress", false)] // skipped accepted
    [InlineData("proposed", "completed", false)]   // skipped accepted and in_progress
    [InlineData("accepted", "completed", false)]   // skipped in_progress
    [InlineData("in_progress", "accepted", false)] // backward transition
    [InlineData("completed", "in_progress", false)]// backward transition
    [InlineData("completed", "invoiced", false)]   // cannot update stage directly to invoiced
    [InlineData("invoiced", "completed", false)]   // terminal state
    public void BR_DEN_02_ProcedureLifecycle_StrictSequentialProgression(string currentStage, string targetStage, bool expectedValid)
    {
        // BR-DEN-02: Dental procedures must follow a strict sequential state progression:
        // Proposed -> Patient Accepted -> In Progress -> Completed -> Invoiced
        var allowedTransitions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "proposed", "accepted" },
            { "accepted", "in_progress" },
            { "in_progress", "completed" }
        };

        bool isValid = allowedTransitions.TryGetValue(currentStage, out var nextAllowed) &&
                       string.Equals(targetStage, nextAllowed, StringComparison.OrdinalIgnoreCase);

        Assert.Equal(expectedValid, isValid);
    }

    [Theory]
    [InlineData("proposed", false)]
    [InlineData("accepted", false)]
    [InlineData("in_progress", false)]
    [InlineData("completed", true)]
    [InlineData("invoiced", false)]
    public void BR_DEN_02_InvoicingGuardrail_OnlyCompletedCanBePushedToBilling(string currentStage, bool canPushToBilling)
    {
        // BR-DEN-02 Guardrail: Only procedures in the Completed status can be pushed into the billing module for cashier settlement.
        bool isEligibleForInvoicing = string.Equals(currentStage, "completed", StringComparison.OrdinalIgnoreCase);
        Assert.Equal(canPushToBilling, isEligibleForInvoicing);
    }

    [Fact]
    public void BR_DEN_02_PushToBilling_CreatesInvoice_TransitionsToInvoiced_AndLinksInvoiceId()
    {
        // Arrange
        var completedProcedure = new DentalLog
        {
            Id = "den-proc-101",
            PatientId = "pat-101",
            ToothNumber = "24",
            Treatment = "Porcelain Veneer",
            Cost = 850.00m,
            Stage = "completed",
            IsPlanned = false,
            ClinicId = "clinic-alpha"
        };

        // Act - Simulate Push-to-Billing
        var generatedInvoice = new BillingRecord
        {
            Id = "inv-auto-101",
            PatientId = completedProcedure.PatientId,
            InvoiceNumber = "INV-2026-00042",
            Subtotal = completedProcedure.Cost,
            Amount = completedProcedure.Cost,
            Status = "pending",
            DateIssued = "2026-10-03",
            Description = $"Tooth #{completedProcedure.ToothNumber} - {completedProcedure.Treatment}",
            ClinicId = completedProcedure.ClinicId
        };

        completedProcedure.InvoiceId = generatedInvoice.Id;
        completedProcedure.Stage = "invoiced";

        // Assert
        Assert.Equal("invoiced", completedProcedure.Stage);
        Assert.Equal("inv-auto-101", completedProcedure.InvoiceId);
        Assert.Equal(850.00m, generatedInvoice.Amount);
        Assert.Equal("pending", generatedInvoice.Status);
        Assert.Equal("INV-2026-00042", generatedInvoice.InvoiceNumber);
        Assert.Contains("Tooth #24", generatedInvoice.Description);
    }
}
