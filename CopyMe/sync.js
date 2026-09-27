// Sequenced Silo updates, serialized so Stop follows pending progress writes.
function createReporter(sync, request, report) {
  let sequence=0, chain=Promise.resolve(), lastSent=0, lastPaused, latest, stopped=false, queued=0;
  async function transmit(method, url, body) {
    for (let attempt=0;attempt<3;attempt++) {
      try {
        const response=await request(url,{method,headers:sync.headers,body:JSON.stringify(body),redirect:'error',signal:AbortSignal.timeout(8000)});
        if(response.ok) {report(method==='DELETE'?'Position saved. VLC session ended.':'VLC progress synced to Silo.',false);return;}
        if(response.status<500 && response.status!==429) throw Object.assign(new Error(response.status===401?'Silo login expired. Sign in and reopen the video to resume syncing.':'Silo rejected progress sync (HTTP '+response.status+').'),{terminal:true});
      } catch(e) {if(e.terminal)throw e;}
      if(attempt<2) await new Promise(resolve=>setTimeout(resolve,1000*(attempt+1)));
    }
    throw new Error('Cannot reach Silo. Progress sync failed; check your connection.');
  }
  function enqueue(method,url,body) {
    queued++;
    chain=chain.then(()=>transmit(method,url,body)).catch(e=>report(e.message,true)).finally(()=>{queued--});return chain;
  }
  return {
    sample(event, now=Date.now()) {
      if(stopped || !Number.isFinite(event.position) || event.position<0 || event.position>31536000)return;
      latest=event;
      if(queued)return;
      if(lastSent && now-lastSent<10000 && lastPaused===event.paused)return;
      lastSent=now;lastPaused=event.paused;
      return enqueue('POST',sync.sessionUrl+'/progress',{installation_id:sync.installationId,sequence:++sequence,position:event.position,is_paused:!!event.paused});
    },
    stop(event) {
      if(stopped)return chain;stopped=true;
      const sample=event?.observed && Number.isFinite(event.position) ? event : latest;
      const body={installation_id:sync.installationId,stop_id:crypto.randomUUID()};
      if(sample)Object.assign(body,{sequence:++sequence,position:sample.position,is_paused:true});
      return enqueue('DELETE',sync.sessionUrl,body);
    }
  };
}
function startSync(payload,tabId,reply) {
  const report=(text,error)=>{
    chrome.storage.local.set({syncStatus:text});
    chrome.action.setBadgeText({text:error?'!':''});
    if(error)chrome.tabs.sendMessage(tabId,{type:'sync-status',text}).catch(()=>{});
  };
  const reporter=createReporter(payload.sync,fetch,report);
  const port=chrome.runtime.connectNative('cc.nightbyte.vlc_bridge');
  let replied=false,ended=false;
  port.onMessage.addListener(event=>{
    if(!replied && Object.hasOwn(event,'ok')) {replied=true;reply(event);}
    if(event.type==='sample')reporter.sample(event);
    if(event.type==='warning')report(event.error,true);
    if(event.type==='stopped') {ended=true;reporter.stop(event).finally(()=>port.disconnect());}
  });
  port.onDisconnect.addListener(()=>{
    const error=chrome.runtime.lastError;
    if(!replied)reply({ok:false,error:'Windows launcher disconnected. Install the latest update.'});
    if(!ended && replied) {report('VLC connection lost; saving the last observed position.',true);reporter.stop();}
  });
  // API credentials stay in browser memory; the native process receives only the stream.
  port.postMessage({action:'play',origin:payload.origin,url:payload.url,position:payload.position});
}
