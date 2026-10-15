using System;
using Clinic.Domain.Entities;
using Xunit;

namespace Clinic.UnitTests;

public class PostOpTriageUnitTests
{
    [Fact]
    public void REQ_POSTOP_01_EvaluateEscalation_TriggersOnSeverePain()
    {
        // Arrange
        var session = new PostOpTriageSession
        {
            PainScore = 8,
            BleedingSeverity = "None",
            SwellingSeverity = "None"
        };

        // Act
        session.EvaluateEscalation();

        // Assert
        Assert.True(session.IsEscalated);
        Assert.Equal("Escalated", session.TriageStatus);
        Assert.Contains("Severe pain level reported", session.EscalationReason);
    }

    [Fact]
    public void REQ_POSTOP_02_EvaluateEscalation_TriggersOnSevereBleeding()
    {
        // Arrange
        var session = new PostOpTriageSession
        {
            PainScore = 4,
            BleedingSeverity = "Severe",
            SwellingSeverity = "None"
        };

        // Act
        session.EvaluateEscalation();

        // Assert
        Assert.True(session.IsEscalated);
        Assert.Equal("Escalated", session.TriageStatus);
        Assert.Contains("severe active bleeding", session.EscalationReason);
    }

    [Fact]
    public void REQ_POSTOP_03_EvaluateEscalation_TriggersOnSevereSwelling()
    {
        // Arrange
        var session = new PostOpTriageSession
        {
            PainScore = 3,
            BleedingSeverity = "None",
            SwellingSeverity = "Severe"
        };

        // Act
        session.EvaluateEscalation();

        // Assert
        Assert.True(session.IsEscalated);
        Assert.Equal("Escalated", session.TriageStatus);
        Assert.Contains("Significant acute facial/intraoral swelling", session.EscalationReason);
    }

    [Fact]
    public void REQ_POSTOP_04_EvaluateEscalation_MildSymptomsClassifiesAsRecovering()
    {
        // Arrange
        var session = new PostOpTriageSession
        {
            PainScore = 3,
            BleedingSeverity = "Slight",
            SwellingSeverity = "Moderate"
        };

        // Act
        session.EvaluateEscalation();

        // Assert
        Assert.False(session.IsEscalated);
        Assert.Equal("Recovering", session.TriageStatus);
        Assert.Null(session.EscalationReason);
    }
}
