# CyCapture documentation workspace

This directory is not part of the human manual and is not distributed with CyCapture.

- Authoring sources: `.CyDocBuild/manual/en` and `.CyDocBuild/manual/fr`.
- Generated reader: `CyCapture.App/Documentation`.
- Regeneration: `./scripts/build-documentation.ps1` from the repository root.
- CyDoc revision: `6ddf9a582defb2f11fbb3f992afef6f98dcadc93`.
- Human pages describe current user workflows. Build notes, verification results and migration records remain here.

The English API reference uses the neutral CyDoc import in `.CyDocBuild/api`. The French API pages under `manual/fr/api` carry explicit `sourceRevision` values so translation tracking remains current. Update both from the public C# contracts, then regenerate and review the translation report.
