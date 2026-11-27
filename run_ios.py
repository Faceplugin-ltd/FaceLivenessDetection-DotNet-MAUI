#!/usr/bin/env python3
"""Fetch iOS frameworks if missing, then build/install/launch the .NET MAUI iOS app.

macOS only. Uses xcrun xctrace + devicectl (not ios-deploy) so CoreDevice /
wireless phones are found the same way as Face-LivenessDetection-iOS.
"""
from __future__ import annotations

import os
import shutil
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent
IS_WIN = os.name == "nt"

BUNDLE_ID = "com.faceplugin.faceliveness.app"
PROJ = ROOT / "src" / "FaceLiveness.Net.Maui" / "FaceLiveness.Net.Maui.csproj"
FW = PROJ.parent / "Platforms" / "iOS" / "Frameworks"
APP = (
    PROJ.parent
    / "bin"
    / "Debug"
    / "net8.0-ios"
    / "ios-arm64"
    / "FaceLiveness.Net.Maui.app"
)


def die(msg: str) -> None:
    print(f"ERROR: {msg}", file=sys.stderr)
    raise SystemExit(1)


def which(name: str) -> str | None:
    hits = [shutil.which(name)]
    if IS_WIN:
        hits.extend([shutil.which(name + ".cmd"), shutil.which(name + ".exe")])
    return next((h for h in hits if h), None)


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


def run_soft(args: list[str]) -> int:
    print("+", " ".join(str(a) for a in args))
    return subprocess.run(args, cwd=str(ROOT)).returncode


def print_trust_help() -> None:
    print(
        """
--------------------------------------------------------------------
Install succeeded, but iOS refused to launch (developer not trusted
or nested frameworks need a fresh Trust).

On the iPhone:
  Settings → General → VPN & Device Management
  → Apple Development: <your Apple ID>
  → Trust

Unlock the phone, tap Trust, then open the app from the Home Screen
(or re-run: python3 run_ios.py).
--------------------------------------------------------------------
""",
        file=sys.stderr,
    )


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
        if "simulator" in lower and "==" in line:
            in_sims = True
            continue
        if in_sims or "simulator" in lower:
            continue
        if "(" not in line or ")" not in line:
            continue
        if "mac" in lower or "watch" in lower or "apple tv" in lower:
            continue
        if "iphone" not in lower and "ipad" not in lower:
            continue
        udid = line.rsplit("(", 1)[-1].strip().rstrip(")")
        token = udid.replace("-", "")
        if len(token) >= 16 and token.isalnum():
            print(f"iOS device: {line.strip()}")
            return udid
    die("no physical iPhone/iPad. Unlock one, trust this Mac, and retry.")
    raise SystemExit(1)


def main() -> None:
    import fp_releases

    if sys.platform != "darwin":
        die("iOS builds need macOS and Xcode. Use python run_android.py on this machine.")
    dotnet = need("dotnet", "Install the .NET 8 SDK.")
    print(f"{ROOT.name}: ios (maui)")
    fp_releases.ensure_ios_engine(ROOT, FW, with_bridge=True)
    udid = ios_udid()
    run([dotnet, "build", str(PROJ), "-f", "net8.0-ios", "-r", "ios-arm64"])
    if not APP.is_dir():
        die(f"app bundle missing after build: {APP}")
    run(
        [
            "xcrun",
            "devicectl",
            "device",
            "install",
            "app",
            "--device",
            udid,
            "--timeout",
            "300",
            str(APP),
        ]
    )
    launch = run_soft(
        [
            "xcrun",
            "devicectl",
            "device",
            "process",
            "launch",
            "--terminate-existing",
            "--device",
            udid,
            BUNDLE_ID,
        ]
    )
    if launch != 0:
        print_trust_help()
        raise SystemExit(0)
    print(f"launched {BUNDLE_ID} (no LLDB attach)")


if __name__ == "__main__":
    main()
