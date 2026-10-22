#!/usr/bin/env python3
"""Bounded benchmark on the dedicated e2e test DB; never seeds real traffic into the user database."""
from pathlib import Path
import subprocess,urllib.request,http.cookiejar,json,time,concurrent.futures,statistics,os
root=Path(__file__).resolve().parents[1]
database='saigonguide_e2e_test';assert database.endswith('_test')
sql="""
BEGIN;
TRUNCATE analytics_events,analytics_sessions;
INSERT INTO analytics_sessions(id,visitor_id,consent_policy_version,started_at,last_event_at)
SELECT gen_random_uuid(),gen_random_uuid(),'1.0',now()-interval '5 minutes',now()-interval '10 seconds' FROM generate_series(1,1000);
INSERT INTO analytics_events(id,schema_version,event_type,session_id,place_id,language_code,page_key,client_timestamp,received_at,payload_hash)
SELECT gen_random_uuid(),1,CASE WHEN n%2=0 THEN 'page_view' ELSE 'place_view' END,s.id,
CASE WHEN n%2=1 THEN (SELECT id FROM places WHERE status='published' ORDER BY id LIMIT 1) ELSE NULL END,
CASE WHEN n%2=1 THEN 'vi' ELSE NULL END,CASE WHEN n%2=0 THEN 'explore' ELSE NULL END,
now()-interval '2 minutes',now()-interval '2 minutes',decode(repeat('00',32),'hex')
FROM analytics_sessions s CROSS JOIN generate_series(1,100) n;
ANALYZE analytics_events;
COMMIT;
"""
subprocess.run(['docker','exec','-i','saigonguide-db','psql','-v','ON_ERROR_STOP=1','-U','saigon','-d',database],input=sql,text=True,stdout=subprocess.DEVNULL,check=True)
config=dict(line.split('=',1) for line in (root/'.local/bootstrap.env').read_text().splitlines() if '=' in line)
base='http://127.0.0.1:5081';opener=urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()))
def get(path):
 with opener.open(base+path,timeout=30) as r:return json.load(r)
csrf=get('/api/v1/csrf')['token'];req=urllib.request.Request(base+'/api/v1/auth/login',data=json.dumps({'email':config['Bootstrap__AdminEmail'],'password':config['Bootstrap__AdminPassword']}).encode(),headers={'Content-Type':'application/json','X-CSRF-TOKEN':csrf})
with opener.open(req) as r:assert r.status==200
for _ in range(2):
 d=get('/api/v1/admin/dashboard');assert d['kpis']['pageViews']==50000 and d['kpis']['sessions']==1000 and d['kpis']['visitors']==1000
results={'places':[],'dashboard':[]};errors=[]
def one(i):
 key='places' if i%2==0 else 'dashboard';path='/api/v1/places' if key=='places' else '/api/v1/admin/dashboard';t=time.perf_counter()
 try:
  d=get(path)
  if key=='places':assert len(d)==6
  else:assert d['kpis']['pageViews']==50000 and d['kpis']['placeViews']==50000
  return key,(time.perf_counter()-t)*1000,None
 except Exception as e:return key,(time.perf_counter()-t)*1000,type(e).__name__
start=time.monotonic();duration=300
with concurrent.futures.ThreadPoolExecutor(max_workers=20) as pool:
 wave=0
 while time.monotonic()-start<duration:
  for key,ms,error in pool.map(one,range(20)):
   results[key].append(ms)
   if error:errors.append(error)
  wave+=1
  remaining=4-(time.monotonic()-start-4*(wave-1))
  if remaining>0:time.sleep(remaining)
def describe(v):
 v=sorted(v);return {'requests':len(v),'p50Ms':round(statistics.median(v),2),'p95Ms':round(v[max(0,int(len(v)*.95)-1)],2),'maxMs':round(max(v),2)}
report={'datasetEvents':100000,'sessions':1000,'concurrentClients':20,'durationSeconds':round(time.monotonic()-start,1),'logicalCPUs':os.cpu_count(),'endpoints':{k:describe(v) for k,v in results.items()},'errors':errors,'source':'isolated synthetic benchmark; not real visitors'}
(root/'.local/performance.json').write_text(json.dumps(report,indent=2));print(json.dumps(report,indent=2));raise SystemExit(bool(errors))
