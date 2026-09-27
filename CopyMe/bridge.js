chrome.runtime.onMessage.addListener(message => {
  if(message.type==='sync-status')window.postMessage({channel:'vlc-bridge-status',text:message.text},location.origin);
});
window.addEventListener('message', async event => {
  if (event.source !== window || event.origin !== location.origin || event.data?.channel !== 'vlc-bridge-request') return;
  const {id, payload} = event.data;
  try {
    const result = await chrome.runtime.sendMessage({type:'launch', payload});
    window.postMessage({channel:'vlc-bridge-response', id, result}, location.origin);
  } catch {
    window.postMessage({channel:'vlc-bridge-response', id, result:{ok:false,error:'Reload Silo after loading the extension.'}}, location.origin);
  }
});
