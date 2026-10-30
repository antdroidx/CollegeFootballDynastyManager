using DynastyManager.Core.Models;
namespace ProFootball.Core;
public static class ProBackfieldPositions
{
    // Official-club role review, dated 2026-10-30; sources in docs/data/pro-backfield-position-audit-2026-10-30.json.
    static readonly Dictionary<string,(Position Position,int Code)> Reviewed=new()
    {
        ["00-0040637"]=(Position.FB,2),
        ["00-0040567"]=(Position.FB,2),
        ["00-0038738"]=(Position.FB,2),
        ["00-0038489"]=(Position.FB,2),
        ["00-0041436"]=(Position.FB,2),
        ["00-0041081"]=(Position.FB,2),
        ["00-0036187"]=(Position.FB,2),
        ["00-0033376"]=(Position.FB,2),
        ["00-0041585"]=(Position.FB,2),
        ["00-0038488"]=(Position.FB,2),
        ["00-0041221"]=(Position.FB,2),
        ["00-0040649"]=(Position.FB,2),
        ["00-0039305"]=(Position.TE,4),
    };
    public static ProPlayer Correct(ProPlayer p)=>Reviewed.TryGetValue(p.ExternalId,out var role)&&(p.Football.Position!=role.Position||p.Football.NcaaPositionCode!=role.Code)
        ?ProPlayerRatingService.Refresh(p with{Football=p.Football with{Position=role.Position,NcaaPositionCode=role.Code}},true):p;
    public static int Order(Position position)=>position.RosterOrder();
}
