using System.Text.RegularExpressions;
namespace ProFootball.Core;
public sealed record ProTransactionEntry(Guid Id,int Year,int Week,ProPhase? Phase,string Activity);
public static class ProTransactions
{
    public static string Period(ProPhase? phase,int week)=>phase switch{ProPhase.Preseason=>"P"+week,ProPhase.Playoffs=>"PO"+week,ProPhase.Offseason=>"Offseason "+week,null=>"Week "+week+" (stage unknown)",_=>"W"+week};
    public static ProFranchise Migrate(ProFranchise s)
    {
        if(s.TransactionLog.Count>0||s.Transactions.Count==0)return s;
        var entries=s.Transactions.Select((text,index)=>{
            var m=Regex.Match(text,@"^(\d{4}) W(\d+) · (.*)$");
            var id=new Guid(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(index+"/"+text))[..16]);
            return new ProTransactionEntry(id,m.Success?int.Parse(m.Groups[1].Value):s.Year,m.Success?int.Parse(m.Groups[2].Value):s.Week,null,m.Success?m.Groups[3].Value:text);
        }).ToList();return s with{TransactionLog=entries};
    }
    public static ProFranchise Add(ProFranchise s,string text)
    {
        s=Migrate(s);return s with{Transactions=s.Transactions.Append($"{s.Year} W{s.Week} · {text}").ToList(),TransactionLog=s.TransactionLog.Append(new(Guid.NewGuid(),s.Year,s.Week,s.Phase,text)).ToList()};
    }
}
