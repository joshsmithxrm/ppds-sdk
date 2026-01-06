using FluentAssertions;
using PPDS.Dataverse.BulkOperations;
using Xunit;

namespace PPDS.Dataverse.Tests.BulkOperations;

public class BatchFailureDiagnosticTests
{
    [Fact]
    public void Constructor_DefaultValues_AreCorrect()
    {
        var diagnostic = new BatchFailureDiagnostic();

        diagnostic.RecordId.Should().Be(Guid.Empty);
        diagnostic.RecordIndex.Should().Be(0);
        diagnostic.FieldName.Should().BeEmpty();
        diagnostic.ReferencedId.Should().Be(Guid.Empty);
        diagnostic.ReferencedEntityName.Should().BeNull();
        diagnostic.Pattern.Should().BeEmpty();
        diagnostic.Suggestion.Should().BeNull();
    }

    [Fact]
    public void Properties_CanBeSetViaInitializers()
    {
        var recordId = Guid.NewGuid();
        var referencedId = Guid.NewGuid();

        var diagnostic = new BatchFailureDiagnostic
        {
            RecordId = recordId,
            RecordIndex = 5,
            FieldName = "parentaccountid",
            ReferencedId = referencedId,
            ReferencedEntityName = "account",
            Pattern = "SELF_REFERENCE",
            Suggestion = "Defer this field to a second pass"
        };

        diagnostic.RecordId.Should().Be(recordId);
        diagnostic.RecordIndex.Should().Be(5);
        diagnostic.FieldName.Should().Be("parentaccountid");
        diagnostic.ReferencedId.Should().Be(referencedId);
        diagnostic.ReferencedEntityName.Should().Be("account");
        diagnostic.Pattern.Should().Be("SELF_REFERENCE");
        diagnostic.Suggestion.Should().Be("Defer this field to a second pass");
    }

    [Theory]
    [InlineData("SELF_REFERENCE")]
    [InlineData("MISSING_PARENT")]
    [InlineData("MISSING_REFERENCE")]
    public void Pattern_AcceptsKnownPatternValues(string pattern)
    {
        var diagnostic = new BatchFailureDiagnostic { Pattern = pattern };

        diagnostic.Pattern.Should().Be(pattern);
    }

    [Fact]
    public void SelfReferenceDiagnostic_HasAllRelevantFields()
    {
        var recordId = Guid.NewGuid();

        var diagnostic = new BatchFailureDiagnostic
        {
            RecordId = recordId,
            RecordIndex = 3,
            FieldName = "parentaccountid",
            ReferencedId = recordId, // References itself
            ReferencedEntityName = "account",
            Pattern = "SELF_REFERENCE",
            Suggestion = "Record references itself before creation. Defer this field."
        };

        // Self-reference: RecordId == ReferencedId
        diagnostic.RecordId.Should().Be(diagnostic.ReferencedId);
        diagnostic.Pattern.Should().Be("SELF_REFERENCE");
    }

    [Fact]
    public void MissingParentDiagnostic_HasDifferentRecordAndReferencedIds()
    {
        var childId = Guid.NewGuid();
        var parentId = Guid.NewGuid();

        var diagnostic = new BatchFailureDiagnostic
        {
            RecordId = childId,
            RecordIndex = 7,
            FieldName = "parentaccountid",
            ReferencedId = parentId,
            ReferencedEntityName = "account",
            Pattern = "MISSING_PARENT",
            Suggestion = "Import parent record before child"
        };

        diagnostic.RecordId.Should().NotBe(diagnostic.ReferencedId);
        diagnostic.Pattern.Should().Be("MISSING_PARENT");
    }
}
