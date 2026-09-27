importScripts('sync.js');
chrome.action.onClicked.addListener(() => chrome.runtime.openOptionsPage());
chrome.runtime.onInstalled.addListener(() => chrome.runtime.openOptionsPage());
chrome.runtime.onMessage.addListener((message, sender, reply) => {
  if (sender.frameId !== 0 || message?.type !== 'launch') return;
  (async () => {
    const {server} = await chrome.storage.local.get('server');
    if (!server || sender.origin !== new URL(server).origin) return reply({ok:false,error:'Server is not configured.'});
    const payload = {...message.payload, origin:new URL(server).origin};
    if (payload.action === 'play') {
      if (new URL(payload.url).origin !== payload.origin || new URL(payload.sync?.sessionUrl).origin !== payload.origin || !new URL(payload.sync.sessionUrl).pathname.match(/\/api\/v2\/playback\/[^/]+$/)) throw new Error('Invalid session');
      startSync(payload, sender.tab.id, reply);
      return;
    }
    chrome.runtime.sendNativeMessage('cc.nightbyte.vlc_bridge', payload, result => {
      const error = chrome.runtime.lastError;
      reply(error ? {ok:false, error:'Open the latest VLC-Bridge-Setup.exe and click Install / update.'} : result?.version !== '0.2.0' ? {ok:false,error:'Update the Windows launcher with VLC-Bridge-Setup.exe to enable progress sync.'} : result);
    });
  })().catch(() => reply({ok:false,error:'Invalid launcher request.'}));
  return true;
});
