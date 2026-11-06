// Downloads one product image per two-wheeler colour from BikeWale.
// Usage: node _download_images.js [map|download|report]
//   map      - fetch brand pages, match CSV models to BikeWale model slugs -> _model_map.json (review/edit it)
//   download - fetch colour pages, match colours, download images (resumable, skips existing files)
// Default: map (if no map) then download.
'use strict';
const fs = require('fs');
const path = require('path');
const https = require('https');

const ROOT = __dirname;
const CSV = path.join(ROOT, '_TWColorImageList.csv');
const MAP = path.join(ROOT, '_model_map.json');
const CACHE = process.env.BW_CACHE || path.join(require('os').tmpdir(), 'bw_cache');
fs.mkdirSync(CACHE, { recursive: true });
const UA = 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Safari/537.36';

const BRAND_SLUG = {
  'Ampere Vehicles': 'ampere', 'Aprilia': 'aprilia', 'Ather Energy': 'ather', 'Bajaj': 'bajaj', 'Benelli': 'benelli',
  'BMW': 'bmw', 'Bounce infinity': 'bounce', 'BSA': 'bsa', 'CF Moto': 'cfmoto', 'Detel EV': 'detel', 'Ducati': 'ducati',
  'Evolet': 'evolet', 'Harley Davidson': 'harleydavidson', 'Hero': 'hero', 'Honda': 'honda', 'Husqvarna': 'husqvarna',
  'Indian': 'indian', 'JAWA': 'jawa', 'Joy e-Bike': 'joyebike', 'Kabira Mobility': 'kabira-mobility', 'Kawasaki': 'kawasaki',
  'Keeway': 'keeway', 'Kinetic Green': 'kinetic-green', 'KTM': 'ktm', 'Moto Guzzi': 'motoguzzi', 'Moto Morini': 'motomorini',
  'Oben Electric': 'oben', 'Okaya EV': 'okaya', 'Ola Electric': 'ola', 'PURE EV': 'pureev', 'QJ Motor': 'qjmotor',
  'Revolt Motors': 'revolt', 'Royal Enfield': 'royalenfield', 'Simple Energy': 'simple', 'Suzuki': 'suzuki', 'Triumph': 'triumph',
  'TVS': 'tvs', 'Ultraviolette Automotive': 'ultraviolette', 'Yamaha': 'yamaha', 'Yezdi': 'yezdi', 'Zontes': 'zontes',
};

// Manual corrections (reviewed by hand). Key "Brand|TwoWheeler".
// Value: array of BikeWale paths (tried in order until the colour matches), or
// { variants: { "<Variant>": [paths] }, default: [paths] }. [] = not on BikeWale.
const O = {
  'Ampere Vehicles|Ampere Reo Li/LA Plus': ['ampere-bikes/reo'],
  'Aprilia|Aprilia SR': { variants: { 'Aprilia SR Storm STD': ['aprilia-bikes/storm-125'], 'Aprilia SR 125 STD': ['aprilia-bikes/sr-125'] }, default: ['aprilia-bikes/sr-160'] },
  'Benelli|Benelli TOURING': { variants: { 'Benelli TRK 502 Standard': ['benelli-bikes/trk-502', 'benelli-bikes/trk-502-2020-2024'], 'Benelli TRK 502X': ['benelli-bikes/trk-502x', 'benelli-bikes/trk-502x-2020-2024'] } },
  'Benelli|Benelli NAKED': ['benelli-bikes/502c'],
  'BMW|BMW K 1600 GTL': ['bmw-bikes/k-1600', 'bmw-bikes/k1600gt', 'bmw-bikes/k1600-gtl-2019'],
  'BMW|BMW K 1600 B': ['bmw-bikes/k-1600', 'bmw-bikes/k-1600-b-2019'],
  'Bounce infinity|Infinity E.1': ['bounce-bikes/infinity-e1'],
  'BSA|BSA': ['bsa-bikes/gold-star-650'],
  'Ducati|Ducati Panigale': { variants: { 'Ducati Panigale V4 R': ['ducati-bikes/panigale-v4-r'], 'Ducati Panigale V2': ['ducati-bikes/panigale-v2'], 'Ducati Panigale V2 S Red': ['ducati-bikes/panigale-v2'], 'Ducati Panigale V2 S Yellow': ['ducati-bikes/panigale-v2'] }, default: ['ducati-bikes/panigale-v4'] },
  'Ducati|Ducati Scrambler 2G': { variants: { 'Ducati Scrambler 2G Nightshift': ['ducati-bikes/scrambler-nightshift'], 'Ducati Scrambler 2G Full Throttle': ['ducati-bikes/scrambler-full-throttle'] }, default: ['ducati-bikes/scrambler-icon'] },
  'Ducati|Ducati Streetfighter': { variants: { 'Ducati Streetfighter V4 S': ['ducati-bikes/streetfighter-v4'], 'Ducati Streetfighter V4': ['ducati-bikes/streetfighter-v4'] }, default: ['ducati-bikes/streetfighter-v2'] },
  'KTM|KTM 890 Duke': { variants: { 'KTM 890 Duke R': ['ktm-bikes/890-duke-r', 'ktm-bikes/890-duke'] }, default: ['ktm-bikes/890-duke'] },
  'Ducati|Ducati Hypermotard': ['ducati-bikes/hypermotard-698-mono'],
  'Ducati|Ducati Multistrada': { variants: { 'Ducati Multistrada V4 RS': ['ducati-bikes/multistrada-v4-rs', 'ducati-bikes/multistrada-v4'], 'Ducati Multistrada V4 Rally': ['ducati-bikes/multistrada-v4-rally'], 'Ducati Multistrada V2 S': ['ducati-bikes/multistrada-v2'], 'Ducati Multistrada V2 S Ducati Red': ['ducati-bikes/multistrada-v2'], 'Ducati Multistrada V2 S Storm Green': ['ducati-bikes/multistrada-v2'] }, default: ['ducati-bikes/multistrada-v4'] },
  'Harley Davidson|Harley Davidson Fat Boy 114': ['harleydavidson-bikes/fat-boy', 'harleydavidson-bikes/fat-boy-114'],
  'Harley Davidson|Harley Davidson Road Glide Special': ['harleydavidson-bikes/road-glide', 'harleydavidson-bikes/road-glide-special-2022'],
  'Hero|Hero HF 100': ['hero-bikes/hf-100'],
  'Hero|Vida V2': ['vida-bikes/v2'],
  'Hero|Vida VX2': ['vida-bikes/vx2'],
  'Hero|Vida Dirt.E K3': [],
  'Indian|Indian New Scout 1250cc': { variants: { 'Indian Scout Bobber': ['indian-bikes/scout-bobber'], 'Indian Scout Classic': ['indian-bikes/scout-classic'], 'Indian Sport Scout': ['indian-bikes/sport-scout'], 'Indian 101 Scout': ['indian-bikes/101-scout'], 'Indian Super Scout': ['indian-bikes/super-scout'] } },
  'Indian|Indian Scout 1133cc': ['indian-bikes/scout-bobber-2017-2020', 'indian-bikes/scout'],
  'Indian|Indian Cruiser': { variants: { 'Indian Super Chief Limited': ['indian-bikes/super-chief-limited'], 'Indian Chief Dark Horse': ['indian-bikes/chief-dark-horse'], 'Indian Chief Bobber Dark Horse': ['indian-bikes/chief-bobber-dark-horse'] } },
  'Indian|Indian Dark Horse': { variants: { 'Indian Chief Dark Horse': ['indian-bikes/chief-dark-horse'], 'Indian Chief Bobber Dark Horse': ['indian-bikes/chief-bobber-dark-horse'], 'Indian Chieftain Dark Horse': ['indian-bikes/chieftain-dark-horse'], 'Indian Springfield Dark Horse': ['indian-bikes/springfield'], 'Indian Roadmaster Dark Horse': ['indian-bikes/roadmaster'] } },
  'Indian|Indian Touring': { variants: { 'Indian Roadmaster': ['indian-bikes/roadmaster'], 'Indian Roadmaster Dark Horse': ['indian-bikes/roadmaster'], 'Indian Roadmaster Limited': ['indian-bikes/roadmaster'] } },
  'Indian|Indian Bagger': { variants: { 'Indian Chieftain Limited': ['indian-bikes/chieftain'], 'Indian Chieftain Dark Horse': ['indian-bikes/chieftain-dark-horse'], 'Indian Springfield': ['indian-bikes/springfield'], 'Indian Springfield Dark Horse': ['indian-bikes/springfield'] } },
  'Indian|Indian Challenger': [],
  'Indian|Indian Super Chief': ['indian-bikes/super-chief-limited'],
  'Indian|Indian Super Chief Dark Horse': ['indian-bikes/super-chief-limited'],
  'Indian|Indian Chief Bobber': ['indian-bikes/chief-bobber-dark-horse'],
  'JAWA|JAWA 42': ['jawa-bikes/42', 'jawa-bikes/42-2024'],
  'JAWA|JAWA 42 Bobber': ['jawa-bikes/42-bobber', 'jawa-bikes/42-bobber-2022'],
  'JAWA|Jawa 42 FJ': ['jawa-bikes/42-fj', 'jawa-bikes/42-fj-2024'],
  'Joy e-Bike|Gen-Next Nanu': ['joyebike-bikes/gen-nxt'],
  'Joy e-Bike|Gen-Next Nanu Eco': ['joyebike-bikes/gen-nxt'],
  'Joy e-Bike|Gen-Next Nanu Plus': ['joyebike-bikes/gen-nxt'],
  'Kawasaki|Kawasaki Ninja': { variants: { 'Kawasaki Ninja ZX-10R': ['kawasaki-bikes/ninja-zx-10r'], 'Kawasaki Ninja 300': ['kawasaki-bikes/ninja-300', 'kawasaki-bikes/ninja-300-2026'], 'Kawasaki Ninja ZX-6R': ['kawasaki-bikes/ninja-zx-6r'], 'Kawasaki Ninja 650': ['kawasaki-bikes/ninja-650', 'kawasaki-bikes/ninja-650-2025'], 'Kawasaki Ninja 500': ['kawasaki-bikes/ninja-500'] } },
  'Kawasaki|Kawasaki Z': { variants: { 'Kawasaki Z900': ['kawasaki-bikes/z900'], 'Kawasaki Z650': ['kawasaki-bikes/z650'], 'Kawasaki Z650RS': ['kawasaki-bikes/z650rs', 'kawasaki-bikes/z650rs-2025'] } },
  'Kawasaki|Kawasaki Dual Sport': ['kawasaki-bikes/klx230', 'kawasaki-bikes/klx230-2025', 'kawasaki-bikes/klx230-s'],
  'Kawasaki|Kawasaki W': ['kawasaki-bikes/w175'],
  'Kawasaki|Kawasaki Urban Cruiser': { variants: { 'Kawasaki Vulcan S': ['kawasaki-bikes/vulcan-s'], 'Kawasaki Eliminator': ['kawasaki-bikes/eliminator'] } },
  'Kawasaki|Kawasaki KLX': { variants: { 'Kawasaki KLX 230R S': ['kawasaki-bikes/klx230r-s', 'kawasaki-bikes/klx230r-2025'], 'Kawasaki KLX 140R F': ['kawasaki-bikes/klx-140r-f'], 'Kawasaki KLX 110R L': ['kawasaki-bikes/klx110r-l'], 'Kawasaki KLX 450R': ['kawasaki-bikes/klx450r'] } },
  'Kawasaki|Kawasaki KX': { variants: { 'Kawasaki KX450': ['kawasaki-bikes/kx450'], 'Kawasaki KX112': ['kawasaki-bikes/kx112'] } },
  'Kawasaki|Kawasaki Versys 650': ['kawasaki-bikes/versys-650-2026', 'kawasaki-bikes/versys-650-2027'],
  'Kinetic Green|Zoom': [],
  // distinct models whose closest BikeWale page is a different bike -> BikeDekho only
  'Bajaj|Bajaj Pulsar N160 S': [], 'PURE EV|ePluto 7G MAX': [], 'Ola Electric|Ola S1 Pro Sport': [], 'Honda|Honda Shine 100 DX': [], 'Hero|Hero HF Deluxe Pro': [],
  'Royal Enfield|Royal Enfield Himalyan': ['royalenfield-bikes/himalayan'],
  'TVS|TVS XL 100': ['tvs-bikes/xl-100-comfort', 'tvs-bikes/xl-100-heavy-duty', 'tvs-bikes/xl-comfort'],
  'Yamaha|Yamaha R15M': ['yamaha-bikes/r15'],
  'Yamaha|Yamaha MT-15 Ver 2.0': ['yamaha-bikes/mt-15'],
  'Yamaha|Yamaha FZ-S Fi Ver 4.0': ['yamaha-bikes/fzs-fi-v4'],
  'Yamaha|Yamaha FZ-S Fi Ver 4.0 DLX': ['yamaha-bikes/fzs-fi-v4'],
  'Yamaha|Yamaha FZ-S Fi': ['yamaha-bikes/fz-s', 'yamaha-bikes/fz-s-hybrid', 'yamaha-bikes/fzs-fi-v4'],
  'Yamaha|Yamaha Aerox 155 Version S': ['yamaha-bikes/aerox-155'],
  'Detel EV|Detel EV Easy Plus': [],
  'Kabira Mobility|KM3000': [], 'Kabira Mobility|KM4000': [], 'Kabira Mobility|Hermes 75': [],
  'Ultraviolette Automotive|F77': ['ultraviolette-bikes/f77-mach-2', 'ultraviolette-bikes/f77-2022'],
  'Honda|Honda CB350 H\'ness': ['honda-bikes/cb350', 'honda-bikes/new-cb350', 'honda-bikes/hness-cb350-2022'],
  'Honda|Honda Shine 125': ['honda-bikes/shine'],
};
function candidates(r, map) {
  const k = r.Brand + '|' + r.TwoWheeler; const o = O[k];
  let list;
  if (Array.isArray(o)) list = o;
  else if (o) list = (o.variants && o.variants[r.Variant]) || o.default || [];
  else list = map[k] && map[k].url ? [map[k].url] : [];
  list = list.map(u => u.startsWith('http') ? u.replace(/\/?$/, '/') : `https://www.bikewale.com/${u}/`);
  // add other generations of the SAME model (e.g. "CB350 [2017-2026]", "New CB350", "Old Generation X")
  const bm = brandModelsCache();
  const out = [...list];
  for (const u of list) {
    const m = u.match(/bikewale\.com\/([^/]+)-bikes\/([^/]+)\//); if (!m) continue;
    const all = bm[r.Brand] || [];
    const me = all.find(x => x.slug === m[2]); if (!me) continue;
    const base = genBase(me.name);
    for (const x of all) if (x.slug !== m[2] && genBase(x.name) === base) { const v = `https://www.bikewale.com/${x.makeSlug}-bikes/${x.slug}/`; if (!out.includes(v)) out.push(v); }
  }
  return out;
}
const genBase = n => norm(String(n).replace(/\[[^\]]*\]/g, ' ')).replace(/^(old generation|new|all new)\s+/, '').replace(/\b(19|20)\d\d\b/g, '').replace(/\s+/g, ' ').trim();
let _bm = null;
function brandModelsCache() { if (!_bm) { const f = path.join(CACHE, 'brandModels.json'); _bm = fs.existsSync(f) ? JSON.parse(fs.readFileSync(f, 'utf8')) : {}; } return _bm; }

// ---------- helpers ----------
function parseCSV(text) {
  text = text.replace(/^﻿/, '');
  const rows = []; let row = [], f = '', q = false;
  for (let i = 0; i < text.length; i++) {
    const c = text[i];
    if (q) { if (c === '"') { if (text[i + 1] === '"') { f += '"'; i++; } else q = false; } else f += c; }
    else if (c === '"') q = true;
    else if (c === ',') { row.push(f); f = ''; }
    else if (c === '\n') { row.push(f.replace(/\r$/, '')); rows.push(row); row = []; f = ''; }
    else f += c;
  }
  if (f.length || row.length) { row.push(f.replace(/\r$/, '')); rows.push(row); }
  const hdr = rows.shift();
  return rows.filter(r => r.length >= hdr.length).map(r => Object.fromEntries(hdr.map((h, i) => [h, r[i]])));
}
function csvCell(v) { v = v == null ? '' : String(v); return /[",\r\n]/.test(v) ? '"' + v.replace(/"/g, '""') + '"' : v; }
const sleep = ms => new Promise(r => setTimeout(r, ms));
let lastReq = 0;
async function polite() { const w = lastReq + 1000 - Date.now(); if (w > 0) await sleep(w); lastReq = Date.now(); }

function rawGet(url, binary, redirects = 5) {
  return new Promise((resolve, reject) => {
    const req = https.get(url, { headers: { 'User-Agent': UA, 'Accept': binary ? 'image/webp,image/jpeg,image/png,*/*;q=0.5' : 'text/html,*/*', 'Accept-Language': 'en-IN,en;q=0.9' }, timeout: 30000 }, res => {
      if ([301, 302, 303, 307, 308].includes(res.statusCode) && res.headers.location && redirects > 0) {
        res.resume(); return resolve(rawGet(new URL(res.headers.location, url).href, binary, redirects - 1));
      }
      const chunks = []; res.on('data', d => chunks.push(d));
      res.on('end', () => resolve({ status: res.statusCode, url, headers: res.headers, body: Buffer.concat(chunks) }));
    });
    req.on('timeout', () => req.destroy(new Error('timeout')));
    req.on('error', reject);
  });
}
async function get(url, binary) {
  for (let a = 0; a < 4; a++) {
    await polite();
    try {
      const r = await rawGet(url, binary);
      if (r.status === 404 || r.status === 410) return r;
      if (r.status >= 200 && r.status < 300) return r;
      if (r.status === 429) await sleep(10000);
    } catch (e) { /* retry */ }
    await sleep(2000 * Math.pow(2, a));
  }
  return { status: 0, url, body: Buffer.alloc(0) };
}
function cacheName(url) { return url.replace(/^https?:\/\//, '').replace(/[^a-z0-9]+/gi, '_').slice(0, 180) + '.json'; }
// Fetch a BikeWale page and return its parsed initial-state (cached). Returns {status, finalUrl, state}
async function getState(url) {
  const cf = path.join(CACHE, cacheName(url));
  if (fs.existsSync(cf)) return JSON.parse(fs.readFileSync(cf, 'utf8'));
  const r = await get(url, false);
  let state = null;
  if (r.status === 200) {
    const m = r.body.toString('utf8').match(/<script[^>]*id=["']initial-state["'][^>]*>([\s\S]*?)<\/script>/);
    if (m) { try { state = JSON.parse(m[1]); } catch (e) { } }
  }
  const out = { status: r.status, finalUrl: r.url, state };
  if (r.status === 200 || r.status === 404 || r.status === 410) fs.writeFileSync(cf, JSON.stringify(out));
  return out;
}

const norm = s => String(s || '').toLowerCase().replace(/&/g, ' and ').replace(/[^a-z0-9]+/g, ' ').trim();
const toks = s => norm(s).split(' ').filter(Boolean);

// ---------- phase 1: model mapping ----------
function brandWords(brand, bwMake) {
  const w = new Set([...toks(brand), ...toks(bwMake || '')]);
  ['vehicles', 'energy', 'electric', 'ev', 'motors', 'automotive', 'mobility', 'infinity', 'e', 'bike'].forEach(x => w.add(x));
  return w;
}
function stripBrand(name, brand) {
  let n = norm(name);
  for (const b of [norm(brand), ...toks(brand)].sort((a, b) => b.length - a.length)) {
    if (b && (n === b || n.startsWith(b + ' '))) { n = n.slice(b.length).trim(); }
  }
  return n;
}
function scoreModel(csvModelStripped, bwName) {
  const a = csvModelStripped.replace(/ /g, ''), b = norm(bwName).replace(/ /g, '');
  if (a === b) return 100;
  const ta = new Set(csvModelStripped.split(' ')), tb = new Set(toks(bwName));
  let inter = 0; ta.forEach(t => { if (tb.has(t)) inter++; });
  const jac = inter / (ta.size + tb.size - inter);
  let s = jac * 80;
  if (b.includes(a) || a.includes(b)) s = Math.max(s, 60 * Math.min(a.length, b.length) / Math.max(a.length, b.length) + 20);
  return Math.round(s);
}

async function buildMap(rows) {
  const old = fs.existsSync(MAP) ? JSON.parse(fs.readFileSync(MAP, 'utf8')) : {};
  const models = {};
  for (const r of rows) { const k = r.Brand + '|' + r.TwoWheeler; models[k] = models[k] || { brand: r.Brand, model: r.TwoWheeler }; }
  const brandModels = {};
  for (const brand of [...new Set(rows.map(r => r.Brand))]) {
    const slug = BRAND_SLUG[brand];
    const list = [];
    for (const kind of ['bikes', 'scooters']) {
      const res = await getState(`https://www.bikewale.com/${slug}-${kind}/`);
      const mp = res.state && res.state.makePage;
      if (!mp) continue;
      for (const m of [...(mp.models || []), ...(mp.upcomingModels || []), ...(mp.discontinuedModels || [])]) {
        if (m.modelMaskingName && !list.find(x => x.slug === m.modelMaskingName)) list.push({ name: m.modelName, slug: m.modelMaskingName, makeSlug: m.makeMaskingName || slug, makeName: m.makeName, discontinued: (mp.discontinuedModels || []).includes(m) });
      }
    }
    brandModels[brand] = list;
    console.log(`brand ${brand} (${slug}): ${list.length} BikeWale models`);
  }
  const map = {};
  for (const [k, v] of Object.entries(models)) {
    if (old[k] && old[k].manual) { map[k] = old[k]; continue; }
    const list = brandModels[v.brand] || [];
    const stripped = stripBrand(v.model, v.brand);
    let best = null;
    for (const m of list) {
      let s = scoreModel(stripped, m.name);
      if (m.discontinued) s -= 3;
      if (!best || s > best.score) best = { ...m, score: s };
    }
    map[k] = best && best.score >= 40 ? { brand: v.brand, model: v.model, bwName: best.name, url: `https://www.bikewale.com/${best.makeSlug}-bikes/${best.slug}/`, score: best.score }
      : { brand: v.brand, model: v.model, bwName: null, url: null, score: best ? best.score : 0, bestGuess: best ? best.name + ' ' + best.slug : null };
  }
  fs.writeFileSync(MAP, JSON.stringify(map, null, 1));
  fs.writeFileSync(path.join(CACHE, 'brandModels.json'), JSON.stringify(brandModels, null, 1));
  return map;
}

// ---------- fallback source: BikeDekho ----------
const BD_BRAND_GUESS = {
  'Ampere Vehicles': ['ampere'], 'Ather Energy': ['ather'], 'Bounce infinity': ['bounce-infinity', 'bounce'], 'CF Moto': ['cfmoto', 'cf-moto'],
  'Detel EV': ['detel'], 'Harley Davidson': ['harley-davidson'], 'Joy e-Bike': ['joy-e-bike'], 'Kabira Mobility': ['kabira-mobility', 'kabira'],
  'Kinetic Green': ['kinetic-green'], 'Moto Guzzi': ['moto-guzzi'], 'Moto Morini': ['moto-morini'], 'Oben Electric': ['oben'],
  'Okaya EV': ['okaya'], 'Ola Electric': ['ola-electric', 'ola'], 'PURE EV': ['pure-ev', 'pure'], 'QJ Motor': ['qj-motor'],
  'Revolt Motors': ['revolt'], 'Royal Enfield': ['royal-enfield'], 'Simple Energy': ['simple-energy', 'simple'],
  'Ultraviolette Automotive': ['ultraviolette'], 'Hero': ['hero', 'vida'],
};
const slugify = s => norm(s).replace(/ /g, '-');
async function bdGet(url) {
  const cf = path.join(CACHE, 'bd_' + cacheName(url));
  if (fs.existsSync(cf)) return JSON.parse(fs.readFileSync(cf, 'utf8'));
  const r = await get(url, false);
  let colours = null;
  if (r.status === 200) {
    const h = r.body.toString('utf8'); const i = h.indexOf('window.__INITIAL_STATE__');
    if (i >= 0) {
      let s = h.slice(i); s = s.slice(s.indexOf('=') + 1); s = s.slice(0, s.indexOf('</script>')).trim().replace(/;\s*window\.__[\s\S]*$/, '').replace(/;$/, '');
      try { const st = JSON.parse(s); colours = (st.colors || []).filter(c => c.name && (c.image || c.url)).map(c => ({ name: c.name, img: c.image || c.url })); } catch (e) { }
    }
  }
  const out = { status: r.status, finalUrl: r.url, colours };
  if (r.status === 200 || r.status === 404 || r.status === 410) fs.writeFileSync(cf, JSON.stringify(out));
  return out;
}
function bdCandidates(r, bwUrls) {
  const brands = BD_BRAND_GUESS[r.Brand] || [slugify(r.Brand)];
  const models = new Set();
  for (const u of bwUrls) { const m = u.match(/-bikes\/([^/]+)\//); if (m) models.add(m[1].toLowerCase().replace(/-(19|20)\d\d(-(19|20)\d\d)?$/, '')); }
  const strip = n => slugify(stripBrand(n, r.Brand).replace(/^(vida|kabira|kinetic green|gen next)\s+/, ''));
  models.add(strip(r.TwoWheeler)); models.add(strip(r.Variant.replace(/\b(std|standard|base|stx?)\b.*$/i, '')));
  const out = [];
  for (const b of brands) for (const m of models) if (m) out.push(`https://www.bikedekho.com/${b}/${m}/colors`);
  return [...new Set(out)];
}

// ---------- phase 2: colours ----------
function scoreColour(csvC, bwC) {
  const a = norm(csvC), b = norm(bwC);
  if (a === b) return 100;
  const sa = a.replace(/ /g, ''), sb = b.replace(/ /g, '');
  if (sa === sb) return 98;
  // common abbreviations / spelling
  const ca = canonColour(a), cb = canonColour(b);
  if (!ca || !cb) return 0;
  if (ca === cb || ca.replace(/ /g, '') === cb.replace(/ /g, '')) return 95;
  const ta = [...new Set(ca.split(' '))], tb = [...new Set(cb.split(' '))];
  let inter = 0, typo = false; const used = new Set();
  for (const t of ta) {
    let j = tb.findIndex((u, i) => !used.has(i) && u === t);
    if (j < 0) { j = tb.findIndex((u, i) => !used.has(i) && tokTypo(t, u)); if (j >= 0) typo = true; }
    if (j >= 0) { used.add(j); inter++; }
  }
  let s = Math.round(90 * inter / (ta.length + tb.length - inter));
  // every basic colour word on either side must be matched, otherwise it's a different scheme
  const unmatchedA = ta.filter(t => !tb.some(u => u === t || tokTypo(t, u)));
  const unmatchedB = tb.filter(u => !ta.some(t => u === t || tokTypo(t, u)));
  if ([...unmatchedA, ...unmatchedB].some(t => BASIC_COLOURS.has(t))) s = Math.min(s, 60);
  return s === 90 && typo ? 85 : s;
}
const BASIC_COLOURS = new Set(['red', 'blue', 'green', 'yellow', 'orange', 'black', 'white', 'gray', 'silver', 'gold', 'golden', 'brown', 'purple', 'violet', 'pink', 'maroon', 'beige', 'bronze', 'copper', 'cyan', 'teal', 'turquoise', 'khaki', 'ivory', 'chrome', 'magenta', 'lime', 'navy', 'tan', 'aqua', 'wine', 'burgundy', 'mint', 'olive', 'cream']);
// Words that don't change which paint scheme it is (trim/edition codes, finish adjectives BikeWale adds, brand prefixes)
const COLOUR_NOISE = new Set(['colour', 'color', 'and', 'with', 'n', 'bluetooth', 'obd', 'obd2', 'drum', 'disc', 'spl', 'standard', 'std', 'be', 'igo', 'gloss', 'glossy', 'vivid', 'livery', 'ducati', 'all', 'star', 'stars', 'dt', 'dlx', 'dual', 'tone', 'laser', 'shadow', 'airstrike', 'no']);
function canonColour(s) {
  return norm(s).replace(/\bgrey\b/g, 'gray').replace(/\b(mat|matt|matte)\b/g, 'matte').replace(/\b(met|metalic|mettalic|mettallic)\b/g, 'metallic')
    .replace(/\bltd\b/g, 'limited').replace(/\blustre\b/g, 'luster').replace(/\b(19|20)\d\d\b/g, ' ').replace(/\b\d\b/g, ' ')
    .split(' ').filter(t => t && !COLOUR_NOISE.has(t)).join(' ');
}
function osa(a, b) { // optimal string alignment distance
  const d = Array.from({ length: a.length + 1 }, (_, i) => [i, ...Array(b.length).fill(0)]);
  for (let j = 0; j <= b.length; j++) d[0][j] = j;
  for (let i = 1; i <= a.length; i++) for (let j = 1; j <= b.length; j++) {
    const c = a[i - 1] === b[j - 1] ? 0 : 1;
    d[i][j] = Math.min(d[i - 1][j] + 1, d[i][j - 1] + 1, d[i - 1][j - 1] + c);
    if (i > 1 && j > 1 && a[i - 1] === b[j - 2] && a[i - 2] === b[j - 1]) d[i][j] = Math.min(d[i][j], d[i - 2][j - 2] + 1);
  }
  return d[a.length][b.length];
}
function tokTypo(a, b) {
  if (/\d/.test(a) || /\d/.test(b)) return false;
  const m = Math.min(a.length, b.length);
  if (m < 4) return false;
  return osa(a, b) <= (m >= 8 ? 2 : 1);
}
function bigImg(p) {
  // p is like https://imgd.aeplcdn.com/664x374/n/xxx.jpg?q=80 or /n/xxx.jpg
  let rel = p.replace(/^https?:\/\/imgd\.aeplcdn\.com\/\d+x\d+/, '').replace(/^https?:\/\/imgd\.aeplcdn\.com/, '');
  rel = rel.split('?')[0];
  if (!rel.startsWith('/')) rel = '/' + rel;
  return `https://imgd.aeplcdn.com/1056x594${rel}?q=80`;
}
function extFrom(buf, ct) {
  if (buf.slice(0, 4).toString('hex') === '89504e47') return 'png';
  if (buf.slice(0, 3).toString('hex') === 'ffd8ff') return 'jpg';
  if (buf.slice(0, 4).toString() === 'RIFF' && buf.slice(8, 12).toString() === 'WEBP') return 'webp';
  if (buf.slice(4, 12).toString().includes('ftypavif')) return 'avif';
  if (/png/.test(ct)) return 'png'; if (/webp/.test(ct)) return 'webp'; if (/jpe?g/.test(ct)) return 'jpg';
  return null;
}
function existing(dir, base) {
  for (const e of ['webp', 'jpg', 'png', 'avif']) { const f = path.join(dir, base + '.' + e); if (fs.existsSync(f)) return f; }
  return null;
}

async function download(rows, map) {
  const colourCache = {}; // url -> [{name,img}]
  const results = new Map();
  const groups = {};
  rows.forEach(r => { const c = candidates(r, map); const k = c.join(' ') + '|' + norm(r.Color) + '|' + r.Brand + '|' + r.TwoWheeler; (groups[k] = groups[k] || { cands: c, rows: [] }).rows.push(r); });
  let n = 0; const total = Object.keys(groups).length;
  for (const [k, g] of Object.entries(groups)) {
    n++;
    const grp = g.rows; const r0 = grp[0];
    const dests = grp.map(r => ({ r, dir: path.join(ROOT, r.RelativeFolder), base: r.FileBase }));
    const have = dests.map(d => existing(d.dir, d.base)).find(Boolean);
    let res = { status: 'NotFound', src: '', file: null, note: '' };
    let best = null, bestUrl = null;
    if (norm(r0.Color)) for (const url of g.cands) {
      if (!colourCache[url]) {
        const st = await getState(url + 'colours/');
        const cols = [];
        const id = st.state && st.state.imageDetails;
        if (id) {
          for (const c of id.colors || []) if (c.imagePath) cols.push({ name: c.name, img: bigImg(c.imagePath) });
          for (const arr of Object.values(id.versionsColours || {})) for (const c of arr) if (c.imagePath && !cols.find(x => norm(x.name) === norm(c.name))) cols.push({ name: c.name, img: bigImg(c.imagePath) });
        }
        colourCache[url] = cols.filter(c => !/grey-16x9|placeholder|\.svg/i.test(c.img));
      }
      for (const c of colourCache[url]) { const s = scoreColour(r0.Color, c.name); if (!best || s > best.s) { best = { ...c, s }; bestUrl = url; } }
      if (best && best.s >= 70) break;
    }
    const bwBest = best;
    let srcTag = 'BW';
    if (norm(r0.Color) && !(best && best.s >= 70)) { // fallback: BikeDekho
      let bdBest = null, bdUrl = null;
      for (const url of bdCandidates(r0, g.cands)) {
        const res2 = await bdGet(url);
        for (const c of res2.colours || []) { const s = scoreColour(r0.Color, c.name); if (!bdBest || s > bdBest.s) { bdBest = { ...c, s }; bdUrl = url; } }
        if (bdBest && bdBest.s >= 70) break;
      }
      if (bdBest && bdBest.s >= 70) { best = bdBest; bestUrl = bdUrl; srcTag = 'BD'; }
    }
    if (norm(r0.Color) && (g.cands.length || (best && best.s >= 70))) {
      if (best && best.s >= 70) {
        res.src = best.img; res.note = `${srcTag} ${bestUrl.replace(/https:\/\/www\.(bikewale|bikedekho)\.com\//, '')} colour "${best.name}" score ${best.s}`;
        if (have) { res.status = 'Downloaded'; res.file = have; }
        else {
          const img = await get(best.img, true);
          const ext = img.status === 200 ? extFrom(img.body, img.headers['content-type'] || '') : null;
          if (ext && img.body.length > 2000) {
            const d0 = dests[0]; fs.mkdirSync(d0.dir, { recursive: true });
            res.file = path.join(d0.dir, d0.base + '.' + ext); fs.writeFileSync(res.file, img.body); res.status = 'Downloaded';
          } else res.note += ` | image fetch failed (${img.status})`;
        }
      } else res.note = bwBest ? `closest BW colour "${bwBest.name}" score ${bwBest.s} (rejected); not on BikeDekho either` : 'no colours on BikeWale/BikeDekho page';
      if (have && !(best && best.s >= 70)) { res.note += ' | WARNING existing file not confirmed by current matcher'; console.log('WARN', have); }
    } else if (have) { res.status = 'Downloaded'; res.file = have; res.note = 'pre-existing file'; }
    else res.note = norm(r0.Color) ? 'model not found on BikeWale/BikeDekho' : 'colour name empty in CSV';
    // copy into every variant folder
    for (const d of dests) {
      let saved = existing(d.dir, d.base);
      if (!saved && res.file) { fs.mkdirSync(d.dir, { recursive: true }); saved = path.join(d.dir, d.base + path.extname(res.file)); fs.copyFileSync(res.file, saved); }
      results.set(d.r, { status: saved ? 'Downloaded' : 'NotFound', src: res.src, saved: saved ? path.relative(ROOT, saved) : '', note: res.note });
    }
    if (n % 25 === 0) console.log(`${n}/${total} colour groups processed`);
  }
  // report
  const hdr = ['Brand', 'TwoWheeler', 'Variant', 'Color', 'TwoWheelerId', 'TWVarientId', 'RelativeFolder', 'FileBase', 'Status', 'SourceURL', 'SavedPath', 'MatchNote'];
  const lines = [hdr.join(',')];
  const missing = [];
  for (const r of rows) {
    const x = results.get(r);
    lines.push([...hdr.slice(0, 8).map(h => r[h]), x.status, x.src, x.saved, x.note].map(csvCell).join(','));
    if (x.status !== 'Downloaded') missing.push(`${r.Brand} | ${r.TwoWheeler} | ${r.Variant} | ${r.Color}`);
  }
  fs.writeFileSync(path.join(ROOT, '_image_report.csv'), '﻿' + lines.join('\r\n') + '\r\n', 'utf8');
  fs.writeFileSync(path.join(ROOT, '_missing_colours.txt'), missing.join('\r\n') + '\r\n', 'utf8');
  console.log(`Rows downloaded: ${rows.length - missing.length}, not found: ${missing.length}`);
}

(async () => {
  const rows = parseCSV(fs.readFileSync(CSV, 'utf8'));
  const mode = process.argv[2] || 'all';
  let map = fs.existsSync(MAP) ? JSON.parse(fs.readFileSync(MAP, 'utf8')) : null;
  if (mode === 'map' || !map) map = await buildMap(rows);
  if (mode === 'download' || mode === 'all') await download(rows, map);
})().catch(e => { console.error(e); process.exit(1); });
