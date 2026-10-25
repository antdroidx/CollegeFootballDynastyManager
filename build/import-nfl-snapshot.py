"""Build an offline NFL snapshot from public, cached inputs. No runtime network dependency.
Usage: python build/import-nfl-snapshot.py PATH_TO_SOURCE_CACHE
EA numeric ratings only; nflverse identities/statuses; official team coaching pages.
"""
import csv,datetime,hashlib,json,re,sys,uuid,collections,unicodedata,urllib.request,concurrent.futures
from pathlib import Path
cache=Path(sys.argv[1]);root=Path(__file__).resolve().parents[1]
def read(name):return json.loads((cache/name).read_text(encoding="utf-8"))
def norm(n):
 n=unicodedata.normalize("NFKD",n).encode("ascii","ignore").decode().lower()
 return re.sub(r"[^a-z0-9]","",re.sub(r"\b(jr|sr|ii|iii|iv)\b","",n))
colors=list(csv.DictReader((cache/"colors.csv").open(encoding="utf-8-sig")))
active=[t["team"] for t in read("teams.json")["sports"][0]["leagues"][0]["teams"] if t["team"]["isActive"] and not t["team"]["isAllStar"]]
byname={t["team_name"]:t for t in colors};teams=[]
for t in active:
 c=byname[t["displayName"]];division=c["team_division"].split()[-1]
 teams.append(dict(Id=c["team_abbr"],City=t["location"],Nickname=t["name"],Abbreviation=t["abbreviation"],Conference=c["team_conf"],Division=division,SeedRank=0,Primary=c["team_color"],Secondary=c["team_color2"],LogoFile="nfl_"+c["team_abbr"].lower()+".png"))
# Stable division order is consumed by the existing rotating schedule builder.
teams.sort(key=lambda t:(["AFC","NFC"].index(t["Conference"]),["East","North","South","West"].index(t["Division"]),t["Id"]))
for conf in ["AFC","NFC"]:
 for division in ["East","North","South","West"]:
  for rank,t in enumerate(t for t in teams if t["Conference"]==conf and t["Division"]==division):t["SeedRank"]=rank
league=dict(Version=1,Name="National Football League",ActiveRosterLimit=53,PracticeSquadLimit=16,Teams=teams)
rows=list(csv.DictReader((cache/"roster_weekly_2026.csv").open(encoding="utf-8-sig")));week=max(int(r["week"]) for r in rows)
teamweeks={team:max(int(r["week"]) for r in rows if r["team"]==team and r["game_type"]=="REG") for team in {r["team"] for r in rows}}
rows=[r for r in rows if int(r["week"])==teamweeks[r["team"]] and r["game_type"]=="REG"]
latest={}
for r in sorted(rows,key=lambda r:-int(r["week"])):
 key=r["gsis_id"] or r["espn_id"] or norm(r["full_name"])
 if key not in latest or int(r["week"])==int(latest[key]["week"]) and {"ACT":0,"DEV":1,"RES":2,"EXE":3}.get(r["status"],9)<{"ACT":0,"DEV":1,"RES":2,"EXE":3}.get(latest[key]["status"],9):latest[key]=r
madden={norm(p["firstName"]+" "+p["lastName"]):p for p in read("ea-players.json")}
launch={norm(p["full_name"]):p for p in csv.DictReader((cache/"madden27.csv").open(encoding="utf-8-sig"))}
teammap={t["Id"]:t for t in teams};players=[];covered=0;estimated=0;counts=collections.Counter()
def number(value,default=0):
 try:return int(float(value))
 except (ValueError,TypeError):return default
positions={"QB":"QB","RB":"RB","HB":"RB","FB":"RB","WR":"WR","TE":"TE","LS":"TE","OL":"OL","LT":"OL","LG":"OL","C":"OL","RG":"OL","RT":"OL","T":"OL","G":"OL","DE":"DE","LEDG":"DE","REDG":"DE","DT":"DT","DL":"DT","CB":"CB","FS":"FS","SS":"SS","S":"FS","DB":"CB","MLB":"MLB","ILB":"MLB","MIKE":"MLB","LB":"MLB","OLB":"OLB","LOLB":"OLB","ROLB":"OLB","SAM":"OLB","WILL":"OLB","K":"K","P":"K"}
for r in sorted(latest.values(),key=lambda r:(r["team"],r["status"],r["full_name"])):
 code=r["team"];code={"LA":"LAR","WSH":"WAS"}.get(code,code)
 if code not in teammap:continue
 m=madden.get(norm(r["full_name"]));l=launch.get(norm(r["full_name"])) if m is None else None
 source="Madden 27 · EA Week 6" if m else "Madden 27 · launch archive" if l else "Estimated · not in Madden snapshot"
 overall=number(m["overallRating"] if m else l["overall"] if l else 60)
 covered+=int(m is not None or l is not None);estimated+=int(m is None and l is None)
 posraw=(m or {}).get("position",{}).get("shortLabel",(m or {}).get("position",{}).get("label","")) if m else (l or {}).get("position","")
 pos=positions.get(posraw,positions.get(r["depth_chart_position"],positions.get(r["position"],"OLB")))
 def rating(ea,col,default=overall):return max(1,min(99,number(m.get("stats",{}).get(ea,{}).get("value"),default))) if m else max(1,min(99,number((l or {}).get(col),default)))
 birth=r["birth_date"][:10]
 try:age=2026-int(birth[:4])-(birth[5:] > "09-01")
 except ValueError:age=number((m or {}).get("age"),number((l or {}).get("age"),24))
 exp=max(0,min(number(r["years_exp"]),max(0,age-20)))
 status=r["status"];practice=status=="DEV" and counts[(code,"DEV")]<16;reserve=status in ["RES","EXE"] or status=="DEV" and not practice
 free=status not in ["ACT","DEV","RES","EXE","INA"]
 if status in ["ACT","INA"] and counts[(code,"ACTIVE")]>=53:reserve=True
 if status in ["ACT","INA"]:counts[(code,"ACTIVE")]+=1
 counts[(code,status)]+=1
 team="Free agent" if free else teammap[code]["City"]+" "+teammap[code]["Nickname"]
 football=dict(PlayerId=str(uuid.uuid5(uuid.NAMESPACE_URL,"nfl:"+(r["gsis_id"] or r["espn_id"] or norm(r["full_name"])))),FullName=r["full_name"],TeamName=team,Position=pos,ClassYear=4,OverallRating=overall,SeasonStartOverall=overall,PotentialRating=min(99,overall+max(1,29-age)),SpeedRating=rating("speed","speed_rating"),StrengthRating=rating("strength","strength_rating"),AgilityRating=rating("agility","agility_rating"),AwarenessRating=rating("awareness","awareness_rating"),InjuryRating=rating("injury","injury_rating",85),JerseyNumber=number(r["jersey_number"],number((m or {}).get("jerseyNum"),0)),HeightInches=number(r["height"],73),WeightPounds=number(r["weight"],220),DepthChartOrder=1,
 Skills=dict(PassingPower=rating("throwPower","throw_power_rating"),PassingAccuracy=round((rating("throwAccuracyShort","throw_acc_short_rating")+rating("throwAccuracyMid","throw_acc_mid_rating")+rating("throwAccuracyDeep","throw_acc_deep_rating"))/3),Catching=rating("catching","catch_rating"),BreakTackle=rating("breakTackle","break_tackle_rating"),Tackling=rating("tackle","tackle_rating"),RunBlocking=rating("runBlock","run_block_rating"),PassBlocking=rating("passBlock","pass_block_rating"),BallCarrying=rating("carrying","carry_rating"),Acceleration=rating("acceleration","accel_rating"),Leadership=rating("awareness","awareness_rating"),Discipline=75,Jumping=rating("jumping","jump_rating"),KickPower=rating("kickPower","kick_power_rating"),KickAccuracy=rating("kickAccuracy","kick_acc_rating")))
 if r["position"] in ["K","P"]:football["NcaaPositionCode"]=20 if r["position"]=="P" else 19
 elif pos=="OL":football["NcaaPositionCode"]={"LT":5,"RT":9,"LG":6,"RG":8,"C":7}.get(posraw,{"LT":5,"RT":9,"LG":6,"RG":8,"C":7,"G":6,"T":5}.get(r["depth_chart_position"],5))
 players.append(dict(Football=football,Age=age,Experience=exp,PracticeSquad=practice,Reserve=reserve,RatingSource=source,ExternalId=r["gsis_id"] or r["espn_id"],RosterStatus=status,Retired=status=="RET"))
for team in teams:
 name=team["City"]+" "+team["Nickname"]
 for pos in set(p["Football"]["Position"] for p in players):
  a=sorted([p for p in players if p["Football"]["TeamName"]==name and p["Football"]["Position"]==pos],key=lambda p:(p["Reserve"],p["PracticeSquad"],-p["Football"]["OverallRating"]))
  for depth,p in enumerate(a,1):p["Football"]["DepthChartOrder"]=depth
coaches=read("coaches.json")
for c in coaches:c["TeamId"]={"LA":"LAR","WSH":"WAS"}.get(c["TeamId"],c["TeamId"])
# Tampa Bay lists no separate defensive coordinator. Preserve that vacancy rather than inventing one.
coaches.append(dict(TeamId="TB",Role="DefensiveCoordinator",Name="Vacant",Title="No separate DC listed · Todd Bowles calls defense",Source="https://www.buccaneers.com/team/coaches-roster/",Vacant=True))
assert len(teams)==32 and len(coaches)==128
out=root/"src/ProFootball.Core"; (out/"league.json").rename(out/"fictional-league.json") if not (out/"fictional-league.json").exists() else None
(out/"league.json").write_text(json.dumps(league,indent=2),encoding="utf-8")
from bs4 import BeautifulSoup
candidates=[];known={norm(c["Name"]) for c in coaches}
for path in cache.glob("*-coaches.html"):
 code=path.name.split("-")[0];code={"LA":"LAR"}.get(code,code)
 if code not in teammap:continue
 soup=BeautifulSoup(path.read_text(encoding="utf-8"),"html.parser")
 for card in soup.select(".d3-o-media-object__body"):
  n=card.select_one(".d3-o-media-object__title");r=card.select_one(".d3-o-media-object__roofline")
  if not n or not r:continue
  name=n.get_text(" ",strip=True).replace("�","'");title=r.get_text(" ",strip=True)
  if norm(name) in known or len(name.split())<2 or "strength" in title.lower() or "performance" in title.lower():continue
  known.add(norm(name));candidates.append(dict(Name=name,SourceTeam=code,Title=title,Source=next(c["Source"] for c in coaches if c["TeamId"]==code)))
pack=dict(Candidates=candidates,Version=1,Season=2026,Week=week,RetrievedUtc=datetime.datetime.now(datetime.timezone.utc).isoformat(),MaddenMatched=covered,Estimated=estimated,Players=players,Coaches=coaches)
(out/"nfl-snapshot.json").write_text(json.dumps(pack,separators=(",",":"),ensure_ascii=False),encoding="utf-8")
assets=root/"src/ProFootball.App/Resources/Images";assets.mkdir(exist_ok=True)
espn={t["displayName"]:t for t in active}
def logo(t):
 url=espn[t["City"]+" "+t["Nickname"]]["logos"][0]["href"];target=assets/t["LogoFile"]
 if not target.exists():target.write_bytes(urllib.request.urlopen(url,timeout=40).read())
 return dict(Team=t["Id"],Url=url,Sha256=hashlib.sha256(target.read_bytes()).hexdigest())
logos=list(concurrent.futures.ThreadPoolExecutor(max_workers=4).map(logo,teams))
manifest=dict(RetrievedUtc=pack["RetrievedUtc"],RosterUrl="https://github.com/nflverse/nflverse-data/releases/download/weekly_rosters/roster_weekly_2026.csv",MaddenUrl="https://www.ea.com/games/madden-nfl/ratings",MaddenIteration="Madden 27 Week 6",LaunchFallback="https://github.com/zachxwalton/madden-ratings-breakdown",RosterWeek=week,TeamRosterWeeks=teamweeks,Matched=covered,Estimated=estimated,Logos=logos,CoachingSources=sorted(set(c["Source"] for c in coaches)),InputHashes={p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in cache.iterdir() if p.name in ["roster_weekly_2026.csv","ea-players.json","madden27.csv","colors.csv","coaches.json"]})
(root/"docs/nfl-data-manifest.json").write_text(json.dumps(manifest,indent=2),encoding="utf-8")
print(json.dumps(dict(teams=len(teams),players=len(players),coaches=len(coaches),madden=covered,estimated=estimated,week=week)))
