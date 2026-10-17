import { cp, mkdir, readFile, readdir, rm, writeFile } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';
import path from 'node:path';
import { brotliDecompressSync, gunzipSync } from 'node:zlib';

const root = fileURLToPath(new URL('../', import.meta.url));
const source = path.resolve(root, '../Builds/WebGL');
const output = path.join(root, 'public');
// Decode Unity's HTTP-compressed files before packaging: the miniapp CDN
// does not inherit Unity's server-specific Content-Encoding configuration.
await readFile(path.join(source, 'index.html'));
await rm(output, { recursive: true, force: true });
await mkdir(output, { recursive: true });
await cp(source, output, { recursive: true });
for (const name of await readdir(path.join(output, 'Build'))) {
  if (!/\.(br|gz)$/.test(name)) continue;
  const file = path.join(output, 'Build', name);
  const data = await readFile(file);
  await writeFile(file.replace(/\.(br|gz)$/, ''), name.endsWith('.br') ? brotliDecompressSync(data) : gunzipSync(data));
  await rm(file);
}
let html = await readFile(path.join(output, 'index.html'), 'utf8');
html = html.replace(/\.(br|gz)(?=["'])/g, '');
html = html.replace('</head>', `<style>
html,body{margin:0;width:100%;height:100%;overflow:hidden;background:#eae6dd}
#unity-container{position:fixed!important;inset:0!important;transform:none!important;width:100%!important;height:100%!important}
#unity-fullscreen-container{width:100%!important;height:100%!important}
#unity-canvas{width:100%!important;height:100%!important;display:block;touch-action:none}
#unity-footer{display:none!important}
</style></head>`);
html = html.replace('.then((unityInstance) => {', '.then((unityInstance) => { window.paperShotUnity = unityInstance;');
html = html.replace('</body>', '<script type="module" src="/src/bridge.js"></script></body>');
await writeFile(path.join(root, 'index.html'), html);
await rm(path.join(output, 'index.html'));
console.log('Prepared Unity WebGL files for Toss.');
