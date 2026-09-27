const vm=require('node:vm'),fs=require('node:fs'),assert=require('node:assert/strict');
const ctx={crypto:require('node:crypto').webcrypto,AbortSignal,setTimeout:fn=>{fn()},console};vm.createContext(ctx);vm.runInContext(fs.readFileSync(require('node:path').join(__dirname,'../CopyMe/sync.js'),'utf8'),ctx);
(async()=>{
 const calls=[],reports=[];let retry=true;
 const r=ctx.createReporter({sessionUrl:'https://example.test/api/v2/playback/session',installationId:'install',headers:{Authorization:'Bearer test'}},async(url,options)=>{
  calls.push({url,...options,body:JSON.parse(options.body)});
  if(retry){retry=false;return {ok:false,status:503}}return {ok:true};
 },(message,error)=>reports.push({message,error}));
 await r.sample({position:100,paused:false},10000);
 r.sample({position:102,paused:false},12000); // throttled, but retained for close
 await r.sample({position:103,paused:true},13000); // immediate pause
 await r.sample({position:30,paused:false},14000); // backwards seek, resume
 await r.stop({observed:true,position:31});
 await r.stop({observed:true,position:999});
 assert.equal(calls.length,5);assert.deepEqual(calls[0].body,calls[1].body);
 assert.deepEqual(calls.map(c=>c.body.sequence),[1,1,2,3,4]);
 assert.equal(calls[2].body.is_paused,true);assert.equal(calls[3].body.position,30);
 assert.equal(calls[4].method,'DELETE');assert.equal(calls[4].body.position,31);assert.ok(calls[4].body.stop_id);
 const failures=[];const denied=ctx.createReporter({sessionUrl:'https://example.test/api/v2/playback/session'},async()=>({ok:false,status:401}),(text,error)=>failures.push({text,error}));
 await denied.sample({position:12,paused:false});assert.equal(failures[0].error,true);assert.match(failures[0].text,/expired/);
 const empty=[];const unopened=ctx.createReporter({sessionUrl:'https://example.test/api/v2/playback/session'},async(u,o)=>{empty.push(JSON.parse(o.body));return {ok:true}},()=>{});await unopened.stop({position:42,observed:false});assert.equal(empty[0].position,undefined);
 console.log('PASS: progress cadence, pause/resume, backward seek, ordered stop, identical retries, no duplicate Stop, failed authentication, no invented position.');
})().catch(e=>{console.error(e);process.exit(1)});

