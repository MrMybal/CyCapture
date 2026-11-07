import { readFile, writeFile } from "node:fs/promises";
import { resolve } from "node:path";

const [htmlPathArgument, versionArgument] = process.argv.slice(2);

if (!htmlPathArgument || !versionArgument) {
  console.error("Usage: node scripts/version-pages-export.mjs <Documentation.html> <version>");
  process.exit(1);
}

const htmlPath = resolve(htmlPathArgument);
const version = encodeURIComponent(versionArgument.trim());
const html = await readFile(htmlPath, "utf8");
const versioned = html.replace(
  /((?:href|src)=["']DocData\/[^"'?]+)(?:\?[^"']*)?(["'])/g,
  `$1?v=${version}$2`,
);

if (versioned === html) {
  throw new Error(`No CyDoc assets were found in ${htmlPath}.`);
}

await writeFile(htmlPath, versioned, "utf8");
console.log(`Versioned GitHub Pages assets with ${versionArgument}.`);
