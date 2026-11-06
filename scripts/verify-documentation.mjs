import fs from 'node:fs';
import path from 'node:path';

const repositoryRoot = path.resolve(import.meta.dirname, '..');
const outputRoot = path.resolve(process.argv[2] || path.join(repositoryRoot, 'CyCapture.App', 'Documentation'));
const configPath = path.resolve(process.argv[3] || path.join(repositoryRoot, 'CyCapture.App', '.CyDocBuild', 'cydoc.config.json'));
const dataRoot = path.join(outputRoot, 'DocData');
const errors = [];

function fail(message) {
  errors.push(message);
}

function readJson(filePath, label) {
  try {
    return JSON.parse(fs.readFileSync(filePath, 'utf8'));
  } catch (error) {
    fail(`${label} cannot be read: ${error.message}`);
    return null;
  }
}

function safeOutputPath(relativePath, label) {
  if (typeof relativePath !== 'string' || relativePath.length === 0) {
    fail(`${label} is empty.`);
    return null;
  }
  const normalized = relativePath.replaceAll('\\', '/');
  if (normalized.startsWith('/') || /^[a-zA-Z]:/.test(normalized) || normalized.split('/').includes('..')) {
    fail(`${label} is not a safe relative path: ${relativePath}`);
    return null;
  }
  const resolved = path.resolve(outputRoot, normalized);
  const prefix = `${outputRoot}${path.sep}`;
  if (resolved !== outputRoot && !resolved.startsWith(prefix)) {
    fail(`${label} escapes the documentation export: ${relativePath}`);
    return null;
  }
  return resolved;
}

function requireFile(relativePath, label = relativePath) {
  const resolved = safeOutputPath(relativePath, label);
  if (!resolved) return;
  try {
    const stat = fs.lstatSync(resolved);
    if (!stat.isFile() || stat.isSymbolicLink()) fail(`${label} must be a regular file.`);
  } catch {
    fail(`${label} is missing (${relativePath}).`);
  }
}

if (!fs.existsSync(outputRoot)) throw new Error(`Documentation export does not exist: ${outputRoot}`);

const rootEntries = fs.readdirSync(outputRoot).sort();
const expectedRootEntries = ['DocAI.md', 'DocData', 'Documentation.html'];
if (JSON.stringify(rootEntries) !== JSON.stringify(expectedRootEntries)) {
  fail(`Documentation root must contain only ${expectedRootEntries.join(', ')}; found ${rootEntries.join(', ')}.`);
}

const config = readJson(configPath, 'CyDoc configuration');
const manifest = readJson(path.join(dataRoot, 'manifest.json'), 'CyDoc manifest');
const translationReport = readJson(path.join(dataRoot, 'translation-report.json'), 'Translation report');

for (const file of [
  'Documentation.html',
  'DocAI.md',
  'DocData/viewer.js',
  'DocData/viewer.css',
  'DocData/data.js',
  'DocData/manifest.json',
  'DocData/translation-report.json',
  'DocData/media-image.js',
  'DocData/media-gallery.js',
  'DocData/assets/cycapture-logo.svg',
  'DocData/assets/licenses/CyDoc-MIT.txt'
]) requireFile(file);

const mediaModules = fs.readdirSync(dataRoot, { withFileTypes: true })
  .filter(entry => entry.isFile() && /^media-.*\.js$/i.test(entry.name))
  .map(entry => entry.name)
  .sort();
const expectedMediaModules = ['media-gallery.js', 'media-image.js'];
if (JSON.stringify(mediaModules) !== JSON.stringify(expectedMediaModules)) {
  fail(`Unexpected CyDoc media modules: ${mediaModules.join(', ') || 'none'}.`);
}

if (config && manifest) {
  if (config.defaultLocale !== 'en' || manifest.defaultLocale !== 'en') fail('English must be the source and default locale.');
  const expectedLocales = ['en', 'fr'];
  const manifestLocales = (manifest.locales || []).map(locale => locale.code).sort();
  if (JSON.stringify(manifestLocales) !== JSON.stringify(expectedLocales)) {
    fail(`The generated locales must be exactly English and French; got ${manifestLocales.join(', ')}.`);
  }
  if (manifest.logo !== 'assets/cycapture-logo.svg') fail('The CyCapture logo is not configured in the generated manifest.');

  const pageByRoute = new Map();
  for (const page of manifest.pages || []) {
    const key = `${page.locale}/${page.id}`;
    if (pageByRoute.has(key)) fail(`Duplicate page route: ${key}.`);
    pageByRoute.set(key, page);
    if (page.kind === 'source') fail(`Raw source browsing page is not allowed: ${key}.`);
    if (page.id === 'development' || page.id === 'translation-report') fail(`Internal page leaked into the human manual: ${key}.`);
    for (const asset of page.assets || []) requireFile(path.posix.join('DocData', asset), `Asset used by ${key}`);
  }

  const pagesByLocale = new Map(expectedLocales.map(locale => [locale, new Map()]));
  for (const page of manifest.pages || []) {
    if (!pagesByLocale.has(page.locale)) {
      fail(`Page ${page.id} uses undeclared locale ${page.locale}.`);
      continue;
    }
    pagesByLocale.get(page.locale).set(page.id, page);
  }
  const englishIds = [...pagesByLocale.get('en').keys()].sort();
  const frenchIds = [...pagesByLocale.get('fr').keys()].sort();
  if (JSON.stringify(englishIds) !== JSON.stringify(frenchIds)) fail('Page identifiers differ between English and French.');

  for (const required of ['getting-started', 'print-screen', 'selection', 'image-annotation', 'video', 'gif', 'audio', 'gallery', 'tray', 'storage', 'plugins', 'settings', 'updates', 'troubleshooting', 'plugin-api', 'licenses']) {
    if (!pagesByLocale.get('en').has(required) || !pagesByLocale.get('fr').has(required)) fail(`Required bilingual page is missing: ${required}.`);
  }
  if (!pagesByLocale.get('en').get('settings')?.parent || !pagesByLocale.get('en').get('plugin-api')?.parent) {
    fail('The generated manual does not contain the expected parent hierarchy.');
  }

  for (const page of manifest.pages || []) {
    const sourceRoute = `${page.locale}/${page.id}`;
    for (const link of page.links || []) {
      if (!link.startsWith('#/')) continue;
      const match = /^#\/([^/]+)\/([^/?]+)(?:\/([^?]+))?/.exec(link);
      if (!match) {
        fail(`Malformed internal link in ${sourceRoute}: ${link}.`);
        continue;
      }
      const locale = decodeURIComponent(match[1]);
      const pageId = decodeURIComponent(match[2]);
      const anchor = match[3] ? decodeURIComponent(match[3]) : undefined;
      const target = pageByRoute.get(`${locale}/${pageId}`);
      if (!target) {
        fail(`Broken page link in ${sourceRoute}: ${link}.`);
        continue;
      }
      if (anchor) {
        const anchors = new Set([...(target.anchors || []), ...(target.headings || []).map(heading => heading.id)]);
        if (!anchors.has(anchor)) fail(`Broken anchor link in ${sourceRoute}: ${link}.`);
      }
    }
  }

  const humanPageIds = new Set((manifest.pages || []).filter(page => page.kind !== 'api').map(page => page.id));
  const docAi = fs.readFileSync(path.join(outputRoot, 'DocAI.md'), 'utf8');
  if (!docAi.includes('CyCapture') || humanPageIds.has('docai')) fail('DocAI.md is missing or exposed as a human page.');
}

if (translationReport) {
  for (const category of ['missing', 'outdated', 'untracked']) {
    if (!Array.isArray(translationReport[category]) || translationReport[category].length !== 0) {
      fail(`Translation report contains ${category} entries.`);
    }
  }
  if (translationReport.defaultLocale !== 'en') fail('Translation report default locale is not English.');
}

const htmlPath = path.join(outputRoot, 'Documentation.html');
if (fs.existsSync(htmlPath)) {
  const html = fs.readFileSync(htmlPath, 'utf8');
  const tagPattern = /<(?:script|link|img|source)\b[^>]*?\b(?:src|href)=["']([^"']+)["'][^>]*>/gi;
  for (const match of html.matchAll(tagPattern)) {
    const reference = match[1];
    if (/^(?:https?:)?\/\//i.test(reference)) {
      fail(`Remote runtime resource is not allowed: ${reference}.`);
      continue;
    }
    if (/^(?:data:|#)/i.test(reference)) continue;
    requireFile(reference.split(/[?#]/, 1)[0], `Runtime resource ${reference}`);
  }
  if (!html.includes('DocData/data.js') || !html.includes('DocData/viewer.js')) fail('Documentation.html is not the direct generated CyDoc reader.');
}

function inspectTree(directory) {
  for (const entry of fs.readdirSync(directory, { withFileTypes: true })) {
    const fullPath = path.join(directory, entry.name);
    const stat = fs.lstatSync(fullPath);
    if (stat.isSymbolicLink()) fail(`Symbolic links are not allowed in the documentation export: ${path.relative(outputRoot, fullPath)}.`);
    else if (stat.isDirectory()) inspectTree(fullPath);
  }
}
inspectTree(outputRoot);

if (errors.length > 0) {
  console.error(`Documentation verification failed with ${errors.length} error(s):`);
  for (const error of errors) console.error(`- ${error}`);
  process.exit(1);
}

console.log(`Documentation verified: ${manifest?.pages?.length || 0} pages, ${manifest?.locales?.length || 0} locales, ${mediaModules.length} media modules.`);
