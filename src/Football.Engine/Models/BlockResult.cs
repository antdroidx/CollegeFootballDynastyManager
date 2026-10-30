namespace DynastyManager.Core.Models;
public sealed record BlockResult(Guid BlockerId,Guid? DefenderId,bool PassBlock,bool Won,double DefeatedAt);
