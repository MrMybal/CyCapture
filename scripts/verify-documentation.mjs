import fs from 'node:fs';
import path from 'node:path';

const repositoryRoot = path.resolve(import.meta.dirname, '..');
const outputRoot = path.resolve(process.argv[2] || path.join(repositoryRoot, 'docs', 'cydoc', '.output', 'help'));
const configPath = path.resolve(process.argv[3] || path.join(repositoryRoot, 'docs', 'cydoc', 'cydoc.config.json'));
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
    if (!stat.isFile() || stat.isSymbolicLink()) {
      fail(`${label} must be a regular file.`);
    }
  } catch {
    fail(`${label} is missing (${relativePath}).`);
  }
}

if (!fs.existsSync(outputRoot)) {
  throw new Error(`Documentation export does not exist: ${outputRoot}`);
}

const config = readJson(configPath, 'CyDoc configuration');
const manifest = readJson(path.join(outputRoot, 'manifest.json'), 'CyDoc manifest');
const translationReport = readJson(path.join(outputRoot, 'translation-report.json'), 'Translation report');

for (const file of ['index.html', 'viewer.js', 'viewer.css', 'data.js', 'manifest.json', 'translation-report.json', 'media-image.js', 'media-gallery.js', 'assets/favicon.svg']) {
  requireFile(file);
}

const rootMediaModules = fs.readdirSync(outputRoot, { withFileTypes: true })
  .filter(entry => entry.isFile() && /^media-.*\.js$/i.test(entry.name))
  .map(entry => entry.name)
  .sort();
const expectedMediaModules = ['media-gallery.js', 'media-image.js'];
if (JSON.stringify(rootMediaModules) !== JSON.stringify(expectedMediaModules)) {
  fail(`Unexpected CyDoc media modules: ${rootMediaModules.join(', ') || 'none'}.`);
}

if (config && manifest) {
  const expectedLocales = config.locales.map(locale => locale.code).sort();
  const manifestLocales = manifest.locales.map(locale => locale.code).sort();
  if (manifest.defaultLocale !== config.defaultLocale) {
    fail(`Default locale mismatch: expected ${config.defaultLocale}, got ${manifest.defaultLocale}.`);
  }
  if (JSON.stringify(manifestLocales) !== JSON.stringify(expectedLocales)) {
    fail(`Locale mismatch: expected ${expectedLocales.join(', ')}, got ${manifestLocales.join(', ')}.`);
  }

  const pageByRoute = new Map();
  for (const page of manifest.pages || []) {
    const key = `${page.locale}/${page.id}`;
    if (pageByRoute.has(key)) fail(`Duplicate page route: ${key}.`);
    pageByRoute.set(key, page);
    for (const asset of page.assets || []) requireFile(asset, `Asset used by ${key}`);
  }

  const pageIdsByLocale = new Map(expectedLocales.map(locale => [locale, new Set()]));
  for (const page of manifest.pages || []) {
    if (!pageIdsByLocale.has(page.locale)) {
      fail(`Page ${page.id} uses undeclared locale ${page.locale}.`);
      continue;
    }
    pageIdsByLocale.get(page.locale).add(page.id);
  }

  const defaultIds = [...(pageIdsByLocale.get(config.defaultLocale) || [])].sort();
  for (const locale of expectedLocales) {
    const localizedIds = [...pageIdsByLocale.get(locale)].sort();
    if (JSON.stringify(localizedIds) !== JSON.stringify(defaultIds)) {
      fail(`Page identifiers differ between ${config.defaultLocale} and ${locale}.`);
    }
  }

  for (const page of manifest.pages || []) {
    const sourceRoute = `${page.locale}/${page.id}`;
    for (const link of page.links || []) {
      if (!link.startsWith('#/')) continue;
      const match = /^#\/([^/]+)\/([^/]+)(?:\/(.+))?$/.exec(link);
      if (!match) {
        fail(`Malformed internal link in ${sourceRoute}: ${link}.`);
        continue;
      }

      const [, locale, pageId, anchor] = match.map(value => value ? decodeURIComponent(value) : value);
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
}

if (translationReport) {
  for (const category of ['missing', 'outdated', 'untracked']) {
    if (!Array.isArray(translationReport[category]) || translationReport[category].length !== 0) {
      fail(`Translation report contains ${category} entries.`);
    }
  }
  if (translationReport.defaultLocale !== config?.defaultLocale) {
    fail('Translation report default locale does not match the CyDoc configuration.');
  }
}

const indexPath = path.join(outputRoot, 'index.html');
if (fs.existsSync(indexPath)) {
  const html = fs.readFileSync(indexPath, 'utf8');
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
}

function inspectTree(directory) {
  for (const entry of fs.readdirSync(directory, { withFileTypes: true })) {
    const fullPath = path.join(directory, entry.name);
    const stat = fs.lstatSync(fullPath);
    if (stat.isSymbolicLink()) {
      fail(`Symbolic links are not allowed in the Pages artifact: ${path.relative(outputRoot, fullPath)}.`);
    } else if (stat.isDirectory()) {
      inspectTree(fullPath);
    }
  }
}
inspectTree(outputRoot);

if (errors.length > 0) {
  console.error(`Documentation verification failed with ${errors.length} error(s):`);
  for (const error of errors) console.error(`- ${error}`);
  process.exit(1);
}

console.log(`Documentation verified: ${manifest?.pages?.length || 0} pages, ${manifest?.locales?.length || 0} locales, ${rootMediaModules.length} media modules.`);
