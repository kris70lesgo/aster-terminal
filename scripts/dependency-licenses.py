"""Inventory restored NuGet licence metadata; does not replace native payload review."""
import json
import os
from pathlib import Path
import xml.etree.ElementTree as ET

root = Path(__file__).resolve().parents[1]
cache = Path(os.environ.get("NUGET_PACKAGES", Path.home() / ".nuget/packages"))
packages = {}
for lock in sorted(root.glob("**/packages.lock.json")):
    if ".research" in lock.parts or "obj" in lock.parts:
        continue
    runtime = "tests" not in lock.parts
    for dependencies in json.loads(lock.read_text())["dependencies"].values():
        for name, item in dependencies.items():
            if item["type"] == "Project":
                continue
            version = item["resolved"]
            key = (name.lower(), version)
            if key not in packages:
                nuspec = cache / name.lower() / version / (name.lower() + ".nuspec")
                metadata = ET.parse(nuspec).getroot()
                fields = {node.tag.split("}")[-1]: node for node in metadata.iter()}
                licence = fields.get("license")
                repository = fields.get("repository")
                packages[key] = {
                    "id": name, "version": version,
                    "license": None if licence is None else licence.text,
                    "license_type": None if licence is None else licence.get("type"),
                    "source": None if repository is None else repository.get("url"),
                    "application_project_reference": runtime,
                }
            else:
                packages[key]["application_project_reference"] |= runtime
report = {"verified_date": "2026-10-16", "scope": "NuGet metadata only; application_project_reference includes build tools and is not proof of runtime payload inclusion. Native runtime/font payloads are audited separately.", "packages": list(packages.values())}
destination = root / "docs/evidence/dependency-licenses.json"
destination.write_text(json.dumps(report, indent=2) + "\n")
print(f"Inventoried {len(packages)} package versions into {destination}")
for package in packages.values():
    if package["application_project_reference"]:
        print(f"{package['id']} {package['version']}: {package['license_type']} {package['license']}")
