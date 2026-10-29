"""Fit frozen, rank-matched positional salary curves from the developer canonical-rating audit.
Usage: python build/calibrate-pro-market.py AUDIT.json OUTPUT.json
Never writes a franchise or adjusts an existing contract.
"""
import argparse,json,math
from pathlib import Path
p=argparse.ArgumentParser(description=__doc__);p.add_argument('audit',type=Path);p.add_argument('output',type=Path);a=p.parse_args()
s=json.loads(a.audit.read_text(encoding='utf-8'))
minimum=16;groups={g['position']:g['players'] for g in s['positions']}
groups['K']=[r for key,rows in list(groups.items()) if key.startswith('K/') for r in rows]
def quantile(values,q):
 values=sorted(values);at=(len(values)-1)*q;lo=int(at);hi=math.ceil(at)
 return values[lo]+(values[hi]-values[lo])*(at-lo)
curves=[];sparse=[]
for key,rows in sorted(groups.items()):
 if len(rows)<minimum:sparse.append(key);continue
 ratings=[r['overall'] for r in rows];pay=[r['annualValue'] for r in rows]
 ceiling=round(min(max(pay),quantile(pay,.95)*1.25))
 points={40.0:300000}
 for q in [.05,.25,.5,.75,.9,.95]:
  rating=max(41.,min(98.,quantile(ratings,q)));salary=round(min(ceiling,max(300000,quantile(pay,q))))
  points[rating]=max(points.get(rating,0),salary)
 points[99.]=ceiling
 anchors=[];last=0
 for rating,salary in sorted(points.items()):
  last=max(last,salary);anchors.append(dict(overall=rating,annual=last))
 curves.append(dict(position=key,samples=len(rows),ceiling=ceiling,anchors=anchors))
required={'QB','RB','WR','TE','OL','DE','DT','OLB','MLB','CB','FS','SS','K'}
assert required <= {c['position'] for c in curves},'Insufficient broad-position samples; review fallback before publishing'
result=dict(version=1,baseCap=s['baseCap'],contractPackSha256=s['contractPackSha256'],rosterPackSha256=s['rosterPackSha256'],minimumSamples=minimum,
 method='Within-position rating/pay quantile rank mapping (.05,.25,.5,.75,.90,.95), monotone log-dollar interpolation;300K lower-tail anchor, upper tail bounded by lesser of observed maximum and125% of95th percentile. Signed source APY distributions are2026 benchmarks, not guaranteed obligations. Sparse specialist groups fall back to broad K. No historical signing-cap normalization.',sparseGroups=sparse,curves=curves)
a.output.write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8');print('Fit',len(curves),'curves; sparse groups:',sparse)
