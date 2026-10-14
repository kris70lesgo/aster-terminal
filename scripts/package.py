"""Build portable native-OS packages from pinned source; verify the final executable."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import platform
import plistlib
import shutil
import subprocess
import tarfile
import zipfile

root = Path(__file__).resolve().parents[1]
parser = argparse.ArgumentParser()
parser.add_argument("--rid", required=True, choices=["osx-arm64", "osx-x64", "win-x64", "linux-x64", "linux-arm64"])
parser.add_argument("--smoke", action="store_true")
args = parser.parse_args()
rid = args.rid
expected = "Darwin" if rid.startswith("osx") else "Windows" if rid.startswith("win") else "Linux"
if platform.system() != expected:
    raise SystemExit("Build and verify this package on its target OS.")
dotnet = os.environ.get("ASTER_DOTNET", "dotnet")
out = root / "artifacts/packages" / rid
payload = out / "payload"
if payload.exists(): shutil.rmtree(payload)
payload.mkdir(parents=True)
subprocess.run([dotnet, "publish", str(root / "src/App/App.csproj"), "-c", "Release", "-r", rid, "--self-contained", "true", "-o", str(payload)], cwd=root, check=True)
shutil.copy2(root / "LICENSE", payload / "LICENSE")
shutil.copy2(root / "NOTICE.md", payload / "NOTICE.md")
shutil.copy2(root / "docs/licensing.md", payload / "LICENSING.md")
for unwanted in payload.glob("ShellIntegration/zsh/.zsh_history*"): unwanted.unlink()
version = "0.2.0"
name = f"aster-terminal-{version}-{rid}"
package_root = payload
if rid.startswith("osx"):
    bundle = out / "Aster Terminal.app"
    if bundle.exists(): shutil.rmtree(bundle)
    contents = bundle / "Contents"
    contents.mkdir(parents=True)
    shutil.copytree(payload, contents / "MacOS")
    (contents / "Resources").mkdir()
    with (contents / "Info.plist").open("wb") as stream:
        plistlib.dump({"CFBundleName": "Aster Terminal", "CFBundleDisplayName": "Aster Terminal", "CFBundleExecutable": "Aster.Terminal", "CFBundleIdentifier": "io.asterterminal.desktop", "CFBundlePackageType": "APPL", "CFBundleShortVersionString": version, "CFBundleVersion": version, "NSHighResolutionCapable": True, "LSMinimumSystemVersion": "13.0"}, stream)
    subprocess.run(["codesign", "--force", "--deep", "--sign", "-", str(bundle)], check=True)
    package_root = bundle
    executable = contents / "MacOS/Aster.Terminal"
else:
    executable = payload / ("Aster.Terminal.exe" if rid.startswith("win") else "Aster.Terminal")
    if rid.startswith("linux"):
        (payload / "aster-terminal.desktop").write_text("[Desktop Entry]\nType=Application\nName=Aster Terminal\nComment=Native terminal emulator\nExec=Aster.Terminal\nTerminal=false\nCategories=System;TerminalEmulator;\n")
if args.smoke:
    subprocess.run([str(executable), "--smoke-test", "--smoke-report", str(out / "native-smoke")], cwd=root, check=True, timeout=90)
    report = json.loads((out / "native-smoke/native-smoke.json").read_text())
    if not report["passed"]: raise SystemExit("Final package failed native acceptance.")
if rid.startswith("linux"):
    artifact = out.parent / (name + ".tar.gz")
    with tarfile.open(artifact, "w:gz") as archive: archive.add(package_root, arcname=name)
else:
    artifact = out.parent / (name + ".zip")
    with zipfile.ZipFile(artifact, "w", zipfile.ZIP_DEFLATED) as archive:
        for path in package_root.rglob("*"):
            if path.is_file(): archive.write(path, path.relative_to(package_root.parent))
digest = hashlib.sha256(artifact.read_bytes()).hexdigest()
artifact.with_suffix(artifact.suffix + ".sha256").write_text(f"{digest}  {artifact.name}\n")
print(artifact)
