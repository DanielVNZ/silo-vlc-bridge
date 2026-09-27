const feedback=(id,text,state='')=>{const element=document.getElementById(id);element.textContent=text;element.dataset.state=state;};
const serverState=server=>{const badge=document.getElementById('server-state');badge.textContent=server?'Configured':'Not configured';badge.classList.toggle('ready',!!server);};
document.getElementById('version').textContent='V'+chrome.runtime.getManifest().version+' / SETTINGS';
document.getElementById('test').addEventListener('click', () => {
  const button=document.getElementById('test');button.disabled=true;button.textContent='Checking…';
  feedback('test-result','Connecting to the Windows launcher…');
  chrome.runtime.sendNativeMessage('cc.nightbyte.vlc_bridge',{action:'check'}, result => {
    const error = chrome.runtime.lastError;
    const ready=!error && result?.ok && result.version==='0.2.0';
    feedback('test-result',ready?'Connected. VLC is ready on this PC.':error?'Launcher unavailable. Open VLC Bridge Setup and choose Install / update.':result?.ok?'Launcher update needed. Run the latest VLC Bridge Setup.':result?.error || 'Launcher check failed.',ready?'success':'error');
    document.getElementById('launcher-dot').className='state-dot '+(ready?'ready':'error');
    button.disabled=false;button.textContent='Check connection ↗';
  });
});
chrome.storage.local.get('syncStatus').then(({syncStatus})=>{document.getElementById('sync-status').textContent=syncStatus || 'No synced playback yet.';});
chrome.storage.onChanged.addListener(changes=>{if(changes.syncStatus)document.getElementById('sync-status').textContent=changes.syncStatus.newValue;});
chrome.storage.local.get('server').then(({server}) => {document.getElementById('server').value=server || '';serverState(server);});
document.getElementById('settings').addEventListener('submit', async e => {
  e.preventDefault();
  const status = document.getElementById('status');
  const save=document.getElementById('save');
  try {
    const url = new URL(document.getElementById('server').value);
    if (!['http:','https:'].includes(url.protocol) || url.username || url.password || url.search || url.hash) throw new Error('Enter an HTTP(S) server URL without credentials, query parameters or fragments.');
    const match = url.origin + '/*';
    if (!await chrome.permissions.request({origins:[match]})) throw new Error('Server access was not granted.');
    save.disabled=true;save.textContent='Saving…';
    const previous = await chrome.storage.local.get('server');
    await chrome.scripting.unregisterContentScripts();
    await chrome.scripting.registerContentScripts([
      {id:'bridge',matches:[match],js:['bridge.js'],runAt:'document_start',world:'ISOLATED'},
      {id:'player',matches:[match],js:['player.js'],runAt:'document_start',world:'MAIN'}
    ]);
    await chrome.storage.local.set({server:url.href});
    if (previous.server && new URL(previous.server).origin !== url.origin) await chrome.permissions.remove({origins:[new URL(previous.server).origin+'/*']});
    serverState(url.href);
    feedback('status','Connection saved. Reload your Silo tab and press play.','success');
  } catch (error) {feedback('status',error.message,'error');}
  finally {save.disabled=false;save.textContent='Save connection ↗';}
});
