(() => {
  const originalFetch = window.fetch.bind(window);
  const video = ['h264','hevc','vp8','vp9','av1','mpeg2video','mpeg4','vc1','theora'];
  const audio = ['aac','ac3','eac3','dts','truehd','flac','alac','opus','vorbis','mp3','pcm_s16le','pcm_s24le'];
  const containers = ['mkv','matroska','mp4','mov','avi','webm','mpegts','ts','m4v','ogg'];
  const pending = new Map();
  window.addEventListener('message', e => {
    if(e.source===window && e.origin===location.origin && e.data?.channel==='vlc-bridge-status')notice(e.data.text);
    if (e.source === window && e.origin === location.origin && e.data?.channel === 'vlc-bridge-response') {
      pending.get(e.data.id)?.(e.data.result);
    }
  });
  function launch(payload) {
    return new Promise((resolve, reject) => {
      const id = crypto.randomUUID();
      const timer = setTimeout(() => {pending.delete(id); reject(new Error('VLC launcher timed out.'));}, 15000);
      pending.set(id, result => {clearTimeout(timer); pending.delete(id); result?.ok ? resolve() : reject(new Error(result?.error || 'VLC launch failed.'));});
      window.postMessage({channel:'vlc-bridge-request', id, payload}, location.origin);
    });
  }
  function notice(text) {
    document.getElementById('vlc-bridge-notice')?.remove();
    const box = document.createElement('div'); box.id = 'vlc-bridge-notice';
    box.style.cssText = 'position:fixed;z-index:2147483647;bottom:24px;left:24px;max-width:480px;padding:20px;background:#17202b;color:white;border:1px solid #ffae45;border-radius:12px;font:15px/1.5 system-ui;box-shadow:0 8px 32px #0008';
    const label = document.createElement('div'); label.textContent = text; box.append(label);
    const close = document.createElement('button'); close.textContent = 'Dismiss'; close.style.cssText='margin-top:12px;padding:6px 12px;cursor:pointer'; close.onclick=()=>box.remove(); box.append(close);
    (document.body || document.documentElement).append(box);
  }
  window.fetch = async function(input, init) {
    const req = new Request(input, init);
    const url = new URL(req.url);
    if (url.origin !== location.origin || !url.pathname.endsWith('/api/v2/playback/start') || req.method !== 'POST') return originalFetch(input, init);
    let body;
    try {body = await req.clone().json();} catch {return originalFetch(input, init);}
    if (body.protocol_version !== 3) return originalFetch(input, init);
    let createdSession;
    let handedOff = false;
    try {
      // Fail before creating a playback session if installation is incomplete.
      await launch({action:'check'});
      body.quality_preference = 'original';
      body.allow_alternate_versions = false;
      body.client_features = ['playback_plan_v3'];
      // VLC owns presentation, not the browser's MediaSource/display probes.
      // Do not promise native Dolby Vision output or a server-side conversion.
      const hdr = {hdr10:false,hdr10_plus:false,hlg:false,dolby_vision_profiles:[]};
      body.client_capabilities = {video_evidence:'declared', audio_evidence:'declared', codecs_video:video, codecs_video_hardware:[], codecs_audio:audio, containers, max_resolution:'8k',hdr:false,hdr_details:hdr};
      const context = body.client_playback_context;
      if (!context?.deliveries?.original_http) throw new Error('Unsupported Silo playback context.');
      context.app_version = 'vlc-bridge-0.2.1';
      context.device = {platform:'windows'};
      context.form_factor = 'desktop';
      context.output = {};
      context.deliveries = {original_http:{enabled:true,supported_on_device:true,containers,video_codecs:video,audio_decode_codecs:audio,audio_passthrough_codecs:[],hdr_details:hdr,features:[],auth_header_refresh:false,validated_claims:['client_managed_dynamic_range_v1'],transformations:[],subtitles:{embedded_text:true,sidecar_text:false,ass_styling:true,embedded_bitmap:true,sidecar_bitmap:false,font_attachments:true}}};
      const response = await originalFetch(new Request(req, {body:JSON.stringify(body)}));
      if (!response.ok) {notice('Silo refused original playback. Check your server playback restrictions.'); return response;}
      const decision = await response.clone().json();
      const plan = decision.playback_plan;
      createdSession = decision.session_id || plan?.session_id;
      if (!plan || plan.delivery !== 'original_http') throw new Error((decision.terminal?.message || 'Silo did not provide an original-file stream.') + ' Original-file-only mode is enabled; no converted stream was opened.');
      if (Object.keys(plan.stream.headers || {}).length) throw new Error('This stream requires custom headers; this bridge version cannot launch it.');
      const stream = new URL(plan.stream.url, location.origin);
      if (stream.origin !== location.origin) throw new Error('Stream uses a separate worker host. This version supports streams on the configured server host.');
      const auth = req.headers.get('Authorization');
      if (auth?.startsWith('Bearer ') && !stream.searchParams.has('token')) stream.searchParams.set('token',auth.slice(7));
      if (!createdSession) throw new Error('Silo did not provide a playback session for progress sync.');
      const sessionUrl = new URL(url.href);
      sessionUrl.pathname=sessionUrl.pathname.replace(/start$/,encodeURIComponent(createdSession));
      const headers={};
      for(const name of ['Authorization','X-Profile-Id','X-Profile-Token','X-Silo-Device-Id','X-Silo-Client','X-Silo-Client-Version']) {
        const value=req.headers.get(name);if(value)headers[name]=value;
      }
      headers['Content-Type']='application/json';
      await launch({action:'play',url:stream.href,position:plan.timeline?.source_start_seconds || 0,sync:{sessionUrl:sessionUrl.href,installationId:body.installation_id,headers}});
      handedOff = true;
      notice('Opened in VLC with progress sync enabled. Keep this browser running while watching. Use VLC for audio and subtitle selection.');
      // Do not give the web player the stream, which would cause duplicate playback.
      return new Response(JSON.stringify({error:{code:'external_player',message:'Opened in VLC'}}),{status:409,headers:{'Content-Type':'application/json'}});
    } catch (error) {
      if (createdSession && !handedOff) {
        const stopUrl = new URL(url.href);
        stopUrl.pathname = stopUrl.pathname.replace(/start$/, encodeURIComponent(createdSession));
        await originalFetch(stopUrl, {method:'DELETE',headers:req.headers,body:JSON.stringify({installation_id:body.installation_id,stop_id:crypto.randomUUID()})}).catch(()=>{});
      }
      notice(error.message + ' Disable the extension and reload to use Silo’s web player.');
      return new Response(JSON.stringify({error:{code:'external_player',message:error.message}}),{status:409,headers:{'Content-Type':'application/json'}});
    }
  };
})();
