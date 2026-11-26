#!/usr/bin/env python3
# Windows:  python run_ios.py          (prints that iOS needs a Mac)
# macOS:    python3 run_ios.py   or  ./run_ios.py
"""Deploy the Face Liveness MAUI iOS demo to a plugged-in iPhone."""
from __future__ import annotations

import os
import shutil
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent
IS_MAC = sys.platform == "darwin"
IOS_BUNDLE_ID = "com.faceplugin.faceliveness.app"
PROJ = ROOT / "src" / "FaceLiveness.Net.Maui" / "FaceLiveness.Net.Maui.csproj"
FW_DEST = PROJ.parent / "Platforms" / "iOS" / "Frameworks"
IOS_APP = ROOT.parent / "Face-LivenessDetection-iOS"
ENGINE_NAMES = (
    "facerecognitionsdk.framework",
    "FacePluginSDK.xcframework",
    "FacePlugin.xcframework",
    "FaceDetect.xcframework",
)
BRIDGE_NAMES = (
    "FaceLivenessBridge.xcframework",
    "FaceLivenessKit.xcframework",
)


def die(msg: str) -> None:
    print(f"ERROR: {msg}", file=sys.stderr)
    raise SystemExit(1)


def which(name: str) -> str | None:
    return shutil.which(name)


def need(name: str, hint: str) -> str:
    path = which(name)
    if not path:
        die(f"{name} is not on PATH. {hint}")
    return path


def run(args: list[str], cwd: Path | None = None) -> None:
    print("+", " ".join(str(a) for a in args))
    r = subprocess.run(args, cwd=str(cwd or ROOT))
    if r.returncode:
        raise SystemExit(r.returncode)


def copy_tree(src: Path, dest: Path) -> None:
    if dest.exists():
        shutil.rmtree(dest)
    dest.parent.mkdir(parents=True, exist_ok=True)
    shutil.copytree(src, dest, symlinks=True)
    print(f"copied {src.name} -> {dest}")


def ios_udid() -> str:
    need("xcrun", "Install Xcode and its command-line tools.")
    raw = subprocess.check_output(
        ["xcrun", "xctrace", "list", "devices"],
        text=True,
        stderr=subprocess.STDOUT,
        errors="replace",
    )
    in_sims = False
    for line in raw.splitlines():
        lower = line.lower()
        if "simulator" in lower:
            in_sims = True
            continue
        if in_sims or "(" not in line or ")" not in line:
            continue
        if "mac" in lower or "watch" in lower or "apple tv" in lower:
            continue
        udid = line.rsplit("(", 1)[-1].strip().rstrip(")")
        token = udid.replace("-", "")
        if len(token) >= 16 and token.isalnum():
            print(f"iOS device: {line.strip()}")
            return udid
    die("no physical iPhone. Plug one in and trust this computer.")
    raise SystemExit(1)


def ensure_engine_frameworks() -> None:
    FW_DEST.mkdir(parents=True, exist_ok=True)
    missing = [n for n in ENGINE_NAMES if not (FW_DEST / n).is_dir()]
    if not missing:
        return
    if not all((IOS_APP / n).exists() for n in ENGINE_NAMES):
        die(
            "missing iOS engine frameworks under Platforms/iOS/Frameworks.\n"
            f"Copy them from {IOS_APP}."
        )
    for n in ENGINE_NAMES:
        if not (FW_DEST / n).is_dir():
            copy_tree(IOS_APP / n, FW_DEST / n)
    # FacePPlugin is the device slice inside FacePlugin.xcframework — useful for ld.
    slice_fw = FW_DEST / "FacePlugin.xcframework" / "ios-arm64" / "FacePPlugin.framework"
    if slice_fw.is_dir() and not (FW_DEST / "FacePPlugin.framework").is_dir():
        copy_tree(slice_fw, FW_DEST / "FacePPlugin.framework")


def ensure_bridge_frameworks() -> None:
    if all((FW_DEST / n).is_dir() for n in BRIDGE_NAMES):
        return
    script = IOS_APP / "scripts" / "build_bridge.sh"
    if not script.is_file():
        die(f"missing {script} — cannot build FaceLivenessBridge.xcframework")
    run(["bash", str(script)], cwd=IOS_APP)
    built = IOS_APP / "build" / "xcframeworks"
    for n in BRIDGE_NAMES:
        src = built / n
        if not src.is_dir():
            die(f"bridge build did not produce {src}")
        copy_tree(src, FW_DEST / n)


def find_app() -> Path:
    base = PROJ.parent / "bin"
    candidates = list(base.glob("**/FaceLiveness.Net.Maui.app"))
    # Prefer Debug ios-arm64, then any.
    preferred = [
        p
        for p in candidates
        if "ios-arm64" in str(p) and "Debug" in str(p)
    ]
    for p in preferred + candidates:
        if p.is_dir():
            return p
    die(f"MAUI app bundle missing under {base}")
    raise SystemExit(1)


def main() -> None:
    if not IS_MAC:
        die("iOS builds need macOS and Xcode. Use python run_android.py on this machine.")
    print(f"{ROOT.name}: ios (maui)")
    dotnet = need("dotnet", "Install the .NET 8 SDK.")
    ensure_engine_frameworks()
    ensure_bridge_frameworks()
    device = ios_udid()
    run(
        [
            dotnet,
            "build",
            str(PROJ),
            "-f",
            "net8.0-ios",
            "-r",
            "ios-arm64",
            "-p:RuntimeIdentifier=ios-arm64",
        ],
        cwd=ROOT,
    )
    app = find_app()
    run(["xcrun", "devicectl", "device", "install", "app", "--device", device, str(app)])
    run(
        [
            "xcrun",
            "devicectl",
            "device",
            "process",
            "launch",
            "--terminate-existing",
            "--device",
            device,
            IOS_BUNDLE_ID,
        ]
    )


if __name__ == "__main__":
    main()
