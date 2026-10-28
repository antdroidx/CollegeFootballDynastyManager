using ProFootball.Core;
using Xunit;
namespace ProFootball.Tests;
public class TransactionPeriodTests
{
    [Theory]
    [InlineData(ProPhase.Preseason,"P1")]
    [InlineData(ProPhase.RegularSeason,"W1")]
    [InlineData(ProPhase.Playoffs,"PO1")]
    [InlineData(ProPhase.Offseason,"Offseason 1")]
    public void PeriodIdentifiesCalendarPhase(ProPhase phase,string expected)=>Assert.Equal(expected,ProTransactions.Period(phase,1));
    [Fact]public void LegacyUnknownStageIsNotInvented()=>Assert.Equal("Week 1 (stage unknown)",ProTransactions.Period(null,1));
}
