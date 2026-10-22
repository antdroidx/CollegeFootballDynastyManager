"""Collect public season-specific athletics pages. Output is a review queue, NEVER a release dataset."""
import concurrent.futures, json, re, urllib.request
from pathlib import Path
from bs4 import BeautifulSoup
SCHOOLS = """
Air Force|goairforcefalcons.com
Akron|gozips.com
Alabama|rolltide.com
Appalachian State|appstatesports.com
Arizona|arizonawildcats.com
Arizona State|thesundevils.com
Arkansas|arkansasrazorbacks.com
Arkansas State|astateredwolves.com
Army|goarmywestpoint.com
Auburn|auburntigers.com
Ball State|ballstatesports.com
Baylor|baylorbears.com
Boise State|broncosports.com
Boston College|bceagles.com
Bowling Green|bgsufalcons.com
Buffalo|ubbulls.com
BYU|byucougars.com
California|calbears.com
Central Michigan|cmuchippewas.com
Charlotte|charlotte49ers.com
Cincinnati|gobearcats.com
Clemson|clemsontigers.com
Coastal Carolina|goccusports.com
Colorado|cubuffs.com
Colorado State|csurams.com
Connecticut|uconnhuskies.com
Duke|goduke.com
East Carolina|ecupirates.com
Eastern Michigan|emueagles.com
Florida|floridagators.com
Florida Atl|fausports.com
Florida Intl|fiusports.com
Florida State|seminoles.com
Fresno State|gobulldogs.com
Georgia|georgiadogs.com
Georgia Southern|gseagles.com
Georgia State|georgiastatesports.com
Georgia Tech|ramblinwreck.com
Hawaii|hawaiiathletics.com
Houston|uhcougars.com
Illinois|fightingillini.com
Indiana|iuhoosiers.com
Iowa|hawkeyesports.com
Iowa State|cyclones.com
Jacksonville State|jaxstatesports.com
James Madison|jmusports.com
Kansas|kuathletics.com
Kansas State|kstatesports.com
Kennesaw State|ksuowls.com
Kent State|kentstatesports.com
Kentucky|ukathletics.com
LA Lafayette|ragincajuns.com
LA Monroe|ulmwarhawks.com
LA Tech|latechsports.com
Liberty|libertyflames.com
Louisville|gocards.com
LSU|lsusports.net
Marshall|herdzone.com
Maryland|umterps.com
Memphis|gotigersgo.com
Miami|miamihurricanes.com
Miami OH|miamiredhawks.com
Michigan|mgoblue.com
Michigan State|msuspartans.com
Mid Tenn State|goblueraiders.com
Minnesota|gophersports.com
Mississippi State|hailstate.com
Missouri|mutigers.com
Navy|navysports.com
Nebraska|huskers.com
Nevada|nevadawolfpack.com
New Mexico|golobos.com
New Mexico St|nmstatesports.com
North Carolina|goheels.com
NC State|gopack.com
North Texas|meangreensports.com
Northern Illinois|niuhuskies.com
Northwestern|nusports.com
Notre Dame|fightingirish.com
Ohio|ohiobobcats.com
Ohio State|ohiostatebuckeyes.com
Oklahoma|soonersports.com
Oklahoma State|okstate.com
Old Dominion|odusports.com
Ole Miss|olemisssports.com
Oregon|goducks.com
Oregon State|osubeavers.com
Penn State|gopsusports.com
Pittsburgh|pittsburghpanthers.com
Purdue|purduesports.com
Rice|riceowls.com
Rutgers|scarletknights.com
San Diego State|goaztecs.com
San Jose State|sjsuspartans.com
South Alabama|usajaguars.com
South Carolina|gamecocksonline.com
South Florida|gousfbulls.com
Southern Methodist|smumustangs.com
Southern Miss|southernmiss.com
Stanford|gostanford.com
Syracuse|cuse.com
TCU|gofrogs.com
Temple|owlsports.com
Tennessee|utsports.com
Texas|texaslonghorns.com
Texas AM|12thman.com
Texas State|txst.com
Texas Tech|texastech.com
Toledo|utrockets.com
Troy|troytrojans.com
Tulane|tulanegreenwave.com
Tulsa|tulsahurricane.com
UAB|uabsports.com
Central Florida|ucfknights.com
UCLA|uclabruins.com
UMass|umassathletics.com
UNLV|unlvrebels.com
USC|usctrojans.com
UT El Paso|utepminers.com
UT San Antonio|goutsa.com
Utah|utahutes.com
Utah State|utahstateaggies.com
Vanderbilt|vucommodores.com
Virginia|virginiasports.com
Virginia Tech|hokiesports.com
Wake Forest|godeacs.com
Washington|gohuskies.com
Washington State|wsucougars.com
West Virginia|wvusports.com
Western Kentucky|wkusports.com
Western Michigan|wmubroncos.com
Wisconsin|uwbadgers.com
Wyoming|gowyo.com
Delaware|bluehens.com
Missouri State|missouristatebears.com
Sam Houston|gobearkats.com
North Dakota State|gobison.com
Sacramento State|hornetsports.com
""".strip().splitlines()
OUT=Path('.artifacts/coaches-2025');OUT.mkdir(parents=True,exist_ok=True)
def collect(row):
    team,domain=row.split('|'); path=OUT/(re.sub(r'[^a-z0-9]','_',team.lower())+'.json')
    if path.exists(): return team,'cached'
    errors=[]
    for suffix in ['/sports/football/roster/2025','/sports/football/coaches/2025','/sports/football/roster/season/2025-26','/sports/football/roster/season/2025','/sports/football/roster/2025-26']:
        url='https://'+domain+suffix
        try:
            response=urllib.request.urlopen(urllib.request.Request(url,headers={'User-Agent':'Mozilla/5.0'}),timeout=20)
            html=response.read().decode('utf-8',errors='replace'); soup=BeautifulSoup(html,'html.parser')
            heading=' '.join(x.get_text(' ',strip=True) for x in soup.find_all(['h1','title']))
            # A path containing 2025 can silently show today's staff: reject it.
            if '2025' not in heading: raise ValueError('No 2025 season in page title: '+heading[:150])
            candidates=[]
            for a in soup.find_all('a',href=True):
                if not re.search(r'/(?:coaches|staff)/[^/]+',a['href']): continue
                name=a.get_text(' ',strip=True)
                if not name or len(name)>65 or 'Full Bio' in name: continue
                text=''
                for parent in list(a.parents)[:8]:
                    text=parent.get_text(' ',strip=True)
                    if re.search(r'coordinator|head.*coach',text,re.I) and len(text)<900: break
                if re.search(r'coordinator|head.*coach',text,re.I) and len(text)<900:
                    candidates.append({'name':name,'context':text,'bio':a['href']})
            unique=list({(c['name'],c['context']):c for c in candidates}.values())
            if not unique: raise ValueError('No recognized staff markup')
            result={'team':team,'source':url,'title':heading,'candidates':unique}
            path.write_text(json.dumps(result,indent=2,ensure_ascii=False),encoding='utf-8')
            return team,str(len(unique))
        except Exception as e: errors.append(str(e)[:180])
    return team,'FAILED '+str(errors)
if __name__=='__main__':
    with concurrent.futures.ThreadPoolExecutor(max_workers=4) as pool:
        for team,result in pool.map(collect,SCHOOLS): print(team+': '+result,flush=True)
