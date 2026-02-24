using EdgeFront.Builder.Domain;
using EdgeFront.Builder.Domain.Entities;
using FluentAssertions;

namespace EdgeFront.Builder.Tests.Domain;

public class WarmRuleEvaluatorTests
{
    private readonly InternalDomainFilter _noInternals = new([]);
    private readonly WarmRuleEvaluator _evaluator;

    public WarmRuleEvaluatorTests()
    {
        _evaluator = new WarmRuleEvaluator(_noInternals);
    }

    // --- W1 tests ---

    [Fact]
    public void EvaluateW1_TwoDistinctEmailsSameDomain_ShouldReturnDomain()
    {
        var sessionId = Guid.NewGuid();
        var attendances = new List<NormalizedAttendance>
        {
            MakeAttendance(sessionId, "alice@acme.com", "acme.com"),
            MakeAttendance(sessionId, "bob@acme.com", "acme.com"),
        };

        var result = _evaluator.EvaluateW1(attendances);
        result.Should().ContainSingle().Which.Should().Be("acme.com");
    }

    [Fact]
    public void EvaluateW1_OneEmailDomain_ShouldNotTrigger()
    {
        var sessionId = Guid.NewGuid();
        var attendances = new List<NormalizedAttendance>
        {
            MakeAttendance(sessionId, "alice@acme.com", "acme.com"),
        };

        var result = _evaluator.EvaluateW1(attendances);
        result.Should().BeEmpty();
    }

    [Fact]
    public void EvaluateW1_SameEmailTwice_ShouldNotTrigger()
    {
        var sessionId = Guid.NewGuid();
        var attendances = new List<NormalizedAttendance>
        {
            MakeAttendance(sessionId, "alice@acme.com", "acme.com"),
            MakeAttendance(sessionId, "alice@acme.com", "acme.com"),
        };

        var result = _evaluator.EvaluateW1(attendances);
        result.Should().BeEmpty();
    }

    [Fact]
    public void EvaluateW1_InternalDomain_ShouldBeExcluded()
    {
        var internalFilter = new InternalDomainFilter(["acme.com"]);
        var evaluator = new WarmRuleEvaluator(internalFilter);
        var sessionId = Guid.NewGuid();
        var attendances = new List<NormalizedAttendance>
        {
            MakeAttendance(sessionId, "alice@acme.com", "acme.com"),
            MakeAttendance(sessionId, "bob@acme.com", "acme.com"),
        };

        var result = evaluator.EvaluateW1(attendances);
        result.Should().BeEmpty();
    }

    // --- W2 tests ---

    [Fact]
    public void EvaluateW2_SameEmailInTwoSessions_ShouldReturnDomain()
    {
        var session1 = Guid.NewGuid();
        var session2 = Guid.NewGuid();
        var attendances = new List<NormalizedAttendance>
        {
            MakeAttendance(session1, "alice@acme.com", "acme.com"),
            MakeAttendance(session2, "alice@acme.com", "acme.com"),
        };

        var result = _evaluator.EvaluateW2(attendances);
        result.Should().ContainSingle().Which.Should().Be("acme.com");
    }

    [Fact]
    public void EvaluateW2_SameEmailSameSession_ShouldNotTrigger()
    {
        var sessionId = Guid.NewGuid();
        var attendances = new List<NormalizedAttendance>
        {
            MakeAttendance(sessionId, "alice@acme.com", "acme.com"),
            MakeAttendance(sessionId, "alice@acme.com", "acme.com"),
        };

        var result = _evaluator.EvaluateW2(attendances);
        result.Should().BeEmpty();
    }

    [Fact]
    public void EvaluateW2_DifferentEmailsSameSession_ShouldNotTrigger()
    {
        var sessionId = Guid.NewGuid();
        var attendances = new List<NormalizedAttendance>
        {
            MakeAttendance(sessionId, "alice@acme.com", "acme.com"),
            MakeAttendance(sessionId, "bob@acme.com", "acme.com"),
        };

        var result = _evaluator.EvaluateW2(attendances);
        result.Should().BeEmpty();
    }

    [Fact]
    public void EvaluateW2_InternalDomain_ShouldBeExcluded()
    {
        var internalFilter = new InternalDomainFilter(["acme.com"]);
        var evaluator = new WarmRuleEvaluator(internalFilter);
        var session1 = Guid.NewGuid();
        var session2 = Guid.NewGuid();
        var attendances = new List<NormalizedAttendance>
        {
            MakeAttendance(session1, "alice@acme.com", "acme.com"),
            MakeAttendance(session2, "alice@acme.com", "acme.com"),
        };

        var result = evaluator.EvaluateW2(attendances);
        result.Should().BeEmpty();
    }

    // --- W2 > W1 precedence (handled in metrics engine, but evaluators return their own sets) ---

    [Fact]
    public void EvaluateW1_MultipleDomains_ReturnsAllQualifyingDomains()
    {
        var sessionId = Guid.NewGuid();
        var attendances = new List<NormalizedAttendance>
        {
            MakeAttendance(sessionId, "alice@acme.com", "acme.com"),
            MakeAttendance(sessionId, "bob@acme.com", "acme.com"),
            MakeAttendance(sessionId, "carol@widgets.io", "widgets.io"),
            MakeAttendance(sessionId, "dave@widgets.io", "widgets.io"),
        };

        var result = _evaluator.EvaluateW1(attendances);
        result.Should().BeEquivalentTo(new[] { "acme.com", "widgets.io" });
    }

    private static NormalizedAttendance MakeAttendance(Guid sessionId, string email, string emailDomain) =>
        new()
        {
            AttendanceId = Guid.NewGuid(),
            SessionId = sessionId,
            OwnerUserId = "owner-1",
            Email = email,
            EmailDomain = emailDomain,
            Attended = true,
        };
}
